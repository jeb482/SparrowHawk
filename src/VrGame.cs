using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using Valve.VR;
using Rhino.Geometry;
using Emgu.CV.Structure;
using Emgu.CV;

namespace SparrowHawk
{
    public class VrGame : OpenTK.GameWindow
    {
        CVRSystem mHMD;
        CVRRenderModels mRenderModels;
        Scene mScene;
        VrRenderer mRenderer;
        Rhino.RhinoDoc mDoc;
        String mStrDriver = "No Driver";
	    String mStrDisplay = "No Display";
        String mTitleBase;
        uint mRenderWidth = 1280;
        uint mRenderHeight = 720;


        TrackedDevicePose_t[] renderPoseArray, gamePoseArray;
        DateTime mLastFrameTime;

        int mFrameCount;
        double frameRateTimer = 1;

        // Callibration
        List<Vector3> robotCallibrationPoints = new List<Vector3>();
        List<Vector3> vrCallibrationPoints = new List<Vector3>();
        //using opencv by Eric
        List<MCvPoint3D32f> robotCallibrationPoints_cv = new List<MCvPoint3D32f>();
        List<MCvPoint3D32f> vrCallibrationPoints_cv = new List<MCvPoint3D32f>();
        Matrix<double> mVRtoRobot;
        Matrix4 glmVRtoMarker;
        byte[] inliers;

        bool manualCallibration = false;



        public VrGame(ref Rhino.RhinoDoc doc)
        {
            mDoc = doc;
            if (init())
                Rhino.RhinoApp.WriteLine("Initialization complete!");

            Rhino.RhinoApp.WriteLine("Directory: " + System.IO.Directory.GetCurrentDirectory());
            //Run();  

            // Manual callibration
            
            if (manualCallibration)
            {
                //robotCallibrationPoints.Add(new Vector3(0, 0, 0));
                //robotCallibrationPoints.Add(new Vector3(0, 95.0f, 0));
                //robotCallibrationPoints.Add(new Vector3(95.0f, 95.0f, 0));
                //robotCallibrationPoints.Add(new Vector3(95.0f, 0, 0));
                //robotCallibrationPoints.Add(new Vector3(0, 0, 95.0f));
                //robotCallibrationPoints.Add(new Vector3(0, 95.0f, 95.0f));
                //robotCallibrationPoints.Add(new Vector3(95.0f, 95.0f, 95.0f));
                //robotCallibrationPoints.Add(new Vector3(95.0f, 0, 95.0f));

                //using opencv by eric
                robotCallibrationPoints_cv.Add(new MCvPoint3D32f(0, 0, 0));
                robotCallibrationPoints_cv.Add(new MCvPoint3D32f(0, 95.0f, 0));
                robotCallibrationPoints_cv.Add(new MCvPoint3D32f(95.0f, 95.0f, 0));
                robotCallibrationPoints_cv.Add(new MCvPoint3D32f(95.0f, 0, 0));
                robotCallibrationPoints_cv.Add(new MCvPoint3D32f(0, 0, 95.0f));
                robotCallibrationPoints_cv.Add(new MCvPoint3D32f(0, 95.0f, 95.0f));
                robotCallibrationPoints_cv.Add(new MCvPoint3D32f(95.0f, 95.0f, 95.0f));
                robotCallibrationPoints_cv.Add(new MCvPoint3D32f(95.0f, 0, 95.0f));


                //               mScene.mInteractionStack.Pop();
                mScene.mInteractionStack.Push(new Interaction.PickPoint(ref mScene, ref vrCallibrationPoints));
            }
            

        }

