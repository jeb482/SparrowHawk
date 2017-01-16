using System;
using Valve.VR;

namespace SparrowHawk
{
    class VrGame
    {
        CVRSystem mHMD;
        CVRRenderModels mRenderModels;
        Rhino.RhinoDoc mDoc;
        String mStrDriver = "No Driver";
	    String mStrDisplay = "No Display";
        String mWindowTitle;
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
            mStrDriver = Util.GetTrackedDeviceString(ref mHMD, OpenVR.k_unTrackedDeviceIndex_Hmd, ETrackedDeviceProperty.Prop_IconPathName_String);
            mStrDisplay = Util.GetTrackedDeviceString(ref mHMD, OpenVR.k_unTrackedDeviceIndex_Hmd, ETrackedDeviceProperty.Prop_SerialNumber_String);
            mWindowTitle = "SparrowHawk - " + mStrDriver + " " + mStrDisplay;
            
            // set screen title
            // make glfwContextCurrent

            if (!initCompositor())
            {
                Util.WriteLine(mDoc, "Failed to initialize the Compositor.");
                return false; 
            }
            Util.WriteLine(mDoc, "Compositor Initialized.");

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


        public bool initWindow()
        {
            return true;
        }

        public bool initCompositor()
        {
            return true;
        }

        public void runMainLoop()
        {

        }




    }
}