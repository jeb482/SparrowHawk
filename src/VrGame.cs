using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using Valve.VR;



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



        public VrGame(ref Rhino.RhinoDoc doc)
        {
            mDoc = doc;
            if (init())
                Rhino.RhinoApp.WriteLine("Initialization complete!");

            Rhino.RhinoApp.WriteLine("Directory: " + System.IO.Directory.GetCurrentDirectory());
            //Run();  

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
                Matrix4 view_tmp = Util.steamVRMatrixToMatrix4(gamePoseArray[OpenVR.k_unTrackedDeviceIndex_Hmd].mDeviceToAbsoluteTracking);
                Matrix4 view_tmp_inv = view_tmp.Inverted();
                mScene.mHMDPose = Util.steamVRMatrixToMatrix4(gamePoseArray[OpenVR.k_unTrackedDeviceIndex_Hmd].mDeviceToAbsoluteTracking).Inverted();
            }

            if (mScene.leftControllerIdx > 0)
                mScene.leftControllerNode.transform = mScene.mDevicePose[mScene.leftControllerIdx];
            if (mScene.rightControllerIdx > 0)
                mScene.rightControllerNode.transform = mScene.mDevicePose[mScene.rightControllerIdx];
        }


        protected void handleInteractions()
        {
            if (mScene.mInteractionStack.Count == 0)
                mScene.mInteractionStack.Push(new Interaction.CreateCylinder(ref mScene));
            mScene.mInteractionStack.Peek().handleInput();
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
                        Vector3 robotPoint = new Vector3(s.data[0] - 8, s.data[1], s.data[2] - 240)/1000;
                        robotCallibrationPoints.Add(robotPoint);
                        if (mScene.leftControllerIdx < 0)
                            break;
                        Vector3 vrPoint = Util.getTranslationVector3(mScene.mDevicePose[mScene.leftControllerIdx]);
                        vrCallibrationPoints.Add(vrPoint);
                        Util.MarkPoint(ref mScene.staticGeometry, vrPoint, 1, 1, 0);
                        if (robotCallibrationPoints.Count == 5)
                        {
                            Util.solveForAffineTransform(vrCallibrationPoints, robotCallibrationPoints, ref mScene.vrToRobot);
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
            GL.ClearColor(0, 0, 0, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Finish();
            GL.Flush();
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

            mRenderer = new VrRenderer(ref mHMD, ref mScene, mRenderWidth, mRenderHeight);

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

        protected override void OnKeyPress(OpenTK.KeyPressEventArgs e)
        {
            if (e.KeyChar == 'C' || e.KeyChar == 'c')
                mRenderer.ovrvision_controller.getMatrixHeadtoCamera();

            if (e.KeyChar == 'D' || e.KeyChar == 'd')
                mRenderer.ovrvision_controller.setDefaultMatrixHC();

            if (e.KeyChar == 'S' || e.KeyChar == 's')
            {
                mRenderer.switchAR();
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