        /**
         * Updates the poses of all tracked devices in the Matrix4 format. 
         * Also handles new tracked devices, setting them up in the scene
         * and loading their render models.
         */
        void updateMatrixPose()
        {
            if (mHMD == null)
                return;

            OpenVR.Compositor.WaitGetPoses(mScene.mTrackedDevices, gamePoseArray);

            for (uint i = 0; i < mScene.mTrackedDevices.Length; i++)
            {
                var device = gamePoseArray[i];
                if (device.bPoseIsValid)
                {
                    // TODO: Store it
                    mScene.mDevicePose[i] = Util.steamVRMatrixToMatrix4(mScene.mTrackedDevices[i].mDeviceToAbsoluteTracking);
                    mHMD.GetTrackedDeviceClass(i);
                    if (mScene.mDeviceClassChar[i] == 0)
                    {
                        switch (mHMD.GetTrackedDeviceClass(i))
                        {
                            case ETrackedDeviceClass.Controller:
                                mScene.mDeviceClassChar[i] = 'C';
                                string name = Util.GetTrackedDeviceString(ref mHMD, i, ETrackedDeviceProperty.Prop_RenderModelName_String);
                                if (name.ToLower().Contains("left"))
                                    mScene.leftControllerIdx = (int)i;
                                else if (name.ToLower().Contains("right"))
                                    mScene.rightControllerIdx = (int)i;
                                else if (mScene.leftControllerIdx < 0)
                                    mScene.leftControllerIdx = (int) i;
                                else if (mScene.rightControllerIdx < 0)
                                    mScene.rightControllerIdx = (int) i;
                                break;
                            case ETrackedDeviceClass.HMD: mScene.mDeviceClassChar[i] = 'H'; break;
                            case ETrackedDeviceClass.Invalid: mScene.mDeviceClassChar[i] = 'I'; break;
                            case ETrackedDeviceClass.GenericTracker: mScene.mDeviceClassChar[i] = 'G'; break;
                            case ETrackedDeviceClass.TrackingReference: mScene.mDeviceClassChar[i] = 'T'; break;
                            default: mScene.mDeviceClassChar[i] = '?'; break;
                        }
                    }                  
                }
  
            }

            if (gamePoseArray[OpenVR.k_unTrackedDeviceIndex_Hmd].bPoseIsValid)
            {
                mScene.mHMDPose = Util.steamVRMatrixToMatrix4(gamePoseArray[OpenVR.k_unTrackedDeviceIndex_Hmd].mDeviceToAbsoluteTracking).Inverted();
            }

            if (mScene.leftControllerIdx > 0)
                mScene.leftControllerNode.transform = mScene.mDevicePose[mScene.leftControllerIdx];
            if (mScene.rightControllerIdx > 0)
                mScene.rightControllerNode.transform = mScene.mDevicePose[mScene.rightControllerIdx];
        }

        protected void handleInteractions()
        {

            //default interaction
            if (mScene.mInteractionStack.Count == 0)
            {
                mScene.mInteractionStack.Push(new Interaction.CreateCylinder(ref mScene));
                //mScene.mInteractionStack.Push(new Interaction.Stroke(ref mScene));
            }

            Interaction.Interaction current_i = mScene.mInteractionStack.Peek();
            current_i.handleInput();
            
            //TODO: if we can detect the hold event, then we can move this to eventHandler
            //if (current_i.GetType() == typeof(Interaction.Closedcurve))
            //{
            //    ((Interaction.Closedcurve)current_i).draw(true, mScene.leftControllerIdx);
            //
            //}
            //else if (current_i.GetType() == typeof(Interaction.Stroke))
            //{
            //    ((Interaction.Stroke)current_i).draw(true, mScene.leftControllerIdx);
            //}
            //else if (current_i.GetType() == typeof(Interaction.Sweep))
            //{
             //   ((Interaction.Sweep)current_i).draw(true, mScene.leftControllerIdx);
            //}//

            
            if (manualCallibration && vrCallibrationPoints.Count == 8)
            {
                manualCallibration = false; // HACK to not reset my cylinder forever.
                //Util.solveForAffineTransform(vrCallibrationPoints, robotCallibrationPoints, ref mScene.vrToRobot);

                //using opencv by Eric                
                foreach (Vector3 p in vrCallibrationPoints)
                {
                    vrCallibrationPoints_cv.Add(new MCvPoint3D32f(p.X, p.Y, p.Z));
                }
                CvInvoke.EstimateAffine3D(vrCallibrationPoints_cv.ToArray(), robotCallibrationPoints_cv.ToArray(), out mVRtoRobot, out inliers, 3, 0.99);

                mScene.vrToRobot = new Matrix4(
                    (float)mVRtoRobot[0, 0], (float)mVRtoRobot[0, 1], (float)mVRtoRobot[0, 2], (float)mVRtoRobot[0, 3],
                    (float)mVRtoRobot[1, 0], (float)mVRtoRobot[1, 1], (float)mVRtoRobot[1, 2], (float)mVRtoRobot[1, 3],
                    (float)mVRtoRobot[2, 0], (float)mVRtoRobot[2, 1], (float)mVRtoRobot[2, 2], (float)mVRtoRobot[2, 3],
                    0, 0, 0, 1
                );

                mScene.mInteractionStack.Pop();
                mScene.mInteractionStack.Push(new Interaction.CreateCylinder(ref mScene));
            }


        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            if (mFrameCount >= 30)
            {
                this.Title = mTitleBase + " - " + 1 / ((TimeSpan)(System.DateTime.Now - mLastFrameTime)).TotalSeconds + "fps.";
                
                mFrameCount = 0;
            } 
            mFrameCount += 1;
            mLastFrameTime = DateTime.Now;
        }

