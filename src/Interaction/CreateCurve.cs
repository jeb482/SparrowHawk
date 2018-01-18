using OpenTK;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;
using static SparrowHawk.Scene;

namespace SparrowHawk.Interaction
{
    class CreateCurve : Interaction
    {
        public enum State
        {
            READY = 0, DRAW = 1, MOVEPLANE =2, END = 3
        };

        protected State currentState;
        protected Geometry.Geometry stroke_g;
        protected Material.Material stroke_m;
        protected uint primaryDeviceIndex;
        protected List<Vector3> reducePoints = new List<Vector3>();

        public bool isClosed = false;

        protected Guid targetPRhObjID;
        protected SceneNode drawPoint;
        protected SceneNode snapPointSN;
        protected Rhino.Geometry.Point3d projectP;

        //testing rhino curve
        private List<Point3d> rhinoCurvePoints = new List<Point3d>();
        private Rhino.Geometry.Curve rhinoCurve;
        private Plane proj_plane;
        private List<Point3d> simplifiedCurvePoints = new List<Point3d>();
        private Rhino.Geometry.NurbsCurve simplifiedCurve;
        private Rhino.Geometry.NurbsCurve editCurve; //for extrude
        protected ObjRef curveOnObjRef; //in case that before modelComplete targetPRhObj become null
        private SceneNode renderObjSN;
        private SceneNode strokeSN;

        //dynamic rendering
        private bool backgroundStart = false;
        private float displacement = 0;
        Brep dynamicBrep;
        IAsyncResult R;
        public delegate void generateModel_Delegate();
        generateModel_Delegate d = null;
        private string modelName = "tprint";
        public string dynamicRender = "none";
        private Material.Material mesh_m;
        private Material.Material railPlane_m;

        private List<Point3d> snapPointsList = new List<Point3d>();
        private SceneNode railPlaneSN;

        //0:3D, 1:onDPlanes, 2: onSurfaces, 3: onTargets - before we modified
        private DrawnType drawnType = DrawnType.None;
        private List<ObjRef> rayCastingObjs;
        private int toleranceMax = 100000;
        private bool isSnap = false;
        private bool shouldSnap = false;
        private float snapDistance = 20; //mm

        //moving XYZPlanes
        private Point3d moveControlerOrigin = new Point3d();
        private Rhino.Geometry.Vector3d planeNormal = new Rhino.Geometry.Vector3d();
        private ObjRef movePlaneRef;
        private float lastTranslate = 0.0f;

        private Plane curvePlane;

        //testing thread issues
        private List<Curve> localListCurve = new List<Curve>();

        private int beforeCurveCount = 0;
        private int afterCurveCount = 0;
        string oldCurveOnObjID;
        string oldPlaneOrigin;
        string oldPlaneNormal;

        

