using System;
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
        uint mRenderWidth = 1280;
        uint mRenderHeight = 720;
        TrackedDevicePose_t[] renderPoseArray, gamePoseArray;


        public VrGame(ref Rhino.RhinoDoc doc)
        {
            mDoc = doc;
            if (init())
                Util.WriteLine(ref mDoc, "Initialization complete!");

            Util.WriteLine(ref mDoc, "Directory: " + System.IO.Directory.GetCurrentDirectory());
            Run();  

        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            MakeCurrent();
            OpenVR.Compositor.WaitGetPoses(renderPoseArray, gamePoseArray);
            GL.ClearColor(1, 0, 1, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            mRenderer.renderFrame();
            //mRenderer.RenderScene(EVREye.Eye_Left);
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

        public bool init()
        {
            // Set up HMD
            EVRInitError eError = EVRInitError.None;
            mHMD = OpenVR.Init(ref eError, EVRApplicationType.VRApplication_Scene);
            

            bool can = OpenVR.Compositor.CanRenderScene();
            
            if (eError == EVRInitError.None)
                Util.WriteLine(ref mDoc, "Booted VR System");
            else
            {
                Util.WriteLine(ref mDoc, "Failed to boot");
                return false;
            }

            // Get render models
            renderPoseArray = new TrackedDevicePose_t[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];
            gamePoseArray = new TrackedDevicePose_t[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];
            // mRenderModels = OpenVR.GetGenericInterface(OpenVR.IVRRenderModels_Version, ref eError);

            // Window Setup Info
            mStrDriver = Util.GetTrackedDeviceString(ref mHMD, OpenVR.k_unTrackedDeviceIndex_Hmd, ETrackedDeviceProperty.Prop_TrackingSystemName_String);
            mStrDisplay = Util.GetTrackedDeviceString(ref mHMD, OpenVR.k_unTrackedDeviceIndex_Hmd, ETrackedDeviceProperty.Prop_SerialNumber_String);
            Title = "SparrowHawk - " + mStrDriver + " " + mStrDisplay;



            MakeCurrent();

            mScene = new Scene(ref mDoc);
            mHMD.GetRecommendedRenderTargetSize(ref mRenderWidth, ref mRenderHeight);


            Geometry.Geometry g = new Geometry.Geometry();
            g.mGeometry = new float[] { -1f, -1f, 0f, 1f, -1f, 0f, 0f, 1f, 0f };
            g.mNormals = new float[] { -1f, -1f, 0f, 1f, -1f, 0f, 0f, 1f, 0f };
            g.mGeometryIndices = new int[] { 0, 1, 2 };
            g.mNumPrimitives = 1;
            g.primitiveType = BeginMode.Triangles;
           
            Material.Material m = new Material.NaiveMaterial(mDoc);
            SceneNode cube = new SceneNode("Triangle", ref g, ref m); ;
            mScene.staticGeometry.add(ref cube);

            mRenderer = new VrRenderer(ref mHMD, ref mScene, mRenderWidth, mRenderWidth);
        

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


  


       // public void runMainLoop()
      //  {
      // /     // Not sure if this is right. How do we close it?
      // /     while (true)
      //      {
      //          mRenderer.renderFrame();
      //      }
      //  }




    }
}