        // TODO: Only works for oculus
        protected void handleSignals()
        {
            SparrowHawkSignal s = SparrowHawkEventListeners.Instance.getOneSignal();
            if (s == null)
                return;
            switch (s.type)
            {
                case SparrowHawkSignal.ESparrowHawkSigalType.InitType:
                    if (s.data.Length >= 3)
                    {
                        Vector3 robotPoint = new Vector3(s.data[0] - 8, s.data[1], s.data[2] - 240);
                        robotCallibrationPoints.Add(robotPoint);
                        if (mScene.leftControllerIdx < 0)
                            break;
                        Vector3 vrPoint = Util.getTranslationVector3(mScene.mDevicePose[mScene.leftControllerIdx]);
                        vrCallibrationPoints.Add(vrPoint);
                        Util.MarkPoint(ref mScene.staticGeometry, vrPoint, 1, 1, 0);
                        if (robotCallibrationPoints.Count >= 8)
                        {
                            Util.solveForAffineTransformOpenCV(vrCallibrationPoints, robotCallibrationPoints, ref mScene.vrToRobot);
                            foreach (Vector3 v in robotCallibrationPoints)
                            {
                                Vector4 v4 = new Vector4(v.X, v.Y, v.Z, 1);
                                v4 = mScene.vrToRobot.Inverted() * v4;
                                Util.MarkPoint(ref mScene.staticGeometry, new Vector3(v4.X, v4.Y, v4.Z), 0, 1, 0);
                            }
                        }
                    }
                    break;
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            MakeCurrent();
            updateMatrixPose();
            handleSignals();
            handleInteractions();
            mRenderer.renderFrame();
            SwapBuffers();
        }

        protected override void Dispose(bool manual)
        {
            base.Dispose(manual);
        }

        /*
        void FindOrLoadRenderModel(string modelName)
        {
            RenderModel_t model;
            EVRRenderModelError error;
            IntPtr pRenderModel = new IntPtr();

            error = OpenVR.RenderModels.LoadRenderModel_Async(modelName, ref pRenderModel);
        }

        protected Geometry.Geometry SetupRenderModelForTrackedDevice(uint trackedDeviceIndex)
        {
            if (trackedDeviceIndex >= OpenVR.k_unMaxTrackedDeviceCount)
                return null;
            FindOrLoadRenderModel();

        }
        */
        public bool init()
        {
            // Set up HMD
            EVRInitError eError = EVRInitError.None;
            mHMD = OpenVR.Init(ref eError, EVRApplicationType.VRApplication_Scene);
            

            bool can = OpenVR.Compositor.CanRenderScene();
            
            if (eError == EVRInitError.None)
                Rhino.RhinoApp.WriteLine("Booted VR System");
            else
            {
                Rhino.RhinoApp.WriteLine("Failed to boot");
                return false;
            }

            // Get render models
            renderPoseArray = new TrackedDevicePose_t[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];
            gamePoseArray = new TrackedDevicePose_t[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];
            // mRenderModels = OpenVR.GetGenericInterface(OpenVR.IVRRenderModels_Version, ref eError);

            // Window Setup Info
            mStrDriver = Util.GetTrackedDeviceString(ref mHMD, OpenVR.k_unTrackedDeviceIndex_Hmd, ETrackedDeviceProperty.Prop_TrackingSystemName_String);
            mStrDisplay = Util.GetTrackedDeviceString(ref mHMD, OpenVR.k_unTrackedDeviceIndex_Hmd, ETrackedDeviceProperty.Prop_SerialNumber_String);
            mTitleBase = "SparrowHawk - " + mStrDriver + " " + mStrDisplay;
            Title = mTitleBase;


            MakeCurrent();

            mScene = new Scene(ref mDoc, ref mHMD);
            if (mStrDriver.Contains("oculus")) mScene.isOculus = true; else mScene.isOculus = false;
            mHMD.GetRecommendedRenderTargetSize(ref mRenderWidth, ref mRenderHeight);


            
            
            Geometry.Geometry g = new Geometry.Geometry("C:/workspace/Kestrel/resources/meshes/bunny.obj");

            //Material.Material m = new Material.SingleColorMaterial(mDoc,1f,1f,1f,1f);
            Material.Material m = new Material.SingleColorMaterial(1,0,1,1);
            SceneNode cube = new SceneNode("Triangle", ref g, ref m);
            cube.transform = new Matrix4(1, 0, 0, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1);
            mScene.staticGeometry.add(ref cube);

            g = new Geometry.PointMarker(new Vector3(0, 1, 0));
            m = new Material.SingleColorMaterial(1, 1, 1, 1);
            SceneNode point = new SceneNode("Point 1", ref g, ref m);
            mScene.staticGeometry.add(ref point);
            point.transform = new Matrix4(1, 0, 0, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1);

            // Left
            g = new Geometry.PointMarker(new Vector3(0, 0, 0));
            m = new Material.SingleColorMaterial(1, 0, 0, 1);
            point = new SceneNode("Left Cursor", ref g, ref m);
            mScene.leftControllerNode.add(ref point);
            point.transform = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

            g = new Geometry.PointMarker(new Vector3(0, 0, 0));
            m = new Material.SingleColorMaterial(0, 0, 1, 1);
            point = new SceneNode("Right Cursor", ref g, ref m);
            mScene.rightControllerNode.add(ref point);
            point.transform = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);


            //Rhino brep and mesh rendering testing
            //Mesh base_mesh = Mesh.CreateFromSphere(new Sphere(Point3d.Origin, 0.5), 20, 20);
            //Mesh base_mesh = Mesh.CreateFromCylinder(new Cylinder(new Circle(Point3d.Origin, 0.25),0.5), 20, 20);

            //instead of creating mesh directly, we create the brep first.
            /*
            Rhino.Geometry.Point3d center_point = new Rhino.Geometry.Point3d(0, 0, 0);
            Rhino.Geometry.Point3d height_point = new Rhino.Geometry.Point3d(0, 0, 0.5);
            Rhino.Geometry.Vector3d zaxis = height_point - center_point;
            Rhino.Geometry.Plane plane = new Rhino.Geometry.Plane(center_point, zaxis);
            const double radius = 0.25;
            Rhino.Geometry.Circle circle = new Rhino.Geometry.Circle(plane, radius);
            Rhino.Geometry.Cylinder cylinder = new Rhino.Geometry.Cylinder(circle, zaxis.Length);
            //brep = cylinder.ToBrep(true, true);
            */
            //create a one-face brep for testing extrusion
            //brep = Brep.CreateFromCornerPoints(new Point3d(0.0, 0.0, 0.2), new Point3d(0.0, 0.1, 0.2), new Point3d(0.1, 0.0, 0.2), mDoc.ModelAbsoluteTolerance);
            
            //rhino extrusion test
            Rhino.Collections.Point3dList points = new Rhino.Collections.Point3dList(5);
            points.Add(0.0, 0.0, 0.2);
            points.Add(0.0, 0.1, 0.2);
            points.Add(0.1, 0.1, 0.2);
            points.Add(0.1, 0.0, 0.2);
            Rhino.Geometry.NurbsCurve nc = Rhino.Geometry.NurbsCurve.Create(true, 3, points);
            //Rhino.Geometry.Curve nc = Curve.CreateInterpolatedCurve(points,3);
            //nc.SetEndPoint(nc.PointAtStart);

            //create surface from curve extruve CreateExtrusion => create brep from surface
            Surface s_extrude = Surface.CreateExtrusion(nc, new Rhino.Geometry.Vector3d(0, 0, 0.15));
            brep = Brep.CreateFromSurface(s_extrude);
     
            //brep = cylinder.ToBrep(true, true);
            Rhino.Geometry.BrepFace face = brep.Faces[0];

            Mesh base_mesh = new Mesh();
            if (brep != null)
            {
                Mesh[] meshes = Mesh.CreateFromBrep(brep, MeshingParameters.Default);

                foreach (Mesh mesh in meshes)
                    base_mesh.Append(mesh);

                mDoc.Objects.AddMesh(base_mesh);
                mDoc.Views.Redraw();
            }

            //mDoc.Objects.AddMesh(((Geometry.RhinoMesh)rhinoMesh).triMesh);
            //mDoc.Views.Redraw();

            Geometry.Geometry rhinoMesh = new Geometry.RhinoMesh(ref base_mesh);
            Material.Material rhinoMseh_m = new Material.SingleColorMaterial(0, 1, 0, 1);
            SceneNode rhinoCylinder = new SceneNode("RhinoCylinder", ref rhinoMesh, ref rhinoMseh_m);
            mScene.staticGeometry.add(ref rhinoCylinder);

            mRenderer = new VrRenderer(ref mHMD, ref mScene, mRenderWidth, mRenderHeight);

            // build shaders? Maybe in renderer!
            // setup texture maps is commented out.

            // TODO: Encoder Init
            // TODO: Setup Cameras
            // TODO: Setup OVRVision
            // TODO: Setup StereoRenderTargets
            // TODO: Setup Distortion
            // TODO: Setup DeviceModels
            // TODO: Setup Interactions

            return true;
        }

