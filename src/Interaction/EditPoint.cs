using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using SparrowHawk.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    class EditPoint : Interaction
    {
        public enum State
        {
            Start = 0, Snap = 1, End =2
        };

        protected State currentState;
        public bool onPlane = false;
        protected int primaryDeviceIndex;
        protected SceneNode targetPSN;
        protected RhinoObject targetPRhObj;
        private int snapIndex = -1;
        private bool isSnap = false;
        protected RhinoObject rhinoPlane;
        SceneNode drawPoint;
        OpenTK.Vector3 projectP;
        private Rhino.Geometry.NurbsCurve closedCurve;
        private PolylineCurve polyline;
        private List<Curve> curveList= new List<Curve>();
        List<Point3d> curvePoints = new List<Point3d>();
        Geometry.Geometry stroke_g2;
        Material.Material stroke_m2 = new Material.SingleColorMaterial(0, 0, 1, 1);
        SceneNode stroke;
        private Material.Material mesh_m;

        List<SceneNode> pointMarkers = new List<SceneNode>();
        private Guid surfaceID;
        string type;

        //for sweep2
        Guid sGuid;
        Guid eGuid;

        float mimD = 1000000f;

        public EditPoint(ref Scene scene) : base(ref scene)
        {
            stroke_g2 = new Geometry.GeometryStroke(ref mScene);
        }

        public EditPoint(ref Scene scene, ref RhinoObject rp, bool drawOnP, List<Curve> curveL, Guid sid, string t) : base(ref scene)
        {
            stroke_g2 = new Geometry.GeometryStroke(ref mScene);
            onPlane = drawOnP;
            if (onPlane)
            {
                //clear previous drawpoint
                if (mScene.tableGeometry.children.Count > 0)
                {
                    foreach (SceneNode sn in mScene.tableGeometry.children)
                    {
                        if (sn.name == "drawPoint")
                        {
                            mScene.tableGeometry.children.Remove(sn);
                            break;
                        }
                    }
                }

                Geometry.Geometry geo = new Geometry.PointMarker(new OpenTK.Vector3(0, 0, 0));
                Material.Material m = new Material.SingleColorMaterial(250 / 255, 128 / 255, 128 / 255, 1);
                drawPoint = new SceneNode("drawPoint", ref geo, ref m);
                drawPoint.transform = new OpenTK.Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
                mScene.tableGeometry.add(ref drawPoint);

                //TODO-support both controllers
                primaryDeviceIndex = mScene.leftControllerIdx;

                rhinoPlane = rp;
                // closedCurve = curve;
                curveList = curveL;
                closedCurve = curveList[0].ToNurbsCurve();
                surfaceID = sid;
                type = t;
                mesh_m = new Material.RGBNormalMaterial(.5f);

                //init and render control points of curve
                for (int i = 0; i < closedCurve.Points.Count; i++)
                {
                    Point3d ep = new Point3d(closedCurve.Points.ElementAt(i).Location.X, closedCurve.Points.ElementAt(i).Location.Y, closedCurve.Points.ElementAt(i).Location.Z);
                    curvePoints.Add(ep);
                    // p is the point before apply tableGeometry.transform inverted so we need to transfrom here
                    SceneNode sn = Util.MarkPointSN(ref mScene.tableGeometry, Util.transformPoint(mScene.tableGeometry.transform.Inverted(),Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)ep.X, (float)ep.Y, (float)ep.Z))), 0, 1, 0);
                    pointMarkers.Add(sn);
                    
                }

                //render the rhino curve
                polyline = closedCurve.ToPolyline(0, 0, 0, 0, 0, 1, 1, 0, true);
                if (stroke == null)
                {
                    for (int i = 0; i < polyline.PointCount; i++)
                    {
                        ((Geometry.GeometryStroke)stroke_g2).addPoint(Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)polyline.Point(i).X, (float)polyline.Point(i).Y, (float)polyline.Point(i).Z)));

                    }
                    stroke = new SceneNode("EditCurve", ref stroke_g2, ref stroke_m2);
                    mScene.tableGeometry.add(ref stroke);
                }

                if(type == "Sweep-rail")
                {

                    generatePlane();
                }

                currentState = State.Start;
            }

        }

        private void generatePlane()
        {
            Curve[] overlap_curves;
            Point3d[] inter_points;
            List<Vector3d> normals = new List<Vector3d>();
            List<Point3d> planePoints = new List<Point3d>();
            Rhino.DocObjects.ObjectEnumeratorSettings settings = new Rhino.DocObjects.ObjectEnumeratorSettings();
            settings.ObjectTypeFilter = Rhino.DocObjects.ObjectType.Brep;
            foreach (Rhino.DocObjects.RhinoObject rhObj in mScene.rhinoDoc.Objects.GetObjectList(settings))
            {
                if (rhObj.Attributes.Name.Contains("plane"))
                {
                    continue;
                }

                if (Intersection.CurveBrep(closedCurve, rhObj.Geometry as Brep, mScene.rhinoDoc.ModelAbsoluteTolerance, out overlap_curves, out inter_points))
                {
                    if (overlap_curves.Length > 0 || inter_points.Length > 0)
                    {
                        foreach (Point3d interPoint in inter_points)
                        {
                            planePoints.Add(interPoint);
                            foreach (Surface surface in ((Brep)rhObj.Geometry).Surfaces)
                            {
                                double u, v;
                                if (surface.ClosestPoint(interPoint, out u, out v))
                                {
                                    normals.Add(((Brep)rhObj.Geometry).Surfaces[0].NormalAt(u, v));
                                }
                            }

                        }

                    }
                }
            }
            Rhino.RhinoApp.WriteLine("normal counts: " + normals.Count);

            //if there is not intersect, then init the normal 
            if (normals.Count == 0)
            {
                planePoints.Add(polyline.PointAtStart);
                normals.Add(polyline.TangentAtStart);
                planePoints.Add(polyline.PointAtEnd);
                normals.Add(polyline.TangentAtEnd);

            }
            else if (normals.Count == 1)
            {
                planePoints.Add(polyline.PointAtEnd);
                normals.Add(polyline.TangentAtEnd);
            }

            //covert from Nurbcurvie to curve                 
            Plane planeStart = new Plane(planePoints[0], normals[0]);
            PlaneSurface planeStart_surface = new PlaneSurface(planeStart,
              new Interval(-30, 30),
              new Interval(-30, 30));

            Plane planeEnd = new Plane(planePoints[1], normals[1]);
            PlaneSurface planeEnd_surface = new PlaneSurface(planeEnd,
              new Interval(-30, 30),
              new Interval(-30, 30));

            Brep startPlane = Brep.CreateFromSurface(planeStart_surface);
            Brep endPlane = Brep.CreateFromSurface(planeEnd_surface);

            if (startPlane != null && endPlane != null)
            {
                if (sGuid != Guid.Empty && eGuid != Guid.Empty)
                {
                    Util.removeSceneNode(ref mScene, sGuid);
                    Util.removeSceneNode(ref mScene, eGuid);

                }
                sGuid = Util.addSceneNode(ref mScene, startPlane, ref mesh_m, "planeStart");
                eGuid = Util.addSceneNode(ref mScene, endPlane, ref mesh_m, "planeEnd");

            }
        }
    

        public override void draw(bool inTop)
        {
            if (currentState == State.End)
                return;

            if (onPlane)
            {
                //ray casting to the pre-defind planes
                OpenTK.Vector4 controller_p = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, 0, 1);
                OpenTK.Vector4 controller_pZ = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, -1, 1);
                Point3d controller_pRhino = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)));
                Point3d controller_pZRhin = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_pZ.X, controller_pZ.Y, controller_pZ.Z)));

                Vector3d direction = new Vector3d(controller_pZRhin.X - controller_pRhino.X, controller_pZRhin.Y - controller_pRhino.Y, controller_pZRhin.Z - controller_pRhino.Z);
                Ray3d ray = new Ray3d(controller_pRhino, direction);

                //Todo- support multiple plane
                List<GeometryBase> geometries = new List<GeometryBase>();
                geometries.Add(rhinoPlane.Geometry);
                //must be a brep or surface, not mesh
                Point3d[] rayIntersections = Rhino.Geometry.Intersect.Intersection.RayShoot(ray, geometries, 1);
                if (rayIntersections != null)
                {
                    projectP = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)rayIntersections[0].X, (float)rayIntersections[0].Y, (float)rayIntersections[0].Z));
                    
                    if (currentState == State.Start)
                    {
                        mimD = 1000000f;
                        OpenTK.Vector3 snapP = new OpenTK.Vector3();
                        for (int i = 0; i < closedCurve.Points.Count; i++)
                        {
                            ControlPoint cp = closedCurve.Points.ElementAt(i);
                            OpenTK.Vector3 pVR = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)cp.Location.X, (float)cp.Location.Y, (float)cp.Location.Z));
                            float distance = (float)Math.Sqrt(Math.Pow(projectP.X - pVR.X, 2) + Math.Pow(projectP.Y - pVR.Y, 2) + Math.Pow(projectP.Z - pVR.Z, 2));
                            //snap to point
                            if (distance <= 0.03 && distance <= mimD)
                            {
                                mimD = distance;
                                snapP = pVR;
                                snapIndex = i;
                            }
                            
                        }

                        if(mimD <= 0.03f)
                        {
                            isSnap = true;
                            projectP = snapP;
                            for (int i = 0; i < pointMarkers.Count; i++)
                            {
                                if (i == snapIndex)
                                {
                                    pointMarkers[i].material = new Material.SingleColorMaterial(250 / 255, 128 / 255, 128 / 255, 1);
                                }else
                                {
                                    pointMarkers[i].material = new Material.SingleColorMaterial(0, 1, 0, 1);
                                }
                            }
                                
                        }
                        else
                        {
                            isSnap = false;
                            //set to default color
                            foreach (SceneNode sn in pointMarkers)
                            {
                                sn.material = new Material.SingleColorMaterial(0,1,0,1);
                            }
                        }

                    }
                    else if (currentState == State.Snap)
                    {

                        //update the curve
                        OpenTK.Vector3 ep = Util.vrToPlatformPoint(ref mScene, projectP);
                        curvePoints[snapIndex] = Util.openTkToRhinoPoint(ep);

                        if (closedCurve.IsClosed)
                        {
                            closedCurve = Rhino.Geometry.NurbsCurve.Create(true, 3, curvePoints.ToArray());
                        }else
                        {
                            closedCurve = Rhino.Geometry.NurbsCurve.Create(false, 3, curvePoints.ToArray());
                        }
                        //prevent crash
                        if (closedCurve == null)
                            return;
                        //remove and visualize the new control points
                        for (int i = 0; i < pointMarkers.Count; i++)
                        {
                            SceneNode sn = pointMarkers[i];
                            mScene.tableGeometry.remove(ref sn);
                        }
                        pointMarkers.Clear();
                        foreach (ControlPoint cp in closedCurve.Points)
                        {
                            //rotation inverted before visualizing
                            SceneNode sn = Util.MarkPointSN(ref mScene.tableGeometry, Util.transformPoint(mScene.tableGeometry.transform.Inverted(), Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)cp.Location.X, (float)cp.Location.Y, (float)cp.Location.Z))), 0, 1, 0);
                            pointMarkers.Add(sn);
                        }

                        //render the rhino curve
                        polyline = closedCurve.ToPolyline(0, 0, 0, 0, 0, 1, 1, 0, true);
                        if (stroke == null)
                        {
                            for (int i = 0; i < polyline.PointCount; i++)
                            {
                                ((Geometry.GeometryStroke)stroke_g2).addPoint(Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)polyline.Point(i).X, (float)polyline.Point(i).Y, (float)polyline.Point(i).Z)));

                            }
                            stroke = new SceneNode("EditCurve", ref stroke_g2, ref stroke_m2);
                            mScene.tableGeometry.add(ref stroke);
                        }
                        else
                        {
                            ((Geometry.GeometryStroke)stroke_g2).removePoint();
                            for (int i = 0; i < polyline.PointCount; i++)
                            {
                                ((Geometry.GeometryStroke)stroke_g2).addPoint(Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)polyline.Point(i).X, (float)polyline.Point(i).Y, (float)polyline.Point(i).Z)));
                            }
                        }

                    }

                    //visualize the projection point on the plane
                    //OpenTK.Matrix4 t = OpenTK.Matrix4.CreateTranslation(Util.transformPoint(mScene.tableGeometry.transform.Inverted(), projectP));
                    //t.Transpose();
                    //drawPoint.transform = t;
                    targetPSN = mScene.brepToSceneNodeDic[rhinoPlane.Id];
                    targetPRhObj = rhinoPlane;

                }
                else
                {
                    isSnap = false;
                    targetPSN = null;
                    targetPRhObj = null;
                    projectP = new OpenTK.Vector3(100, 100, 100); //make it invisable
                }

                //visualize the projection point on the plane
                OpenTK.Matrix4 t = OpenTK.Matrix4.CreateTranslation(Util.transformPoint(mScene.tableGeometry.transform.Inverted(), projectP));
                t.Transpose();
                drawPoint.transform = t;
            }
        }

        protected override void onClickOculusTrigger(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("mim D: " + mimD);
            if (isSnap)
            {
                currentState = State.Snap;
            }
        }

        protected override void onReleaseOculusTrigger(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("oculus grip release event test");
            if (currentState == State.Snap)
            {
                //update the surface
                if (type == "ClosedCurve" && surfaceID != Guid.Empty) // closed curve
                {
                    Util.removeSceneNode(ref mScene, surfaceID);
                    Brep[] shapes = Brep.CreatePlanarBreps(closedCurve);
                    Brep curve_s = shapes[0];

                    surfaceID = Util.addSceneNode(ref mScene, curve_s, ref mesh_m);
                }
                else if (type == "Sweep-rail")
                {
                    generatePlane();
                }

                isSnap = false;
                currentState = State.Start;
            }
        }

        protected override void onClickOculusAX(ref VREvent_t vrEvent)
        {
            if (type == "Revolve")
            {
                Line axis = new Line(new Point3d(0, 0, 0), new Point3d(0, 0, 1));
                RevSurface revsrf = RevSurface.Create(closedCurve, axis);
                Brep brepRevolve = Brep.CreateFromRevSurface(revsrf, false, false);
                Util.addSceneNode(ref mScene, brepRevolve, ref mesh_m, "aprint");



                //clear the curve and points
                if (mScene.tableGeometry.children.Count > 0)
                {
                    // need to remove rerverse since the list update dynamically
                    foreach (SceneNode sn in mScene.tableGeometry.children.Reverse<SceneNode>())
                    {
                        if (sn.name == "EditCurve" || sn.name == "drawPoint" || sn.name == "EditPoint")
                        {
                            mScene.tableGeometry.children.Remove(sn);
                        }
                    }
                }

                mScene.popInteraction();
                currentState = State.End;
            }
            else if(type.Contains("Sweep-rail"))
            {
                mScene.popInteraction();
                mScene.pushInteraction(new SweepShapeCircle(ref mScene, true, closedCurve, sGuid, eGuid));
                currentState = State.End;
            }
            else if (type.Contains("Sweep2"))
            {

                //clear the curve and points
                if (mScene.tableGeometry.children.Count > 0)
                {
                    // need to remove rerverse since the list update dynamically
                    foreach (SceneNode sn in mScene.tableGeometry.children.Reverse<SceneNode>())
                    {
                        if (sn.name == "EditCurve" || sn.name == "drawPoint" || sn.name == "EditPoint")
                        {
                            mScene.tableGeometry.children.Remove(sn);
                        }else if (sn.name == "planeStart" || sn.name == "planeEnd")
                        {
                            RhinoObject rhobj = mScene.SceneNodeToBrepDic[sn.guid];
                            Util.removeSceneNode(ref mScene, rhobj.Id);
                        }
                    }
                }

                string sweepType;

                if (type.Contains("start"))
                    sweepType = "start";
                else
                    sweepType = "end";


                Plane proj_plane = new Plane();
                Plane.FitPlaneToPoints(curvePoints.ToArray(), out proj_plane);
                Curve proj_curve = Curve.ProjectToPlane(closedCurve, proj_plane);

                //TODO: make sure the proj_curve is on the same plane ? or it's beacuse not enough points
                Brep[] breps = null;
                if (sweepType == "start")
                    breps = Brep.CreateFromSweep(curveList[1], closedCurve, false, mScene.rhinoDoc.ModelAbsoluteTolerance);
                else if (sweepType == "end")
                {
                    curveList[1].Reverse();
                    breps = Brep.CreateFromSweep(curveList[1], closedCurve, false, mScene.rhinoDoc.ModelAbsoluteTolerance);
                }
                Brep brep = breps[0];

                if (brep != null)
                {
                    Util.addSceneNode(ref mScene, brep, ref mesh_m, "aprint");

                    //Util.removeSceneNode(ref mScene, sGuid);
                    //Util.removeSceneNode(ref mScene, eGuid);

                    mScene.rhinoDoc.Views.Redraw();

                    //mScene.popInteraction();
                    //mScene.pushInteraction(new Sweep2(ref mScene));
                }
                mScene.popInteraction();
                currentState = State.End;
            }

            


        }

    }
}
