using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    class MarkingMenu : Interaction
    {
        public MarkingMenu(ref Scene scene)
        {
            mScene = scene;
        }

        protected override void onClickViveTrigger(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("Pulled the Vive trigger");
        }
        protected override void onClickViveTouchpad(ref VREvent_t vrEvent)
        {
            float r, theta;
            getViveTouchpadPoint(vrEvent.trackedDeviceIndex, out r, out theta);
            Rhino.RhinoApp.WriteLine("Clicked the Vive touchpad at r = " + r + ", theta = " + theta + ".");
        }
        protected override void onClickViveGrip(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("Grabbed the Vive grip");
        }
        protected override void onClickViveAppMenu(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("Pressed the Vive application menu button");
        }

        protected override void onReleaseViveTrigger(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("Released the Vive trigger");
        }
        protected override void onReleaseViveTouchpad(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("Released the Vive touchpad");
        }
        protected override void onReleaseViveGrip(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("Released the Vive grip");
        }
        protected override void onReleaseViveAppMenu(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("Released the Vive application menu button");
        }

    }
}