        //add key event handler
        List<Point3d> curvePoints = new List<Point3d>();
        Rhino.Geometry.Brep brep;
        protected override void OnKeyPress(OpenTK.KeyPressEventArgs e)
        {
            if (e.KeyChar == 'C' || e.KeyChar == 'c')
                mRenderer.ovrvision_controller.getMatrixHeadtoCamera();

            if (e.KeyChar == 'D' || e.KeyChar == 'd')
                mRenderer.ovrvision_controller.setDefaultMatrixHC();

            if (e.KeyChar == 'S' || e.KeyChar == 's')
            {
                Interaction.Interaction i = mScene.mInteractionStack.Peek();
                if (i.GetType() == typeof(Interaction.Closedcurve))
                {
                    Rhino.Geometry.NurbsCurve curve = ((Interaction.Closedcurve)i).closedCurve;
                    mScene.mInteractionStack.Pop();
                    mScene.mInteractionStack.Push(new Interaction.Sweep(ref mScene, ref curve));
                }
            }

            //rhino extrude and curve testing
            if (e.KeyChar == 'R' || e.KeyChar == 'r')
            {
                mScene.mInteractionStack.Pop();
                mScene.mInteractionStack.Push(new Interaction.Closedcurve(ref mScene));
            }

        }


      // public void runMainLoop()
      //  {
      // /     // Not sure if this is right. How do we close it?
      // /     while (true)
      //      {
      //          mRenderer.renderFrame();
      //      }
      //  }
        

