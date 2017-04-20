using Rhino.DocObjects;
using Rhino.Geometry;
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
            Start = 0, Snap = 1
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
        private List<NurbsCurve> curveList= new List<NurbsCurve>();
        List<Point3d> curvePoints = new List<Point3d>();
        Geometry.Geometry stroke_g2 = new Geometry.GeometryStroke();
        Material.Material stroke_m2 = new Material.SingleColorMaterial(0, 0, 1, 1);
        SceneNode stroke;
        private Material.Material mesh_m;

        List<SceneNode> pointMarkers = new List<SceneNode>();
        private Guid surfaceID;
        string type;

        public EditPoint(ref Scene scene)
        {
            mScene = scene;
        }

        public EditPoint(ref Scene scene, ref RhinoObject rp, bool drawOnP, List<NurbsCurve> curveL, Guid sid, string t)
        {
            mScene = scene;

            onPlane = drawOnP;
            if (onPlane)
            {
                Geometry.Geometry geo = new Geometry.PointMarker(new OpenTK.Vector3(0, 0, 0));
                Material.Material m = new Material.SingleColorMaterial(250 / 255, 128 / 255, 128 / 255, 1);
                drawPoint = new SceneNode("Point", ref geo, ref m);
                drawPoint.transform = new OpenTK.Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
                mScene.tableGeometry.add(ref drawPoint);

                //TODO-support both controllers
                primaryDeviceIndex = mScene.leftControllerIdx;

                rhinoPlane = rp;
                // closedCurve = curve;
                curveList = curveL;
                closedCurve = curveList[0];
                surfaceID = sid;
                type = t;
                mesh_m = new Material.RGBNormalMaterial(.5f);

                //init and render control points of curve
                for (int i = 0; i < closedCurve.Points.Count; i++)
                {
                    Point3d ep = new Point3d(closedCurve.Points.ElementAt(i).Location.X, closedCurve.Points.ElementAt(i).Location.Y, closedCurve.Points.ElementAt(i).Location.Z);
                    curvePoints.Add(ep);

                    SceneNode sn = Util.MarkPointSN(ref mScene.staticGeometry, Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)ep.X, (float)ep.Y, (float)ep.Z)), 0, 1, 0);
                    pointMarkers.Add(sn);
                    
                }

                currentState = State.Start;
            }

        }

        public override void draw(bool inTop)
        {
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
                        for (int i = 0; i < closedCurve.Points.Count; i++)
                        {
                            ControlPoint cp = closedCurve.Points.ElementAt(i);
                            OpenTK.Vector3 pVR = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)cp.Location.X, (float)cp.Location.Y, (float)cp.Location.Z));

                            //snap to point
                            if (Math.Sqrt(Math.Pow(projectP.X - pVR.X, 2) + Math.Pow(projectP.Y - pVR.Y, 2) + Math.Pow(projectP.Z - pVR.Z, 2)) < 0.03)
                            {
                                projectP = pVR;
                                snapIndex = i;
                                isSnap = true;
                                break;
                            }
                            else
                            {
                                isSnap = false;
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
                        //remove and visualize the new control points
                        for (int i = 0; i < pointMarkers.Count; i++)
                        {
                            SceneNode sn = pointMarkers[i];
                            mScene.staticGeometry.remove(ref sn);
                        }
                        pointMarkers.Clear();
                        foreach (ControlPoint cp in closedCurve.Points)
                        {
                            SceneNode sn = Util.MarkPointSN(ref mScene.staticGeometry, Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)cp.Location.X, (float)cp.Location.Y, (float)cp.Location.Z)), 0, 1, 0);
                            pointMarkers.Add(sn);
                        }

                        //render the rhino curve
                        PolylineCurve polyline = closedCurve.ToPolyline(0, 0, 0, 0, 0, 1, 1, 0, true);
                        if (stroke == null)
                        {
                            for (int i = 0; i < polyline.PointCount; i++)
                            {
                                ((Geometry.GeometryStroke)stroke_g2).addPoint(Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)polyline.Point(i).X, (float)polyline.Point(i).Y, (float)polyline.Point(i).Z)));

                            }
                            stroke = new SceneNode("Stroke2", ref stroke_g2, ref stroke_m2);
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
                    OpenTK.Matrix4 t = OpenTK.Matrix4.CreateTranslation(projectP);
                    t.Transpose();
                    drawPoint.transform = t;
                    targetPSN = mScene.brepToSceneNodeDic[rhinoPlane.Id];
                    targetPRhObj = rhinoPlane;

                }
                else
                {
                    isSnap = false;
                }

            }
        }

        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            if (isSnap)
            {
                currentState = State.Snap;
            }
        }

        protected override void onReleaseOculusGrip(ref VREvent_t vrEvent)
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

                isSnap = false;
                currentState = State.Start;
            }
        }

        protected override void onClickOculusAX(ref VREvent_t vrEvent)
        {
            if(type == "Revolve")
            {
                Line axis = new Line(new Point3d(0, 0, 0), new Point3d(0, 0, 1));
                RevSurface revsrf = RevSurface.Create(closedCurve, axis);
                Brep brepRevolve = Brep.CreateFromRevSurface(revsrf, true, true);
                Util.addSceneNode(ref mScene, brepRevolve, ref mesh_m, "aprint");
            }
            else if (type.Contains("Sweep2"))
            {
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
            }
        }

    }
}
