using OpenTK;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;
using static SparrowHawk.Scene;

namespace SparrowHawk.Interaction
{
    class CreatePlane2 : Interaction
    {
        private Material.Material mesh_m;
        private Guid renderObjId = Guid.Empty;
        private Guid previewObjId = Guid.Empty;
        protected uint primaryDeviceIndex;

        protected int mCurrentSelection = -1;
        protected double mInitialSelectOKTime = 0;
        protected double mSelectOKTime = 0;
        double defaultInitialDelay = .2;
        float mMinSelectionRadius;
        float mCurrentRadius;

        int mNumSectors = 4;
        int selectedSector = 0;

        float radius = 20;
        float width = 40;
        float height = 30;
        float delta = 0.6f;

        private Plane modelPlane;
        private float planeSize = 240;
        private NurbsCurve modelcurve;
        private Brep modelBrep;
        private bool hitPlane = false;
        private bool iStart = false;

        private ShapeType shapeType = ShapeType.None;
        private bool isProjection = false;

        public CreatePlane2(ref Scene scene) : base(ref scene)
        {
            mesh_m = new Material.SingleColorMaterial(0.5f, 0, 0, 0.4f);
        }

        public CreatePlane2(ref Scene scene, CurveID curveID) : base(ref scene)
        {
            mScene = scene;

            mesh_m = new Material.SingleColorMaterial(0.5f, 0.5f, 0, 0.4f);

            if (curveID == CurveID.ProfileCurve1)
            {
                shapeType = (ShapeType)mScene.selectionDic[SelectionKey.Profile1Shape];
            }
            else if (curveID == CurveID.ProfileCurve2)
            {
                shapeType = (ShapeType)mScene.selectionDic[SelectionKey.Profile2Shape];
            }

            mNumSectors = 4;
            mCurrentSelection = -1;

            if (scene.isOculus)
            {
                mMinSelectionRadius = 0.2f;
                //mOuterSelectionRadius = 0.8f;
            }
            else
            {
                mMinSelectionRadius = 0.4f;
                //mOuterSelectionRadius = 0.6f;
            }

        }

        public float getAngularMenuOffset(int numOptions)
        {
            if (numOptions <= 1) return 0;
            return (float)(-2 * Math.PI) / (2 * numOptions);
        }

        public override void leaveTop()
        {
            //clearDrawing is a bit tricky here since we need to save the shape for later.
        }

        public override void deactivate()
        {
            if (previewObjId != Guid.Empty)
            {
                Util.removeSceneNodeWithoutDraw(ref mScene, previewObjId);
                previewObjId = Guid.Empty;
            }

            //already click confirm button
            if (renderObjId != Guid.Empty)
            {
                Util.removeSceneNode(ref mScene, renderObjId);
            }

            resetVariables();
        }

        private void resetVariables()
        {
            mCurrentSelection = -1;
            mInitialSelectOKTime = 0;
            mSelectOKTime = 0;
            defaultInitialDelay = .2;
            mCurrentRadius = 0; ;
            selectedSector = 0;

            modelPlane = new Plane();
            modelcurve = null;
            modelBrep = null;
            hitPlane = iStart = false;
        }

        public override void init()
        {
            resetVariables();
            //support undo function
            if (mScene != null)
            {
                if(previewObjId != Guid.Empty)
                {
                    Util.removeSceneNodeWithoutDraw(ref mScene, previewObjId);
                }

                //already click confirm button
                if(renderObjId != Guid.Empty)
                {
                    Util.removeSceneNode(ref mScene, renderObjId);
                    mScene.iCurveList.RemoveAt(mScene.iCurveList.Count-1);
                    mScene.iPlaneList.RemoveAt(mScene.iPlaneList.Count-1);
                    mScene.iPlaneList.RemoveAt(mScene.iPlaneList.Count-1);
                }
            }

            // Set initial timeout that cannot be skipped to prevent double selections.
            mInitialSelectOKTime = mScene.gameTime + defaultInitialDelay;
        }

        public override void activate()
        {

            
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

            //prevent changing size immediately
            if (!iStart && mCurrentRadius < mMinSelectionRadius)
            {
                iStart = true;
            }

            // If in midlle selection ring, check delay
            if (mCurrentRadius > mMinSelectionRadius && iStart)
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

                changeSize(selectedSector);
            }
            else //trigger change size. discrete changing
            {
                /*
                if (selectedSector != 0)
                {
                    changeSize(selectedSector);
                    selectedSector = 0;
                }*/
            }

            createModel();

        }

