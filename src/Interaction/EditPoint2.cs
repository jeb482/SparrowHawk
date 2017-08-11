using OpenTK;
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
    class EditPoint2 : Interaction
    {
        public enum State
        {
            Start = 0, Snap = 1, End = 2
        };

        protected State currentState;
        public bool onPlane = false;
        public string dynamicRender = "none";
        Guid renderObjId = Guid.Empty;
        protected int primaryDeviceIndex;
        protected SceneNode targetPSN;
        protected RhinoObject targetPRhObj;
        private int snapIndex = -1;
        private bool isSnap = false;
        protected RhinoObject rhinoPlane;
        SceneNode drawPoint;
        OpenTK.Vector3 projectP;
        private PolylineCurve polyline;
        private List<Curve> curveList = new List<Curve>();
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

        private Rhino.Geometry.NurbsCurve editCurve;

        public EditPoint2(ref Scene scene) : base(ref scene)
        {
            stroke_g2 = new Geometry.GeometryStroke(ref mScene);

        }

        public EditPoint2(ref Scene scene, bool drawOnP) : base(ref scene)
        {
            stroke_g2 = new Geometry.GeometryStroke(ref mScene);

            onPlane = drawOnP;
        }

        public EditPoint2(ref Scene scene, bool drawOnP, string render) : base(ref scene)
        {
            stroke_g2 = new Geometry.GeometryStroke(ref mScene);

            onPlane = drawOnP;

            dynamicRender = render;
            mesh_m = new Material.RGBNormalMaterial(0.5f);
        }

        public override void init()
        {
            if (onPlane)
            {
                //add projectionPoint
                drawPoint = Util.MarkProjectionPoint(ref mScene, new OpenTK.Vector3(0, 0, 0), 1, 1, 1);
                rhinoPlane = mScene.iRhObjList.ElementAt(mScene.iRhObjList.Count - 1);
            }

            editCurve = (NurbsCurve)mScene.iCurveList.ElementAt(mScene.iCurveList.Count - 1);

            //null check
            if (editCurve == null)
            {
                while (!mScene.interactionStackEmpty())
                    mScene.popInteraction();
                return;
            }

            d = new generateModel_Delegate(generateModel);

            renderEditCurve();


        }


        public override void draw(bool inTop)
        {
            if (currentState == State.End)
                return;

            Vector3 pos = new Vector3();
            if (onPlane)
            {
                //ray casting to the targetObj
                OpenTK.Vector4 controller_p = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, 0, 1);
                OpenTK.Vector4 controller_pZ = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, -1, 1);
                Point3d controller_pRhino = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)));
                Point3d controller_pZRhin = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_pZ.X, controller_pZ.Y, controller_pZ.Z)));

                Rhino.Geometry.Vector3d direction = new Rhino.Geometry.Vector3d(controller_pZRhin.X - controller_pRhino.X, controller_pZRhin.Y - controller_pRhino.Y, controller_pZRhin.Z - controller_pRhino.Z);
                Ray3d ray = new Ray3d(controller_pRhino, direction);

                //Todo- support multiple plane
                List<GeometryBase> geometries = new List<GeometryBase>();
                geometries.Add(rhinoPlane.Geometry);
                //must be a brep or surface, not mesh
                Point3d[] rayIntersections = Rhino.Geometry.Intersect.Intersection.RayShoot(ray, geometries, 1);
                if (rayIntersections != null)
                {
                    projectP = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)rayIntersections[0].X, (float)rayIntersections[0].Y, (float)rayIntersections[0].Z));
                    targetPSN = mScene.brepToSceneNodeDic[rhinoPlane.Id];
                    targetPRhObj = rhinoPlane;
                }
                else
                {
                    targetPSN = null;
                    targetPRhObj = null;
                    projectP = new OpenTK.Vector3(100, 100, 100); //make it invisable
                }

                pos = projectP;

            }
            else
            {
                pos = Util.getTranslationVector3(Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx));
            }

            if (currentState == State.Start)
            {
                mimD = 1000000f;
                OpenTK.Vector3 snapP = new OpenTK.Vector3();
                for (int i = 0; i < editCurve.Points.Count; i++)
                {
                    ControlPoint cp = editCurve.Points.ElementAt(i);
                    OpenTK.Vector3 pVR = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)cp.Location.X, (float)cp.Location.Y, (float)cp.Location.Z));
                    float distance = (float)Math.Sqrt(Math.Pow(pos.X - pVR.X, 2) + Math.Pow(pos.Y - pVR.Y, 2) + Math.Pow(pos.Z - pVR.Z, 2));
                    //snap to point
                    if (distance <= 0.03 && distance <= mimD)
                    {
                        mimD = distance;
                        snapP = pVR;
                        snapIndex = i;
                    }

                }

                if (mimD <= 0.03f)
                {
                    isSnap = true;
                    pos = snapP;
                    for (int i = 0; i < pointMarkers.Count; i++)
                    {
                        if (i == snapIndex)
                        {
                            pointMarkers[i].material = new Material.SingleColorMaterial(1, 1, 1, 1);
                        }
                        else
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
                        sn.material = new Material.SingleColorMaterial(0, 1, 0, 1);
                    }
                }

            }
            else if (currentState == State.Snap)
            {

                //update the curve, noiticed that we can't update editCurve.Points[snapIndex] directly
                OpenTK.Vector3 ep = Util.vrToPlatformPoint(ref mScene, pos);
                curvePoints[snapIndex] = Util.openTkToRhinoPoint(ep);
                int order = 3;
                if (editCurve.IsClosed)
                {
                    //null check
                    while (order >= 1)
                    {
                        editCurve = Rhino.Geometry.NurbsCurve.Create(true, order, curvePoints.ToArray());
                        if (editCurve != null)
                            break;
                        order--;
                    }
                }
                else
                {
                    //null check
                    while (order >= 1)
                    {
                        editCurve = Rhino.Geometry.NurbsCurve.Create(false, order, curvePoints.ToArray());
                        if (editCurve != null)
                            break;
                        order--;
                    }
                }

                //prevent crash
                if (editCurve == null)
                {
                    while (!mScene.interactionStackEmpty())
                        mScene.popInteraction();
                    return;
                }

                //remove and visualize the new control points
                for (int i = 0; i < pointMarkers.Count; i++)
                {
                    SceneNode sn = pointMarkers[i];
                    mScene.tableGeometry.remove(ref sn);
                }
                pointMarkers.Clear();

                renderEditCurve();

            }

            //visualize the projection point on the plane
            if (onPlane)
            {
                OpenTK.Matrix4 t = OpenTK.Matrix4.CreateTranslation(Util.transformPoint(mScene.tableGeometry.transform.Inverted(), pos));
                t.Transpose();
                drawPoint.transform = t;
            }


        }

        private void renderEditCurve()
        {
            //init and render control points of curve
            if (curvePoints.Count == 0)
            {
                for (int i = 0; i < editCurve.Points.Count; i++)
                {
                    Point3d ep = new Point3d(editCurve.Points.ElementAt(i).Location.X, editCurve.Points.ElementAt(i).Location.Y, editCurve.Points.ElementAt(i).Location.Z);
                    curvePoints.Add(ep);
                    // p is the point before apply tableGeometry.transform inverted so we need to transfrom here
                    SceneNode sn = Util.MarkPointSN(ref mScene.tableGeometry, Util.transformPoint(mScene.tableGeometry.transform.Inverted(), Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)ep.X, (float)ep.Y, (float)ep.Z))), 0, 1, 0);
                    pointMarkers.Add(sn);

                }

                //renderModel at start
                DynamicRender(dynamicRender, "tprint");

            }
            else
            {
                foreach (ControlPoint cp in editCurve.Points)
                {
                    //rotation inverted before visualizing
                    SceneNode sn = Util.MarkPointSN(ref mScene.tableGeometry, Util.transformPoint(mScene.tableGeometry.transform.Inverted(), Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)cp.Location.X, (float)cp.Location.Y, (float)cp.Location.Z))), 0, 1, 0);
                    pointMarkers.Add(sn);
                }
            }

            //render the rhino curve
            polyline = editCurve.ToPolyline(0, 0, 0, 0, 0, 1, 1, 0, true);
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

            //TODO: dynamic render- fix effiency
            /*
            if (dynamicRender == "Revolve")
            {
                R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);
            }*/
            /*
            DynamicRender(dynamicRender, "tprint");
            */
        }

        Brep brepRevolve;
        IAsyncResult R;
        public delegate void generateModel_Delegate();
        generateModel_Delegate d = null;
        public void generateModel()
        {
            Line axis = new Line(new Point3d(0, 0, 0), new Point3d(0, 0, 1));
            RevSurface revsrf = RevSurface.Create(editCurve, axis);
            brepRevolve = Brep.CreateFromRevSurface(revsrf, false, false);

        }

        public void modelCompleted(IAsyncResult R)
        {
            //TODO: remove can't find guid

            if (renderObjId != Guid.Empty)
                Util.removeSceneNode(ref mScene, renderObjId);

            renderObjId = Util.addSceneNode(ref mScene, brepRevolve, ref mesh_m, "aprint");

        }

        protected override void onClickOculusTrigger(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("mim D: " + mimD);
            if (isSnap)
            {
                currentState = State.Snap;
            }
        }

        private void clearDrawing()
        {
            //clear the curve and points
            if (mScene.tableGeometry.children.Count > 0)
            {
                // need to remove rerverse since the list update dynamically
                foreach (SceneNode sn in mScene.tableGeometry.children.Reverse<SceneNode>())
                {
                    if (dynamicRender != "none")
                    {
                        if (sn.name == "EditCurve" || sn.name == "drawPoint" || sn.name == "EditPoint" || sn.name == "panel" || sn.name == "circle" || sn.name == "rect")
                        {
                            mScene.tableGeometry.children.Remove(sn);
                        }
                    }else
                    {
                        //keep editcurve, circle, rect
                        if (sn.name == "drawPoint" || sn.name == "EditPoint" || sn.name == "panel")
                        {
                            mScene.tableGeometry.children.Remove(sn);
                        }
                    }
                }
            }
        }


        protected override void onReleaseOculusTrigger(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("oculus grip release event test");
            if (currentState == State.Snap)
            {

                isSnap = false;
                currentState = State.Start;

                //update the current interaction curve
                mScene.iCurveList[mScene.iCurveList.Count - 1] = editCurve;
                DynamicRender(dynamicRender, "tprint");

            }
        }

        protected override void onClickOculusAX(ref VREvent_t vrEvent)
        {
            //start slicing model by changing the name of the model 
            DynamicRender(dynamicRender, "aprint");

            //clear the curve and points
            clearDrawing();
            mScene.popInteraction();
            if (!mScene.interactionStackEmpty())
            {
                mScene.peekInteraction().init();
            }
            else
            {
                Util.clearPlanePoints(ref mScene);
                Util.clearCurveTargetRhObj(ref mScene);
            }

            //TODO-change the model name to aprint here

        }

        private void DynamicRender(string renderType, string modelName)
        {
            if (renderType == "none")
            {
                return;
            }
            else if (renderType == "Revolve")
            {
                Line axis = new Line(new Point3d(0, 0, 0), new Point3d(0, 0, 1));
                RevSurface revsrf = RevSurface.Create(editCurve, axis);

                Brep brepRevolve = Brep.CreateFromRevSurface(revsrf, false, false);

                //remove the current model
                if (renderObjId != Guid.Empty)
                    Util.removeSceneNode(ref mScene, renderObjId);

                renderObjId = Util.addSceneNode(ref mScene, brepRevolve, ref mesh_m, modelName);

            }
            else if (renderType == "Loft")
            {
                List<Curve> loftcurves = new List<Curve>();
                foreach (Curve curve in mScene.iCurveList)
                {
                    loftcurves.Add(curve);
                }
                Brep[] loftBreps = Brep.CreateFromLoft(loftcurves, Point3d.Unset, Point3d.Unset, LoftType.Tight, false);
                Brep brep = new Brep();
                foreach (Brep bp in loftBreps)
                {
                    brep.Append(bp);
                }

                Mesh base_mesh = new Mesh();

                //remove the current model
                if (renderObjId != Guid.Empty)
                    Util.removeSceneNode(ref mScene, renderObjId);

                // TODO: fix the issue that sometimes the brep is empty. Check the directions of open curves or the seams of closed curves. 
                if (brep != null && brep.Edges.Count != 0)
                {
                    renderObjId = Util.addSceneNode(ref mScene, brep, ref mesh_m, modelName);
                }
            }
            else if (renderType == "Sweep")
            {
                //compute the normal of the first point of the rail curve
                NurbsCurve rail = (NurbsCurve)mScene.iCurveList[1];
                PolylineCurve railPL = rail.ToPolyline(0, 0, 0, 0, 0, 1, 1, 0, true);
                OpenTK.Vector3 railStartPoint = new OpenTK.Vector3((float)railPL.PointAtStart.X, (float)railPL.PointAtStart.Y, (float)railPL.PointAtStart.Z);
                OpenTK.Vector3 railNormal = new OpenTK.Vector3((float)railPL.TangentAtStart.X, (float)railPL.TangentAtStart.Y, (float)railPL.TangentAtStart.Z);

                //need to calculate the center and normal from curve
                //OpenTK.Vector3 shapeCenter = Util.vrToPlatformPoint(ref mScene, mScene.iPointList[0]);
                //OpenTK.Vector3 shapeNormal = new OpenTK.Vector3((float)mScene.iPlaneList[0].Normal.X, (float)mScene.iPlaneList[0].Normal.Y, (float)mScene.iPlaneList[0].Normal.Z);
                OpenTK.Vector3 shapeCenter = new Vector3((float)mScene.iCurveList[0].GetBoundingBox(true).Center.X, (float)mScene.iCurveList[0].GetBoundingBox(true).Center.Y, (float)mScene.iCurveList[0].GetBoundingBox(true).Center.Z);
                Plane curvePlane;
                OpenTK.Vector3 shapeNormal = new Vector3(0, 0, 0);
                Double tolerance = 0;
                while (tolerance < 100) {
                    if (mScene.iCurveList[0].TryGetPlane(out curvePlane, tolerance))
                    {
                        shapeNormal = new OpenTK.Vector3((float)curvePlane.Normal.X, (float)curvePlane.Normal.Y, (float)curvePlane.Normal.Z);
                        break;
                    }
                    tolerance++;
                }              

                OpenTK.Matrix4 transM = Util.getCoordinateTransM(shapeCenter, railStartPoint, shapeNormal, railNormal);
                Transform t = Util.OpenTKToRhinoTransform(transM);
                ((NurbsCurve)mScene.iCurveList[0]).Transform(t);
                NurbsCurve circleCurve = (NurbsCurve)mScene.iCurveList[0];

                //another solution-create a new circle at startpoint
                /*
                Plane plane = new Plane(railPL.PointAtStart, railPL.TangentAtStart);
                Point3d origin = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, mScene.iPointList[0]));
                Point3d circleP = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, mScene.iPointList[1]));
                float radius = (float)Math.Sqrt(Math.Pow(circleP.X - origin.X, 2) + Math.Pow(circleP.Y - origin.Y, 2) + Math.Pow(circleP.Z - origin.Z, 2));
                Rhino.Geometry.Circle circle = new Rhino.Geometry.Circle(plane, railPL.PointAtStart, radius);
                NurbsCurve circleCurve = circle.ToNurbsCurve();
                */

                //remove the current model
                if (renderObjId != Guid.Empty)
                    Util.removeSceneNode(ref mScene, renderObjId);

                //cruves coordinate are in rhino
                Brep[] breps = Brep.CreateFromSweep(mScene.iCurveList[1], circleCurve, false, mScene.rhinoDoc.ModelAbsoluteTolerance);
                Brep brep = breps[0];
                if (brep != null)
                {
                    renderObjId = Util.addSceneNode(ref mScene, brep, ref mesh_m, modelName);
                }

                //reverse transfrom the curvelist
                Transform invT;
                if(t.TryGetInverse(out invT))
                    ((NurbsCurve)mScene.iCurveList[0]).Transform(invT);
            }
        }

    }
}
