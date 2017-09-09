using OpenTK;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    class CreateCurve : Interaction
    {
        public enum State
        {
            READY = 0, PAINT = 1
        };

        protected State currentState;
        protected Geometry.Geometry stroke_g;
        protected Material.Material stroke_m;
        protected uint primaryDeviceIndex;
        protected Guid strokeId;
        protected List<Vector3> reducePoints = new List<Vector3>();

        // Pops this interaction of the stack after releasing stroke if true.
        bool mPopAfterStroke = false;

        //0:3D, 1:onDPlanes, 2: onSurfaces, 3: onTargets
        public int type = 0;
        public bool isClosed = false;
        public List<Guid> ListTargets = new List<Guid>(); //could be added in init() pass argument

        private bool hitPlane = false;
        private bool lockPlane = false;
        protected SceneNode targetPSN;
        protected RhinoObject targetPRhObj;
        protected SceneNode drawPoint;
        protected OpenTK.Vector3 projectP;

        //testing rhino curve
        private List<Point3d> rhinoCurvePoints = new List<Point3d>();
        private Rhino.Geometry.Curve rhinoCurve;
        private Plane proj_plane;
        private List<Point3d> simplifiedCurvePoints = new List<Point3d>();
        private Rhino.Geometry.NurbsCurve simplifiedCurve;
        private Rhino.Geometry.NurbsCurve editCurve; //for extrude
        protected RhinoObject curveOnObj;
        private Guid renderObjId;

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

        private List<Vector3> snapPointsList = new List<Vector3>();

        public CreateCurve(ref Scene scene) : base(ref scene)
        {
            stroke_g = new Geometry.GeometryStroke(ref mScene);
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            currentState = State.READY;
        }
        public CreateCurve(ref Scene scene, int _type, bool _isClosed) : base(ref scene)
        {
            mScene = scene;
            stroke_g = new Geometry.GeometryStroke(ref mScene);
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            currentState = State.READY;
            type = _type;
            isClosed = _isClosed;

            if (type != 0)
            {

                // visualizing projection point with white color
                drawPoint = Util.MarkProjectionPoint(ref mScene, new OpenTK.Vector3(0, 0, 0), 1, 1, 1);

            }

        }

        public CreateCurve(ref Scene scene, int _type, bool _isClosed, string renderType) : base(ref scene)
        {
            mScene = scene;
            stroke_g = new Geometry.GeometryStroke(ref mScene);
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            currentState = State.READY;
            type = _type;
            isClosed = _isClosed;
            dynamicRender = renderType;
            mesh_m = new Material.RGBNormalMaterial(0.5f);

            snapPointsList.Clear();

            /*
            if (type != 0)
            {

                // visualizing projection point with white color
                drawPoint = Util.MarkProjectionPoint(ref mScene, new OpenTK.Vector3(0, 0, 0), 1, 1, 1);

                if (type == 3)
                {
                    //render the object plane
                    float planeSize = 240;
                    PlaneSurface plane_surface2 = new PlaneSurface(mScene.iPlaneList[mScene.iPlaneList.Count - 1], new Interval(-planeSize, planeSize), new Interval(-planeSize, planeSize));
                    Brep railPlane2 = Brep.CreateFromSurface(plane_surface2);
                    Util.addSceneNode(ref mScene, railPlane2, ref mesh_m, "railPlane");

                    snapPointsList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(railPlane2.GetBoundingBox(true).Center)));
                }

            }
            d = new generateModel_Delegate(generateModel);
            */
        }

        public CreateCurve(ref Scene scene, uint devIndex) : base(ref scene)
        {
            stroke_g = new Geometry.GeometryStroke(ref scene);
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            currentState = State.READY;
            primaryDeviceIndex = devIndex;
        }

        public override void init()
        {

            if (type != 0)
            {
                // visualizing projection point with white color
                drawPoint = Util.MarkProjectionPoint(ref mScene, new OpenTK.Vector3(0, 0, 0), 1, 1, 1);

                if (type == 3)
                {
                    //render the object plane
                    float planeSize = 240;
                    PlaneSurface plane_surface2 = new PlaneSurface(mScene.iPlaneList[mScene.iPlaneList.Count - 1], new Interval(-planeSize, planeSize), new Interval(-planeSize, planeSize));
                    Brep railPlane2 = Brep.CreateFromSurface(plane_surface2);
                    Util.addSceneNode(ref mScene, railPlane2, ref mesh_m, "railPlane");

                    snapPointsList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(railPlane2.GetBoundingBox(true).Center)));
                }
            }

            d = new generateModel_Delegate(generateModel);
        }

        public override void draw(bool isTop)
        {

            //visualize the point on the plane for type = 1, 2, 3
            if (type != 0 && isTop)
            {
                //ray casting to the pre-defind planes
                OpenTK.Vector4 controller_p = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, 0, 1);
                OpenTK.Vector4 controller_pZ = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, -1, 1);
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
                        bool b3 = (type == 3) && rhObj.Attributes.Name.Contains("railPlane");

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
                                    // = mScene.brepToSceneNodeDic[rhObj.Id];
                                    targetPRhObj = rhObj;
                                    mimD = distance;
                                    projectP = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)rayIntersections[0].X, (float)rayIntersections[0].Y, (float)rayIntersections[0].Z));
                                }
                            }
                        }
                    }

                    projectP = snapToPoints(projectP, snapPointsList);
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
                    }
                }

                if (!hitPlane)
                {
                    if (!lockPlane)
                    {
                        //targetPSN = null;
                        targetPRhObj = null;
                    }
                    projectP = new OpenTK.Vector3(100, 100, 100); //make it invisable

                }

                //visualize the projection points
                // inverted rotation first

                OpenTK.Matrix4 t = OpenTK.Matrix4.CreateTranslation(Util.transformPoint(mScene.tableGeometry.transform.Inverted(), projectP));
                t.Transpose();
                drawPoint.transform = t;

                curveOnObj = targetPRhObj;
            }

            if (currentState != State.PAINT || !isTop)
            {
                return;
            }

            // drawing curve
            Vector3 pos = new Vector3();
            if (type != 0)
            {
                pos = projectP;
                if (hitPlane)
                {
                    //GeometryStroke handle rotation
                    ((Geometry.GeometryStroke)stroke_g).addPoint(pos);
                    rhinoCurvePoints.Add(Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, pos)));
                    //store the targeObj
                    //curveOnObj = targetPRhObj;
                }

            }
            else
            {
                pos = Util.getTranslationVector3(Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx));
                //GeometryStroke handle rotation already
                ((Geometry.GeometryStroke)stroke_g).addPoint(pos);
                rhinoCurvePoints.Add(Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, pos)));
            }

            if (((Geometry.GeometryStroke)stroke_g).mNumPrimitives == 1)
            {
                SceneNode stroke = new SceneNode("Stroke", ref stroke_g, ref stroke_m);
                mScene.tableGeometry.add(ref stroke);
                strokeId = stroke.guid;
            }

            //testing the performance of rhino curve and might be used for dynamically rendering
            if (rhinoCurvePoints.Count == 2)
            {
                rhinoCurve = Rhino.Geometry.Curve.CreateInterpolatedCurve(rhinoCurvePoints.ToArray(), 3);
            }
            else if (rhinoCurvePoints.Count > 2)
            {
                double length1 = rhinoCurve.GetLength();
                rhinoCurve = Rhino.Geometry.Curve.CreateInterpolatedCurve(rhinoCurvePoints.ToArray(), 3);
                double length2 = rhinoCurve.GetLength();
                displacement = displacement + (float)Math.Abs(length2 - length1);

                //TODO-Debug why it fail
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
                    mScene.iCurveList.Add(simplifiedCurve);
                    if (type != 0 && curveOnObj != null)
                    {
                        mScene.iRhObjList.Add(curveOnObj);
                    }
                }
                else
                {
                    mScene.iCurveList[0] = simplifiedCurve;
                    if (type != 0 && curveOnObj != null)
                    {
                        mScene.iRhObjList[mScene.iRhObjList.Count - 1] = curveOnObj;
                    }
                }

                dynamicBrep = Util.RevolveFunc(ref mScene, ref mScene.iCurveList);
            }
            else if (dynamicRender == "Loft")
            {
                if (mScene.iCurveList.Count == 1)
                {
                    mScene.iCurveList.Add(simplifiedCurve);
                    if (type != 0 && curveOnObj != null)
                    {
                        mScene.iRhObjList.Add(curveOnObj);
                    }
                }
                else
                {
                    mScene.iCurveList[1] = simplifiedCurve;
                    if (type != 0 && curveOnObj != null)
                    {
                        mScene.iRhObjList[mScene.iRhObjList.Count - 1] = curveOnObj;
                    }
                }

                dynamicBrep = Util.LoftFunc(ref mScene, ref mScene.iCurveList);
            }
            else if (dynamicRender == "Extrude")
            {
                //TODO-generate perendicular curve and find intersect point-following code duplicate in EditPoint2
                //compute the plane from RhinoObj
                RhinoObject newObj = mScene.rhinoDoc.Objects.Find(mScene.iRhObjList[mScene.iRhObjList.Count - 1].Id);
                Brep targetBrep = (Brep)(newObj.Geometry);

                Curve[] overlap_curves;
                Point3d[] inter_points;

                if (Intersection.CurveBrep(simplifiedCurve, targetBrep, mScene.rhinoDoc.ModelAbsoluteTolerance, out overlap_curves, out inter_points))
                {
                    if (overlap_curves.Length > 0 || inter_points.Length > 0)
                    {
                        //assume only one intersect point
                        //compute the brepFace where the intersection is on
                        int faceIndex = -1;
                        for (int i = 0; i < targetBrep.Faces.Count; i++)
                        {
                            //cast BrepFace to Brep for ClosestPoint(P) menthod
                            double dist = targetBrep.Faces[i].DuplicateFace(false).ClosestPoint(inter_points[0]).DistanceTo(inter_points[0]);
                            //tolerance mScene.rhinoDoc.ModelAbsoluteTolerance too low
                            if (dist < mScene.rhinoDoc.ModelAbsoluteTolerance)
                            {
                                faceIndex = i;
                                break;
                            }
                        }

                        List<Point3d> extrudeCurveP = new List<Point3d>();
                        extrudeCurveP.Add(inter_points[0]);
                        extrudeCurveP.Add(simplifiedCurve.PointAtEnd);
                        //update the edit curve
                        editCurve = Rhino.Geometry.NurbsCurve.Create(false, 1, extrudeCurveP.ToArray());

                        if (mScene.iCurveList.Count == 1)
                        {
                            mScene.iCurveList.Add(editCurve);
                            if (type != 0 && curveOnObj != null)
                            {
                                mScene.iRhObjList.Add(curveOnObj);
                            }
                        }
                        else
                        {
                            mScene.iCurveList[1] = editCurve;
                            if (type != 0 && curveOnObj != null)
                            {
                                mScene.iRhObjList[mScene.iRhObjList.Count - 1] = curveOnObj;
                            }
                        }

                        dynamicBrep = Util.ExtrudeFunc(ref mScene, ref mScene.iCurveList);

                    }else
                    {
                        //assume only one intersect point
                        //compute the brepFace where the intersection is on
                        int faceIndex = -1;
                        float mimD = 1000000f;
                        for (int i = 0; i < targetBrep.Faces.Count; i++)
                        {
                            //cast BrepFace to Brep for ClosestPoint(P) menthod
                            double dist = targetBrep.Faces[i].DuplicateFace(false).ClosestPoint(simplifiedCurve.PointAtStart).DistanceTo(simplifiedCurve.PointAtStart);
                            //tolerance mScene.rhinoDoc.ModelAbsoluteTolerance too low
                            if (dist < mimD)
                            {
                                mimD = (float)dist;
                                faceIndex = i;
                            }
                        }

                        List<Point3d> extrudeCurveP = new List<Point3d>();
                        extrudeCurveP.Add(((Surface)targetBrep.Faces[faceIndex]).GetBoundingBox(true).Center);
                        extrudeCurveP.Add(simplifiedCurve.PointAtEnd);
                        //update the edit curve
                        editCurve = Rhino.Geometry.NurbsCurve.Create(false, 1, extrudeCurveP.ToArray());

                        if (mScene.iCurveList.Count == 1)
                        {
                            mScene.iCurveList.Add(editCurve);
                            if (type != 0 && curveOnObj != null)
                            {
                                mScene.iRhObjList.Add(curveOnObj);
                            }
                        }
                        else
                        {
                            mScene.iCurveList[1] = editCurve;
                            if (type != 0 && curveOnObj != null)
                            {
                                mScene.iRhObjList[mScene.iRhObjList.Count - 1] = curveOnObj;
                            }
                        }

                        dynamicBrep = Util.ExtrudeFunc(ref mScene, ref mScene.iCurveList);
                    }

                }

                /*
                if (mScene.iCurveList.Count == 1)
                {
                    mScene.iCurveList.Add(simplifiedCurve);
                    if (type != 0 && curveOnObj != null)
                    {
                        mScene.iRhObjList.Add(curveOnObj);
                    }
                }
                else
                {
                    mScene.iCurveList[1] = simplifiedCurve;
                    if (type != 0 && curveOnObj != null)
                    {
                        mScene.iRhObjList[mScene.iRhObjList.Count - 1] = curveOnObj;
                    }
                }

                //TODO-using Sweep fnction to do and find the intersect point             
                dynamicBrep = Util.ExtrudeFunc(ref mScene, ref mScene.iCurveList);
                */
            }
            else if (dynamicRender == "Sweep")
            {
                if (mScene.iCurveList.Count == 1)
                {
                    mScene.iCurveList.Add(simplifiedCurve);
                    if (type != 0 && curveOnObj != null)
                    {
                        mScene.iRhObjList.Add(curveOnObj);
                    }
                }
                else
                {
                    mScene.iCurveList[1] = simplifiedCurve;
                    if (type != 0 && curveOnObj != null)
                    {
                        mScene.iRhObjList[mScene.iRhObjList.Count - 1] = curveOnObj;
                    }
                }

                dynamicBrep = Util.SweepFun(ref mScene, ref mScene.iCurveList);
            }
            /*
            else if (dynamicRender == "Sweep-Circle")
            {
                dynamicBrep = Util.SweepCapFun(ref mScene, ref mScene.iCurveList);
            }*/

        }

        public void modelCompleted(IAsyncResult R)
        {
            if (dynamicBrep != null)
            {
                if (modelName == "tprint")
                {
                    renderObjId = Util.addSceneNodeWithoutDraw(ref mScene, dynamicBrep, ref mesh_m, modelName);
                }
                /*
                else if (modelName == "aprint")
                {
                    renderObjId = Util.addSceneNode(ref mScene, dynamicBrep, ref mesh_m, modelName);

                    clearDrawing();
                    Util.clearPlanePoints(ref mScene);
                    Util.clearCurveTargetRhObj(ref mScene);
                }*/
            }
            dynamicBrep = null;
            backgroundStart = false;
            displacement = 0;

        }

        private Vector3 snapToPoints(Vector3 projectP, List<Vector3> pointsList)
        {
            bool snap = false;
            foreach (Vector3 p in pointsList)
            {
                //snap to origin
                if (Math.Sqrt(Math.Pow(projectP.X - p.X, 2) + Math.Pow(projectP.Y - p.Y, 2) + Math.Pow(projectP.Z - p.Z, 2)) < 0.02)
                {
                    projectP = p;
                    snap = true;
                    break;
                }
            }

            return projectP;
        }

        protected override void onClickOculusTrigger(ref VREvent_t vrEvent)
        {
            //Rhino.RhinoApp.WriteLine("oculus grip click event test");
            primaryDeviceIndex = vrEvent.trackedDeviceIndex;
            if (currentState == State.READY)
            {
                lockPlane = true;
                stroke_g = new Geometry.GeometryStroke(ref mScene);
                reducePoints = new List<Vector3>();
                currentState = State.PAINT;

                //hide two other design plane
                if (curveOnObj != null && type == 1)
                {
                    Util.hideOtherPlanes(ref mScene, curveOnObj.Attributes.Name);
                }

                //detecting plane
                Double tolerance = 0;
                Plane curvePlane = new Plane();
                if (type != 0)
                {
                    Brep targetBrep = (Brep)(curveOnObj.Geometry);

                    //TODO- topLeftP won't be on the face in the 3D case. so probably use orgin
                    Point3d projectPRhino = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, projectP));
                    int faceIndex = -1;
                    for (int i = 0; i < targetBrep.Faces.Count; i++)
                    {
                        //cast BrepFace to Brep for ClosestPoint(P) menthod
                        double dist = targetBrep.Faces[i].DuplicateFace(false).ClosestPoint(projectPRhino).DistanceTo(projectPRhino);
                        //debuging mScene.rhinoDoc.ModelAbsoluteTolerance                   
                        if (dist < mScene.rhinoDoc.ModelAbsoluteTolerance)
                        {
                            faceIndex = i;
                            break;
                        }
                    }
                    Surface s = targetBrep.Faces[faceIndex];
                    //surface might not be a perfect planar surface                     
                    while (tolerance < 100)
                    {
                        if (s.TryGetPlane(out curvePlane, tolerance))
                        {
                            break;
                        }
                        tolerance++;
                    }
                }

                //add plane to iPlaneList since Sweep fun need it's info
                if (tolerance < 100)
                {
                    //type 3 already add a plane
                    if(type != 3) 
                        mScene.iPlaneList.Add(curvePlane);
                }
                else
                {
                    curvePlane = new Plane(new Point3d(-100, -100, -100), new Rhino.Geometry.Vector3d(0, 0, 0));
                    mScene.iPlaneList.Add(curvePlane);
                }
            }

        }

        protected override void onReleaseOculusTrigger(ref VREvent_t vrEvent)
        {

            if (currentState == State.PAINT)
            {
                lockPlane = false;

                //simplfy the curve first before doing next interaction
                if (((Geometry.GeometryStroke)(stroke_g)).mPoints.Count >= 2)
                {

                    simplifyCurve(ref ((Geometry.GeometryStroke)(stroke_g)).mPoints);

                    //add to Scene curve object ,targetRhobj and check the next interaction

                   
                    if (dynamicRender == "none")
                    {
                        mScene.iCurveList.Add(simplifiedCurve);
                        if (type != 0 && curveOnObj != null)
                        {
                            mScene.iRhObjList.Add(curveOnObj);
                        }

                    }
                    else
                    {
                        //TODO-extrude curve isn't the simplifiedCurve
                        if (dynamicRender == "Extrude")
                        {
                            mScene.iCurveList[mScene.iCurveList.Count - 1] = editCurve;
                        }
                        else
                        {
                            mScene.iCurveList[mScene.iCurveList.Count - 1] = simplifiedCurve;
                        }

                        if (type != 0 && curveOnObj != null)
                        {
                            mScene.iRhObjList[mScene.iRhObjList.Count - 1] = curveOnObj;

                        }
                        
                    }

                    //go to editcurve interaction
                    clearDrawing();
                    mScene.popInteraction();
                    mScene.peekInteraction().init();

                    currentState = State.READY;
                    curveOnObj = null;

                }
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
                    if (sn.guid == strokeId)
                    {
                        mScene.tableGeometry.children.Remove(sn);

                    }
                    else if (sn.name == "drawPoint")
                    {
                        mScene.tableGeometry.children.Remove(sn);

                    }
                }
            }
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