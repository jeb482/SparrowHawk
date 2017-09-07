using OpenTK;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    class AddPoint : Interaction
    {
        //0:3D, 1:onDPlanes, 2: onSurfaces, 3: onTargets
        public int type = 0;
        public int maxNumPoint = 100;
        public List<Guid> ListTargets = new List<Guid>();
        protected Geometry.Geometry point_g;
        protected Material.Material point_m;

        private bool hitPlane = false;
        private bool lockPlane = false;
        protected SceneNode targetPSN;
        protected RhinoObject targetPRhObj;
        protected SceneNode drawPoint;
        protected OpenTK.Vector3 projectP;
        Vector3 pos = new Vector3();
        protected RhinoObject pointOnObj;
        private string renderType = "none";
        private List<Point3d> pointsList = new List<Point3d>();

        List<SceneNode> pointMarkers = new List<SceneNode>();
        private Material.Material profile_m;

        private bool isMove = false;
        private Brep designPlane;
        private Material.Material plane_m;
        private Point3d endP;
        Double tolerance = 0;
        private Plane curvePlane;
        private Rhino.Geometry.Curve contourCurve = null;
        private List<Vector3> snapPointsList = new List<Vector3>();

        private OpenTK.Vector3 planeNormal;
        private RhinoObject movePlane;
        private Point3d moveControlerOrigin = new Point3d();
        private Point3d movePlaneOrigin = new Point3d();
        private Guid movePlaneRenderID = Guid.Empty;

        public AddPoint(ref Scene scene) : base(ref scene)
        {
            mScene = scene;
            point_g = new Geometry.PointMarker(new Vector3());
            point_m = new Material.SingleColorMaterial(0f, .5f, 1f, 1f);
        }

        public AddPoint(ref Scene scene, int _type) : base(ref scene)
        {
            mScene = scene;
            point_g = new Geometry.PointMarker(new Vector3());
            point_m = new Material.SingleColorMaterial(0f, .5f, 1f, 1f);
            type = _type;
            if (type != 0)
            {
                // visualizing projection point with white color
                drawPoint = Util.MarkProjectionPoint(ref mScene, new OpenTK.Vector3(0, 0, 0), 1, 1, 1);
            }
        }

        public AddPoint(ref Scene scene, int _type, int num) : base(ref scene)
        {
            mScene = scene;
            point_g = new Geometry.PointMarker(new Vector3());
            point_m = new Material.SingleColorMaterial(0f, .5f, 1f, 1f);
            type = _type;
            maxNumPoint = num;
            if (type != 0)
            {
                // visualizing projection point with white color
                drawPoint = Util.MarkProjectionPoint(ref mScene, new OpenTK.Vector3(0, 0, 0), 1, 1, 1);
                contourCurve = null;
                snapPointsList.Clear();
            }
        }

        public AddPoint(ref Scene scene, int _type, int num, string rtype) : base(ref scene)
        {
            mScene = scene;
            point_g = new Geometry.PointMarker(new Vector3());
            point_m = new Material.SingleColorMaterial(0f, .5f, 1f, 1f);
            type = _type;
            renderType = rtype;
            profile_m = new Material.SingleColorMaterial(0.5f, 0, 0, 0.4f);
            maxNumPoint = num;
            if (type != 0)
            {
                // visualizing projection point with white color
                drawPoint = Util.MarkProjectionPoint(ref mScene, new OpenTK.Vector3(0, 0, 0), 1, 1, 1);
                contourCurve = null;
                snapPointsList.Clear();
            }
        }

        public override void init()
        {
            if ((type == 3) && mScene.iRhObjList.Count != 0)
            {
                foreach (Rhino.DocObjects.RhinoObject RhObj in mScene.iRhObjList)
                {
                    ListTargets.Add(RhObj.Id);
                }

            }
        }

        public override void draw(bool isTop)
        {
            //visualize the point on the plane for type = 1, 2, 3
            if (type != 0 && isTop)
            {
                //ray casting to the pre-defind planes
                OpenTK.Vector4 controller_p = Util.getControllerTipPosition(ref mScene, primaryControllerIdx == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, 0, 1);
                OpenTK.Vector4 controller_pZ = Util.getControllerTipPosition(ref mScene, primaryControllerIdx == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, -1, 1);
                Point3d controller_pRhino = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)));
                Point3d controller_pZRhin = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_pZ.X, controller_pZ.Y, controller_pZ.Z)));

                Rhino.Geometry.Vector3d direction = new Rhino.Geometry.Vector3d(controller_pZRhin.X - controller_pRhino.X, controller_pZRhin.Y - controller_pRhino.Y, controller_pZRhin.Z - controller_pRhino.Z);
                Ray3d ray = new Ray3d(controller_pRhino, direction);

                Rhino.DocObjects.ObjectEnumeratorSettings settings = new Rhino.DocObjects.ObjectEnumeratorSettings();
                settings.ObjectTypeFilter = Rhino.DocObjects.ObjectType.Brep;
                //settings.NameFilter = "plane";
                float mimD = 1000000f;
                hitPlane = false;
                //lock the active plane when users start drawing
                if (!lockPlane)
                {
                    foreach (Rhino.DocObjects.RhinoObject rhObj in mScene.rhinoDoc.Objects.GetObjectList(settings))
                    {
                        //check for different drawing curve types
                        bool b1 = (type == 1) && rhObj.Attributes.Name.Contains("plane");
                        bool b2 = (type == 2) && (rhObj.Attributes.Name.Contains("brepMesh") || rhObj.Attributes.Name.Contains("aprint") || rhObj.Attributes.Name.Contains("patchSurface"));
                        bool b3 = (type == 3) && ListTargets.Contains(rhObj.Id);

                        //only drawing on planes for now rhObj.Attributes.Name.Contains("brepMesh") || rhObj.Attributes.Name.Contains("aprint") || rhObj.Attributes.Name.Contains("plane")
                        //if (rhObj.Attributes.Name.Contains("plane"))
                        if (b1 || b2 || b3)
                        {
                            List<GeometryBase> geometries = new List<GeometryBase>();
                            geometries.Add(rhObj.Geometry);
                            //must be a brep or surface, not mesh
                            Point3d[] rayIntersections = Rhino.Geometry.Intersect.Intersection.RayShoot(ray, geometries, 1);
                            if (rayIntersections != null)
                            {
                                //get the nearest one
                                OpenTK.Vector3 tmpP = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)rayIntersections[0].X, (float)rayIntersections[0].Y, (float)rayIntersections[0].Z));
                                float distance = (float)Math.Sqrt(Math.Pow(tmpP.X - controller_p.X, 2) + Math.Pow(tmpP.Y - controller_p.Y, 2) + Math.Pow(tmpP.Z - controller_p.Z, 2));

                                if (distance < mimD)
                                {
                                    hitPlane = true;
                                    targetPSN = mScene.brepToSceneNodeDic[rhObj.Id];
                                    targetPRhObj = rhObj;
                                    mimD = distance;
                                    projectP = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)rayIntersections[0].X, (float)rayIntersections[0].Y, (float)rayIntersections[0].Z));
                                }

                            }

                        }
                    }

                    if (isMove)
                    {
                        //OpenTK.Vector4 controller_p = Util.getControllerTipPosition(ref mScene, primaryControllerIdx == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, 0, 1);
                        //Point3d controller_pRhino = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)));

                        OpenTK.Vector3 controllerVector = Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)) - Util.RhinoToOpenTKPoint(moveControlerOrigin);

                        float translate = OpenTK.Vector3.Dot(controllerVector, planeNormal) / planeNormal.Length;
                        //move from the porjection point not origin
                        endP = new Point3d(movePlaneOrigin.X + translate * planeNormal.X, movePlaneOrigin.Y + translate * planeNormal.Y, movePlaneOrigin.Z + translate * planeNormal.Z);

                        Plane newPlane = new Plane(endP, new Rhino.Geometry.Vector3d(planeNormal.X, planeNormal.Y, planeNormal.Z));
                        int size = 240;
                        PlaneSurface plane_surface = new PlaneSurface(newPlane, new Interval(-size, size), new Interval(-size, size));
                        designPlane = Brep.CreateFromSurface(plane_surface);

                        movePlaneRenderID =Util.addSceneNodeWithoutDraw(ref mScene, designPlane, ref plane_m, "Move-" + movePlane.Attributes.Name);

                    }

                }
                else
                {
                    if (targetPRhObj != null)
                    {
                        List<GeometryBase> geometries = new List<GeometryBase>();
                        geometries.Add(targetPRhObj.Geometry);
                        //must be a brep or surface, not mesh
                        Point3d[] rayIntersections = Rhino.Geometry.Intersect.Intersection.RayShoot(ray, geometries, 1);
                        if (rayIntersections != null)
                        {
                            //get the nearest one
                            OpenTK.Vector3 tmpP = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)rayIntersections[0].X, (float)rayIntersections[0].Y, (float)rayIntersections[0].Z));
                            float distance = (float)Math.Sqrt(Math.Pow(tmpP.X - controller_p.X, 2) + Math.Pow(tmpP.Y - controller_p.Y, 2) + Math.Pow(tmpP.Z - controller_p.Z, 2));

                            if (distance < mimD)
                            {
                                hitPlane = true;
                                mimD = distance;
                                projectP = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)rayIntersections[0].X, (float)rayIntersections[0].Y, (float)rayIntersections[0].Z));
                            }
                        }

                        //move the plane if user needs
                        if (isMove)
                        {
                            //OpenTK.Vector4 controller_p = Util.getControllerTipPosition(ref mScene, primaryControllerIdx == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, 0, 1);
                            //Point3d controller_pRhino = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)));

                            OpenTK.Vector3 controllerVector = Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)) - Util.RhinoToOpenTKPoint(moveControlerOrigin);

                            float translate = OpenTK.Vector3.Dot(controllerVector, planeNormal) / planeNormal.Length;
                            endP = new Point3d(movePlaneOrigin.X + translate * planeNormal.X, movePlaneOrigin.Y + translate * planeNormal.Y, movePlaneOrigin.Z + translate * planeNormal.Z);

                            Plane newPlane = new Plane(endP, new Rhino.Geometry.Vector3d(planeNormal.X, planeNormal.Y, planeNormal.Z));
                            int size = 240;
                            PlaneSurface plane_surface = new PlaneSurface(newPlane, new Interval(-size, size), new Interval(-size, size));
                            designPlane = Brep.CreateFromSurface(plane_surface);

                            movePlaneRenderID = Util.addSceneNodeWithoutDraw(ref mScene, designPlane, ref plane_m, "Move-" + movePlane.Attributes.Name);

                        }
                    }
                }

                if (!hitPlane)
                {
                    if (!lockPlane)
                    {
                        targetPSN = null;
                        targetPRhObj = null;
                    }
                    projectP = new OpenTK.Vector3(100, 100, 100); //make it invisable
                }

                pointOnObj = targetPRhObj;

                if (hitPlane && type != 0)
                {
                    //create contour curve and snap points
                    if(!lockPlane)
                        computeContourCurve();
                    projectP = snapToPoints(projectP, snapPointsList);

                }

                //visualize the projection points
                // inverted rotation first

                OpenTK.Matrix4 t = OpenTK.Matrix4.CreateTranslation(Util.transformPoint(mScene.tableGeometry.transform.Inverted(), projectP));
                t.Transpose();
                drawPoint.transform = t;

                pos = projectP;
            }
            else
            {
                pos = Util.getTranslationVector3(Util.getControllerTipPosition(ref mScene, primaryControllerIdx == mScene.leftControllerIdx));
            }
        }

        public void clearDrawing()
        {
            //clear the curve and points
            if (mScene.tableGeometry.children.Count > 0)
            {
                // need to remove rerverse since the list update dynamically
                foreach (SceneNode sn in mScene.tableGeometry.children.Reverse<SceneNode>())
                {
                    if (sn.name == "drawPoint" || sn.name == "EditPoint")
                    {
                        mScene.tableGeometry.children.Remove(sn);
                    }
                }
            }

            pointsList.Clear();
        }


        private Vector3 snapToPoints(Vector3 projectP, List<Vector3> pointsList)
        {
            bool snap = false;
            foreach (Vector3 p in pointsList)
            {
                //snap to origin
                if (Math.Sqrt(Math.Pow(projectP.X - p.X, 2) + Math.Pow(projectP.Y - p.Y, 2) + Math.Pow(projectP.Z - p.Z, 2)) < 0.01)
                {
                    projectP = p;
                    snap = true;
                    break;
                }
            }

            return projectP;
        }

        private void computeContourCurve()
        {
            snapPointsList = new List<Vector3>();
            //detecting plane
            Brep targetBrep = (Brep)(pointOnObj.Geometry);
            //compute the brepFace where the curve is on
            //Surface s = targetBrep.Faces[0];

            //TODO- topLeftP won't be on the face in the 3D case. so probably use orgin
            Point3d projectPRhino = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, projectP));
            int faceIndex = -1;
            for (int i = 0; i < targetBrep.Faces.Count; i++)
            {
                //cast BrepFace to Brep for ClosestPoint(P) menthod, and there still isn't a PointList item so we use projectP
                double dist = targetBrep.Faces[i].DuplicateFace(false).ClosestPoint(projectPRhino).DistanceTo(projectPRhino);
                //debuging mScene.rhinoDoc.ModelAbsoluteTolerance                   
                if (dist < mScene.rhinoDoc.ModelAbsoluteTolerance)
                {
                    faceIndex = i;
                    break;
                }
            }
            Surface s = targetBrep.Faces[faceIndex];
            //testing finding the edge curve
            Rhino.Geometry.Curve[] edgeCurves = (targetBrep.Faces[faceIndex].DuplicateFace(false)).DuplicateEdgeCurves(true);
            double tol = mScene.rhinoDoc.ModelAbsoluteTolerance * 2.1;
            edgeCurves = Rhino.Geometry.Curve.JoinCurves(edgeCurves, tol);
            contourCurve = edgeCurves[0];
            //detect whether it's rect or circle then generate a snap pointList
            Circle circle;
            Rhino.Geometry.Polyline polyline;
            if (contourCurve.TryGetCircle(out circle))
            {
                snapPointsList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(circle.Center)));
                snapPointsList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(circle.PointAt(0))));
                snapPointsList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(circle.PointAt(Math.PI/2))));
                snapPointsList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(circle.PointAt(Math.PI))));
                snapPointsList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(circle.PointAt(Math.PI * 1.5))));

            }
            else if (contourCurve.TryGetPolyline(out polyline))
            {
                Rectangle3d rect = Rectangle3d.CreateFromPolyline(polyline);
                snapPointsList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(rect.Center)));
                snapPointsList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(rect.Corner(0))));
                snapPointsList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(rect.Corner(1))));
                snapPointsList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(rect.Corner(2))));
                snapPointsList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(rect.Corner(3))));
            }
            else
            {
                double u = 0;
                double v = 0;
                s.ClosestPoint(s.GetBoundingBox(true).Center, out u, out v);
                snapPointsList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(s.PointAt(u, v))));
            }

            curvePlane = new Plane();
            //surface might not be a perfect planar surface
            tolerance = 0;
            while (tolerance < 100)
            {
                if (s.TryGetPlane(out curvePlane, tolerance))
                {
                    break;
                }
                tolerance++;
            }
        }

        private void createCustomPlane(string type, Point3d origin)
        {
            Guid planeId = Guid.Empty;
            int size = 240;
            Plane plane = new Plane();
            if (type == "YZ")
            {
                plane = new Plane(origin, new Rhino.Geometry.Vector3d(1, 0, 0));
            }
            else if (type == "XZ")
            {
                plane = new Plane(origin, new Rhino.Geometry.Vector3d(0, 1, 0));
            }
            else if (type == "XY")
            {
                plane = new Plane(origin, new Rhino.Geometry.Vector3d(0, 0, 1));
            }

            PlaneSurface plane_surface = new PlaneSurface(plane, new Interval(-size, size), new Interval(-size, size));
            designPlane = Brep.CreateFromSurface(plane_surface);

            if (designPlane != null)
            {
                planeId =Util.addSceneNodeWithoutVR(ref mScene, designPlane, "plane" + type);
            }



        }

        protected override void onClickOculusTrigger(ref VREvent_t vrEvent)
        {

            if (projectP.X == 100 && projectP.Y == 100 && projectP.Z == 100)
                return;

            lockPlane = true;

            //testing
            mScene.iPointList.Add(pos); //not using iPointList anymore but added here since EditPoint3 still use
            pointsList.Add(Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, pos)));

            //TODO- figure out why here need mScene.tableGeometry.transform.Inverted() but others don't
            OpenTK.Vector3 p = Util.transformPoint(mScene.tableGeometry.transform.Inverted(), pos);
            SceneNode sn = Util.MarkPointSN(ref mScene.tableGeometry, p, 0, 1, 0);
            pointMarkers.Add(sn);

            //TODO-hide two other design plane
            if (pointMarkers.Count == 1)
            {
                if (pointOnObj != null && type == 1)
                {
                    Util.hideOtherPlanes(ref mScene, pointOnObj.Attributes.Name);
                    computeContourCurve();
                }
                else if (pointOnObj != null && type == 2)
                {
                    computeContourCurve();
                }
            }


            if (maxNumPoint == pointMarkers.Count)
            {

                //tolerance < 100 == finding a cvurve plane
                if (tolerance < 100)
                {
                    NurbsCurve modelcurve = null;
                    Brep modelBrep;

                    if (renderType == "Circle")
                    {
                        float radius = (float)Math.Sqrt(Math.Pow(pointsList[1].X - pointsList[0].X, 2) + Math.Pow(pointsList[1].Y - pointsList[0].Y, 2) + Math.Pow(pointsList[1].Z - pointsList[0].Z, 2));
                        Circle circle = new Rhino.Geometry.Circle(curvePlane, pointsList[0], radius);
                        modelcurve = circle.ToNurbsCurve();

                    }
                    else if (renderType == "Rect")
                    {
                        /*
                        Vector3 rectDiagonalV = new Vector3((float)(pointsList[0].X - pointsList[1].X), (float)(pointsList[0].Y - pointsList[1].Y), (float)(pointsList[0].Z - pointsList[1].Z));
                        float lenDiagonal = rectDiagonalV.Length;
                        Vector3 rectLeftTop = new Vector3((float)pointsList[0].X, (float)pointsList[0].Y, (float)pointsList[0].Z) + lenDiagonal * rectDiagonalV.Normalized();
                        Point3d topLeftP = new Point3d(rectLeftTop.X, rectLeftTop.Y, rectLeftTop.Z);
                        
                        */

                        //using top-left cornel and bottom right

                        Rectangle3d rect = new Rectangle3d(curvePlane, pointsList[0], pointsList[1]);

                        modelcurve = rect.ToNurbsCurve();

                    }

                    Brep[] shapes = Brep.CreatePlanarBreps(modelcurve);
                    modelBrep = shapes[0];
                    Guid renderObjId = Util.addSceneNode(ref mScene, modelBrep, ref profile_m, renderType);
                    //add icurveList since we don't use EditPoint2 for circle and rect
                    mScene.iCurveList.Add(modelcurve);
                    //mScene.iPlaneList.Add(ref curvePlane);
                }


                if (type != 0 && pointOnObj != null)
                {
                    mScene.iRhObjList.Add(pointOnObj); //pointOnObj is new plane after we move
                    mScene.iPlaneList.Add(curvePlane);

                }
                else
                {
                    curvePlane = new Plane(new Point3d(-100, -100, -100), new Rhino.Geometry.Vector3d(0, 0, 0));
                    mScene.iPlaneList.Add(curvePlane);
                }

                lockPlane = false;
                clearDrawing();

                //reset predefined plane position and delete the old one
                //need to careful about rhinoObjList. update it before we delete it
                //reset it;
                if (movePlane != null)
                {
                    DesignPlane3 tempXY = new DesignPlane3(ref mScene, 2);
                    DesignPlane3 tempYZ = new DesignPlane3(ref mScene, 0);
                    DesignPlane3 tempXZ = new DesignPlane3(ref mScene, 1);
                    string name = movePlane.Attributes.Name;

                    for (int i = 0; i < mScene.iRhObjList.Count; i++)
                    {
                        if (mScene.iRhObjList[i].Id == mScene.xyPlane.guid && !name.Contains("planeXY"))
                        {
                            mScene.iRhObjList[i] = mScene.rhinoDoc.Objects.Find(tempXY.guid);
                        }
                        else if (mScene.iRhObjList[i].Id == mScene.yzPlane.guid && !name.Contains("planeYZ"))
                        {
                            mScene.iRhObjList[i] = mScene.rhinoDoc.Objects.Find(tempYZ.guid);
                        }
                        else if (mScene.iRhObjList[i].Id == mScene.xzPlane.guid && !name.Contains("planeXZ"))
                        {
                            mScene.iRhObjList[i] = mScene.rhinoDoc.Objects.Find(tempXZ.guid);
                        }
                    }
                    if (name.Contains("planeXY"))
                    {
                        Util.removeStaticSceneNodeKeepRhio(ref mScene, mScene.xyPlane.guid);
                    }
                    else
                    {
                        Util.removeStaticSceneNode(ref mScene, mScene.xyPlane.guid);
                    }
                    if (name.Contains("planeYZ"))
                    {
                        Util.removeStaticSceneNodeKeepRhio(ref mScene, mScene.yzPlane.guid);
                    }
                    else
                    {
                        Util.removeStaticSceneNode(ref mScene, mScene.yzPlane.guid);
                    }

                    if (name.Contains("planeXZ"))
                    {
                        Util.removeStaticSceneNodeKeepRhio(ref mScene, mScene.xzPlane.guid);
                    }
                    else
                    {
                        Util.removeStaticSceneNode(ref mScene, mScene.xzPlane.guid);
                    }

                    mScene.xyPlane = tempXY;
                    mScene.yzPlane = tempYZ;
                    mScene.xzPlane = tempXZ;

                    Util.setPlaneAlpha(ref mScene, 0.0f);
                }

                mScene.popInteraction();
                if (!mScene.interactionStackEmpty())
                    mScene.peekInteraction().init();
            }
        }

        protected override void onReleaseOculusTrigger(ref VREvent_t vrEvent)
        {
            
        }

        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            if (!hitPlane)
                return;

            isMove = true;

            //get the plane info
            plane_m = new Material.SingleColorMaterial(0, 0, 0, 0.4f);
            planeNormal = new Vector3();
            //PointOnObject still null at this point
            if (targetPRhObj.Attributes.Name.Contains("planeXY"))
            {
                ((Material.SingleColorMaterial)plane_m).mColor.B = .5f;
                planeNormal = new OpenTK.Vector3(0, 0, 1);
                
            }
            else if (targetPRhObj.Attributes.Name.Contains("planeYZ"))
            {
                ((Material.SingleColorMaterial)plane_m).mColor.R = .5f;
                planeNormal = new OpenTK.Vector3(1, 0, 0);
            }
            else if (targetPRhObj.Attributes.Name.Contains("planeXZ"))
            {
                ((Material.SingleColorMaterial)plane_m).mColor.G = .5f;
                planeNormal = new OpenTK.Vector3(0, 1, 0);
            }

            planeNormal.Normalize();

            movePlane = targetPRhObj;

            OpenTK.Vector4 controller_p = Util.getControllerTipPosition(ref mScene, primaryControllerIdx == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, 0, 1);
            OpenTK.Vector3 controllerVector = Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)); //Rhino space
            float translate = OpenTK.Vector3.Dot(controllerVector, planeNormal) / planeNormal.Length;
            //move from the porjection point not origin
            moveControlerOrigin = new Point3d(0 + translate * planeNormal.X, 0 + translate * planeNormal.Y, 0 + translate * planeNormal.Z);

            OpenTK.Vector3 planeVector = Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(projectP.X, projectP.Y, projectP.Z));
            float translate2 = OpenTK.Vector3.Dot(planeVector, planeNormal) / planeNormal.Length;
            //move from the porjection point not origin
            movePlaneOrigin = new Point3d(0 + translate2 * planeNormal.X, 0 + translate2 * planeNormal.Y, 0 + translate2 * planeNormal.Z);


        }

        protected override void onReleaseOculusGrip(ref VREvent_t vrEvent)
        {
            isMove = false;
            string name = movePlane.Attributes.Name;
            Util.removeSceneNodeWithoutDraw(ref mScene, movePlaneRenderID);
            Util.removeStaticSceneNode(ref mScene, movePlane.Id);
            contourCurve = null;
            //Util.addSceneNode(ref mScene, designPlane, ref plane_m, pointOnObj.Attributes.Name);

            if (name.Contains("planeXY"))
            {
                mScene.xyPlane = new DesignPlane3(ref mScene, 2, endP);
                mScene.xyPlane.setAlpha(0.4f);
            }
            else if (name.Contains("planeYZ"))
            {
                mScene.yzPlane = new DesignPlane3(ref mScene, 0, endP);
                mScene.yzPlane.setAlpha(0.4f);
            }
            else if (name.Contains("planeXZ"))
            {
                mScene.xzPlane = new DesignPlane3(ref mScene, 1, endP);
                mScene.xzPlane.setAlpha(0.4f);
            }

        }

    }
}
