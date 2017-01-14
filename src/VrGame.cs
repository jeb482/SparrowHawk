using System;
using Valve.VR;

namespace SparrowHawk
{
    class VrGame
    {
        CVRSystem HMD;

        public VrGame(ref Rhino.RhinoDoc doc)
        {
            int i;
            if (doc != null)
                // do document things here;
                i = 1;

            // Doesn't exist yet
            initWindow();

            // Set up HMD
            EVRInitError eError = EVRInitError.None;
            HMD = OpenVR.Init(ref eError, EVRApplicationType.VRApplication_Scene);
            if (eError == EVRInitError.None)
                Util.WriteLine(doc, "Booted VR System");
            else
            {
                Util.WriteLine(doc, "Failed to boot");
                return;
            }



        }

        public bool initWindow()
        {
            return true;
        }

        public void runMainLoop()
        {

        }




    }
}