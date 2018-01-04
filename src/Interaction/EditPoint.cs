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
using static SparrowHawk.Scene;

namespace SparrowHawk.Interaction
{
    class EditPoint : Interaction
    {
        public enum State
        {
            Start = 0, Snap = 1, End = 2
        };

        protected State currentState;
        public bool onPlane = false;
        public string dynamicRender = "none";
        private SceneNode renderObjSN;
        private SceneNode shapeSN;
        protected int primaryDeviceIndex;
        protected Guid targetPRhObjID;
        private int snapIndex = -1;
        private bool isSnap = false;
        protected ObjRef rhinoPlaneRef;
        SceneNode drawPoint;
        Point3d projectP;
        private PolylineCurve polyline;
        private List<Curve> curveList = new List<Curve>();
        List<Point3d> curvePoints = new List<Point3d>();
        Geometry.Geometry stroke_g2;
        Material.Material stroke_m2 = new Material.SingleColorMaterial(0, 0, 1, 1);
        SceneNode stroke;
        private Material.Material mesh_m;
        private Material.Material profile_m;

        List<SceneNode> pointMarkers = new List<SceneNode>();

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
        float delta = 0.6f;

        float mCurrentRadius;
        float mMinSelectionRadius;
        int selectedSector = 0;

        ShapeType shapeType = ShapeType.None;
        DrawnType drawnType = DrawnType.None;

        private List<ObjRef> rayCastingObjs;
        private List<Point3d> snapPointsList = new List<Point3d>();

        public EditPoint(ref Scene scene) : base(ref scene)
        {
            stroke_g2 = new Geometry.GeometryStroke(ref mScene);

        }

        public EditPoint(ref Scene scene, CurveID curveID) : base(ref scene)
        {
            stroke_g2 = new Geometry.GeometryStroke(ref mScene);
            FunctionType modelFun = (FunctionType)mScene.selectionDic[SelectionKey.ModelFun];

            //TODO: figure out a better way 
            if (curveID == CurveID.ProfileCurve1)
            {
                shapeType = (ShapeType)mScene.selectionDic[SelectionKey.Profile1Shape];
                drawnType = (DrawnType)mScene.selectionDic[SelectionKey.Profile1On];

                //Revolve only needs 1 profilecurve in our case
                if (modelFun == FunctionType.Revolve)
                    dynamicRender = "Revolve";
            }
            else if (curveID == CurveID.ProfileCurve2)
            {
                shapeType = (ShapeType)mScene.selectionDic[SelectionKey.Profile2Shape];
                drawnType = (DrawnType)mScene.selectionDic[SelectionKey.Profile2On];

                //need to visualize the model

                switch (modelFun)
                {
                    case FunctionType.Extrude:
                        dynamicRender = "Extrude";
                        break;
                    case FunctionType.Loft:
                        dynamicRender = "Loft";
                        break;
                    case FunctionType.Sweep:
                        dynamicRender = "Sweep";
                        break;
                }

            }
            else if (curveID == CurveID.EndCapCurve)
            {
                shapeType = (ShapeType)mScene.selectionDic[SelectionKey.Profile1Shape];
                drawnType = DrawnType.Reference; //the plane where the end cap curve on 

                //generate end cap if we use sweep or extrude
                if (modelFun == FunctionType.Extrude)
                {


                    if (shapeType == ShapeType.Circle)
                    {
                        dynamicRender = "Extrude-Circle";
                    }
                    else if (shapeType == ShapeType.Rect)
                    {
                        dynamicRender = "Extrude-Rect";
                    }

                }
                else if (modelFun == FunctionType.Sweep)
                {

                    if (shapeType == ShapeType.Circle)
                    {
                        dynamicRender = "Sweep-Circle";
                    }
                    else if (shapeType == ShapeType.Rect)
                    {
                        dynamicRender = "Sweep-Rect";
                    }
                }
            }

            //mesh_m = new Material.LambertianMaterial(.7f, .7f, .7f, .3f);
            //profile_m = new Material.SingleColorMaterial(0.5f, 0, 0, 0.4f);
            profile_m = new Material.LambertianMaterial(.7f, .7f, .7f, .3f);
            mesh_m = new Material.RGBNormalMaterial(0.5f);

            if (scene.isOculus)
            {
                mMinSelectionRadius = 0.2f;
            }
            else
            {
                mMinSelectionRadius = 0.4f;
            }
        }

