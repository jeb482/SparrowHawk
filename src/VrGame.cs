using System;
using Valve.VR;

namespace SparrowHawk
{
    class VrGame
    {
        CVRSystem mHMD;
        CVRRenderModels mRenderModels;
        Scene mScene;
        VrRenderer mRenderer;
        Rhino.RhinoDoc mDoc;
        String mStrDriver = "No Driver";
	    String mStrDisplay = "No Display";
        String mWindowTitle;
        OpenTK.GameWindow mWindow;
        uint mWindowWidth = 1280;
        uint mWindowHeight = 720;

        public VrGame(ref Rhino.RhinoDoc doc)
        {
            mDoc = doc;
            if (init())
                Util.WriteLine(mDoc, "Initialization complete!");
          

        }

        public bool init()
        {
            // Doesn't exist yet
            if (!initWindow())
                return false;

            // Set up HMD
            EVRInitError eError = EVRInitError.None;
            mHMD = OpenVR.Init(ref eError, EVRApplicationType.VRApplication_Scene);
            if (eError == EVRInitError.None)
                Util.WriteLine(mDoc, "Booted VR System");
            else
            {
                Util.WriteLine(mDoc, "Failed to boot");
                return false;
            }

            // Get render models
            // mRenderModels = OpenVR.GetGenericInterface(OpenVR.IVRRenderModels_Version, ref eError);

            // Window Setup Info
            mStrDriver = Util.GetTrackedDeviceString(ref mHMD, OpenVR.k_unTrackedDeviceIndex_Hmd, ETrackedDeviceProperty.Prop_TrackingSystemName_String);
            mStrDisplay = Util.GetTrackedDeviceString(ref mHMD, OpenVR.k_unTrackedDeviceIndex_Hmd, ETrackedDeviceProperty.Prop_SerialNumber_String);
            mWindowTitle = "SparrowHawk - " + mStrDriver + " " + mStrDisplay;
            mWindow.Title = mWindowTitle;


            // set screen title
            // make glfwContextCurrent

            mRenderer = new VrRenderer(ref mHMD, ref mWindow, ref mScene);
        

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


        // TODO: Not sure if this works or not. Also this should probs be in VrGame
        bool initWindow()
        {
            try
            {
                mWindow = new OpenTK.GameWindow((int) mWindowWidth, (int) mWindowHeight);
                mWindow.Visible = true;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        public void runMainLoop()
        {
            // Not sure if this is right. How do we close it?
            while (true)
            {
                mRenderer.renderFrame();
            }
        }




    }
}