        public CreateCurve(ref Scene scene, bool _isClosed, CurveID curveID) : base(ref scene)
        {
            beforeCurveCount = mScene.iCurveList.Count;
            stroke_g = new Geometry.GeometryStroke(ref mScene);
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.RGBNormalMaterial(0.5f);
            railPlane_m = new Material.SingleColorMaterial(34f / 255f, 139f / 255f, 34f / 255f, 0.4f);
            isClosed = _isClosed;
            rayCastingObjs = new List<ObjRef>();

            resetVariables();

            FunctionType modelFun = (FunctionType)mScene.selectionDic[SelectionKey.ModelFun];
            //0:3D, 1:onDPlanes, 2: onSurfaces, 3: onTargets
            if (curveID == CurveID.ProfileCurve1)
            {
                drawnType = (DrawnType)mScene.selectionDic[SelectionKey.Profile1On];
                //Revolve only needs 1 profilecurve in our case
                if (modelFun == FunctionType.Revolve)
                    dynamicRender = "Revolve";
            }
            else if (curveID == CurveID.ProfileCurve2)
            {
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

            /*
            foreach(Curve c in mScene.iCurveList)
            {
                localListCurve.Add(c);
            }
            */

            //testing
            localListCurve = mScene.iCurveList;

        }

        //TODO- check if the x,y axis of the plane will change whenever we call tryGetPlane
        //railPlaneSN-addRhinoObjSceneNode(draw on referece), curveOnObjRef-addRhinoObj(!=In3D)
        //drawPoint, strokeSN-addSceneNode, renderObjSN-updateSceneNode(Revolve or Curve2)
        public void resetVariables()
        {
            stroke_g = new Geometry.GeometryStroke(ref mScene);
            currentState = State.READY;

            reducePoints = new List<Vector3>();

            targetPRhObjID = Guid.Empty;
            drawPoint = null;
            snapPointSN = null;
            projectP = new Point3d();

            rhinoCurvePoints = new List<Point3d>();
            rhinoCurve = null;
            proj_plane = new Plane();
            simplifiedCurvePoints = new List<Point3d>();
            simplifiedCurve = null;
            editCurve = null; //for extrude
            //curveOnObjRef = null;


            backgroundStart = false;
            displacement = 0;
            dynamicBrep = null;
            modelName = "tprint";
            //dynamicRender = "none"; // need to save same as drawType and shapeType 
            snapPointsList = new List<Point3d>();
            rayCastingObjs = new List<ObjRef>();


            toleranceMax = 100000;
            snapDistance = 40;
            isSnap = false;
            shouldSnap = false;

            moveControlerOrigin = new Point3d();
            movePlaneRef = null;
            planeNormal = new Rhino.Geometry.Vector3d();

            curvePlane = new Plane();
            lastTranslate = 0.0f;
            d = null;

            localListCurve = mScene.iCurveList;
            oldCurveOnObjID = "";
            oldPlaneOrigin = "";
            oldPlaneNormal = "";
        }

        public override void leaveTop()
        {
            clearDrawingLeaveTop();
        }

        public override void deactivate()
        {
            clearDrawingPop();
        }

        public override void init()
        {
            resetVariables();

            //support undo function
            if (mScene != null && (afterCurveCount - beforeCurveCount) > 0)
            {
                
                if (mScene.iCurveList[mScene.iCurveList.Count - 1].GetUserString(CurveData.CurveOnObj.ToString()) != "")
                {
                    Guid curveOnObjId = new Guid(mScene.iCurveList[mScene.iCurveList.Count - 1].GetUserString(CurveData.CurveOnObj.ToString()));
                    ObjRef curveOnObjRef = new ObjRef(curveOnObjId);
                    if (curveOnObjRef.Object().Attributes.Name.Contains("railPlane") || curveOnObjRef.Object().Attributes.Name.Contains("MoveP"))
                    {
                        Util.removeRhinoObject(ref mScene, curveOnObjRef.ObjectId);
                    }

                }
                mScene.iCurveList.RemoveAt(mScene.iCurveList.Count - 1);               

                Util.removeSceneNode(ref mScene, ref strokeSN);
                strokeSN = null;

                //need to clear stroke tprint SceneNode as well here
                if (renderObjSN != null)
                {
                    Util.removeSceneNode(ref mScene, ref renderObjSN);
                    renderObjSN = null;
                }

                //Util.removeRhinoObject(ref mScene, curveOnObjRef.ObjectId);
                curveOnObjRef = null;
                

                if(railPlaneSN != null)
                {
                    Util.removeRhinoObjectSceneNode(ref mScene, ref railPlaneSN);
                    railPlaneSN = null;
                }              

            }


            if (drawnType != DrawnType.In3D && drawnType != DrawnType.None)
            {
                Util.showLaser(ref mScene, true);
                //create and add referece planes to scene            
                if (drawnType == DrawnType.Reference)
                {
            
                    Vector3 railPlaneNormal = Util.RhinoToOpenTKVector(Util.getVectorfromString(mScene.iCurveList[mScene.iCurveList.Count - 1].GetUserString(CurveData.PlaneNormal.ToString())));
                    OpenTK.Vector3 worldUpAxis = new Vector3(0, 0, 1);
                    Plane railPlane = new Plane(mScene.iCurveList[mScene.iCurveList.Count - 1].GetBoundingBox(true).Center, Util.openTkToRhinoVector(Vector3.Cross(railPlaneNormal, worldUpAxis)));
                    float planeSize = 240;
                    PlaneSurface plane_surface2 = new PlaneSurface(railPlane, new Interval(-planeSize, planeSize), new Interval(-planeSize, planeSize));
                    Brep railPlane2 = Brep.CreateFromSurface(plane_surface2);
                    Guid railPlaneGuid = Util.addRhinoObjectSceneNode(ref mScene, ref railPlane2, ref railPlane_m, "railPlane", out railPlaneSN);
                   
                }
                else if (drawnType == DrawnType.Plane)
                {
                    Util.setPlaneAlpha(ref mScene, 0.4f);
                }

                //init rayCastingObjs
                Rhino.DocObjects.ObjectEnumeratorSettings settings = new Rhino.DocObjects.ObjectEnumeratorSettings();
                settings.ObjectTypeFilter = Rhino.DocObjects.ObjectType.Brep;
                foreach (Rhino.DocObjects.RhinoObject rhObj in mScene.rhinoDoc.Objects.GetObjectList(settings))
                {
                    bool b1 = (drawnType == DrawnType.Plane) && rhObj.Attributes.Name.Contains("plane");
                    bool b2 = (drawnType == DrawnType.Surface) && (rhObj.Attributes.Name.Contains("brepMesh") || rhObj.Attributes.Name.Contains("aprint") || rhObj.Attributes.Name.Contains("patchSurface"));
                    bool b3 = (drawnType == DrawnType.Reference) && rhObj.Attributes.Name.Contains("railPlane");

                    if (b1 || b2 || b3)
                    {
                        rayCastingObjs.Add(new ObjRef(rhObj.Id));
                    }
                }
                
                Geometry.Geometry geo = new Geometry.DrawPointMarker(new OpenTK.Vector3(0, 0, 0));
                Material.Material m = new Material.SingleColorMaterial(1, 1, 1, 0);//TODO: teseting alpha working or not
                drawPoint = new SceneNode("drawPoint", ref geo, ref m);            
                Util.addSceneNode(ref mScene, ref drawPoint);
            }else
            {
                Util.showLaser(ref mScene, false);
            }

            //generate snap points when we need to draw from the center of the shapes, drawnType could be DrawnType.Reference or DrawnType.In3D
            if (dynamicRender == "Extrude" || dynamicRender == "Sweep" || drawnType == DrawnType.Reference)
            {
                shouldSnap = true;
                ShapeType shapeType = (ShapeType)mScene.selectionDic[SelectionKey.Profile1Shape];
                Circle circle;
                Rectangle3d rect;
                if (shapeType == ShapeType.Circle)
                {
                    if (mScene.iCurveList[mScene.iCurveList.Count - 1].TryGetCircle(out circle))
                    {
                        snapPointsList.Add(circle.Center);
                    }
                }
                else if (shapeType == ShapeType.Rect)
                {
                    Rhino.Geometry.Polyline polyline;
                    if (mScene.iCurveList[mScene.iCurveList.Count - 1].TryGetPolyline(out polyline))
                    {
                        rect = Rectangle3d.CreateFromPolyline(polyline);
                        snapPointsList.Add(rect.Center);
                    }
                }

                //visualize the snap points
                Geometry.Geometry geo = new Geometry.DrawPointMarker(new Vector3(0, 0, 0));
                Material.Material m = new Material.SingleColorMaterial(0, 1, 0, 1);
                Util.MarkPointVR(ref mScene, Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(snapPointsList[0])), ref geo, ref m, out snapPointSN);
            }

            d = new generateModel_Delegate(generateModel);
        }