        private void changeSize(int sector)
        {
            //mScene.vibrateController(0.1, (uint)primaryControllerIdx);
            //Rhino.RhinoApp.WriteLine("sector: " + sector);
            if (shapeType == ShapeType.Circle)
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
            }
            else if (shapeType == ShapeType.Rect)
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
            }

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

            if (shapeType == ShapeType.Circle)
            {
                Rhino.Geometry.Circle circle = new Rhino.Geometry.Circle(modelPlane, controller_pRhino, radius);
                modelcurve = circle.ToNurbsCurve();

            }
            else if (shapeType == ShapeType.Rect)
            {
                //Rectangle3d rect = new Rectangle3d(modelPlane, width, height);
                Rectangle3d rect = new Rectangle3d(modelPlane, new Interval(-width / 2, width / 2), new Interval(-height / 2, height / 2));
                modelcurve = rect.ToNurbsCurve();
            }

            if (modelcurve != null)
            {
                Brep[] shapes = Brep.CreatePlanarBreps(modelcurve);
                modelBrep = shapes[0];

                previewObjId = Util.addSceneNodeWithoutDraw(ref mScene, modelBrep, ref mesh_m, "3D-" + shapeType.ToString());
            }

        }

        protected override void onClickOculusAX(ref VREvent_t vrEvent)
        {
            //TODO-support projection curve
            Point3d planeCenter = new Point3d();
            if (!isProjection)
            {
                Util.removeSceneNodeWithoutDraw(ref mScene, previewObjId);
                previewObjId = Guid.Empty;
                Brep[] shapes = Brep.CreatePlanarBreps(modelcurve);
                modelBrep = shapes[0];
                //add plane to iRhobj
                renderObjId = Util.addSceneNode(ref mScene, modelBrep, ref mesh_m, shapeType.ToString());

                if (shapeType == ShapeType.Circle)
                {
                    Circle circle;
                    if (modelcurve.TryGetCircle(out circle))
                    {
                        planeCenter = circle.Center;
                    }
                }
                else if (shapeType == ShapeType.Rect)
                {
                    Rhino.Geometry.Polyline polyline;
                    if (mScene.iCurveList[mScene.iCurveList.Count - 1].TryGetPolyline(out polyline))
                    {
                        Rectangle3d rect = Rectangle3d.CreateFromPolyline(polyline);
                        planeCenter = rect.Center;
                    }
                }
            }

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


            //creating perendicular plane
            //TODO-need to decide whether it's XAxis or YAxis as normal
            OpenTK.Vector3 worldUpAxis = new Vector3(0, 0, 1); //in Rhino z axis is up
            OpenTK.Vector3 planeXAxis = Util.RhinoToOpenTKPoint(modelPlane.XAxis);
            OpenTK.Vector3 planeYAxis = Util.RhinoToOpenTKPoint(modelPlane.YAxis);
            OpenTK.Vector3 planeNormal = Util.RhinoToOpenTKPoint(modelPlane.Normal);


            //Plane planeX = new Plane(modelPlane.Origin, modelPlane.XAxis);
            //Plane planeY = new Plane(modelPlane.Origin, modelPlane.YAxis)

            Plane planeX = new Plane(planeCenter, modelPlane.XAxis);
            Plane planeY = new Plane(planeCenter, modelPlane.YAxis);

            float xAngle = OpenTK.Vector3.CalculateAngle(Util.RhinoToOpenTKPoint(planeX.Normal), worldUpAxis);
            float yAngle = OpenTK.Vector3.CalculateAngle(Util.RhinoToOpenTKPoint(planeY.Normal), worldUpAxis);
            Rhino.RhinoApp.WriteLine("xAngle: " + xAngle + " yAngle: " + yAngle);
            Rhino.Geometry.Vector3d normal2;
            Plane plane2;
            if (yAngle > xAngle)
            {
                if (yAngle <= Math.PI / 2)
                    plane2 = planeY;
                else
                    plane2 = planeX;
            }
            else
            {
                if (xAngle < Math.PI / 2)
                    plane2 = planeX;
                else
                    plane2 = planeY;
            }

            /*
            PlaneSurface plane_surfaceX = new PlaneSurface(planeX, new Interval(-planeSize, planeSize), new Interval(-planeSize, planeSize));
            Brep railPlaneX = Brep.CreateFromSurface(plane_surfaceX);

            PlaneSurface plane_surfaceY = new PlaneSurface(planeY, new Interval(-planeSize, planeSize), new Interval(-planeSize, planeSize));
            Brep railPlaneY = Brep.CreateFromSurface(plane_surfaceY);
            */

            //testing new method
            plane2 = new Plane(planeCenter, Util.openTkToRhinoVector(Vector3.Cross(planeNormal, worldUpAxis)));

            PlaneSurface plane_surface2 = new PlaneSurface(plane2, new Interval(-planeSize, planeSize), new Interval(-planeSize, planeSize));
            Brep railPlane2 = Brep.CreateFromSurface(plane_surface2);

            //testing
            /* 
            Material.Material mesh_mX = new Material.SingleColorMaterial(0.5f, 0.0f, 0, 0.4f);
            Material.Material mesh_mY = new Material.SingleColorMaterial(0.0f, 0.5f, 0, 0.4f);
            Util.addSceneNode(ref mScene, railPlaneX, ref mesh_mX, "railPlaneX");
            Util.addSceneNode(ref mScene, railPlaneY, ref mesh_mY, "railPlaneY");
            */
            //Util.addSceneNode(ref mScene, railPlane2, ref mesh_m, "railPlane");


            //add icurveList since we don't use EditPoint2 for circle and rect
            mScene.iCurveList.Add(modelcurve);
            mScene.iPlaneList.Add(curvePlane);
            mScene.iPlaneList.Add(plane2);

            //updating iPointList
            /*
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
            */
            //push creatcurve for rail curve type-3 for railPlane

            //mScene.popInteraction();
            //mScene.pushInteraction(new EditPoint3(ref mScene, true, "Sweep"));
            //mScene.pushInteraction(new CreateCurve(ref mScene, 3, false, "Sweep"));
            //mScene.peekInteraction().init();

            //call next interaction in the chain
            mScene.pushInteractionFromChain();

        }

        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            /*
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
            isProjection = true;
            */
        }
        protected override void onReleaseOculusTrigger(ref VREvent_t vrEvent)
        {
            //mScene.popInteraction();
            //mScene.peekInteraction().init();
        }
    }
}
