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
    class EditPoint3 : Interaction
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
        private Material.Material profile_m;

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
        int refIndex = 0;
        Point3d startP;

        private bool backgroundStart = false;
        private float displacement = 0;
        Brep dynamicBrep;
        IAsyncResult R;
        public delegate void generateModel_Delegate();
        generateModel_Delegate d = null;
        private string modelName = "tprint";

        private bool isEditCircle = false;
        private bool isEditRect = false;

        Circle circle;
        Rectangle3d rect;
        float radius = 0;
        float width = 0;
        float height = 0;
        float delta = 2;

        float mCurrentRadius;
        float mMinSelectionRadius;
        int selectedSector = 0;


        public EditPoint3(ref Scene scene) : base(ref scene)
        {
            stroke_g2 = new Geometry.GeometryStroke(ref mScene);

        }

        public EditPoint3(ref Scene scene, bool drawOnP) : base(ref scene)
        {
            stroke_g2 = new Geometry.GeometryStroke(ref mScene);

            onPlane = drawOnP;
        }

        public EditPoint3(ref Scene scene, bool drawOnP, string render) : base(ref scene)
        {
            stroke_g2 = new Geometry.GeometryStroke(ref mScene);

            onPlane = drawOnP;

            dynamicRender = render;
            mesh_m = new Material.LambertianMaterial(.7f, .7f, .7f, .3f);
            profile_m = new Material.SingleColorMaterial(0.5f, 0, 0, 0.4f);
            //mesh_m = new Material.RGBNormalMaterial(0.5f);

            if (scene.isOculus)
            {
                mMinSelectionRadius = 0.2f;
            }
            else
            {
                mMinSelectionRadius = 0.4f;
            }
        }

        public override void init()
        {
            isEditCircle = mScene.selectionList[mScene.selectionList.Count - 1] == "Circle" || dynamicRender == "Sweep-Circle" || dynamicRender == "Extrude-Circle";
            isEditRect = mScene.selectionList[mScene.selectionList.Count - 1] == "Rect" || dynamicRender == "Sweep-Rect" || dynamicRender == "Extrude-Rect";

            if (onPlane)
            {
                //add projectionPoint
                drawPoint = Util.MarkProjectionPoint(ref mScene, new OpenTK.Vector3(0, 0, 0), 1, 1, 1);

                rhinoPlane = mScene.iRhObjList.ElementAt(mScene.iRhObjList.Count - 1);

                //testing get curveOnplane here.
                curvePlane = mScene.iPlaneList[mScene.iPlaneList.Count - 1];

            }

            //create editcurve, find the curve plane, render circle, rect
            if (dynamicRender == "Extrude")
            {
                //TODO-PointStart should be the center and PointEnd and should be the vertial line
                /* 
                List<Point3d> extrudeCurveP = new List<Point3d>();
                extrudeCurveP.Add(mScene.iCurveList[mScene.iCurveList.Count - 1].PointAtStart);
                extrudeCurveP.Add(mScene.iCurveList[mScene.iCurveList.Count - 1].PointAtEnd);
                //update the curve
                mScene.iCurveList[mScene.iCurveList.Count - 1] = Rhino.Geometry.NurbsCurve.Create(false, 1, extrudeCurveP.ToArray());
                */
                NurbsCurve railCurve = mScene.iCurveList[mScene.iCurveList.Count - 1].ToNurbsCurve();
                double height = 0;
                if (mScene.iCurveList[mScene.iCurveList.Count - 2].TryGetPlane(out curvePlane))
                {
                    OpenTK.Vector3 heightVector = Util.RhinoToOpenTKPoint(railCurve.PointAtEnd) - Util.RhinoToOpenTKPoint(railCurve.PointAtStart);
                    OpenTK.Vector3 planeNormal = Util.RhinoToOpenTKPoint(curvePlane.Normal);
                    planeNormal.Normalize();
                    height = OpenTK.Vector3.Dot(heightVector, planeNormal) / planeNormal.Length;

                    //update rail curve and using sweepCap
                    List<Point3d> extrudeCurveP = new List<Point3d>();
                    //extrudeCurveP.Add(curvePlane.Origin); //plane origin will be in the corner
                    //try using geting bounding box
                    Point3d startP = Brep.CreatePlanarBreps(mScene.iCurveList[mScene.iCurveList.Count - 2])[0].GetBoundingBox(true).Center;
                    extrudeCurveP.Add(startP);
                    Point3d endP = new Point3d(startP.X + height * planeNormal.X, startP.Y + height * planeNormal.Y, startP.Z + height * planeNormal.Z);
                    extrudeCurveP.Add(endP);
                    //update the edit curve
                    editCurve = Rhino.Geometry.NurbsCurve.Create(false, 1, extrudeCurveP.ToArray());
                    mScene.iCurveList[mScene.iCurveList.Count - 1] = editCurve;
                }
            }
            else if (isEditCircle)
            {
                if (mScene.iCurveList[mScene.iCurveList.Count - 1].TryGetCircle(out circle))
                {
                    List<Point3d> circlePoints = new List<Point3d>();
                    circlePoints.Add(circle.Center);
                    circlePoints.Add(mScene.iCurveList[mScene.iCurveList.Count - 1].PointAtStart);
                    editCurve = Rhino.Geometry.NurbsCurve.Create(false, 1, circlePoints.ToArray());

                    //curvePlane = circle.Plane;
                    radius = (float)circle.Radius;
                }

            }
            else if (isEditRect)
            {
                Rhino.Geometry.Polyline polyline;
                if (mScene.iCurveList[mScene.iCurveList.Count - 1].TryGetPolyline(out polyline))
                {
                    rect = Rectangle3d.CreateFromPolyline(polyline);
                    List<Point3d> rectPoints = new List<Point3d>();
                    rectPoints.Add(rect.Center);
                    rectPoints.Add(rect.Corner(3));
                    editCurve = Rhino.Geometry.NurbsCurve.Create(false, 1, rectPoints.ToArray());
                    //rect.Plane's center is incorrect
                    //curvePlane = rect.Plane;
                    //curvePlane = new Plane(rect.Center, rect.Plane.Normal);

                    width = (float)rect.Width;
                    height = (float)rect.Height;
                }

            }
            else
            {
                //editCurve might not in a planar surface
                editCurve = mScene.iCurveList[mScene.iCurveList.Count - 1].ToNurbsCurve();
                /*
                Plane cPlane = new Plane();
                Double tolerance = 0;
                while (tolerance < 400)
                {
                    if (editCurve.TryGetPlane(out cPlane, tolerance))
                    {
                        curvePlane = cPlane;
                        break;
                    }
                    tolerance++;
                }*/
            }

            //null check
            if (editCurve == null)
            {
                while (!mScene.interactionStackEmpty())
                    mScene.popInteraction();
                return;
            }

            //store the plane when we created a curve
            //rotate the curvePlane to match roatation
            //Transform rotPlatform = Transform.Rotation(mScene.rhinoTheta, new Rhino.Geometry.Vector3d(0, 0, 1), new Point3d(0, 0, 0));
            //curvePlane.Transform(rotPlatform);

            d = new generateModel_Delegate(generateModel);

            renderEditCurve();
            //render model
            R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);

        }


        public override void draw(bool inTop)
        {
            if (currentState == State.End)
                return;

            //Joystick edit
            if (isEditCircle || isEditRect)
            {
                joystickControl();
            }

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
                    //projectP = new OpenTK.Vector3(100, 100, 100); //make it invisable
                    //testng using last frame position
                    projectP = projectP;
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
                    //not allow to edit the center in Circle or topleft in Rect
                    if (i == 0)
                    {
                        if (isEditCircle || isEditRect)
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

                    if (isEditCircle || isEditRect)
                    {
                        //render only 1 edit point
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

                //update the curve- we can't update editCurve.Points[snapIndex] directly (bug)
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
                        if (mScene.selectionList[mScene.selectionList.Count - 1] == "Circle" || dynamicRender == "Sweep-Circle" || dynamicRender == "Extrude-Circle")
                        {
                            continue;
                        }
                        else if (mScene.selectionList[mScene.selectionList.Count - 1] == "Rect" || dynamicRender == "Sweep-Rect" || dynamicRender == "Extrude-Rect")
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
                        if (mScene.selectionList[mScene.selectionList.Count - 1] == "Circle" || dynamicRender == "Sweep-Circle" || dynamicRender == "Extrude-Circle")
                        {
                            continue;
                        }
                        else if (mScene.selectionList[mScene.selectionList.Count - 1] == "Rect" || dynamicRender == "Sweep-Rect" || dynamicRender == "Extrude-Rect")
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

        private void updateEditCurve()
        {
            //create and render interaction curve and editcurve
            if (dynamicRender == "Extrude")
            {
                /*
                List<Point3d> extrudeCurveP = new List<Point3d>();
                extrudeCurveP.Add(editCurve.PointAtStart);
                extrudeCurveP.Add(editCurve.PointAtEnd);
                //update the curve
                mScene.iCurveList[mScene.iCurveList.Count - 1] = Rhino.Geometry.NurbsCurve.Create(false, 1, extrudeCurveP.ToArray());*/

                if (curvePlane != null)
                {
                    OpenTK.Vector3 heightVector = Util.RhinoToOpenTKPoint(editCurve.PointAtEnd) - Util.RhinoToOpenTKPoint(editCurve.PointAtStart);
                    OpenTK.Vector3 planeNormal = Util.RhinoToOpenTKPoint(curvePlane.Normal);
                    planeNormal.Normalize();
                    height = OpenTK.Vector3.Dot(heightVector, planeNormal) / planeNormal.Length;

                    //update rail curve and using sweepCap
                    List<Point3d> extrudeCurveP = new List<Point3d>();
                    Point3d startP = editCurve.PointAtStart;
                    extrudeCurveP.Add(startP);
                    Point3d endP = new Point3d(startP.X + height * planeNormal.X, startP.Y + height * planeNormal.Y, startP.Z + height * planeNormal.Z);
                    extrudeCurveP.Add(endP);
                    //update the edit curve
                    editCurve = Rhino.Geometry.NurbsCurve.Create(false, 1, extrudeCurveP.ToArray()); //need to edit editcurve as well for extrude
                    mScene.iCurveList[mScene.iCurveList.Count - 1] = editCurve;
                }

            }
            else if (isEditCircle)
            {
                Point3d circleP = editCurve.PointAtEnd;
                Point3d origin = editCurve.PointAtStart;
                radius = (float)Math.Sqrt(Math.Pow(circleP.X - origin.X, 2) + Math.Pow(circleP.Y - origin.Y, 2) + Math.Pow(circleP.Z - origin.Z, 2));

                if (curvePlane != null)
                {
                    circle = new Rhino.Geometry.Circle(curvePlane, origin, radius);
                    circleCurve = circle.ToNurbsCurve();
                    Brep[] shapes = Brep.CreatePlanarBreps(circleCurve);
                    Brep circle_s = shapes[0];
                    //don't need to update the RhinoView
                    renderObjId = Util.addSceneNodeWithoutDraw(ref mScene, circle_s, ref profile_m, "Circle");

                    //updating the iPointList and iCurveList
                    mScene.iCurveList[mScene.iCurveList.Count - 1] = circleCurve;
                    mScene.iPointList[mScene.iPointList.Count - 2] = Util.platformToVRPoint(ref mScene, new Vector3((float)origin.X, (float)origin.Y, (float)origin.Z));
                    mScene.iPointList[mScene.iPointList.Count - 1] = Util.platformToVRPoint(ref mScene, new Vector3((float)circleP.X, (float)circleP.Y, (float)circleP.Z));

                }
            }
            else if (isEditRect)
            {

                Point3d rectCenter = editCurve.PointAtStart;
                Point3d bottomRightP = editCurve.PointAtEnd;
                if (curvePlane != null)
                {
                    Vector3 rectDiagonalV = new Vector3((float)(rectCenter.X - bottomRightP.X), (float)(rectCenter.Y - bottomRightP.Y), (float)(rectCenter.Z - bottomRightP.Z));
                    float lenDiagonal = rectDiagonalV.Length;
                    Vector3 rectLeftTop = new Vector3((float)rectCenter.X, (float)rectCenter.Y, (float)rectCenter.Z) + lenDiagonal * rectDiagonalV.Normalized();
                    Point3d topLeftP = new Point3d(rectLeftTop.X, rectLeftTop.Y, rectLeftTop.Z);

                    rect = new Rectangle3d(curvePlane, topLeftP, bottomRightP);
                    rectCurve = rect.ToNurbsCurve();
                    Brep[] shapes = Brep.CreatePlanarBreps(rectCurve);
                    Brep rectBrep = shapes[0];

                    //don't need to update the RhinoView
                    renderObjId = Util.addSceneNodeWithoutDraw(ref mScene, rectBrep, ref profile_m, "Rect");

                    //updating the iPointList and iCurveList
                    mScene.iCurveList[mScene.iCurveList.Count - 1] = rectCurve;
                    mScene.iPointList[mScene.iPointList.Count - 2] = Util.platformToVRPoint(ref mScene, new Vector3((float)rectCenter.X, (float)rectCenter.Y, (float)rectCenter.Z));
                    mScene.iPointList[mScene.iPointList.Count - 1] = Util.platformToVRPoint(ref mScene, new Vector3((float)bottomRightP.X, (float)bottomRightP.Y, (float)bottomRightP.Z));

                    width = (float)rect.Width;
                    height = (float)rect.Height;


                }
            }
            else
            {
                mScene.iCurveList[mScene.iCurveList.Count - 1] = editCurve;
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
            }
            else if (dynamicRender == "Sweep-Rect")
            {
                dynamicBrep = Util.SweepCapFun(ref mScene, ref mScene.iCurveList);
            }
            else if (dynamicRender == "Extrude-Circle")
            {
                //dynamicBrep = Util.ExtrudeCapFunc(ref mScene, ref mScene.iCurveList);
                dynamicBrep = Util.SweepCapFun(ref mScene, ref mScene.iCurveList);
            }
            else if (dynamicRender == "Extrude-Rect")
            {
                //dynamicBrep = Util.ExtrudeCapFunc(ref mScene, ref mScene.iCurveList);
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
                    if (dynamicRender == "Sweep-Circle" || dynamicRender == "Sweep-Rect" || dynamicRender == "Extrude-Circle" || dynamicRender == "Loft")
                    {
                        Rhino.RhinoApp.WriteLine("Dot: " + mScene.angleD);
                        Rhino.RhinoApp.WriteLine("c1 dir: " + mScene.c1D);
                        Rhino.RhinoApp.WriteLine("c2 dir: " + mScene.c2D);
                        Rhino.RhinoApp.WriteLine("c2_n dir: " + mScene.c3D);

                        //TODO-can't draw the point here !!!! WHY?

                    }

                }
                else if (modelName.Contains("aprint"))
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
                    if (dynamicRender == "Revolve" || dynamicRender == "Loft" || dynamicRender == "Sweep-Circle" || dynamicRender == "Sweep-Rect" || dynamicRender == "Extrude-Circle" || dynamicRender == "Extrude-Rect")
                    {
                        if (sn.name.Contains("tprint") || sn.name == "EditCurve" || sn.name == "drawPoint" || sn.name == "EditPoint" || sn.name.Contains("panel") || sn.name.Contains("Circle") || sn.name.Contains("Rect") || sn.name.Contains("railPlane") || sn.name.Contains("patchSurface"))
                        {
                            //We don't delete the patch surface for later use
                            if (sn.name.Contains("railPlane"))
                            {
                                //panel didn't have the sceneNode in VR.
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
                        if (dynamicRender == "Sweep" || dynamicRender == "Extrude")
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
                    //mScene.popInteraction();
                    mScene.pushInteraction(new EditPoint3(ref mScene, true, "Extrude-Circle"));
                    mScene.peekInteraction().init();
                }
                else if (mScene.selectionList[1] == "Rect")
                {
                    generateEndCap();
                    //mScene.popInteraction();
                    mScene.pushInteraction(new EditPoint3(ref mScene, true, "Extrude-Rect"));
                    mScene.peekInteraction().init();
                }
            }
            else if (dynamicRender == "Sweep") //TODO-implement edit endCurve
            {
                clearDrawing();
                if (mScene.selectionList[1] == "Circle")
                {
                    generateEndCap();
                    //mScene.popInteraction();
                    mScene.pushInteraction(new EditPoint3(ref mScene, true, "Sweep-Circle"));
                    mScene.peekInteraction().init();
                }
                else if (mScene.selectionList[1] == "Rect")
                {
                    generateEndCap();
                    //mScene.popInteraction();
                    mScene.pushInteraction(new EditPoint3(ref mScene, true, "Sweep-Rect"));
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
            else if (dynamicRender == "Extrude-Circle")
            {
                modelName = "aprint";
                R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);

            }
            else if (dynamicRender == "Extrude-Rect")
            {
                modelName = "aprintcap";
                R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);

            }
            else
            {
                clearDrawing();
            }

            currentState = State.End;

        }

        private void generateEndCap()
        {

            //TODO: using dynamicBrep cap to find endPlane, but endCurve still using the same way or using duplicate surface boarder.

            NurbsCurve rail = mScene.iCurveList[mScene.iCurveList.Count - 1].ToNurbsCurve();

            //general way to get the center of closed curve
            OpenTK.Vector3 shapeCenter = Util.RhinoToOpenTKPoint(mScene.iCurveList[mScene.iCurveList.Count - 2].GetBoundingBox(true).Center);
            OpenTK.Vector3 shapeP = Util.RhinoToOpenTKPoint(mScene.iCurveList[mScene.iCurveList.Count - 2].PointAtStart);
            OpenTK.Vector3 shapeNormal = new Vector3(0, 0, 0);

            //get curvePlane. TODO-can store first whenever we add a curve
            Plane curvePlane = new Plane();
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

            OpenTK.Vector3 railStartPoint = Util.RhinoToOpenTKPoint(rail.PointAtStart);
            OpenTK.Vector3 railStartNormal = Util.RhinoToOpenTKPoint(rail.TangentAtStart);
            OpenTK.Vector3 railEndPoint = Util.RhinoToOpenTKPoint(rail.PointAtEnd);
            OpenTK.Vector3 railEndNormal = Util.RhinoToOpenTKPoint(rail.TangentAtEnd);

            Plane endPlane = new Plane(rail.PointAtEnd, rail.TangentAtEnd);
            //compute the transform from profile curve to railstart and railend
            OpenTK.Matrix4 transMStart = new Matrix4();
            transMStart = Util.getCoordinateTransM(shapeCenter, railStartPoint, shapeNormal, railStartNormal);
            Transform tStart = Util.OpenTKToRhinoTransform(transMStart);

            OpenTK.Matrix4 transMEnd = new Matrix4();
            transMEnd = Util.getCoordinateTransM(shapeCenter, railEndPoint, shapeNormal, railEndNormal);
            Transform tEnd = Util.OpenTKToRhinoTransform(transMEnd);

            if (mScene.selectionList[1] == "Circle")
            {
                //Method 1--Add iPointList for endCurve. Careful it's in VR space
                //OpenTK.Vector3 newCenter = Util.transformPoint(transMEnd, shapeCenter);
                //OpenTK.Vector3 newCircleP = Util.transformPoint(transMEnd, shapeP);
                //mScene.iPointList.Add(Util.platformToVRPoint(ref mScene, newCenter));
                //mScene.iPointList.Add(Util.platformToVRPoint(ref mScene, newCircleP));

                //Method 2--Add endCurve
                /*
                NurbsCurve endCurve = mScene.iCurveList[mScene.iCurveList.Count - 2].DuplicateCurve().ToNurbsCurve();
                endCurve.Transform(tEnd);
                mScene.iCurveList.Add(endCurve);
                */

                //Method 3--create new circle and add curve

                Circle circle;
                if (mScene.iCurveList[mScene.iCurveList.Count - 2].TryGetCircle(out circle, mScene.rhinoDoc.ModelAbsoluteTolerance * 2.1))
                {
                    Circle endCircle = new Circle(endPlane, circle.Radius);
                    mScene.iCurveList.Add(endCircle.ToNurbsCurve());
                    mScene.iPlaneList.Add(endPlane);
                    mScene.iPointList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(endCircle.Center)));
                    mScene.iPointList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(endCircle.PointAt(0))));
                }

                Brep[] shapes = Brep.CreatePlanarBreps(mScene.iCurveList[mScene.iCurveList.Count - 1]);
                //don't need to update the RhinoView
                renderObjId = Util.addSceneNodeWithoutDraw(ref mScene, shapes[0], ref profile_m, "Circle");

            }
            else if (mScene.selectionList[1] == "Rect")
            {
                //TODO--Debugging for different methods
                //bug1 - transM will cause unintended rotation around normal axis

                //Method 1--Add iPointList for endCurve. Careful it's in VR space
                /*
                Vector3 rectCenterRhino = Util.vrToPlatformPoint(ref mScene, mScene.iPointList[mScene.iPointList.Count - 2]);
                Vector3 rectBottomRightRhino = Util.vrToPlatformPoint(ref mScene, mScene.iPointList[mScene.iPointList.Count - 1]);
                OpenTK.Vector3 newCenterRhino = Util.transformPoint(transMEnd, rectCenterRhino);
                OpenTK.Vector3 newBottomRightRhino = Util.transformPoint(transMEnd, rectBottomRightRhino);
                */

                //Method 2--Add endCurve
                NurbsCurve endCurve = mScene.iCurveList[mScene.iCurveList.Count - 2].DuplicateCurve().ToNurbsCurve();
                endCurve.Transform(tEnd);
                mScene.iCurveList.Add(endCurve);
                mScene.iPlaneList.Add(endPlane);

                Rhino.Geometry.Polyline polyline;
                if (endCurve.TryGetPolyline(out polyline))
                {
                    Rectangle3d endRect = Rectangle3d.CreateFromPolyline(polyline);
                    mScene.iPointList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(endRect.Center)));
                    mScene.iPointList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(endRect.Corner(3))));
                }

                //Method 3--create new rect and add curve
                /*
                Rhino.Geometry.Polyline polyline;
                if (mScene.iCurveList[mScene.iCurveList.Count - 2].TryGetPolyline(out polyline))
                {
                    Rectangle3d startRect = Rectangle3d.CreateFromPolyline(polyline);
                    Rectangle3d endRect = new Rectangle3d(endPlane, new Interval(-startRect.Width / 2, startRect.Width / 2), new Interval(-startRect.Height / 2, startRect.Height / 2));
                    mScene.iCurveList.Add(endRect.ToNurbsCurve());
                }*/

                //method4- create new rect and add iPointList 


                Brep[] shapes = Brep.CreatePlanarBreps(mScene.iCurveList[mScene.iCurveList.Count - 1]);
                //don't need to update the RhinoView
                renderObjId = Util.addSceneNodeWithoutDraw(ref mScene, shapes[0], ref profile_m, "Rect");

            }
            else
            {
                //Can only use method 2--Add endCurve
                NurbsCurve endCurve = mScene.iCurveList[mScene.iCurveList.Count - 2].DuplicateCurve().ToNurbsCurve();
                endCurve.Transform(tEnd);
                mScene.iCurveList.Add(endCurve);

                if (mScene.iCurveList[mScene.iCurveList.Count - 1].IsClosed)
                {
                    Brep[] shapes = Brep.CreatePlanarBreps(mScene.iCurveList[mScene.iCurveList.Count - 1]);
                    //don't need to update the RhinoView
                    renderObjId = Util.addSceneNodeWithoutDraw(ref mScene, shapes[0], ref profile_m, "ClosedCurve");
                    mScene.iPlaneList.Add(endPlane);
                }

                if (mScene.iCurveList[mScene.iCurveList.Count - 1].IsPlanar())
                {
                    mScene.iPlaneList.Add(endPlane);
                }
                else
                {
                    Plane plane = new Plane(new Point3d(-100, -100, -100), new Rhino.Geometry.Vector3d(0, 0, 0));
                    mScene.iPlaneList.Add(plane);
                }

            }

            //update the profile curve1 to the railStart
            mScene.iCurveList[mScene.iCurveList.Count - 3].Transform(tStart);

            //create endPlane Rhino Object                
            PlaneSurface plane_surface = new PlaneSurface(endPlane, new Interval(-120, 120), new Interval(-120, 120));
            Brep designPlane = Brep.CreateFromSurface(plane_surface);

            if (designPlane != null)
            {
                Guid guid = Util.addSceneNodeWithoutVR(ref mScene, designPlane, ref mesh_m, "panel");
                mScene.iRhObjList.Add(mScene.rhinoDoc.Objects.Find(guid));
            }
        }

        private void joystickControl()
        {
            // get R and Theta and the associated sector
            float theta = 0;
            if (mScene.isOculus)
            {
                getOculusJoystickPoint((uint)primaryControllerIdx, out mCurrentRadius, out theta);
            }
            else
            {
                getViveTouchpadPoint((uint)primaryControllerIdx, out mCurrentRadius, out theta);
            }


            if (theta < 0) { theta += (float)(2 * Math.PI); }

            // If in midlle selection ring, check delay
            if (mCurrentRadius > mMinSelectionRadius)
            {

                theta = (float)((theta / Math.PI) * 180);
                if (theta > 0 && (theta < 45 || theta > 315))
                {
                    selectedSector = 1;
                }
                else if (theta > 45 && theta < 135)
                {
                    selectedSector = 2;
                }
                else if (theta > 135 && theta < 225)
                {
                    selectedSector = 3;
                }
                else if (theta > 225 && theta < 315)
                {
                    selectedSector = 4;
                }
                else
                {
                    selectedSector = 0;
                }

                editCircleRect(selectedSector);

            }
            else // //trigger change size. discrete changing
            {
                //trigger change size. discrete changing
                /*
                if (selectedSector != 0)
                {
                    editCircleRect(selectedSector);
                    selectedSector = 0;
                }*/
            }
        }

        private void editCircleRect(int sector)
        {
            //Rhino.RhinoApp.WriteLine("sector:" + sector);
            if (isEditCircle)
            {
                if (sector == 1)
                {
                    radius += delta;
                }
                else if (sector == 3)
                {
                    radius -= delta;
                    if (radius <= 0)
                    {
                        radius = 1;
                    }
                }

                //using the original x-axis and y-axis align, but adjust the center of the plane
                Plane newPlane = new Plane(circle.Center, curvePlane.Normal);
                newPlane.XAxis = curvePlane.XAxis;
                newPlane.YAxis = curvePlane.YAxis;
                Circle newCircle = new Circle(newPlane, radius);
                List<Point3d> circlePoints = new List<Point3d>();
                circlePoints.Add(newCircle.Center);
                circlePoints.Add(newCircle.ToNurbsCurve().PointAtStart);
                editCurve = Rhino.Geometry.NurbsCurve.Create(false, 1, circlePoints.ToArray());
                renderEditCurve();
                updateEditCurve();

                R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);

            }
            else if (isEditRect)
            {
                if (sector == 1)
                {
                    width += delta;
                }
                else if (sector == 3)
                {
                    width -= delta;
                    if (width <= 0)
                    {
                        width = 1;
                    }
                }
                else if (sector == 2)
                {
                    height += delta;
                }
                else if (sector == 4)
                {
                    height -= delta;
                    if (height <= 0)
                    {
                        height = 1;
                    }
                }

                //Rectangle3d newRect = new Rectangle3d(curvePlane, width, height);
                //fix-origin change so we can't use the curvePlane directly
                Plane newPlane = new Plane(rect.Center, curvePlane.Normal);
                newPlane.XAxis = curvePlane.XAxis;
                newPlane.YAxis = curvePlane.YAxis;
                Rectangle3d newRect = new Rectangle3d(newPlane, new Interval(-width / 2, width / 2), new Interval(-height / 2, height / 2));
                List<Point3d> rectPoints = new List<Point3d>();
                rectPoints.Add(newRect.Center);
                rectPoints.Add(newRect.Corner(3));
                editCurve = Rhino.Geometry.NurbsCurve.Create(false, 1, rectPoints.ToArray());
                renderEditCurve();
                updateEditCurve();

                R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);

            }
        }

        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {

            //testing projection
            if ((mScene.selectionList[1] == "Rect") && (dynamicRender == "Sweep-Rect" || dynamicRender == "Extrude-Rect"))
            {
                refIndex++;
                if (refIndex >= 4)
                {
                    refIndex = 0;
                }
                Rhino.Geometry.Polyline polylineEnd;
                if (mScene.iCurveList[mScene.iCurveList.Count - 1].TryGetPolyline(out polylineEnd))
                {
                    Rectangle3d endRect = Rectangle3d.CreateFromPolyline(polylineEnd);
                    Point3d referencePoint = endRect.Corner(refIndex);
                    double curveT = 0;
                    mScene.iCurveList[mScene.iCurveList.Count - 1].ClosestPoint(referencePoint, out curveT);
                    mScene.iCurveList[mScene.iCurveList.Count - 1].ChangeClosedCurveSeam(curveT);
                    R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);
                }

            }
            else if ((mScene.selectionList[1] == "Circle") && (dynamicRender == "Sweep-Circle" || dynamicRender == "Extrude-Circle") || dynamicRender == "Loft")
            {

                OpenTK.Vector3 p1 = Util.transformPoint(mScene.tableGeometry.transform.Inverted(), Util.platformToVRPoint(ref mScene, new Vector3((float)mScene.eStartP.X, (float)mScene.eStartP.Y, (float)mScene.eStartP.Z)));
                OpenTK.Vector3 p2 = Util.transformPoint(mScene.tableGeometry.transform.Inverted(), Util.platformToVRPoint(ref mScene, new Vector3((float)mScene.sStartP.X, (float)mScene.sStartP.Y, (float)mScene.sStartP.Z)));
                Util.MarkDebugPoint(ref mScene.tableGeometry, p1, 0f, 0f, 1f);
                Util.MarkDebugPoint(ref mScene.tableGeometry, p2, 0f, 0f, 1f);
            }
        }

    }
}