     Geometry.Geometry FindOrLoadRenderModel(string modelName)
      {
          RenderModel_t model;
          EVRRenderModelError error;
          IntPtr pRenderModel = new IntPtr();
          while (true)
            {
                error = OpenVR.RenderModels.LoadRenderModel_Async(modelName, ref pRenderModel);
                if (error != EVRRenderModelError.Loading)
                    break;
                System.Threading.Thread.Sleep(1);
            }

            if ( error != EVRRenderModelError.None)
            {
                Rhino.RhinoApp.WriteLine("Unable to load render model " + modelName + " -- " + OpenVR.RenderModels.GetRenderModelErrorNameFromEnum(error));
                return null;
            }


            // Unpack
            int nTexId;
            Geometry.Geometry mesh = new Geometry.Geometry(pRenderModel, out nTexId);

            //unsafe
            //{
            //    RenderModel_TextureMap_t* pTexture;
                
            //    IntPtr ppTexture = (IntPtr) &pTexture;
            //    while (true)
            //    {
            //        error = OpenVR.RenderModels.LoadTexture_Async(nTexId, ref ppTexture);



            //    }
            //}

            return null;
      }
 
      //protected Geometry.Geometry SetupRenderModelForTrackedDevice(uint trackedDeviceIndex)
      //{
      //    if (trackedDeviceIndex >= OpenVR.k_unMaxTrackedDeviceCount)
      //        return null;
      //    FindOrLoadRenderModel("");
 
      //}
 
         


    }
}