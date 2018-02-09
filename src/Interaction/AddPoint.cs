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
    class AddPoint : Interaction
    {
        public enum State
        {
            READY = 0, MOVEPLANE = 1
        };

        public int maxNumPoint = 100;
        protected Geometry.Geometry point_g;
        protected Material.Material point_m;
        private Material.Material profile_m;
        protected State currentState;
        protected uint primaryDeviceIndex;

        protected Guid targetPRhObjID;
        protected SceneNode drawPoint;
        protected Rhino.Geometry.Point3d projectP;

        protected Rhino.DocObjects.ObjRef pointOnObjRef; //in case that before modelComplete targetPRhObj become null
        private List<Point3d> pointsList = new List<Point3d>();
        List<SceneNode> pointMarkers = new List<SceneNode>();
        private Rhino.Geometry.Curve contourCurve = null;
        private Plane curvePlane;


        private List<Point3d> snapPointsList = new List<Point3d>();

        private DrawnType drawnType = DrawnType.None;
        private ShapeType shapeType = ShapeType.None;
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
        private int beforeCurveCount = 0;
        private int afterCurveCount = 0;

        public AddPoint(ref Scene scene) : base(ref scene)
        {
            mScene = scene;
            point_g = new Geometry.PointMarker(new Vector3());
            point_m = new Material.SingleColorMaterial(0f, .5f, 1f, 1f);
            currentState = State.READY;
        }

        public AddPoint(ref Scene scene, int num, CurveID curveID) : base(ref scene)
        {
            mScene = scene;
            beforeCurveCount = mScene.iCurveList.Count;
            point_g = new Geometry.PointMarker(new Vector3());
            point_m = new Material.SingleColorMaterial(0f, .5f, 1f, 1f);

            if (curveID == CurveID.ProfileCurve1)
            {
                drawnType = (DrawnType)mScene.selectionDic[SelectionKey.Profile1On];
                shapeType = (ShapeType)mScene.selectionDic[SelectionKey.Profile1Shape];
            }
            else if (curveID == CurveID.ProfileCurve2)
            {
                drawnType = (DrawnType)mScene.selectionDic[SelectionKey.Profile2On];
                shapeType = (ShapeType)mScene.selectionDic[SelectionKey.Profile2Shape];
            }

            maxNumPoint = num;
            profile_m = new Material.SingleColorMaterial(0.5f, 0, 0, 0.4f);
            rayCastingObjs = new List<ObjRef>();

            resetVariables();

        }

        //addRhinoObject-pointOnObjRef (where curve on)
        //drawPoint,editPointSN in PointMarkers-addSceneNode  
        public void resetVariables()
        {
            point_g = new Geometry.PointMarker(new Vector3());
            currentState = State.READY;

            targetPRhObjID = Guid.Empty;
            //pointOnObjRef = null;
            drawPoint = null;
            projectP = new Point3d();

            snapPointsList = new List<Point3d>();
            rayCastingObjs = new List<ObjRef>();

            pointsList = new List<Point3d>();
            pointMarkers = new List<SceneNode>();
            contourCurve = null;
            curvePlane = new Plane();

            toleranceMax = 100000;
            snapDistance = 40;
            isSnap = false;
            shouldSnap = false;

            moveControlerOrigin = new Point3d();
            movePlaneRef = null;
            planeNormal = new Rhino.Geometry.Vector3d();

            lastTranslate = 0.0f;

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
            if (mScene != null && (afterCurveCount-beforeCurveCount) >  0)
            {
                if (mScene.iCurveList[mScene.iCurveList.Count - 1].GetUserString(CurveData.CurveOnObj.ToString()) != "")
                {
                    Guid curveOnObjId = new Guid(mScene.iCurveList[mScene.iCurveList.Count - 1].GetUserString(CurveData.CurveOnObj.ToString()));
                    ObjRef curveOnObjRef = new ObjRef(curveOnObjId);
                    if (curveOnObjRef.Object().Attributes.Name.Contains("MoveP"))
                    {
                        UtilOld.removeRhinoObject(ref mScene, curveOnObjRef.ObjectId);
                    }

                }
                mScene.iCurveList.RemoveAt(mScene.iCurveList.Count - 1);
                
                pointOnObjRef = null;

            }

            if (drawnType != DrawnType.In3D && drawnType != DrawnType.None)
            {
                UtilOld.showLaser(ref mScene, true);
                if (drawnType == DrawnType.Plane)
                {
                    UtilOld.setPlaneAlpha(ref mScene, 0.4f);
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
                Material.Material m = new Material.SingleColorMaterial(1, 1, 1, 0);
                drawPoint = new SceneNode("drawPoint", ref geo, ref m);
                UtilOld.addSceneNode(ref mScene, ref drawPoint);

            }else
            {
                UtilOld.showLaser(ref mScene, false);
            }

        }


        public override void draw(bool isTop)
        {
            if (!isTop)
            {
                return;
            }


            OpenTK.Vector4 controller_p = UtilOld.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, 0, 1);
            OpenTK.Vector4 controller_pZ = UtilOld.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, -1, 1);
            Point3d controller_pRhino = UtilOld.openTkToRhinoPoint(UtilOld.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)));
            Point3d controller_pZRhin = UtilOld.openTkToRhinoPoint(UtilOld.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_pZ.X, controller_pZ.Y, controller_pZ.Z)));
            Rhino.Geometry.Vector3d direction = new Rhino.Geometry.Vector3d(controller_pZRhin.X - controller_pRhino.X, controller_pZRhin.Y - controller_pRhino.Y, controller_pZRhin.Z - controller_pRhino.Z);

            if (drawnType != DrawnType.In3D)
            {
                UtilOld.rayCasting(controller_pRhino, direction, ref rayCastingObjs, ref projectP, out targetPRhObjID);
            }
            else
            {
                projectP = controller_pRhino;
            }

            //TODO-only snap for the first drawing point
            if(pointMarkers.Count == 0 && targetPRhObjID != Guid.Empty)
            {
                computeContourCurve();
                UtilOld.snapToPoints(ref projectP, ref snapPointsList);
            }       

            Vector3 projectPVR = UtilOld.platformToVRPoint(ref mScene, UtilOld.RhinoToOpenTKPoint(projectP));
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
                OpenTK.Vector3 controllerVector = UtilOld.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)) - UtilOld.RhinoToOpenTKPoint(moveControlerOrigin);
             
                float translate = OpenTK.Vector3.Dot(controllerVector, UtilOld.RhinoToOpenTKVector(planeNormal)) / (float)planeNormal.Length;
                float relTranslate = translate - lastTranslate;
                lastTranslate = translate;

                Matrix4 transM = Matrix4.CreateTranslation(new Vector3(relTranslate * (float)planeNormal.X, relTranslate * (float)planeNormal.Y, relTranslate * (float)planeNormal.Z));
                transM.Transpose();
                UtilOld.updateRhinoObjectSceneNode(ref mScene, ref movePlaneRef, UtilOld.OpenTKToRhinoTransform(transM));
                
            }

        }

        protected override void onClickOculusTrigger(ref VREvent_t vrEvent)
        {

            primaryDeviceIndex = vrEvent.trackedDeviceIndex;
            if (currentState != State.READY || targetPRhObjID == Guid.Empty)
            {
                return;
            }

            pointOnObjRef = new ObjRef(targetPRhObjID);
            //chage to only raycasting to the obj where we draw, if not snap to the origin
            if (!(projectP.X == 0 && projectP.Y == 0 && projectP.Z == 0))
            {
                rayCastingObjs.Clear();
                rayCastingObjs.Add(pointOnObjRef);
            }

            //testing
            Vector3 projectPVR = UtilOld.platformToVRPoint(ref mScene, UtilOld.RhinoToOpenTKPoint(projectP));
            mScene.iPointList.Add(UtilOld.platformToVRPoint(ref mScene, projectPVR)); //not use at the current version, point is VR coordinate
            pointsList.Add(projectP);

            //render edit points in VR
            Geometry.Geometry geo = new Geometry.DrawPointMarker(new Vector3(0, 0, 0));
            Material.Material m = new Material.SingleColorMaterial(0, 1, 0, 1);
            SceneNode editPointSN;
            UtilOld.MarkPointVR(ref mScene, projectPVR, ref geo, ref m, out editPointSN);
            pointMarkers.Add(editPointSN);

            //TODO-hide two other design plane
            if (pointMarkers.Count == 1)
            {
                if (pointOnObjRef != null && drawnType == DrawnType.Plane)
                {
                    if (!(projectP.X == 0 && projectP.Y == 0 && projectP.Z == 0))
                    {
                        UtilOld.hideOtherPlanes(ref mScene, pointOnObjRef.Object().Attributes.Name);                  
                    }
                    computeContourCurve();
                }
                else if (pointOnObjRef != null && drawnType == DrawnType.Surface)
                {
                    computeContourCurve();
                }
            }

            if (maxNumPoint == pointMarkers.Count)
            {
                //Assume we always can find a curvePlane sicnce we use a huge tolerance
                NurbsCurve modelcurve = null;
                Brep modelBrep;
                string modelName = "";
                if (shapeType == ShapeType.Circle)
                {
                    float radius = (float)Math.Sqrt(Math.Pow(pointsList[1].X - pointsList[0].X, 2) + Math.Pow(pointsList[1].Y - pointsList[0].Y, 2) + Math.Pow(pointsList[1].Z - pointsList[0].Z, 2));
                    Circle circle = new Rhino.Geometry.Circle(curvePlane, pointsList[0], radius);
                    modelcurve = circle.ToNurbsCurve();
                    modelName = "Circle";

                }
                else if (shapeType == ShapeType.Rect)
                {

                    Vector3 rectDiagonalV = new Vector3((float)(pointsList[0].X - pointsList[1].X), (float)(pointsList[0].Y - pointsList[1].Y), (float)(pointsList[0].Z - pointsList[1].Z));
                    float lenDiagonal = rectDiagonalV.Length;
                    Vector3 rectLeftTop = new Vector3((float)pointsList[0].X, (float)pointsList[0].Y, (float)pointsList[0].Z) + lenDiagonal * rectDiagonalV.Normalized();
                    Point3d topLeftP = new Point3d(rectLeftTop.X, rectLeftTop.Y, rectLeftTop.Z);

                    Rectangle3d rect = new Rectangle3d(curvePlane, topLeftP, pointsList[1]);

                    //using top-left cornel and bottom right

                    //Rectangle3d rect = new Rectangle3d(curvePlane, pointsList[0], pointsList[1]);

                    modelcurve = rect.ToNurbsCurve();
                    modelName = "Rect";

                }

                /*
                Brep[] shapes = Brep.CreatePlanarBreps(modelcurve);
                modelBrep = shapes[0];
                Guid guid = Util.addRhinoObjectSceneNode(ref mScene, ref modelBrep, ref profile_m, modelName, out renderObjSN);
                */            

                //generate new curveOnObj for mvoingPlane cases sicne and move the XYZPlanes to origial positons
                if (drawnType == DrawnType.Plane)
                {
                    Rhino.Geometry.Vector3d newNormal = new Rhino.Geometry.Vector3d(); ;
                    Point3d newOrigin = new Point3d();
                    String planeName = pointOnObjRef.Object().Attributes.Name;
                    Point3d planeOrigin = pointOnObjRef.Object().Geometry.GetBoundingBox(true).Center;
                    if (planeName.Contains("planeXY"))
                    {
                        newNormal = new Rhino.Geometry.Vector3d(0, 0, 1);
                        newOrigin = new Point3d(0, 0, planeOrigin.Z);

                    }
                    else if (planeName.Contains("planeYZ"))
                    {
                        newNormal = new Rhino.Geometry.Vector3d(1, 0, 0);
                        newOrigin = new Point3d(planeOrigin.X, 0, 0);

                    }
                    else if( planeName.Contains("planeXZ"))
                    {
                        newNormal = new Rhino.Geometry.Vector3d(0, 1, 0);
                        newOrigin = new Point3d(0, planeOrigin.Y, 0);
                    }

                    Plane newPlane = new Plane(newOrigin, newNormal);
                    int size = 240;
                    PlaneSurface plane_surface = new PlaneSurface(newPlane, new Interval(-size, size), new Interval(-size, size));
                    Brep newPlaneBrep = Brep.CreateFromSurface(plane_surface);

                    Guid newPlaneID = UtilOld.addRhinoObject(ref mScene, ref newPlaneBrep, "MoveP");
                    //might be better to use Replace(), just need to be careful about the referece count
                    pointOnObjRef = null;
                    pointOnObjRef = new ObjRef(newPlaneID);

                    modelcurve.SetUserString(CurveData.CurveOnObj.ToString(), newPlaneID.ToString());
                    modelcurve.SetUserString(CurveData.PlaneOrigin.ToString(), newPlane.Origin.ToString());
                    modelcurve.SetUserString(CurveData.PlaneNormal.ToString(), newPlane.Normal.ToString());

                }
                else if (drawnType == DrawnType.Surface)
                {
                    modelcurve.SetUserString(CurveData.CurveOnObj.ToString(), pointOnObjRef.ObjectId.ToString());
                    modelcurve.SetUserString(CurveData.PlaneOrigin.ToString(), curvePlane.Origin.ToString());
                    modelcurve.SetUserString(CurveData.PlaneNormal.ToString(), curvePlane.Normal.ToString());
                }

                mScene.iCurveList.Add(modelcurve);

                //call next interaction in the chain
                afterCurveCount = mScene.iCurveList.Count;
                mScene.pushInteractionFromChain();
                currentState = State.READY;


            }
        }

        protected override void onReleaseOculusTrigger(ref VREvent_t vrEvent)
        {
           
        }

        private void clearDrawingLeaveTop()
        {
            if (drawnType == DrawnType.Plane)
            {
                //resetPlane
                mScene.xyPlane.resetOrgin();
                mScene.yzPlane.resetOrgin();
                mScene.xzPlane.resetOrgin();
                UtilOld.setPlaneAlpha(ref mScene, 0.0f);
            }

            for (int i = 0; i < pointMarkers.Count; i++)
            {
                SceneNode sn = pointMarkers[i];
                UtilOld.removeSceneNode(ref mScene, ref sn);
            }

            pointMarkers.Clear();
            pointsList.Clear();

            //clear drawPoint
            UtilOld.removeSceneNode(ref mScene, ref drawPoint);
          
        }

        private void clearDrawingPop()
        {
            if (drawnType == DrawnType.Plane)
            {
                //resetPlane
                mScene.xyPlane.resetOrgin();
                mScene.yzPlane.resetOrgin();
                mScene.xzPlane.resetOrgin();
                UtilOld.setPlaneAlpha(ref mScene, 0.0f);
            }

            for (int i = 0; i < pointMarkers.Count; i++)
            {
                SceneNode sn = pointMarkers[i];
                UtilOld.removeSceneNode(ref mScene, ref sn);
            }

            pointMarkers.Clear();
            pointsList.Clear();

            //clear drawPoint
            UtilOld.removeSceneNode(ref mScene, ref drawPoint);

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

            OpenTK.Vector4 controller_p = UtilOld.getControllerTipPosition(ref mScene, primaryControllerIdx == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, 0, 1);
            OpenTK.Vector3 controllerVector = UtilOld.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z));

            float translate = OpenTK.Vector3.Dot(controllerVector, UtilOld.RhinoToOpenTKVector(planeNormal)) / (float)planeNormal.Length;
            //move from the porjection point not origin
            moveControlerOrigin = new Point3d(0 + translate * planeNormal.X, 0 + translate * planeNormal.Y, 0 + translate * planeNormal.Z);

        }

        protected override void onReleaseOculusGrip(ref VREvent_t vrEvent)
        {
            currentState = State.READY;
            movePlaneRef = null;

        }



        private void computeContourCurve()
        {
            if(targetPRhObjID == Guid.Empty)
            {
                return;
            }
            ObjRef targetpObjRef = new ObjRef(targetPRhObjID);
            snapPointsList = new List<Point3d>();
            Double tolerance = 0;
            if (drawnType != DrawnType.In3D)
            {
                Brep targetBrep = (Brep)(targetpObjRef.Object().Geometry);
                //TODO- topLeftP won't be on the face in the 3D case. so probably use orgin
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

                //testing finding the edge curve
                Rhino.Geometry.Curve[] edgeCurves = (targetBrep.Faces[minIndex].DuplicateFace(false)).DuplicateEdgeCurves(true);
                double tol = mScene.rhinoDoc.ModelAbsoluteTolerance * 2.1;
                edgeCurves = Rhino.Geometry.Curve.JoinCurves(edgeCurves, tol);

                // TODO: Check if null
                contourCurve = edgeCurves[0];

                //detect whether it's rect or circle then generate a snap pointList
                Circle circle;
                Rhino.Geometry.Polyline polyline;
                if (contourCurve.TryGetCircle(out circle))
                {
                    snapPointsList.Add(circle.Center);
                    snapPointsList.Add(circle.PointAt(0));
                    snapPointsList.Add(circle.PointAt(Math.PI / 2));
                    snapPointsList.Add(circle.PointAt(Math.PI));
                    snapPointsList.Add(circle.PointAt(Math.PI * 1.5));

                }
                else if (contourCurve.TryGetPolyline(out polyline))
                {
                    Rectangle3d rect = Rectangle3d.CreateFromPolyline(polyline);
                    snapPointsList.Add(rect.Center);
                    snapPointsList.Add(rect.Corner(0));
                    snapPointsList.Add(rect.Corner(1));
                    snapPointsList.Add(rect.Corner(2));
                    snapPointsList.Add(rect.Corner(3));
                }
                else
                {
                    double u = 0;
                    double v = 0;
                    s.ClosestPoint(s.GetBoundingBox(true).Center, out u, out v);
                    snapPointsList.Add(s.PointAt(u, v));
                }
            }


            


        }




    }
}