        public override void draw(bool isTop)
        {
            if (!isTop)
            {
                return;
            }

            // Clean this monstrosity
            OpenTK.Vector4 controller_p = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, 0, 1);
            OpenTK.Vector4 controller_pZ = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, -1, 1);
            Point3d controller_pRhino = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)));
            Point3d controller_pZRhin = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_pZ.X, controller_pZ.Y, controller_pZ.Z)));
            Rhino.Geometry.Vector3d direction = new Rhino.Geometry.Vector3d(controller_pZRhin.X - controller_pRhino.X, controller_pZRhin.Y - controller_pRhino.Y, controller_pZRhin.Z - controller_pRhino.Z);

            if (drawnType != DrawnType.In3D)
            {
                Util.rayCasting(controller_pRhino, direction, ref rayCastingObjs, ref projectP, out targetPRhObjID);
            }else
            {
                projectP = controller_pRhino;
            }

            //only snap for the first drawing point
            if (currentState != State.DRAW && snapPointsList.Count > 0)
            {
                if (Util.snapToPoints(ref projectP, ref snapPointsList) != -1)
                {
                    isSnap = true;
                    snapPointSN.material = new Material.SingleColorMaterial(1, 1, 1, 1);
                }
                else
                {
                    isSnap = false;
                    snapPointSN.material = new Material.SingleColorMaterial(0, 1, 0, 1);
                }
            }

            //
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




            //moving XYZ planes
            if (currentState == State.MOVEPLANE)
            {
                OpenTK.Vector3 controllerVector = Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)) - Util.RhinoToOpenTKPoint(moveControlerOrigin);

                float translate = OpenTK.Vector3.Dot(controllerVector, Util.RhinoToOpenTKVector(planeNormal)) / (float)planeNormal.Length;
                float relTranslate = translate - lastTranslate;
                lastTranslate = translate;

                Matrix4 transM = Matrix4.CreateTranslation(new Vector3(relTranslate * (float)planeNormal.X, relTranslate * (float)planeNormal.Y, relTranslate * (float)planeNormal.Z));
                transM.Transpose();
                Util.updateRhinoObjectSceneNode(ref mScene, ref movePlaneRef, Util.OpenTKToRhinoTransform(transM));

            }



            if (currentState != State.DRAW)
            {
                return;
            }else 
            {
                //checking the projectPoint is valid
                if (drawnType != DrawnType.In3D && targetPRhObjID == Guid.Empty)
                    return;
            }

            //drawing curve section belows
            if(shouldSnap && ((Geometry.GeometryStroke)stroke_g).mNumPoints == 0)
            {
                ((Geometry.GeometryStroke)stroke_g).addPoint(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(snapPointsList[0])));
            }

            ((Geometry.GeometryStroke)stroke_g).addPoint(projectPVR);
            rhinoCurvePoints.Add(projectP);
            
            if (((Geometry.GeometryStroke)stroke_g).mNumPrimitives == 1)
            {
                strokeSN = new SceneNode("Stroke", ref stroke_g, ref stroke_m);
                Util.addSceneNode(ref mScene, ref strokeSN);
            }

            //create rhino curve for comoputing length of the curve
            if (rhinoCurvePoints.Count == 2)
            {
                if (shouldSnap)
                {
                    //make sure the first point is the snap point if necessary
                    if (Util.computePointDistance(Util.RhinoToOpenTKPoint(rhinoCurvePoints[0]), Util.RhinoToOpenTKPoint(snapPointsList[0])) != 0)
                    {
                        rhinoCurvePoints.Insert(0, snapPointsList[0]);
                    }
                }
                rhinoCurve = Rhino.Geometry.Curve.CreateInterpolatedCurve(rhinoCurvePoints.ToArray(), 3);
            }
            else if (rhinoCurvePoints.Count > 2)
            {
                double length1 = rhinoCurve.GetLength();
                rhinoCurve = Rhino.Geometry.Curve.CreateInterpolatedCurve(rhinoCurvePoints.ToArray(), 3);
                double length2 = rhinoCurve.GetLength();
                displacement = displacement + (float)Math.Abs(length2 - length1);

                //TODO-Debug why it failed
                //rhinoCurve = rhinoCurve.Extend(Rhino.Geometry.CurveEnd.End, Rhino.Geometry.CurveExtensionStyle.Line, rhinoCurvePoints[rhinoCurvePoints.Count - 1]);

                //dynamic render model
                //TODO: finding the right curve
                if (dynamicRender != "none" && backgroundStart == false && displacement > 10)
                {
                    backgroundStart = true;
                    R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);
                }
            }

        }

        public void generateModel()
        {

            //TODO-simplify the curve and pass to model function
            simplifyCurve(ref ((Geometry.GeometryStroke)(stroke_g)).mPoints); //result curve is simplifiedCurve

            if (dynamicRender == "none" || simplifiedCurve == null)
            {
                return;
            }
            else if (dynamicRender == "Revolve")
            {
                if (mScene.iCurveList.Count == 0)
                {
                    if (drawnType != DrawnType.In3D && curveOnObjRef != null)
                    {
                        simplifiedCurve.SetUserString(CurveData.CurveOnObj.ToString(),  curveOnObjRef.ObjectId.ToString());
                        simplifiedCurve.SetUserString(CurveData.PlaneOrigin.ToString(), curvePlane.Origin.ToString());
                        simplifiedCurve.SetUserString(CurveData.PlaneNormal.ToString(), curvePlane.Normal.ToString());
                    }
                 
                    mScene.iCurveList.Add(simplifiedCurve);

                }
                else
                {
                    //get the curve info for later update use
                    string oldCurveOnObjID = mScene.iCurveList[mScene.iCurveList.Count - 1].GetUserString(CurveData.CurveOnObj.ToString());
                    string oldPlaneOrigin = mScene.iCurveList[mScene.iCurveList.Count - 1].GetUserString(CurveData.PlaneOrigin.ToString());
                    string oldPlaneNormal = mScene.iCurveList[mScene.iCurveList.Count - 1].GetUserString(CurveData.PlaneNormal.ToString());

                    simplifiedCurve.SetUserString(CurveData.CurveOnObj.ToString(), oldCurveOnObjID);
                    simplifiedCurve.SetUserString(CurveData.PlaneOrigin.ToString(), oldPlaneOrigin);
                    simplifiedCurve.SetUserString(CurveData.PlaneNormal.ToString(), oldPlaneNormal);

                    mScene.iCurveList[0] = simplifiedCurve;
                }

                dynamicBrep = Util.RevolveFunc(ref mScene, ref mScene.iCurveList);
            }
            else if (dynamicRender == "Loft")
            {
                if (mScene.iCurveList.Count == 1)
                {
                    if (drawnType != DrawnType.In3D && curveOnObjRef != null)
                    {
                        simplifiedCurve.SetUserString(CurveData.CurveOnObj.ToString(), curveOnObjRef.ObjectId.ToString());
                        simplifiedCurve.SetUserString(CurveData.PlaneOrigin.ToString(), curvePlane.Origin.ToString());
                        simplifiedCurve.SetUserString(CurveData.PlaneNormal.ToString(), curvePlane.Normal.ToString());
                    }
                    mScene.iCurveList.Add(simplifiedCurve);
                    //get the curve info for later update use
                    oldCurveOnObjID = mScene.iCurveList[mScene.iCurveList.Count - 1].GetUserString(CurveData.CurveOnObj.ToString());
                    oldPlaneOrigin = mScene.iCurveList[mScene.iCurveList.Count - 1].GetUserString(CurveData.PlaneOrigin.ToString());
                    oldPlaneNormal = mScene.iCurveList[mScene.iCurveList.Count - 1].GetUserString(CurveData.PlaneNormal.ToString());
                }
                else
                {


                    simplifiedCurve.SetUserString(CurveData.CurveOnObj.ToString(), oldCurveOnObjID);
                    simplifiedCurve.SetUserString(CurveData.PlaneOrigin.ToString(), oldPlaneOrigin);
                    simplifiedCurve.SetUserString(CurveData.PlaneNormal.ToString(), oldPlaneNormal);

                    mScene.iCurveList[1] = simplifiedCurve;
                }

                dynamicBrep = Util.LoftFunc(ref mScene, ref mScene.iCurveList);
            }
            else if (dynamicRender == "Extrude")
            {

                //Rhino.Geometry.Vector3d heightVector = simplifiedCurve.PointAtEnd - simplifiedCurve.PointAtStart;
                Rhino.Geometry.Vector3d heightVector = simplifiedCurve.PointAtEnd - snapPointsList[0];
                Rhino.Geometry.Vector3d planeNormal = Util.getVectorfromString(localListCurve[0].GetUserString(CurveData.PlaneNormal.ToString()));
                planeNormal.Unitize();
                double height = Rhino.Geometry.Vector3d.Multiply(heightVector,planeNormal) / planeNormal.Length;

                List<Point3d> extrudeCurveP = new List<Point3d>();
                Point3d startP = snapPointsList[0];
                extrudeCurveP.Add(startP);
                Point3d endP = new Point3d(startP.X + height * planeNormal.X, startP.Y + height * planeNormal.Y, startP.Z + height * planeNormal.Z);
                extrudeCurveP.Add(endP);
                //update the edit curve
                editCurve = Rhino.Geometry.NurbsCurve.Create(false, 1, extrudeCurveP.ToArray());

                if (localListCurve.Count == 1)
                {
                    if (drawnType != DrawnType.In3D && curveOnObjRef != null)
                    {
                        editCurve.SetUserString(CurveData.CurveOnObj.ToString(), curveOnObjRef.ObjectId.ToString());
                        editCurve.SetUserString(CurveData.PlaneOrigin.ToString(), curvePlane.Origin.ToString());
                        editCurve.SetUserString(CurveData.PlaneNormal.ToString(), curvePlane.Normal.ToString());
                    }

                    localListCurve.Add(editCurve);
                    //get the curve info for later update use
                    oldCurveOnObjID = localListCurve[localListCurve.Count - 1].GetUserString(CurveData.CurveOnObj.ToString());
                    oldPlaneOrigin = localListCurve[localListCurve.Count - 1].GetUserString(CurveData.PlaneOrigin.ToString());
                    oldPlaneNormal = localListCurve[localListCurve.Count - 1].GetUserString(CurveData.PlaneNormal.ToString());
                }
                else
                {


                    editCurve.SetUserString(CurveData.CurveOnObj.ToString(), oldCurveOnObjID);
                    editCurve.SetUserString(CurveData.PlaneOrigin.ToString(), oldPlaneOrigin);
                    editCurve.SetUserString(CurveData.PlaneNormal.ToString(), oldPlaneNormal);
                    localListCurve[1] = editCurve;
                }

                dynamicBrep = Util.ExtrudeFunc(ref mScene, ref localListCurve);
                
            }
            else if (dynamicRender == "Sweep")
            {
                if (localListCurve.Count == 1)
                {
                    if (drawnType != DrawnType.In3D && curveOnObjRef != null)
                    {
                        simplifiedCurve.SetUserString(CurveData.CurveOnObj.ToString(), curveOnObjRef.ObjectId.ToString());
                        simplifiedCurve.SetUserString(CurveData.PlaneOrigin.ToString(), curvePlane.Origin.ToString());
                        simplifiedCurve.SetUserString(CurveData.PlaneNormal.ToString(), curvePlane.Normal.ToString());
                    }
                    localListCurve.Add(simplifiedCurve);
                    //get the curve info for later update use
                    oldCurveOnObjID = localListCurve[localListCurve.Count - 1].GetUserString(CurveData.CurveOnObj.ToString());
                    oldPlaneOrigin = localListCurve[localListCurve.Count - 1].GetUserString(CurveData.PlaneOrigin.ToString());
                    oldPlaneNormal = localListCurve[localListCurve.Count - 1].GetUserString(CurveData.PlaneNormal.ToString());

                }
                else
                {
                    simplifiedCurve.SetUserString(CurveData.CurveOnObj.ToString(), oldCurveOnObjID);
                    simplifiedCurve.SetUserString(CurveData.PlaneOrigin.ToString(), oldPlaneOrigin);
                    simplifiedCurve.SetUserString(CurveData.PlaneNormal.ToString(), oldPlaneNormal);
                    localListCurve[1] = simplifiedCurve;
                }

                dynamicBrep = Util.SweepFun(ref mScene, ref localListCurve);
            }

            /*
            mScene.iCurveList.Clear();
            //mScene.iCurveList = localListCurve;
            foreach (Curve c in localListCurve)
            {
                mScene.iCurveList.Add(c);
            }
            */
        }

        public void modelCompleted(IAsyncResult R)
        {
            if (dynamicBrep != null)
            {
                if (modelName == "tprint")
                {
                    Util.updateSceneNode(ref mScene, dynamicBrep, ref mesh_m, modelName, ref renderObjSN);
                }

                //fix the issue, update mSceneNode
                /*
                if (currentState == State.READY)
                {
                    mScene.iCurveList.Clear();
                    mScene.iCurveList = localListCurve;
                    afterCurveCount = mScene.iCurveList.Count;
                    mScene.pushInteractionFromChain();
                }
                */
            }

            dynamicBrep = null;
            backgroundStart = false;
            displacement = 0;

            if(currentState == State.END)
            {
                mScene.pushInteractionFromChain();
            }

        }

        protected override void onClickOculusTrigger(ref VREvent_t vrEvent)
        {

            primaryDeviceIndex = vrEvent.trackedDeviceIndex;

            if ((dynamicRender == "Extrude" || dynamicRender == "Sweep") && !isSnap)
                return;

            if (currentState == State.READY)
            {
                if(drawnType == DrawnType.In3D)
                {
                    stroke_g = new Geometry.GeometryStroke(ref mScene);
                    reducePoints = new List<Vector3>();
                    currentState = State.DRAW;
                }
                else
                {
                    if (targetPRhObjID == Guid.Empty)
                        return;

                    curveOnObjRef = new ObjRef(targetPRhObjID);

                    if (curveOnObjRef != null)
                    {
                        //chage to only raycasting to the obj where we draw
                        rayCastingObjs.Clear();
                        rayCastingObjs.Add(curveOnObjRef);
                        stroke_g = new Geometry.GeometryStroke(ref mScene);
                        reducePoints = new List<Vector3>();
                        currentState = State.DRAW;

                        //TODO- figure out why we do this here
                        if (drawnType == DrawnType.Plane)
                        {
                            //TODO-generalize change SceneNode alpha
                            Util.hideOtherPlanes(ref mScene, curveOnObjRef.Object().Attributes.Name);
                        }
                        else if (drawnType == DrawnType.Reference)
                        {
                            ((Material.SingleColorMaterial)railPlaneSN.material).setAlpha(0.0f);
                        }

                        //detecting projection plane in a Brep
                        Double tolerance = 0;
                        curvePlane = new Plane();
                        if (drawnType != DrawnType.In3D)
                        {
                            Brep targetBrep = (Brep)(curveOnObjRef.Object().Geometry);

                            float minD = 1000000f;
                            int minIndex = -1;
                            for (int i = 0; i < targetBrep.Faces.Count; i++)
                            {
                                //cast BrepFace to Brep for ClosestPoint(P) menthod
                                double dist = targetBrep.Faces[i].DuplicateFace(false).ClosestPoint(projectP).DistanceTo(projectP);                   
                                if (dist < minD)
                                {
                                    minD = (float)dist;
                                    minIndex = i;
                                }
                            }
                            Surface s = targetBrep.Faces[minIndex];
                            //surface might not be a perfect planar surface                     
                            while (tolerance < toleranceMax)
                            {
                                if (s.TryGetPlane(out curvePlane, tolerance))
                                {
                                    break;
                                }
                                tolerance++;
                            }
                        }

                        //add plane to iPlaneList since Sweep fun need it's info
                        if (tolerance < toleranceMax)
                        {
                            //DrawnType.Reference already has a railPlane
                            /*
                            if (drawnType != DrawnType.Reference)
                                mScene.iPlaneList.Add(curvePlane);
                            */
                        }
                        else
                        {
                            Rhino.RhinoApp.WriteLine("Can't find projectPlane");
                        }

                    }
                    
                }              
             
            }

        }

        protected override void onReleaseOculusTrigger(ref VREvent_t vrEvent)
        {

            if (currentState == State.DRAW)
            {  
                //simplfy the curve first before doing next interaction
                if (((Geometry.GeometryStroke)(stroke_g)).mPoints.Count >= 2)
                {
                   
                    //simplifyCurve(ref ((Geometry.GeometryStroke)(stroke_g)).mPoints);

                    //add to Scene curve object ,targetRhobj and check the next interaction

                    //generate new curveOnObj for mvoingPlane cases and move all moveplanes back to original position later
                    if(drawnType == DrawnType.Plane)
                    {
                        Rhino.Geometry.Vector3d newNormal = new Rhino.Geometry.Vector3d(); ;
                        Point3d newOrigin = new Point3d();
                        if (curveOnObjRef.Object().Attributes.Name.Contains("planeXY"))
                        {
                            newNormal = new Rhino.Geometry.Vector3d(0, 0, 1);
                            newOrigin = new Point3d(0, 0, curveOnObjRef.Object().Geometry.GetBoundingBox(true).Center.Z);

                        }
                        else if (curveOnObjRef.Object().Attributes.Name.Contains("planeYZ"))
                        {
                            newNormal = new Rhino.Geometry.Vector3d(1, 0, 0);
                            newOrigin = new Point3d(curveOnObjRef.Object().Geometry.GetBoundingBox(true).Center.X,0,0);

                        }
                        else if (curveOnObjRef.Object().Attributes.Name.Contains("planeXZ"))
                        {
                            newNormal = new Rhino.Geometry.Vector3d(0, 1, 0);
                            newOrigin = new Point3d(0, curveOnObjRef.Object().Geometry.GetBoundingBox(true).Center.Y, 0);
                        }

                        Plane newPlane = new Plane(newOrigin, newNormal);
                        int size = 240;
                        PlaneSurface plane_surface = new PlaneSurface(newPlane, new Interval(-size, size), new Interval(-size, size));
                        Brep newPlaneBrep = Brep.CreateFromSurface(plane_surface);

                        Guid newPlaneID = Util.addRhinoObject(ref mScene, ref newPlaneBrep, "MoveP");
                        curveOnObjRef = null;
                        curveOnObjRef = new ObjRef(newPlaneID);

                        //update curveOnObj to new plane
                        localListCurve[localListCurve.Count-1].SetUserString(CurveData.CurveOnObj.ToString(), curveOnObjRef.ObjectId.ToString());
                    }

                    //since dynamicRender  == "none" then it didn't enter generateModel function
                    if (dynamicRender == "none")
                    {
                        if (drawnType != DrawnType.In3D && curveOnObjRef != null)
                        {
                            simplifyCurve(ref ((Geometry.GeometryStroke)(stroke_g)).mPoints);
                            simplifiedCurve.SetUserString(CurveData.CurveOnObj.ToString(), curveOnObjRef.ObjectId.ToString());
                        }

                        localListCurve.Add(simplifiedCurve);

                    }

                    //call next interaction in the chain
                    currentState = State.END;
                    afterCurveCount = localListCurve.Count;
                    if (dynamicRender != "none" && backgroundStart == false)
                    {
                        backgroundStart = true;
                        R = d.BeginInvoke(new AsyncCallback(modelCompleted), null);
                    }else if (dynamicRender == "none")
                    {
                        mScene.pushInteractionFromChain();
                    }
                    //mScene.pushInteractionFromChain();                  
                    //curveOnObjRef = null;

                }
            }
        }

        private void clearDrawingLeaveTop()
        {
            if (drawnType == DrawnType.Plane)
            {
                //resetPlane
                mScene.xyPlane.resetOrgin();
                mScene.yzPlane.resetOrgin();
                mScene.xzPlane.resetOrgin();
                Util.setPlaneAlpha(ref mScene, 0.0f);            
            }

            Util.removeSceneNode(ref mScene, ref drawPoint);
            if (snapPointSN != null)
            {
                Util.removeSceneNode(ref mScene, ref snapPointSN);
            }

            //clear the curve and points (since editCurve will render it again)
            Util.removeSceneNode(ref mScene, ref strokeSN);

            if(renderObjSN != null)
            {
                Util.removeSceneNode(ref mScene, ref renderObjSN);
                renderObjSN = null;
            }

            //remove railPlane
            /*
            if (drawnType == DrawnType.Reference && railPlaneSN != null)
            {
                //TODO-if we remove here, press undo will casue an error
                Util.removeRhinoObjectSceneNode(ref mScene, ref railPlaneSN);
                railPlaneSN = null;
            }
            */
        }

        private void clearDrawingPop()
        {
            if (drawnType == DrawnType.Plane)
            {

                //resetPlane
                mScene.xyPlane.resetOrgin();
                mScene.yzPlane.resetOrgin();
                mScene.xzPlane.resetOrgin();

                Util.setPlaneAlpha(ref mScene, 0.0f);

            }

            //clear the curve and points
            Util.removeSceneNode(ref mScene, ref strokeSN);
            Util.removeSceneNode(ref mScene, ref drawPoint);
            if (snapPointSN != null)
            {
                Util.removeSceneNode(ref mScene, ref snapPointSN);
            }
  
            //remove railPlane
            if (drawnType == DrawnType.Reference && railPlaneSN != null)
            {
                //TODO-if we remove here, press undo will casue an error
                Util.removeRhinoObjectSceneNode(ref mScene, ref railPlaneSN);
                railPlaneSN = null;
            }

            if (renderObjSN != null)
            {
                Util.removeSceneNode(ref mScene, ref renderObjSN);
                renderObjSN = null;
            }

        }

        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            if (targetPRhObjID == Guid.Empty || currentState != State.READY)
                return;

            currentState = State.MOVEPLANE;
            movePlaneRef = new ObjRef(targetPRhObjID);
            lastTranslate = 0.0f;

            //get the plane info
            RhinoObject movePlaneObj = movePlaneRef.Object();
            planeNormal = new Rhino.Geometry.Vector3d();
            //PointOnObject still null at this point
            if (movePlaneObj.Attributes.Name.Contains("planeXY"))
            {
                planeNormal = new Rhino.Geometry.Vector3d(0, 0, 1);
            }
            else if (movePlaneObj.Attributes.Name.Contains("planeYZ"))
            {
                planeNormal = new Rhino.Geometry.Vector3d(1, 0, 0);
            }
            else if (movePlaneObj.Attributes.Name.Contains("planeXZ"))
            {
                planeNormal = new Rhino.Geometry.Vector3d(0, 1, 0);
            }

            OpenTK.Vector4 controller_p = Util.getControllerTipPosition(ref mScene, primaryControllerIdx == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, 0, 1);
            OpenTK.Vector3 controllerVector = Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z));

            float translate = OpenTK.Vector3.Dot(controllerVector, Util.RhinoToOpenTKVector(planeNormal)) / (float)planeNormal.Length;
            //move from the porjection point not origin
            moveControlerOrigin = new Point3d(0 + translate * planeNormal.X, 0 + translate * planeNormal.Y, 0 + translate * planeNormal.Z);

        }

        protected override void onReleaseOculusGrip(ref VREvent_t vrEvent)
        {
            currentState = State.READY;
            movePlaneRef = null;

        }


        public void simplifyCurve(ref List<Vector3> curvePoints)
        {
            //clear list first
            simplifiedCurvePoints.Clear();
            reducePoints.Clear();

            float pointReductionTubeWidth = 0.002f; //0.002
            reducePoints = DouglasPeucker(ref curvePoints, 0, curvePoints.Count - 1, pointReductionTubeWidth);
            //Rhino.RhinoApp.WriteLine("reduce points from" + curvePoints.Count + " to " + reducePoints.Count);

            //TODO- the curve isn't correct while drawing
            for (int i = 0; i < reducePoints.Count; i++)
            {
                simplifiedCurvePoints.Add(Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, reducePoints[i])));
            }

            if (simplifiedCurvePoints.Count >= 2) //TODO: might need 8 for closecurve check
            {
                int order = 3;
                if (isClosed)
                {
                    while (order >= 1)
                    {
                        simplifiedCurve = Rhino.Geometry.NurbsCurve.Create(true, order, simplifiedCurvePoints.ToArray());
                        if (simplifiedCurve != null)
                            break;
                        order--;
                    }
                }
                else
                {
                    //null check
                    while (order >= 1)
                    {
                        simplifiedCurve = Rhino.Geometry.NurbsCurve.Create(false, order, simplifiedCurvePoints.ToArray());
                        if (simplifiedCurve != null)
                            break;
                        order--;
                    }

                }

                //reduced control points 
                if (simplifiedCurve.Points.Count > 5)
                {
                    simplifiedCurve = simplifiedCurve.Rebuild(5, simplifiedCurve.Degree, false);
                }else
                {
                    simplifiedCurve = simplifiedCurve.Rebuild(simplifiedCurve.Points.Count, simplifiedCurve.Degree, false);
                }
            }
        }

        //Quick test about Douglas-Peucker for rhino points, return point3d with rhino coordinate system
        public List<Vector3> DouglasPeucker(ref List<Vector3> points, int startIndex, int lastIndex, float epsilon)
        {
            float dmax = 0f;
            int index = startIndex;

            for (int i = index + 1; i < lastIndex; ++i)
            {
                float d = PointLineDistance(points[i], points[startIndex], points[lastIndex]);
                if (d > dmax)
                {
                    index = i;
                    dmax = d;
                }
            }

            if (dmax > epsilon)
            {
                List<Vector3> res1 = DouglasPeucker(ref points, startIndex, index, epsilon);
                List<Vector3> res2 = DouglasPeucker(ref points, index, lastIndex, epsilon);

                //watch out the coordinate system
                List<Vector3> finalRes = new List<Vector3>();
                for (int i = 0; i < res1.Count - 1; ++i)
                {
                    finalRes.Add(res1[i]);
                }

                for (int i = 0; i < res2.Count; ++i)
                {
                    finalRes.Add(res2[i]);
                }

                return finalRes;
            }
            else
            {
                return new List<Vector3>(new Vector3[] { points[startIndex], points[lastIndex] });
            }
        }

        public float PointLineDistance(Vector3 point, Vector3 start, Vector3 end)
        {

            if (start == end)
            {
                return (float)Math.Sqrt(Math.Pow(point.X - start.X, 2) + Math.Pow(point.Y - start.Y, 2) + Math.Pow(point.Z - start.Z, 2));
            }

            Vector3 u = new Vector3(end.X - start.X, end.Y - start.Y, end.Z - start.Z);
            Vector3 pq = new Vector3(point.X - start.X, point.Y - start.Y, point.Z - start.Z);

            return Vector3.Cross(pq, u).Length / u.Length;


        }

    }
}