        public void resetVariable()
        {
            currentState = State.Start;
            targetPRhObjID = Guid.Empty;
            snapIndex = -1;
            isSnap = false;
            rhinoPlaneRef = null;
            drawPoint = null;
            projectP = new Point3d();
            polyline = null;
            curveList = new List<Curve>();
            curvePoints = new List<Point3d>();
            //stroke = null;
            stroke_g2 = new Geometry.GeometryStroke(ref mScene);
            pointMarkers = new List<SceneNode>();
            mimD = 1000000f;
            editCurve = circleCurve = rectCurve = null;
            curvePlane = new Plane();
            //Sweep debug
            profileCurves = new List<Curve>();
            angle = 0;
            curveT = 0;
            refIndex = 0;
            startP = new Point3d();

            backgroundStart = false;
            displacement = 0;
            dynamicBrep = null;
            d = null;
            modelName = "tprint";

            isEditCircle = false;
            isEditRect = false;

            circle = new Circle();
            rect = new Rectangle3d();
            radius = 0;
            width = 0;
            height = 0;
            delta = 0.6f;

            mCurrentRadius = 0;
            mMinSelectionRadius = 0;
            selectedSector = 0;

            onPlane = false;

            rayCastingObjs = new List<ObjRef>();
            snapPointsList = new List<Point3d>();
            shapeSN = null;
            renderObjSN = null;
        }

        public override void leaveTop()
        {
            //clean editcurve since we don't need it in xxxx-rect or xxxx-sweep
            if (dynamicRender == "Extrude" || dynamicRender == "Sweep")
            {
                clearDrawing(false);
            }
            else
            {
                clearDrawing(true);
            }

        }

        public override void deactivate()
        {
            clearDrawing(false);

            //deal with when end curve edit pop
            if (dynamicRender == "Extrude-Circle" || dynamicRender == "Extrude-Rect" || dynamicRender == "Sweep-Circle" || dynamicRender == "Sweep-Rect")
            {
                //remove the end cap curve and it's plane
                mScene.iCurveList.RemoveAt(mScene.iCurveList.Count - 1);
                if (mScene.iCurveList[mScene.iCurveList.Count - 1].GetUserString(CurveData.CurveOnObj.ToString()) != null)
                {
                    Guid curveOnObjId = new Guid(mScene.iCurveList[mScene.iCurveList.Count - 1].GetUserString(CurveData.CurveOnObj.ToString()));
                    ObjRef curveOnObjRef = new ObjRef(curveOnObjId);
                    if (curveOnObjRef.Object().Attributes.Name.Contains("Panel"))
                    {
                        Util.removeRhinoObject(ref mScene, curveOnObjRef.ObjectId);
                    }

                }
            }

        }

        public override void init()
        {
            resetVariable();

            if (stroke != null)
            {
                clearDrawing(false); //clear previous stroke
                stroke = null;
            }

            //mScene.selectionList[mScene.selectionList.Count - 1] how to detect 2nd profile?
            isEditCircle = (shapeType == ShapeType.Circle) ? true : false;
            isEditRect = (shapeType == ShapeType.Rect) ? true : false;
            if (drawnType == DrawnType.Plane || drawnType == DrawnType.Surface || drawnType == DrawnType.Reference)
                onPlane = true;

            if (onPlane)
            {
                //init rayCastingObjs
                Guid rhinoPlaneID = new Guid(mScene.iCurveList[mScene.iCurveList.Count - 1].GetUserString(CurveData.CurveOnObj.ToString()));
                rhinoPlaneRef = new ObjRef(rhinoPlaneID);
                //testing get curveOnplane here.
                curvePlane = new Plane(Util.getPointfromString(mScene.iCurveList[mScene.iCurveList.Count - 1].GetUserString(CurveData.PlaneOrigin.ToString())),
                    Util.getVectorfromString(mScene.iCurveList[mScene.iCurveList.Count - 1].GetUserString(CurveData.PlaneNormal.ToString())));
                rayCastingObjs.Add(rhinoPlaneRef);

                Geometry.Geometry geo = new Geometry.DrawPointMarker(new OpenTK.Vector3(0, 0, 0));
                Material.Material m = new Material.SingleColorMaterial(1, 1, 1, 0);//TODO: teseting alpha working or not
                drawPoint = new SceneNode("drawPoint", ref geo, ref m);
                Util.addSceneNode(ref mScene, ref drawPoint);

            }

            //need curvePlane.normal data
            if(drawnType == DrawnType.In3D && dynamicRender == "Extrude")
            {
                mScene.iCurveList[mScene.iCurveList.Count - 2].TryGetPlane(out curvePlane);
            }
                
            if (isEditCircle)
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
                else
                {
                    //if we project to a patch surface, the curve won't match a circle
                    editCurve = mScene.iCurveList[mScene.iCurveList.Count - 1].ToNurbsCurve();
                    isEditCircle = false;
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
                else
                {
                    //if we project to a patch surface, the curve won't match a circle
                    editCurve = mScene.iCurveList[mScene.iCurveList.Count - 1].ToNurbsCurve();
                    isEditRect = false;
                }

            }
            else
            {
                editCurve = mScene.iCurveList[mScene.iCurveList.Count - 1].ToNurbsCurve();
            }

            d = new generateModel_Delegate(generateModel);

            renderEditCurve(); //render curve
            updateEditCurve(); //render shape
            //render model
            R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);

        }


