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
        private Plane curvePlane;
        private NurbsCurve circleCurve;
        private NurbsCurve rectCurve;
        //Sweep debug
        private List<Curve> profileCurves = new List<Curve>();
        double angle = 0;
        double curveT = 0;
        Point3d startP;

        private bool backgroundStart = false;
        private float displacement = 0;
        Brep dynamicBrep;
        IAsyncResult R;
        public delegate void generateModel_Delegate();
        generateModel_Delegate d = null;
        private string modelName = "tprint";


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
            mesh_m = new Material.LambertianMaterial(.7f,.7f,.7f,.3f);
            //mesh_m = new Material.RGBNormalMaterial(0.5f);
        }

        public override void init()
        {
            if (onPlane)
            {
                //add projectionPoint
                drawPoint = Util.MarkProjectionPoint(ref mScene, new OpenTK.Vector3(0, 0, 0), 1, 1, 1);
                rhinoPlane = mScene.iRhObjList.ElementAt(mScene.iRhObjList.Count - 1);
            }

            //create editcurve, find the curve plane, render circle, rect
            if (dynamicRender == "Extrude")
            {
                List<Point3d> extrudeCurveP = new List<Point3d>();
                extrudeCurveP.Add(mScene.iCurveList[mScene.iCurveList.Count - 1].PointAtStart);
                extrudeCurveP.Add(mScene.iCurveList[mScene.iCurveList.Count - 1].PointAtEnd);
                //update the curve
                mScene.iCurveList[mScene.iCurveList.Count - 1] = Rhino.Geometry.NurbsCurve.Create(false, 1, extrudeCurveP.ToArray());
                editCurve = (NurbsCurve)mScene.iCurveList.ElementAt(mScene.iCurveList.Count - 1);
            }
            else if (mScene.selectionList[mScene.selectionList.Count - 1] == "Circle" || dynamicRender == "Sweep-Circle")
            {
                List<Point3d> extrudeCurveP = new List<Point3d>();
                Point3d origin = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, mScene.iPointList[mScene.iPointList.Count - 2]));
                Point3d circleP = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, mScene.iPointList[mScene.iPointList.Count - 1]));
                extrudeCurveP.Add(origin);
                extrudeCurveP.Add(circleP);
                //update the edit curve
                editCurve = Rhino.Geometry.NurbsCurve.Create(false, 1, extrudeCurveP.ToArray());

                //render Circle
                float radius = (float)Math.Sqrt(Math.Pow(circleP.X - origin.X, 2) + Math.Pow(circleP.Y - origin.Y, 2) + Math.Pow(circleP.Z - origin.Z, 2));

                //compute the plane from RhinoObj
                RhinoObject newObj = mScene.rhinoDoc.Objects.Find(mScene.iRhObjList[mScene.iRhObjList.Count - 1].Id);
                Brep targetBrep = (Brep)(newObj.Geometry);
                //compute the brepFace where the curve is on
                int faceIndex = -1;
                for (int i = 0; i < targetBrep.Faces.Count; i++)
                {
                    //cast BrepFace to Brep for ClosestPoint(P) menthod
                    double dist = targetBrep.Faces[i].DuplicateFace(false).ClosestPoint(origin).DistanceTo(origin);
                    //tolerance mScene.rhinoDoc.ModelAbsoluteTolerance too low
                    if (dist < mScene.rhinoDoc.ModelAbsoluteTolerance)
                    {
                        faceIndex = i;
                        break;
                    }
                }

                Surface s = targetBrep.Faces[faceIndex];
                Plane circlePlane;
                if (s.TryGetPlane(out circlePlane))
                {
                    Rhino.Geometry.Circle circle = new Rhino.Geometry.Circle(circlePlane, origin, radius);
                    circleCurve = circle.ToNurbsCurve();
                    Brep[] shapes = Brep.CreatePlanarBreps(circleCurve);
                    Brep circle_s = shapes[0];
                    Brep circleBrep = circle_s;

                    renderObjId = Util.addSceneNode(ref mScene, circleBrep, ref mesh_m, "circle");
                    //add curve to mScene.iCurveList
                    mScene.iCurveList.Add(circleCurve);
                    curvePlane = circlePlane;
                }


            }
            else if (mScene.selectionList[mScene.selectionList.Count - 1] == "Rect" || dynamicRender == "Sweep-Rect")
            {
                List<Point3d> extrudeCurveP = new List<Point3d>();
                
                Point3d topLeftP = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, mScene.iPointList[mScene.iPointList.Count - 2]));
                Point3d bottomRightP = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, mScene.iPointList[mScene.iPointList.Count - 1]));
                extrudeCurveP.Add(topLeftP);
                extrudeCurveP.Add(bottomRightP);
                //update the curve
                editCurve = Rhino.Geometry.NurbsCurve.Create(false, 1, extrudeCurveP.ToArray());

                //render Rect
                Brep targetBrep = (Brep)(mScene.iRhObjList[mScene.iRhObjList.Count - 1].Geometry);
                //compute the brepFace where the curve is on
                //Surface s = targetBrep.Faces[0];

                //TODO- topLeftP won't be on the face in the 3D case. so probably use orgin
                int faceIndex = -1;
                for (int i = 0; i < targetBrep.Faces.Count; i++)
                {
                    //cast BrepFace to Brep for ClosestPoint(P) menthod
                    double dist = targetBrep.Faces[i].DuplicateFace(false).ClosestPoint(topLeftP).DistanceTo(topLeftP);
                    if (dist < mScene.rhinoDoc.ModelAbsoluteTolerance)
                    {
                        faceIndex = i;
                        break;
                    }
                }
                Surface s = targetBrep.Faces[faceIndex];
                Plane rectPlane;
                if (s.TryGetPlane(out rectPlane))
                {
                    //plane - testing Rectangle3d
                    Rectangle3d rect = new Rectangle3d(rectPlane, topLeftP, bottomRightP);

                    rectCurve = rect.ToNurbsCurve();
                    Brep[] shapes = Brep.CreatePlanarBreps(rectCurve);
                    Brep rectBrep = shapes[0];

                    renderObjId = Util.addSceneNode(ref mScene, rectBrep, ref mesh_m, "rect");

                    //add curve to mScene.iCurveList   
                    mScene.iCurveList.Add(rectCurve);
                    curvePlane = rectPlane;
                }

            }
            else
            {
                editCurve = (NurbsCurve)mScene.iCurveList.ElementAt(mScene.iCurveList.Count - 1);
                Plane cPlane;
                if (editCurve.TryGetPlane(out cPlane))
                {
                    curvePlane = cPlane;
                }
            }

            //null check
            if (editCurve == null)
            {
                while (!mScene.interactionStackEmpty())
                    mScene.popInteraction();
                return;
            }

            d = new generateModel_Delegate(generateModel);

            renderEditCurve();
            //render model
            R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);

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
                    //targetPSN = mScene.brepToSceneNodeDic[rhinoPlane.Id];
                    targetPRhObj = rhinoPlane;
                }
                else
                {
                    //targetPSN = null;
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
                    //TODO-force not to move the center in Circle or topleft in Rect
                    if (i == 0)
                    {
                        if (mScene.selectionList[mScene.selectionList.Count - 1] == "Circle" || dynamicRender == "Sweep-Circle")
                        {
                            continue;
                        }
                        else if (mScene.selectionList[mScene.selectionList.Count - 1] == "Rect" || dynamicRender == "Sweep-Rect")
                        {
                            continue;
                        }
                    }

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

                    if ((mScene.selectionList[mScene.selectionList.Count - 1] == "Circle" || dynamicRender == "Sweep-Circle") || (mScene.selectionList[mScene.selectionList.Count - 1] == "Rect" || dynamicRender == "Sweep-Rect"))
                    {
                        //only 1 edit point
                        pointMarkers[0].material = new Material.SingleColorMaterial(1, 1, 1, 1);
                    }
                    else
                    {

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
                //accumulate displacement
                displacement = displacement + (float)Math.Sqrt(Math.Pow(ep.X - curvePoints[snapIndex].X, 2) + Math.Pow(ep.Y - curvePoints[snapIndex].Y, 2) + Math.Pow(ep.Z - curvePoints[snapIndex].Z, 2));
                curvePoints[snapIndex] = Util.openTkToRhinoPoint(ep);


                int order = editCurve.Order;
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


                renderEditCurve();
                updateEditCurve();

                //dynamic render model
                //TODO: dynamic render- fix effiency
                if (backgroundStart == false && displacement > 10)
                {
                    backgroundStart = true;
                    R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);
                }

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

                    //TODO-force not to move the center in Circle or topleft in Rect
                    if (i == 0)
                    {
                        if (mScene.selectionList[mScene.selectionList.Count - 1] == "Circle" || dynamicRender == "Sweep-Circle")
                        {
                            continue;
                        }
                        else if (mScene.selectionList[mScene.selectionList.Count - 1] == "Rect" || dynamicRender == "Sweep-Rect")
                        {
                            continue;
                        }
                    }
                    // p is the point before apply tableGeometry.transform inverted so we need to transfrom here
                    SceneNode sn = Util.MarkPointSN(ref mScene.tableGeometry, Util.transformPoint(mScene.tableGeometry.transform.Inverted(), Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)ep.X, (float)ep.Y, (float)ep.Z))), 0, 1, 0);
                    pointMarkers.Add(sn);
                }

            }
            else
            {
                //remove and visualize the new control points
                for (int i = 0; i < pointMarkers.Count; i++)
                {
                    SceneNode sn = pointMarkers[i];
                    mScene.tableGeometry.remove(ref sn);
                }
                pointMarkers.Clear();

                for (int i = 0; i < editCurve.Points.Count; i++)
                {

                    //TODO-force not to move the center in Circle or topleft in Rect
                    if (i == 0)
                    {
                        if (mScene.selectionList[mScene.selectionList.Count - 1] == "Circle" || dynamicRender == "Sweep-Circle")
                        {
                            continue;
                        }
                        else if (mScene.selectionList[mScene.selectionList.Count - 1] == "Rect" || dynamicRender == "Sweep-Rect")
                        {
                            continue;
                        }
                    }

                    Point3d cp = new Point3d(editCurve.Points.ElementAt(i).Location.X, editCurve.Points.ElementAt(i).Location.Y, editCurve.Points.ElementAt(i).Location.Z);

                    //rotation inverted before visualizing
                    SceneNode sn = Util.MarkPointSN(ref mScene.tableGeometry, Util.transformPoint(mScene.tableGeometry.transform.Inverted(), Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)cp.X, (float)cp.Y, (float)cp.Z))), 0, 1, 0);
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

        }

        public void generateModel()
        {
            if (dynamicRender == "none")
            {
                return;
            }
            else if (dynamicRender == "Revolve")
            {
                dynamicBrep = Util.RevolveFunc(ref mScene, ref mScene.iCurveList);
            }
            else if (dynamicRender == "Loft")
            {
                dynamicBrep = Util.LoftFunc(ref mScene, ref mScene.iCurveList);
            }
            else if (dynamicRender == "Extrude")
            {
                //TODO-using Sweep fnction to do and find the intersect point             
                dynamicBrep = Util.ExtrudeFunc(ref mScene, ref mScene.iCurveList);
            }
            else if (dynamicRender == "Sweep")
            {
                dynamicBrep = Util.SweepFun(ref mScene, ref mScene.iCurveList);
            }
            else if (dynamicRender == "Sweep-Circle")
            {
                dynamicBrep = Util.SweepCapFun(ref mScene, ref mScene.iCurveList);
            }else if (dynamicRender == "Sweep-Rect")
            {
                dynamicBrep = Util.SweepCapFun(ref mScene, ref mScene.iCurveList);
            }

        }

        public void modelCompleted(IAsyncResult R)
        {
            if (dynamicBrep != null)
            {
                if (modelName == "tprint")
                {
                    renderObjId = Util.addSceneNodeWithoutDraw(ref mScene, dynamicBrep, ref mesh_m, modelName);

                    //SweepCapFun debugging
                    if (dynamicRender == "Sweep-Circle")
                    {
                        Rhino.RhinoApp.WriteLine("Dot: " + mScene.angleD);
                        Rhino.RhinoApp.WriteLine("c1 dir: " + mScene.c1D);
                        Rhino.RhinoApp.WriteLine("c2 dir: " + mScene.c2D);
                        Rhino.RhinoApp.WriteLine("c2_n dir: " + mScene.c3D);

                        //TODO-can't draw the point here !!!! WHY?

                    }

                }
                else if (modelName == "aprint")
                {
                    //don't need to removeSceneNode since we only create rhinoObj when we press button
                    /*
                    if (renderObjId != Guid.Empty)
                        Util.removeSceneNode(ref mScene, renderObjId);
                    */
                    renderObjId = Util.addSceneNode(ref mScene, dynamicBrep, ref mesh_m, modelName);

                    clearDrawing();
                    Util.clearPlanePoints(ref mScene);
                    Util.clearCurveTargetRhObj(ref mScene);
                    //TODO- OpenGL compile error why?
                    //Util.setPlaneAlpha(ref mScene, 0.0f);
                }
            }
            dynamicBrep = null;
            backgroundStart = false;
            displacement = 0;

        }

        protected override void onClickOculusTrigger(ref VREvent_t vrEvent)
        {
            //Rhino.RhinoApp.WriteLine("mim D: " + mimD);
            if (isSnap)
            {
                currentState = State.Snap;
            }
        }

        protected override void onReleaseOculusTrigger(ref VREvent_t vrEvent)
        {
            //Rhino.RhinoApp.WriteLine("oculus grip release event test");
            if (currentState == State.Snap)
            {

                isSnap = false;
                currentState = State.Start;
                R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);

            }
        }

        private void clearDrawing()
        {
            //clear the curve and points
            if (mScene.tableGeometry.children.Count > 0)
            {
                // need to remove rerverse since the list update dynamically
                //TODO- check if reverse is ok
                foreach (SceneNode sn in mScene.tableGeometry.children.Reverse<SceneNode>())
                {
                    if (dynamicRender == "Revolve" || dynamicRender == "Loft" || dynamicRender == "Sweep-Circle" || dynamicRender == "Sweep-Rect" || dynamicRender == "Extrude")
                    {
                        if (sn.name.Contains("tprint") || sn.name == "EditCurve" || sn.name == "drawPoint" || sn.name == "EditPoint" || sn.name.Contains("panel") || sn.name.Contains("circle") || sn.name.Contains("rect"))
                        {
                            //only panel will create RhinoObj for ray-tracing
                            if (sn.name.Contains("panel"))
                            {
                                //panel didn't have the sceneNode in VR.
                                //delObj = mScene.SceneNodeToBrepDic[sn.guid];
                                //Util.removeSceneNode(ref mScene, delObj.Id);

                            }
                            else
                            {
                                mScene.tableGeometry.children.Remove(sn);
                            }

                        }
                    }
                    else
                    {
                        if (dynamicRender == "Sweep")
                        {
                            if (sn.name.Contains("tprint"))
                            {
                                mScene.tableGeometry.children.Remove(sn);
                            }
                        }
                        //only clear the drawpoint, editpoint
                        if (sn.name == "drawPoint" || sn.name == "EditPoint")
                        {
                            mScene.tableGeometry.children.Remove(sn);
                        }
                    }

                }
            }
        }


        protected override void onClickOculusAX(ref VREvent_t vrEvent)
        {
            //TODO- need to consider this might be the first time editpoint.
            //start slicing model by changing the name of the model
            mScene.popInteraction();
            Util.setPlaneAlpha(ref mScene, 0.0f);

            if (dynamicRender == "Revolve" || dynamicRender == "Loft")
            {
                modelName = "aprint";
                
                R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);
            }
            else if (dynamicRender == "Extrude")
            {
                clearDrawing();
                if (mScene.selectionList[1] == "Circle")
                {
                    generateEndCap();
                    mScene.popInteraction();
                    mScene.pushInteraction(new EditPoint2(ref mScene, true, "Sweep-Circle"));
                    mScene.peekInteraction().init();
                }
                else if (mScene.selectionList[1] == "Rect")
                {
                    generateEndCap();
                    mScene.popInteraction();
                    mScene.pushInteraction(new EditPoint2(ref mScene, true, "Sweep-Rect"));
                    mScene.peekInteraction().init();
                }
            }
            else if (dynamicRender == "Sweep") //TODO-implement edit endCurve
            {
                clearDrawing();
                if (mScene.selectionList[1] == "Circle") {
                    generateEndCap();
                    mScene.popInteraction();
                    mScene.pushInteraction(new EditPoint2(ref mScene, true, "Sweep-Circle"));
                    mScene.peekInteraction().init();
                }else if (mScene.selectionList[1] == "Rect")
                {
                    generateEndCap();
                    mScene.popInteraction();
                    mScene.pushInteraction(new EditPoint2(ref mScene, true, "Sweep-Rect"));
                    mScene.peekInteraction().init();
                }
                else
                {
                    modelName = "aprint";
                    R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);
                }
            }
            else if (dynamicRender == "Sweep-Circle")
            {
                modelName = "aprint";
                R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);

            }
            else if (dynamicRender == "Sweep-Rect")
            {
                modelName = "aprint";
                R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);

            }
            else
            {
                clearDrawing();
            }

            currentState = State.End;

        }

        private void updateEditCurve()
        {
            //create and render interaction curve and editcurve
            if (dynamicRender == "Extrude")
            {
                List<Point3d> extrudeCurveP = new List<Point3d>();
                extrudeCurveP.Add(editCurve.PointAtStart);
                extrudeCurveP.Add(editCurve.PointAtEnd);
                //update the curve
                mScene.iCurveList[mScene.iCurveList.Count - 1] = Rhino.Geometry.NurbsCurve.Create(false, 1, extrudeCurveP.ToArray());
            }
            else if (mScene.selectionList[mScene.selectionList.Count - 1] == "Circle" || dynamicRender == "Sweep-Circle")
            {
                Point3d circleP = editCurve.PointAtEnd;
                Point3d origin = editCurve.PointAtStart;
                float radius = (float)Math.Sqrt(Math.Pow(circleP.X - origin.X, 2) + Math.Pow(circleP.Y - origin.Y, 2) + Math.Pow(circleP.Z - origin.Z, 2));

                if (curvePlane != null)
                {
                    Rhino.Geometry.Circle circle = new Rhino.Geometry.Circle(curvePlane, origin, radius);
                    circleCurve = circle.ToNurbsCurve();
                    Brep[] shapes = Brep.CreatePlanarBreps(circleCurve);
                    Brep circle_s = shapes[0];
                    //don't need to update the RhinoView
                    renderObjId = Util.addSceneNodeWithoutDraw(ref mScene, circle_s, ref mesh_m, "circle");

                    mScene.iCurveList[mScene.iCurveList.Count - 1] = circleCurve;

                    //TODO-updating the iPointList
                    mScene.iPointList[mScene.iPointList.Count - 2] = Util.platformToVRPoint(ref mScene, new Vector3((float)origin.X, (float) origin.Y, (float) origin.Z));
                    mScene.iPointList[mScene.iPointList.Count - 1] = Util.platformToVRPoint(ref mScene, new Vector3((float)circleP.X, (float)circleP.Y, (float)circleP.Z));

                }
            }
            else if (mScene.selectionList[mScene.selectionList.Count - 1] == "Rect" || dynamicRender == "Sweep-Rect")
            {

                Point3d topLeftP = editCurve.PointAtStart;
                Point3d bottomRightP = editCurve.PointAtEnd;
                if (curvePlane != null)
                {
                    //plane - testing Rectangle3d
                    Rectangle3d rect = new Rectangle3d(curvePlane, topLeftP, bottomRightP);
                    rectCurve = rect.ToNurbsCurve();
                    Brep[] shapes = Brep.CreatePlanarBreps(rectCurve);
                    Brep rectBrep = shapes[0];

                    //don't need to update the RhinoView
                    renderObjId = Util.addSceneNodeWithoutDraw(ref mScene, rectBrep, ref mesh_m, "rect");

                    mScene.iCurveList[mScene.iCurveList.Count - 1] = rectCurve;

                    //TODO-updating the iPointList
                    mScene.iPointList[mScene.iPointList.Count - 2] = Util.platformToVRPoint(ref mScene, new Vector3((float)topLeftP.X, (float)topLeftP.Y, (float)topLeftP.Z));
                    mScene.iPointList[mScene.iPointList.Count - 1] = Util.platformToVRPoint(ref mScene, new Vector3((float)bottomRightP.X, (float)bottomRightP.Y, (float)bottomRightP.Z));

                }
            }
            else
            {
                mScene.iCurveList[mScene.iCurveList.Count - 1] = editCurve;
            }
        }

        private void generateEndCap()
        {
            NurbsCurve rail = (NurbsCurve)mScene.iCurveList[mScene.iCurveList.Count - 1];
            PolylineCurve railPL = rail.ToPolyline(0, 0, 0, 0, 0, 1, 1, 0, true);
            OpenTK.Vector3 shapeCenter = new Vector3((float)mScene.iCurveList[mScene.iCurveList.Count - 2].GetBoundingBox(true).Center.X, (float)mScene.iCurveList[mScene.iCurveList.Count - 2].GetBoundingBox(true).Center.Y, (float)mScene.iCurveList[mScene.iCurveList.Count - 2].GetBoundingBox(true).Center.Z);
            OpenTK.Vector3 shapeP = new Vector3((float)mScene.iCurveList[mScene.iCurveList.Count - 2].PointAtStart.X, (float)mScene.iCurveList[mScene.iCurveList.Count - 2].PointAtStart.Y, (float)mScene.iCurveList[mScene.iCurveList.Count - 2].PointAtStart.Z);
            OpenTK.Vector3 shapeNormal = new Vector3(0, 0, 0);
            Plane curvePlane;
            Double tolerance = 0;
            while (tolerance < 100)
            {
                if (mScene.iCurveList[mScene.iCurveList.Count - 2].TryGetPlane(out curvePlane, tolerance))
                {
                    shapeNormal = new OpenTK.Vector3((float)curvePlane.Normal.X, (float)curvePlane.Normal.Y, (float)curvePlane.Normal.Z);
                    break;
                }
                tolerance++;
            }

            OpenTK.Vector3 railEndPoint = new OpenTK.Vector3((float)railPL.PointAtEnd.X, (float)railPL.PointAtEnd.Y, (float)railPL.PointAtEnd.Z);
            OpenTK.Vector3 railEndNormal = new OpenTK.Vector3((float)railPL.TangentAtEnd.X, (float)railPL.TangentAtEnd.Y, (float)railPL.TangentAtEnd.Z);
            OpenTK.Matrix4 transMEnd = new Matrix4();

            transMEnd = Util.getCoordinateTransM(shapeCenter, railEndPoint, shapeNormal, railEndNormal);

            // index need to improve
            if (mScene.selectionList[1] == "Circle")
            {

                OpenTK.Vector3 newCenter = Util.transformPoint(transMEnd, shapeCenter);
                OpenTK.Vector3 newCircleP = Util.transformPoint(transMEnd, shapeP);
                //Rhino to VR
                mScene.iPointList.Add(Util.platformToVRPoint(ref mScene, newCenter));
                mScene.iPointList.Add(Util.platformToVRPoint(ref mScene, newCircleP));

                //TODO-add endPlane Rhino Object
                Plane endPlane = new Plane(railPL.PointAtEnd, railPL.TangentAtEnd);
                PlaneSurface plane_surface = new PlaneSurface(endPlane, new Interval(-120, 120), new Interval(-120, 120));

                Brep designPlane = Brep.CreateFromSurface(plane_surface);

                if (designPlane != null)
                {
                    Guid guid = Util.addSceneNodeWithoutVR(ref mScene, designPlane, ref mesh_m, "panel");
                    mScene.iRhObjList.Add(mScene.rhinoDoc.Objects.Find(guid));
                }
            }else if (mScene.selectionList[1] == "Rect")
            {
                OpenTK.Vector3 newTopLeft = Util.transformPoint(transMEnd, mScene.iPointList[mScene.iPointList.Count-2]);
                OpenTK.Vector3 newBottomRight = Util.transformPoint(transMEnd, mScene.iPointList[mScene.iPointList.Count - 1]);

                //addd the new endRect to iPointList
                mScene.iPointList.Add(Util.platformToVRPoint(ref mScene, newTopLeft));
                mScene.iPointList.Add(Util.platformToVRPoint(ref mScene, newBottomRight));

                //TODO-add endPlane Rhino Object
                Plane endPlane = new Plane(railPL.PointAtEnd, railPL.TangentAtEnd);
                PlaneSurface plane_surface = new PlaneSurface(endPlane, new Interval(-120, 120), new Interval(-120, 120));

                Brep designPlane = Brep.CreateFromSurface(plane_surface);

                if (designPlane != null)
                {
                    Guid guid = Util.addSceneNodeWithoutVR(ref mScene, designPlane, ref mesh_m, "panel");
                    mScene.iRhObjList.Add(mScene.rhinoDoc.Objects.Find(guid));
                }

            }
        }

        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            //mScene.iCurveList[mScene.iCurveList.Count - 1].Reverse();
            //R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);
            //curveT += 0.1;

            //DynamicRender(dynamicRender, "tprint");

            //profileCurves.ElementAt(1).Reverse();
            //DynamicRender(dynamicRender, "tprint");

            OpenTK.Vector3 p1 = Util.transformPoint(mScene.tableGeometry.transform.Inverted(), Util.platformToVRPoint(ref mScene, new Vector3((float)mScene.eStartP.X, (float)mScene.eStartP.Y, (float)mScene.eStartP.Z)));
            OpenTK.Vector3 p2 = Util.transformPoint(mScene.tableGeometry.transform.Inverted(), Util.platformToVRPoint(ref mScene, new Vector3((float)mScene.sStartP.X, (float)mScene.sStartP.Y, (float)mScene.sStartP.Z)));
            Util.MarkDebugPoint(ref mScene.tableGeometry, p1, 0f, 0f, 1f);
            Util.MarkDebugPoint(ref mScene.tableGeometry, p2, 0f, 0f, 1f);
        }

    }
}
