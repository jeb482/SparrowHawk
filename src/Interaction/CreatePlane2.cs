using OpenTK;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    class CreatePlane2 : Interaction
    {
        private Material.Material mesh_m;
        private Brep designPlane;
        private Guid guid;
        private string renderType = "none";
        private SceneNode selectedSN;
        private Guid renderObjId = Guid.Empty;
        protected uint primaryDeviceIndex;

        protected int mCurrentSelection = -1;
        protected double mInitialSelectOKTime = 0;
        protected double mSelectOKTime = 0;
        double markingMenuFeedbackDelay = .2;
        double markingMenuSelectionDelay = .85f;
        double defaultInitialDelay = .2;
        float mMinSelectionRadius;
        float mOuterSelectionRadius;
        float mCurrentRadius;

        int mNumSectors = 4;
        float mFirstSectorOffsetAngle;
        int selectedSector = 0;
        int lastSector = 0;

        float radius = 20;
        float width = 40;
        float height = 30;

        private Plane modelPlane;
        private float planeSize = 240;
        private NurbsCurve modelcurve;
        private Brep modelBrep;
        private bool hitPlane = false;



        public CreatePlane2(ref Scene scene) : base(ref scene)
        {
            mesh_m = new Material.SingleColorMaterial(0.5f, 0, 0, 0.4f);
        }

        public CreatePlane2(ref Scene scene, string type) : base(ref scene)
        {
            mScene = scene;

            mesh_m = new Material.SingleColorMaterial(0.5f, 0.5f, 0, 0.4f);
            renderType = type;

            mNumSectors = 4;
            mFirstSectorOffsetAngle = getAngularMenuOffset(mNumSectors);
            mCurrentSelection = -1;

            if (scene.isOculus)
            {
                mMinSelectionRadius = 0.2f;
                mOuterSelectionRadius = 0.8f;
            }
            else
            {
                mMinSelectionRadius = 0.4f;
                mOuterSelectionRadius = 0.6f;
            }

        }

        public float getAngularMenuOffset(int numOptions)
        {
            if (numOptions <= 1) return 0;
            return (float)(-2 * Math.PI) / (2 * numOptions);
        }

        public override void activate()
        {

            // Set initial timeout that cannot be skipped to prevent double selections.
            mInitialSelectOKTime = mScene.gameTime + defaultInitialDelay;
        }

        public override void draw(bool isTop)
        {
            // get R and Theta and the associated sector
            float theta = 0;
            float mLastRadius = mCurrentRadius;
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

            }
            else
            {
                //trigger change size. discrete changing
                if (selectedSector != 0)
                {
                    changeSize(selectedSector);
                    selectedSector = 0;
                }
            }

            createModel();

        }

        private void changeSize(int sector)
        {
            //mScene.vibrateController(0.1, (uint)primaryControllerIdx);
            //Rhino.RhinoApp.WriteLine("sector: " + sector);
            if (renderType == "circle")
            {
                if (sector == 1)
                {
                    radius += 20;
                }
                else if (sector == 3)
                {
                    radius -= 20;
                    if (radius <= 0)
                    {
                        radius = 0;
                    }
                }
            }
            else if (renderType == "rect")
            {
                if (sector == 1)
                {
                    width += 20;
                }
                else if (sector == 3)
                {
                    width -= 20;
                    if (width <= 0)
                    {
                        width = 0;
                    }
                }
                else if (sector == 2)
                {
                    height += 20;
                }
                else if (sector == 4)
                {
                    height -= 20;
                    if (height <= 0)
                    {
                        height = 0;
                    }
                }
            }

        }

        public override void init()
        {

        }

        private void createModel()
        {
            /*
            Rhino.Geometry.Point3d origin = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(0, 0, 0)));
            Rhino.Geometry.Point3d normalP1 = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(0, 0, -0.05f)));
            Rhino.Geometry.Point3d normalP2 = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(0, 0, -1)));
            Rhino.Geometry.Vector3d normal = new Rhino.Geometry.Vector3d(normalP2.X - normalP1.X, normalP2.Y - normalP1.Y, normalP2.Z - normalP1.Z);
            Plane plane = new Plane(origin, normal);
            float radius = 20;
            Rhino.Geometry.Circle circle = new Rhino.Geometry.Circle(plane, origin, radius);
            NurbsCurve circleCurve = circle.ToNurbsCurve();

            //compute transform to follow controllers
            OpenTK.Matrix4 tansform = mScene.robotToPlatform * mScene.vrToRobot * Util.getControllerTipPosition(ref mScene, primaryControllerIdx == mScene.leftControllerIdx);
            Transform t = Util.OpenTKToRhinoTransform(tansform);
            circleCurve.Transform(t);
            Brep[] shapes = Brep.CreatePlanarBreps(circleCurve);
            Brep circle_s = shapes[0];
            Brep circleBrep = circle_s;

            Guid renderObjId = Util.addSceneNode(ref mScene, circleBrep, ref mesh_m, "circle");
            */


            //offset the point a little bit to make the plane better
            OpenTK.Vector4 controller_p = Util.getControllerTipPosition(ref mScene, primaryControllerIdx == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, -0.05f, 1);
            OpenTK.Vector4 controller_pZ = Util.getControllerTipPosition(ref mScene, primaryControllerIdx == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, -1, 1);
            Point3d controller_pRhino = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)));
            Point3d controller_pZRhin = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_pZ.X, controller_pZ.Y, controller_pZ.Z)));

            Rhino.Geometry.Vector3d normal = new Rhino.Geometry.Vector3d(controller_pZRhin.X - controller_pRhino.X, controller_pZRhin.Y - controller_pRhino.Y, controller_pZRhin.Z - controller_pRhino.Z);

            //project to xy plane in rhino
            modelPlane = new Plane(controller_pRhino, normal);

            if (renderType == "circle")
            {
                Rhino.Geometry.Circle circle = new Rhino.Geometry.Circle(modelPlane, controller_pRhino, radius);
                modelcurve = circle.ToNurbsCurve();

            }
            else if (renderType == "rect")
            {
                //Rectangle3d rect = new Rectangle3d(modelPlane, width, height);
                Rectangle3d rect = new Rectangle3d(modelPlane, new Interval(-width / 2, width / 2), new Interval(-height / 2, height / 2));
                modelcurve = rect.ToNurbsCurve();
            }

            Brep[] shapes = Brep.CreatePlanarBreps(modelcurve);
            modelBrep = shapes[0];

            renderObjId = Util.addSceneNodeWithoutDraw(ref mScene, modelBrep, ref mesh_m, "3D-" + renderType);

        }

        protected override void onClickOculusAX(ref VREvent_t vrEvent)
        {
            //TODO-support projection curve
            if (renderType != "projection")
            {
                Brep[] shapes = Brep.CreatePlanarBreps(modelcurve);
                modelBrep = shapes[0];
                //add plane to iRhobj
                renderObjId = Util.addSceneNode(ref mScene, modelBrep, ref mesh_m, "3D-" + renderType);
            }
            else
            {
                //update modelPlane and use tolerance to support unplanar surface
                Plane curvePlane = new Plane();
                Double tolerance = 0;
                while (tolerance < 400)
                {
                    if (modelcurve.TryGetPlane(out curvePlane, tolerance))
                    {
                        modelPlane = curvePlane;
                        break;
                    }
                    tolerance++;
                }
            }

            //creating perendicular plane
            //TODO-need to decide whether it's XAxis or YAxis as normal
            OpenTK.Vector3 worldUpAxis = new Vector3(0, 0, 1); //in Rhino z axis is up
            OpenTK.Vector3 planeXAxis = Util.RhinoToOpenTKPoint(modelPlane.XAxis);
            OpenTK.Vector3 planeYAxis = Util.RhinoToOpenTKPoint(modelPlane.YAxis);
            float xAngle = Math.Abs(OpenTK.Vector3.CalculateAngle(planeXAxis, worldUpAxis));
            float yAngle = Math.Abs(OpenTK.Vector3.CalculateAngle(planeYAxis, worldUpAxis));
            Rhino.Geometry.Vector3d normal2;
            if (xAngle < yAngle)
            {
                normal2 = modelPlane.YAxis;
            }
            else
            {
                normal2 = modelPlane.XAxis;
            }
            Plane plane2 = new Plane(modelPlane.Origin, normal2);
            PlaneSurface plane_surface2 = new PlaneSurface(plane2, new Interval(-planeSize, planeSize), new Interval(-planeSize, planeSize));
            Brep railPlane = Brep.CreateFromSurface(plane_surface2);
            Util.addSceneNode(ref mScene, railPlane, ref mesh_m, "railPlane");

            //add icurveList since we don't use EditPoint2 for circle and rect
            mScene.iCurveList.Add(modelcurve);

            //updating iPointList
            if (renderType == "circle")
            {
                mScene.selectionList.Add("Sweep");
                mScene.selectionList.Add("Circle");
                mScene.selectionList.Add("Curve");
                Circle circle;
                if (modelcurve.TryGetCircle(out circle))
                {
                    mScene.iPointList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(circle.Center)));
                    mScene.iPointList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(modelcurve.PointAtStart)));
                }

            }
            else if (renderType == "rect")
            {
                mScene.selectionList.Add("Sweep");
                mScene.selectionList.Add("Rect");
                mScene.selectionList.Add("Curve");
                Polyline polyline;
                if (modelcurve.TryGetPolyline(out polyline))
                {
                    Rectangle3d rect = Rectangle3d.CreateFromPolyline(polyline);
                    mScene.iPointList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(rect.Center)));
                    mScene.iPointList.Add(Util.platformToVRPoint(ref mScene, Util.RhinoToOpenTKPoint(rect.Corner(3))));
                }
            }
            else if (renderType == "projection")
            {
                //check if it's can still be a circle or rect since it might be a planar surface
                Circle circle;
                if (modelcurve.TryGetCircle(out circle))
                {
                    mScene.selectionList.Add("Sweep");
                    mScene.selectionList.Add("Circle");
                    mScene.selectionList.Add("Curve");
                }
                else
                {
                    Polyline polyline;
                    if (modelcurve.TryGetPolyline(out polyline))
                    {
                        mScene.selectionList.Add("Sweep");
                        mScene.selectionList.Add("Rect");
                        mScene.selectionList.Add("Curve");
                    }
                    else
                    {
                        //3D curve Sweep
                        mScene.selectionList.Add("Sweep");
                        mScene.selectionList.Add("Curve");
                        mScene.selectionList.Add("Curve");
                    }
                }


            }

            //push creatcurve for rail curve type-3 for railPlane

            //mScene.popInteraction();
            mScene.pushInteraction(new EditPoint3(ref mScene, true, "Sweep"));
            mScene.pushInteraction(new CreateCurve(ref mScene, 3, false, "Sweep"));
            mScene.peekInteraction().init();

        }

        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            primaryDeviceIndex = vrEvent.trackedDeviceIndex;
            //testing projection
            //ray casting to the pre-defind planes
            OpenTK.Vector4 controller_p = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, 0, 1);
            OpenTK.Vector4 controller_pZ = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, -1, 1);
            Point3d controller_pRhino = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)));
            Point3d controller_pZRhin = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_pZ.X, controller_pZ.Y, controller_pZ.Z)));

            Rhino.Geometry.Vector3d direction = new Rhino.Geometry.Vector3d(controller_pZRhin.X - controller_pRhino.X, controller_pZRhin.Y - controller_pRhino.Y, controller_pZRhin.Z - controller_pRhino.Z);
            Ray3d ray = new Ray3d(controller_pRhino, direction);

            Rhino.DocObjects.ObjectEnumeratorSettings settings = new Rhino.DocObjects.ObjectEnumeratorSettings();
            settings.ObjectTypeFilter = Rhino.DocObjects.ObjectType.Brep;
            float mimD = 1000000f;

            Rhino.DocObjects.RhinoObject targetPRhObj = null;
            OpenTK.Vector3 projectP;
            Rhino.Geometry.Point3d projectPRhino = new Point3d();
            foreach (Rhino.DocObjects.RhinoObject rhObj in mScene.rhinoDoc.Objects.GetObjectList(settings))
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
                        projectPRhino = rayIntersections[0];
                        projectP = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)rayIntersections[0].X, (float)rayIntersections[0].Y, (float)rayIntersections[0].Z));
                    }

                }
            }
            if (hitPlane)
            {
                Brep targetBrep = (Brep)(targetPRhObj.Geometry);
                //compute the brepFace where the curve is on
                int faceIndex = -1;
                for (int i = 0; i < targetBrep.Faces.Count; i++)
                {
                    //cast BrepFace to Brep for ClosestPoint(P) menthod
                    double dist = targetBrep.Faces[i].DuplicateFace(false).ClosestPoint(projectPRhino).DistanceTo(projectPRhino);
                    //tolerance mScene.rhinoDoc.ModelAbsoluteTolerance too low
                    if (dist < mScene.rhinoDoc.ModelAbsoluteTolerance)
                    {
                        faceIndex = i;
                        break;
                    }
                }

                Surface s = targetBrep.Faces[faceIndex];
                double u, v;
                if (s.ClosestPoint(projectPRhino, out u, out v))
                {
                    //update modelCurve and Brep
                    modelcurve = Curve.ProjectToBrep(modelcurve, targetBrep, s.NormalAt(u, v), mScene.rhinoDoc.ModelAbsoluteTolerance)[0].ToNurbsCurve();

                    //render modelCurve
                    Rhino.Geometry.PolylineCurve polyline = modelcurve.ToPolyline(0, 0, 0, 0, 0, 1, 1, 0, true);
                    SceneNode stroke;
                    Geometry.Geometry stroke_g2 = new Geometry.GeometryStroke(ref mScene);
                    Material.Material stroke_m2 = new Material.SingleColorMaterial(0, 0, 1, 1);
                    for (int i = 0; i < polyline.PointCount; i++)
                    {
                        ((Geometry.GeometryStroke)stroke_g2).addPoint(Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)polyline.Point(i).X, (float)polyline.Point(i).Y, (float)polyline.Point(i).Z)));

                    }
                    stroke = new SceneNode("EditCurve", ref stroke_g2, ref stroke_m2);
                    mScene.tableGeometry.add(ref stroke);

                }

            }

            //TODO-remove renderObjId sceneNode

            hitPlane = false;
            renderType = "projection";
        }
        protected override void onReleaseOculusTrigger(ref VREvent_t vrEvent)
        {
            //mScene.popInteraction();
            //mScene.peekInteraction().init();
        }
    }
}
