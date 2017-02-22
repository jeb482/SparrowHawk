using System;
using Rhino;
using Rhino.Commands;
using Valve.VR;

namespace SparrowHawk.Interaction
{
 public class CreateCylinder : Interaction
    {
        enum State {PickOrigin, PickOrientation, PickRadius};
        State mState;
        OpenTK.Vector3 origin;
        OpenTK.Vector3 orientation;
        float radius;
        uint mPrimaryDevice;

        public CreateCylinder()
        {
            origin = new OpenTK.Vector3();
            orientation = new OpenTK.Vector3();
            mState = State.PickOrigin;
        }

        protected void buildCyl()
        {
            var normal = Util.openTkToRhinoVector(Util.vrToPlatformVector(ref mScene, orientation).Normalized());
            var circle = new Rhino.Geometry.Circle(new Rhino.Geometry.Plane(Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, origin)), normal), radius);
            var cyl = new Rhino.Geometry.Cylinder(circle, orientation.Length * 1000);
            var brep = cyl.ToBrep(true, true);
            mScene.rhinoDoc.Objects.AddBrep(brep);
        }

        protected void advanceState(uint trackedDeviceIndex)
        {
            switch (mState)
            {
                case State.PickOrigin:
                    origin = Util.getTranslationVector3(mScene.mDevicePose[trackedDeviceIndex]);
                    mState = State.PickOrientation;
                    mPrimaryDevice = trackedDeviceIndex;
                    break;
                case State.PickOrientation:
                    if (mPrimaryDevice != trackedDeviceIndex)
                        return;
                    orientation = Util.getTranslationVector3(mScene.mDevicePose[trackedDeviceIndex]);
                    orientation -= origin;
                    mState = State.PickRadius;
                    break;
                case State.PickRadius:
                    if (mPrimaryDevice != trackedDeviceIndex)
                        return;
                    OpenTK.Vector3 radial = Util.getTranslationVector3(mScene.mDevicePose[trackedDeviceIndex]);
                    radial -= OpenTK.Vector3.Dot(orientation, radial) * orientation;
                    radius = radial.Length;
                    buildCyl();
                    mState = State.PickOrigin;
                    break;
            }
        }

        protected override void onClickOculusTrigger(ref VREvent_t vrEvent)
        {
            advanceState(vrEvent.trackedDeviceIndex);
        }


    }
}
