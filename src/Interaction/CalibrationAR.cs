using System;
using Rhino;
using Rhino.Commands;
using SparrowHawk.Ovrvision;
using Valve.VR;

namespace SparrowHawk.Interaction
{
   
    public class CalibrationAR : Interaction
    {
        private OvrvisionController ovrcontroller;

        public CalibrationAR(ref Scene scene, ref OvrvisionController controller) : base(ref scene)
        {
            ovrcontroller = controller;
        }

        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            ovrcontroller.getMatrixHeadtoCamera(vrEvent.trackedDeviceIndex);
        }

    }
}
