using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparrowHawk
{
    static class Util
    {
        public static void WriteLine(Rhino.RhinoDoc doc, String str)
        {
            if (doc != null)
                Rhino.RhinoApp.WriteLine(str);
            else
                Console.WriteLine(str);
        }


        // Directly from OpenVr's openGL starter code.
        public static string GetTrackedDeviceString(ref Valve.VR.CVRSystem Hmd, uint unDevice, Valve.VR.ETrackedDeviceProperty prop)
        {
            Valve.VR.ETrackedPropertyError eError = Valve.VR.ETrackedPropertyError.TrackedProp_Success;
            uint unRequiredBufferLen = Hmd.GetStringTrackedDeviceProperty(unDevice, prop, null, 0, ref eError);
            if (unRequiredBufferLen == 0)
                return "";
            System.Text.StringBuilder pchBuffer = new System.Text.StringBuilder();
            unRequiredBufferLen = Hmd.GetStringTrackedDeviceProperty(unDevice, prop, pchBuffer, unRequiredBufferLen, ref eError);
            return pchBuffer.ToString();
        }
    }
}
