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
        private NurbsCurve circleCurve;
        private NurbsCurve rectCurve;

        private bool backgroundStart = false;
        private float displacement =0;


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

            //TODO-generate 2 cp curvev for extrudsion, circle, rect
            //TODO-generate 2 cp for Sweep endCurve 
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
                //testing debug
                RhinoObject newObj = mScene.rhinoDoc.Objects.Find(mScene.iRhObjList[mScene.iRhObjList.Count - 1].Id);
                Brep targetBrep = (Brep)(newObj.Geometry);
                //compute the brepFace where the curve is on
                //Surface s = targetBrep.Faces[0];
                //TODO: check if targetBrep have many faces as expect
                int faceIndex = -1;
                for (int i =0; i < targetBrep.Faces.Count; i++)
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
                //Curve planeCurve = ((Brep)mScene.iRhObjList[mScene.iRhObjList.Count - 1].Geometry).Curves3D.ElementAt(0); //somehow incorrect result
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
                }


            }
            else if (mScene.selectionList[mScene.selectionList.Count - 1] == "Rect")
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
                }

            }
            else
            {
                editCurve = (NurbsCurve)mScene.iCurveList.ElementAt(mScene.iCurveList.Count - 1);
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
                    //TODO-force not to move the center in Circle or topleft in Rect
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
                //accumulate displacement
                displacement = displacement + (float)Math.Sqrt(Math.Pow(ep.X - curvePoints[snapIndex].X, 2) + Math.Pow(ep.Y - curvePoints[snapIndex].Y, 2) + Math.Pow(ep.Z - curvePoints[snapIndex].Z, 2));
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
            if (dynamicRender == "Revolve" && backgroundStart == false && displacement > 10)
            {
                backgroundStart = true;
                R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);
            }*/
            
            //DynamicRender(dynamicRender, "tprint");
            
        }

        Brep brepRevolve;
        IAsyncResult R;
        public delegate void generateModel_Delegate();
        generateModel_Delegate d = null;
        public void generateModel()
        {
            //might need to detect the movement threshold to start rebuild

            Line axis = new Line(new Point3d(0, 0, 0), new Point3d(0, 0, 1));
            RevSurface revsrf = RevSurface.Create(editCurve, axis);
            brepRevolve = Brep.CreateFromRevSurface(revsrf, false, false);
            
            //DynamicRender(dynamicRender, "tprint");
        }

        public void modelCompleted(IAsyncResult R)
        {
            //TODO: remove can't find guid
            /*
            if (renderObjId != Guid.Empty)
                Util.removeSceneNode(ref mScene, renderObjId);
            */
            renderObjId = Util.addSceneNodeWithoutDraw(ref mScene, brepRevolve, ref mesh_m, "tprint");

            /*
            if (currentState == State.Snap && displacement > 10)
            {
                R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);
            }*/
            backgroundStart = false;
            displacement = 0;


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

                    if (dynamicRender == "Revolve" || dynamicRender == "Loft" || dynamicRender == "Sweep-Circle" || dynamicRender == "Extrude")
                    {
                        if (sn.name == "EditCurve" || sn.name == "drawPoint" || sn.name == "EditPoint" || sn.name == "panel" || sn.name == "circle" || sn.name == "rect")
                        {
                            if (sn.name == "panel" || sn.name == "circle" || sn.name == "rect")
                            {
                                RhinoObject delObj = mScene.SceneNodeToBrepDic[sn.guid];
                                Util.removeSceneNode(ref mScene, delObj.Id);
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
                            if (sn.name == "Sweep")
                            {
                                RhinoObject delObj = mScene.SceneNodeToBrepDic[sn.guid];
                                Util.removeSceneNode(ref mScene, delObj.Id);
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


        protected override void onReleaseOculusTrigger(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("oculus grip release event test");
            if (currentState == State.Snap)
            {

                isSnap = false;
                currentState = State.Start;
                DynamicRender(dynamicRender, "tprint");

            }
        }

        protected override void onClickOculusAX(ref VREvent_t vrEvent)
        {
            //TODO- need to consider this might be the first time editpoint.
            //start slicing model by changing the name of the model
            mScene.popInteraction();
            
            //still need panel to render
            if(dynamicRender != "Sweep-Circle")
                clearDrawing();

            if (dynamicRender == "Revolve" || dynamicRender == "Loft" || dynamicRender == "Extrude")
            {
                DynamicRender(dynamicRender, "aprint");
                Util.clearPlanePoints(ref mScene);
                Util.clearCurveTargetRhObj(ref mScene);
            }
            else if (dynamicRender == "Sweep") //TODO-implement edit endCurve
            {
                /*
                DynamicRender(dynamicRender, "aprint");
                Util.clearPlanePoints(ref mScene);
                Util.clearCurveTargetRhObj(ref mScene);
                */

                //TODO- Sweep add endCapCurve to iCurveList
                //Using Selection List to push interaction with correct type
                generateEndCap();

            }
            else if(dynamicRender == "Sweep-Circle")
            {
                DynamicRender(dynamicRender, "aprint");
                Util.clearPlanePoints(ref mScene);
                Util.clearCurveTargetRhObj(ref mScene);
                clearDrawing();
                currentState = State.End;


            }
            else
            {
                DynamicRender(dynamicRender, "tprint");
            }


        }

        private void DynamicRender(string renderType, string modelName)
        {
            //render Cirle and Rect
            if (mScene.selectionList[mScene.selectionList.Count - 1] == "Circle" || dynamicRender == "Sweep-Circle")
            {
                Point3d circleP = editCurve.PointAtEnd;
                Point3d origin = editCurve.PointAtStart;
                float radius = (float)Math.Sqrt(Math.Pow(circleP.X - origin.X, 2) + Math.Pow(circleP.Y - origin.Y, 2) + Math.Pow(circleP.Z - origin.Z, 2));

                Plane curvePlane;

                if (circleCurve.TryGetPlane(out curvePlane))
                {
                    Rhino.Geometry.Circle circle = new Rhino.Geometry.Circle(curvePlane, origin, radius);
                    circleCurve = circle.ToNurbsCurve();
                    Brep[] shapes = Brep.CreatePlanarBreps(circleCurve);
                    Brep circle_s = shapes[0];

                    //remove the current model
                    if (renderObjId != Guid.Empty)
                        Util.removeSceneNode(ref mScene, renderObjId);

                    renderObjId = Util.addSceneNode(ref mScene, circle_s, ref mesh_m, "circle");
                }
            }
            else if (mScene.selectionList[mScene.selectionList.Count - 1] == "Rect")
            {

                Point3d topLeftP = editCurve.PointAtStart;
                Point3d bottomRightP = editCurve.PointAtEnd;
                Plane curvePlane;

                if (rectCurve.TryGetPlane(out curvePlane))
                {
                    //plane - testing Rectangle3d
                    Rectangle3d rect = new Rectangle3d(curvePlane, topLeftP, bottomRightP);

                    rectCurve = rect.ToNurbsCurve();
                    Brep[] shapes = Brep.CreatePlanarBreps(rectCurve);
                    Brep rectBrep = shapes[0];

                    //remove the current model
                    if (renderObjId != Guid.Empty)
                        Util.removeSceneNode(ref mScene, renderObjId);

                    renderObjId = Util.addSceneNode(ref mScene, rectBrep, ref mesh_m, "rect");
                }
            }


            //update the current interaction curve
            if (dynamicRender == "Circle" || mScene.selectionList[mScene.selectionList.Count - 1] == "Circle" || dynamicRender == "Sweep-Circle")
            {
                mScene.iCurveList[mScene.iCurveList.Count - 1] = circleCurve;
            }
            else if (dynamicRender == "Rect" || mScene.selectionList[mScene.selectionList.Count - 1] == "Rect")
            {
                mScene.iCurveList[mScene.iCurveList.Count - 1] = rectCurve;
            }
            else
            {
                mScene.iCurveList[mScene.iCurveList.Count - 1] = editCurve;
            }

            if (renderType == "none")
            {
                return;
            }
            else if (renderType == "Revolve")
            {
                
                Line axis = new Line(new Point3d(0, 0, 0), new Point3d(0, 0, 1));
                RevSurface revsrf = RevSurface.Create(mScene.iCurveList[mScene.iCurveList.Count - 1], axis);

                Brep brepRevolve = Brep.CreateFromRevSurface(revsrf, false, false);

                //remove the current model
                
                if (renderObjId != Guid.Empty)
                    Util.removeSceneNode(ref mScene, renderObjId);

                renderObjId = Util.addSceneNode(ref mScene, brepRevolve, ref mesh_m, modelName);
                
                
                //renderObjId = Util.addSceneNodeWithoutDraw(ref mScene, brepRevolve, ref mesh_m, modelName);

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
            else if (renderType == "Extrude")
            {
                //TODO-using Sweep fnction to do and find the intersect point
                Curve railCurve = mScene.iCurveList[mScene.iCurveList.Count - 1];
                Plane curvePlane;
                double height = 0;
                if (mScene.iCurveList[mScene.iCurveList.Count - 2].TryGetPlane(out curvePlane))
                {
                    OpenTK.Vector3 heightVector = new OpenTK.Vector3((float)(railCurve.PointAtEnd.X - railCurve.PointAtStart.X), (float)(railCurve.PointAtEnd.Y - railCurve.PointAtStart.Y), (float)(railCurve.PointAtEnd.Z - railCurve.PointAtStart.Z));
                    OpenTK.Vector3 planeNormal = new OpenTK.Vector3((float)curvePlane.Normal.X, (float)curvePlane.Normal.Y, (float)curvePlane.Normal.Z);
                    planeNormal.Normalize();
                    height = OpenTK.Vector3.Dot(heightVector, planeNormal) / planeNormal.Length;
                }

                Rhino.Geometry.Extrusion extrusion = Rhino.Geometry.Extrusion.Create(mScene.iCurveList[mScene.iCurveList.Count - 2], height, true);
                Brep extrudeBrep = extrusion.ToBrep();

                //remove the current model
                if (renderObjId != Guid.Empty)
                    Util.removeSceneNode(ref mScene, renderObjId);
                if (extrudeBrep != null)
                {
                    renderObjId = Util.addSceneNode(ref mScene, extrudeBrep, ref mesh_m, modelName);
                }

            }
            else if (renderType == "Sweep")
            {
                //compute the normal of the first point of the rail curve
                NurbsCurve rail = (NurbsCurve)mScene.iCurveList[mScene.iCurveList.Count - 1];
                PolylineCurve railPL = rail.ToPolyline(0, 0, 0, 0, 0, 1, 1, 0, true);
                OpenTK.Vector3 railStartPoint = new OpenTK.Vector3((float)railPL.PointAtStart.X, (float)railPL.PointAtStart.Y, (float)railPL.PointAtStart.Z);
                OpenTK.Vector3 railNormal = new OpenTK.Vector3((float)railPL.TangentAtStart.X, (float)railPL.TangentAtStart.Y, (float)railPL.TangentAtStart.Z);

                //need to calculate the center and normal from curve
                //OpenTK.Vector3 shapeCenter = Util.vrToPlatformPoint(ref mScene, mScene.iPointList[0]);
                //OpenTK.Vector3 shapeNormal = new OpenTK.Vector3((float)mScene.iPlaneList[0].Normal.X, (float)mScene.iPlaneList[0].Normal.Y, (float)mScene.iPlaneList[0].Normal.Z);
                OpenTK.Vector3 shapeCenter = new Vector3((float)mScene.iCurveList[mScene.iCurveList.Count - 2].GetBoundingBox(true).Center.X, (float)mScene.iCurveList[mScene.iCurveList.Count - 2].GetBoundingBox(true).Center.Y, (float)mScene.iCurveList[mScene.iCurveList.Count - 2].GetBoundingBox(true).Center.Z);
                Plane curvePlane;
                OpenTK.Vector3 shapeNormal = new Vector3(0, 0, 0);
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

                OpenTK.Matrix4 transM = Util.getCoordinateTransM(shapeCenter, railStartPoint, shapeNormal, railNormal);
                Transform t = Util.OpenTKToRhinoTransform(transM);
                ((NurbsCurve)mScene.iCurveList[mScene.iCurveList.Count - 2]).Transform(t);
                NurbsCurve circleCurve = (NurbsCurve)mScene.iCurveList[mScene.iCurveList.Count - 2];

                //cruves coordinate are in rhino, somehow cap didn't work and need to call CapPlanarHoles
                Brep[] breps = Brep.CreateFromSweep(mScene.iCurveList[mScene.iCurveList.Count - 1], circleCurve, false, mScene.rhinoDoc.ModelAbsoluteTolerance);
                Brep brep = breps[0];
                Brep capBrep = brep.CapPlanarHoles(mScene.rhinoDoc.ModelAbsoluteTolerance);

                //remove the current model
                
                if (renderObjId != Guid.Empty)
                    Util.removeSceneNode(ref mScene, renderObjId);
                if (brep != null)
                {
                    renderObjId = Util.addSceneNode(ref mScene, brep, ref mesh_m, "Sweep");
                }
                
                //renderObjId = Util.addSceneNodeWithoutDraw(ref mScene, brep, ref mesh_m, modelName);

                //TODO-find the end circle cirve and add to iCurveList. add Sweep2 dynamic type and init the new iCurveList
                //TODO-if profile curve is open then find the Brepedge contains the end point
                //Brep CapPlanarHoles method might be useful
                /*
                Point3d railEndP = mScene.iCurveList[mScene.iCurveList.Count - 1].PointAtEnd;
                int faceIndex = -1;
                for (int i = 0; i < capBrep.Faces.Count; i++)
                {
                    //cast BrepFace to Brep for ClosestPoint(P) menthod
                    double dist = capBrep.Faces[i].DuplicateFace(false).ClosestPoint(railEndP).DistanceTo(railEndP);
                    //tolerance mScene.rhinoDoc.ModelAbsoluteTolerance too low
                    if (dist < mScene.rhinoDoc.ModelAbsoluteTolerance)
                    {
                        faceIndex = i;
                        break;
                    }
                }

                Brep endCap = capBrep.Faces[faceIndex].DuplicateFace(false);
                Curve[] CapCurves = Rhino.Geometry.Curve.JoinCurves(endCap.Edges);
                endCapCurve = CapCurves[0].ToNurbsCurve();
                */

                //another soultion. transform the orginal plan to pointAtEnd and use selectionList to check the type
                //call the generateEndCap function when press AX button


                //reverse transfrom the curvelist
                Transform invT;
                if (t.TryGetInverse(out invT))
                    ((NurbsCurve)mScene.iCurveList[mScene.iCurveList.Count - 2]).Transform(invT);

            }else if (renderType == "Sweep-Circle")
            {
                //Count-1: endCurve, Count - 2: rail, Count-3: startCurve
                NurbsCurve rail = (NurbsCurve)mScene.iCurveList[mScene.iCurveList.Count - 2];
                PolylineCurve railPL = rail.ToPolyline(0, 0, 0, 0, 0, 1, 1, 0, true);
                OpenTK.Vector3 railStartPoint = new OpenTK.Vector3((float)railPL.PointAtStart.X, (float)railPL.PointAtStart.Y, (float)railPL.PointAtStart.Z);
                OpenTK.Vector3 railNormal = new OpenTK.Vector3((float)railPL.TangentAtStart.X, (float)railPL.TangentAtStart.Y, (float)railPL.TangentAtStart.Z);

                OpenTK.Vector3 shapeCenter = new Vector3((float)mScene.iCurveList[mScene.iCurveList.Count - 3].GetBoundingBox(true).Center.X, (float)mScene.iCurveList[mScene.iCurveList.Count - 3].GetBoundingBox(true).Center.Y, (float)mScene.iCurveList[mScene.iCurveList.Count - 3].GetBoundingBox(true).Center.Z);
                Plane curvePlane;
                OpenTK.Vector3 shapeNormal = new Vector3(0, 0, 0);
                Double tolerance = 0;
                while (tolerance < 100)
                {
                    if (mScene.iCurveList[mScene.iCurveList.Count - 3].TryGetPlane(out curvePlane, tolerance))
                    {
                        shapeNormal = new OpenTK.Vector3((float)curvePlane.Normal.X, (float)curvePlane.Normal.Y, (float)curvePlane.Normal.Z);
                        break;
                    }
                    tolerance++;
                }

                OpenTK.Matrix4 transM = Util.getCoordinateTransM(shapeCenter, railStartPoint, shapeNormal, railNormal);
                Transform t = Util.OpenTKToRhinoTransform(transM);
                ((NurbsCurve)mScene.iCurveList[mScene.iCurveList.Count - 3]).Transform(t);
                NurbsCurve circleCurve = (NurbsCurve)mScene.iCurveList[mScene.iCurveList.Count - 3];

                //remove the current model
                if (renderObjId != Guid.Empty)
                    Util.removeSceneNode(ref mScene, renderObjId);

                List<Curve> profileCurves = new List<Curve>();
                profileCurves.Add(circleCurve);
                profileCurves.Add(mScene.iCurveList[mScene.iCurveList.Count - 1]);

                Brep[] breps = Brep.CreateFromSweep(mScene.iCurveList[mScene.iCurveList.Count - 2], profileCurves, false, mScene.rhinoDoc.ModelAbsoluteTolerance);
                Brep brep = breps[0];
                if (brep != null)
                {
                    renderObjId = Util.addSceneNode(ref mScene, brep, ref mesh_m, modelName);
                }

                //reverse transfrom the curvelist
                Transform invT;
                if (t.TryGetInverse(out invT))
                    ((NurbsCurve)mScene.iCurveList[mScene.iCurveList.Count - 3]).Transform(invT);
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
            OpenTK.Matrix4 transMEnd = Util.getCoordinateTransM(shapeCenter, railEndPoint, shapeNormal, railEndNormal);

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
                    Guid guid = Util.addSceneNode(ref mScene, designPlane, ref mesh_m, "panel");
                    mScene.iRhObjList.Add(mScene.rhinoDoc.Objects.Find(guid));
                }
            }
            mScene.popInteraction();
            mScene.pushInteraction(new EditPoint2(ref mScene, true, "Sweep-Circle"));
            mScene.peekInteraction().init();

        }

    }
}
