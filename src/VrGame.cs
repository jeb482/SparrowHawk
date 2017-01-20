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
        uint mWindowWidth = 1280;
        uint mWindowHeight = 720;

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
            GL.ClearColor(1, 0, 1, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            //mRenderer.renderFrame();
            mRenderer.RenderScene(EVREye.Eye_Left);
            SwapBuffers();
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
            if (eError == EVRInitError.None)
                Util.WriteLine(ref mDoc, "Booted VR System");
            else
            {
                Util.WriteLine(ref mDoc, "Failed to boot");
                return false;
            }

            // Get render models
            // mRenderModels = OpenVR.GetGenericInterface(OpenVR.IVRRenderModels_Version, ref eError);

            // Window Setup Info
            mStrDriver = Util.GetTrackedDeviceString(ref mHMD, OpenVR.k_unTrackedDeviceIndex_Hmd, ETrackedDeviceProperty.Prop_TrackingSystemName_String);
            mStrDisplay = Util.GetTrackedDeviceString(ref mHMD, OpenVR.k_unTrackedDeviceIndex_Hmd, ETrackedDeviceProperty.Prop_SerialNumber_String);
            Title = "SparrowHawk - " + mStrDriver + " " + mStrDisplay;



            MakeCurrent();

            mScene = new Scene(ref mDoc);
            //OpenTK.Graphics.Color4 aqua = new OpenTK.Graphics.Color4(,);
            //aqua = OpenTK.Graphics.Color4.Aqua;
            //Geometry.Geometry g = new Geometry.Geometry("C:/workspace/Kestrel/resources/meshes/cube.obj", new OpenTK.Graphics.Color4(0,255,255,255));
            //g.mColors.Clear();
            Geometry.Geometry g = new Geometry.Geometry();
            g.mGeometry.Clear();
            g.mGeometryIndices.Clear();
            g.mNormals.Clear();
            g.mGeometry.AddRange(new float[] { 0, 0, 0, 1, 0, 0, 1, 1, 0, 0,1,0, 0, 0, 1, 1, 0, 1, 1, 1, 1, 0, 1, 1});
            g.mGeometryIndices.AddRange(new int[] {0,1,4,1,5,4,0,1,3,1,2,3,1,5,2,2,5,6,3,2,7,3,2,6,7,4,7,5,5,7,6,0,3,4,3,7,4});
            g.mNormals.AddRange(new float[] { 0, 0, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0, 0, 0, 1, 1, 0, 1, 1, 1, 1, 0, 1, 1 });
            Material.Material m = new Material.RGBNormalMaterial(1, mDoc);
            SceneNode cube = new SceneNode("Cube", ref g, ref m); ;
            mScene.staticGeometry.add(ref cube);

            mRenderer = new VrRenderer(ref mHMD, ref mScene);
        

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