        public override void draw(bool isTop)
        {
            if (!isTop || currentState == State.End)
            {
                return;
            }

            //Joystick edit
            if (isEditCircle || isEditRect)
            {
                joystickControl();
            }

            OpenTK.Vector4 controller_p = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, 0, 1);
            OpenTK.Vector4 controller_pZ = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, -1, 1);
            Point3d controller_pRhino = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)));
            Point3d controller_pZRhin = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_pZ.X, controller_pZ.Y, controller_pZ.Z)));
            Rhino.Geometry.Vector3d direction = new Rhino.Geometry.Vector3d(controller_pZRhin.X - controller_pRhino.X, controller_pZRhin.Y - controller_pRhino.Y, controller_pZRhin.Z - controller_pRhino.Z);

            if (drawnType != DrawnType.In3D)
            {
                Util.rayCasting(controller_pRhino, direction, ref rayCastingObjs, out projectP, out targetPRhObjID);
            }
            else
            {
                projectP = controller_pRhino;
            }

            //generate snap points and check snap
            if (currentState == State.Start)
            {
                snapIndex = -1;
                if (isEditCircle || isEditRect || dynamicRender == "Sweep" || dynamicRender == "Extrude")
                {
                    List<int> ignoreIndexes = new List<int>();
                    ignoreIndexes.Add(0);
                    snapIndex = Util.snapToPoints(ref projectP, ref curvePoints, ignoreIndexes);
                }else
                {
                    snapIndex =Util.snapToPoints(ref projectP, ref curvePoints);
                }

                if (snapIndex != -1)
                {
                    isSnap = true;
                    //highlight the snap edit point
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
                }else
                {
                    isSnap = false;
                    //set to default color
                    foreach (SceneNode sn in pointMarkers)
                    {
                        sn.material = new Material.SingleColorMaterial(0, 1, 0, 1);
                    }
                }
            }

            //render project point
            Vector3 projectPVR = Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(projectP));
            if (drawnType != DrawnType.In3D)
            {
                OpenTK.Matrix4 t = OpenTK.Matrix4.CreateTranslation(projectPVR);
                t.Transpose();
                drawPoint.transform = t;
                if (targetPRhObjID != Guid.Empty)
                {
                    drawPoint.material = new Material.SingleColorMaterial(1, 1, 1, 1);
                }
                else
                {
                    drawPoint.material = new Material.SingleColorMaterial(1, 1, 1, 0);
                }
            }

            //snap state - edit curve
            if (currentState == State.Snap)
            {
                //update the curve- we can't update editCurve.Points[snapIndex] directly (bug)
                OpenTK.Vector3 ep = Util.RhinoToOpenTKPoint(projectP);
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

                renderEditCurve();
                updateEditCurve();

                //dynamic render model
                if (backgroundStart == false && displacement > 10)
                {
                    backgroundStart = true;
                    R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);
                }

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
                    Geometry.Geometry geo = new Geometry.DrawPointMarker(new Vector3(0,0,0));
                    Material.Material m = new Material.SingleColorMaterial(0, 1, 0, 1);
                    SceneNode epSN;
                    Util.MarkPointVR(ref mScene, Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)ep.X, (float)ep.Y, (float)ep.Z)), ref geo, ref m, out epSN);
                    pointMarkers.Add(epSN);
                }

            }
            else
            {
                //remove and visualize the new control points--not efficient bur easy to implement
                for (int i = 0; i < pointMarkers.Count; i++)
                {
                    SceneNode epSN = pointMarkers[i];
                    Util.removeSceneNode(ref mScene, ref epSN);
                }
                pointMarkers.Clear();

                for (int i = 0; i < editCurve.Points.Count; i++)
                {
                    Point3d ep = new Point3d(editCurve.Points.ElementAt(i).Location.X, editCurve.Points.ElementAt(i).Location.Y, editCurve.Points.ElementAt(i).Location.Z);
                    Geometry.Geometry geo = new Geometry.DrawPointMarker(Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)ep.X, (float)ep.Y, (float)ep.Z)));
                    Material.Material m = new Material.SingleColorMaterial(0, 1, 0, 1);
                    SceneNode epSN;
                    Util.MarkPointVR(ref mScene, Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)ep.X, (float)ep.Y, (float)ep.Z)), ref geo, ref m, out epSN);
                    pointMarkers.Add(epSN);
                }
            }

            //Render curve
            polyline = editCurve.ToPolyline(0, 0, 0, 0, 0, 1, 1, 0, true);
            if (stroke == null)
            {
                for (int i = 0; i < polyline.PointCount; i++)
                {
                    ((Geometry.GeometryStroke)stroke_g2).addPoint(Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)polyline.Point(i).X, (float)polyline.Point(i).Y, (float)polyline.Point(i).Z)));

                }
                stroke = new SceneNode("EditCurve", ref stroke_g2, ref stroke_m2);
                Util.addSceneNode(ref mScene, ref stroke);
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
                if (curvePlane != null)
                {
                    OpenTK.Vector3 heightVector = Util.RhinoToOpenTKPoint(editCurve.PointAtEnd) - Util.RhinoToOpenTKPoint(editCurve.PointAtStart);
                    OpenTK.Vector3 planeNormal = Util.RhinoToOpenTKVector(curvePlane.Normal);
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

                    Util.updateSceneNode(ref mScene, circle_s, ref mesh_m, "Circle", out shapeSN);

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

                    Util.updateSceneNode(ref mScene, rectBrep, ref mesh_m, "Rect", out shapeSN);

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
                    Util.updateSceneNode(ref mScene, dynamicBrep, ref mesh_m, modelName, out renderObjSN);

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
                    Guid guid = Util.addRhinoObjectSceneNode(ref mScene, ref dynamicBrep, ref profile_m, modelName, out renderObjSN, false, true);

                    clearDrawing(false);
                    Util.clearPlanePoints(ref mScene);
                    Util.clearCurveTargetRhObj(ref mScene);
                    //TODO- figure out a better cleaardrawing methods
                    for (int i = 0; i < mScene.tableGeometry.children.Count; i++)
                    {
                        SceneNode sn = mScene.tableGeometry.children[i];
                        if (sn.name.Contains("railPlane"))
                        {
                            Util.removeRhinoObjectSceneNode(ref mScene, ref sn);
                        }
                    }

                    //TODO- OpenGL compile error why?
                    //Util.setPlaneAlpha(ref mScene, 0.0f);

                    // once sending to slice, can't go back to edit again
                    mScene.clearInteractionStack(); // thus it will vrgame will push marking main menu interaction
                    mScene.clearIChainsList();
                    mScene.selectionDic.Clear();
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

        private void clearDrawing(bool hasNextI)
        {
            //since we will clear different SceneNode depends on the situation so it migit be better loop throung the tableGeometry instead of checking sceneNode != null
            if (mScene.tableGeometry.children.Count > 0)
            {
                // need to remove rerverse since the list update dynamically

                for(int i = 0; i < mScene.tableGeometry.children.Count; i++)
                {
                    SceneNode sn = mScene.tableGeometry.children[i];
                    if (dynamicRender == "Revolve" || dynamicRender == "Loft" || dynamicRender == "Sweep-Circle" || dynamicRender == "Sweep-Rect" || dynamicRender == "Extrude-Circle" || dynamicRender == "Extrude-Rect")
                    {
                        if (sn.name.Contains("tprint") || sn.name == "EditCurve" || sn.name == "drawPoint" || sn.name == "EditPoint" || sn.name.Contains("panel") || sn.name.Contains("Circle") || sn.name.Contains("Rect") || sn.name.Contains("railPlane") || sn.name.Contains("patchSurface"))
                        {
                            //We don't delete the patch surface for later use
                            if (sn.name.Contains("railPlane"))
                            {
                                //we might still need this if we use undo button
                                //Util.removeRhinoObjectSceneNode(ref mScene, ref sn);
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
                                Util.removeSceneNode(ref mScene, ref sn);
                            }
                        }

                        //only clear the drawpoint, editpoint
                        if (hasNextI)
                        {
                            //clear the drawpoint, editpoint, editcurve. save editcurve if shapetype is curve
                            if (sn.name == "drawPoint" || sn.name == "EditPoint" || (sn.name == "EditCurve" && shapeType != ShapeType.Curve))
                            {
                                Util.removeSceneNode(ref mScene, ref sn);
                            }
                        }
                        else
                        {
                            if (sn.name == "drawPoint" || sn.name == "EditPoint" || sn.name == "EditCurve")
                            {
                                Util.removeSceneNode(ref mScene, ref sn);
                            }
                        }

                    }

                }
            }

        }

        protected override void onClickOculusAX(ref VREvent_t vrEvent)
        {
            //TODO- need to consider this might be the first time editpoint.
            //start slicing model by changing the name of the model
            //mScene.popInteraction();
            Util.setPlaneAlpha(ref mScene, 0.0f);

            if (dynamicRender == "Revolve" || dynamicRender == "Loft")
            {
                modelName = "aprint";
                R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);
            }
            else if (dynamicRender == "Extrude" || dynamicRender == "Sweep")
            {
                ShapeType shapeType1 = (ShapeType)mScene.selectionDic[SelectionKey.Profile1Shape];
                if (shapeType1 == ShapeType.Circle || shapeType1 == ShapeType.Rect)
                {
                    generateEndCap(); //interaction chain already set up in marking menu

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
                //clearDrawing();
            }

            currentState = State.End;

            mScene.pushInteractionFromChain();

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
            OpenTK.Vector3 railStartNormal = Util.RhinoToOpenTKVector(rail.TangentAtStart);
            OpenTK.Vector3 railEndPoint = Util.RhinoToOpenTKPoint(rail.PointAtEnd);
            OpenTK.Vector3 railEndNormal = Util.RhinoToOpenTKVector(rail.TangentAtEnd);

            Plane endPlane = new Plane(rail.PointAtEnd, rail.TangentAtEnd);
            //compute the transform from profile curve to railstart and railend
            OpenTK.Matrix4 transMStart = new Matrix4();
            transMStart = Util.getCoordinateTransM(shapeCenter, railStartPoint, shapeNormal, railStartNormal);
            Transform tStart = Util.OpenTKToRhinoTransform(transMStart);

            OpenTK.Matrix4 transMEnd = new Matrix4();
            transMEnd = Util.getCoordinateTransM(shapeCenter, railEndPoint, shapeNormal, railEndNormal);
            Transform tEnd = Util.OpenTKToRhinoTransform(transMEnd);

            //create endPlane Rhino Object                
            PlaneSurface plane_surface = new PlaneSurface(endPlane, new Interval(-120, 120), new Interval(-120, 120));
            Brep designPlane = Brep.CreateFromSurface(plane_surface);


            Guid guid = Util.addRhinoObject(ref mScene, ref designPlane, "panel");


            if ((ShapeType)mScene.selectionDic[SelectionKey.Profile1Shape] == ShapeType.Circle)
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
                    NurbsCurve endCurve = endCircle.ToNurbsCurve();
                    endCurve.SetUserString(CurveData.PlaneOrigin.ToString(), endPlane.Origin.ToString());
                    endCurve.SetUserString(CurveData.PlaneNormal.ToString(), endPlane.Normal.ToString());
                    if (designPlane != null)
                    {
                        endCurve.SetUserString(CurveData.CurveOnObj.ToString(), guid.ToString());
                    }
                    mScene.iCurveList.Add(endCurve);
 
                    mScene.iPointList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(endCircle.Center)));
                    mScene.iPointList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(endCircle.PointAt(0))));
                }

                /*
                Brep[] shapes = Brep.CreatePlanarBreps(mScene.iCurveList[mScene.iCurveList.Count - 1]);
                //don't need to update the RhinoView
                Util.addSceneNode(ref mScene, ref shapes[0], ref mesh_m, ShapeType.Circle.ToString(), out shapeSN);
                */

            }
            else if ((ShapeType)mScene.selectionDic[SelectionKey.Profile1Shape] == ShapeType.Rect)
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
                endCurve.SetUserString(CurveData.PlaneOrigin.ToString(), endPlane.Origin.ToString());
                endCurve.SetUserString(CurveData.PlaneNormal.ToString(), endPlane.Normal.ToString());
                if (designPlane != null)
                {
                    endCurve.SetUserString(CurveData.CurveOnObj.ToString(), guid.ToString());
                }
                mScene.iCurveList.Add(endCurve);

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

                /*
                Brep[] shapes = Brep.CreatePlanarBreps(mScene.iCurveList[mScene.iCurveList.Count - 1]);
                //don't need to update the RhinoView
                Util.addSceneNode(ref mScene, ref shapes[0], ref mesh_m, ShapeType.Rect.ToString(), out shapeSN);
                */

            }
            else
            {
                //we only support rect and circle cap curve

            }

            //update the profile curve1 to the railStart
            mScene.iCurveList[mScene.iCurveList.Count - 3].Transform(tStart);

           
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
            else 
            {

            }
        }

        private void editCircleRect(int sector)
        {
            //Rhino.RhinoApp.WriteLine("sector:" + sector);
            if (isEditCircle && (sector == 1 || sector == 3))
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
                //TODO- need to change the curvePoint[1], find the direction and move the point directly
                //using the original x-axis and y-axis align, but adjust the center of the plane
                Plane newPlane = new Plane(circle.Center, curvePlane.Normal);
                newPlane.XAxis = curvePlane.XAxis;
                newPlane.YAxis = curvePlane.YAxis;
                Circle newCircle = new Circle(newPlane, radius);
                curvePoints.Clear();
                curvePoints.Add(newCircle.Center);
                curvePoints.Add(newCircle.ToNurbsCurve().PointAtStart);
                editCurve = Rhino.Geometry.NurbsCurve.Create(false, 1, curvePoints.ToArray()); 
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
                //TODO fix the bug of curvePlane
                Plane newPlane = new Plane(rect.Center, curvePlane.Normal);
                newPlane.XAxis = curvePlane.XAxis;
                newPlane.YAxis = curvePlane.YAxis;
                Rectangle3d newRect = new Rectangle3d(newPlane, new Interval(-width / 2, width / 2), new Interval(-height / 2, height / 2));
                curvePoints.Clear();
                curvePoints.Add(newRect.Center);
                curvePoints.Add(newRect.Corner(3));
                editCurve = Rhino.Geometry.NurbsCurve.Create(false, 1, curvePoints.ToArray());
                renderEditCurve();
                updateEditCurve();

                R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);

            }
        }

        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {

            //testing projection
            if (((ShapeType)mScene.selectionDic[SelectionKey.Profile1Shape] == ShapeType.Rect) && (dynamicRender == "Sweep-Rect" || dynamicRender == "Extrude-Rect"))
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
            else if (((ShapeType)mScene.selectionDic[SelectionKey.Profile1Shape] == ShapeType.Circle) && (dynamicRender == "Sweep-Circle" || dynamicRender == "Extrude-Circle") || dynamicRender == "Loft")
            {

                OpenTK.Vector3 p1 = Util.transformPoint(mScene.tableGeometry.transform.Inverted(), Util.platformToVRPoint(ref mScene, new Vector3((float)mScene.eStartP.X, (float)mScene.eStartP.Y, (float)mScene.eStartP.Z)));
                OpenTK.Vector3 p2 = Util.transformPoint(mScene.tableGeometry.transform.Inverted(), Util.platformToVRPoint(ref mScene, new Vector3((float)mScene.sStartP.X, (float)mScene.sStartP.Y, (float)mScene.sStartP.Z)));
                Util.MarkDebugPoint(ref mScene.tableGeometry, p1, 0f, 0f, 1f);
                Util.MarkDebugPoint(ref mScene.tableGeometry, p2, 0f, 0f, 1f);
            }
        }

    }
}
