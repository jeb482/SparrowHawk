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
        OpenTK.Vector3 radius_point;
        float radius;
        uint mPrimaryDevice;

        public CreateCylinder(ref Scene scene)
        {
            mScene = scene;
            origin = new OpenTK.Vector3();
            orientation = new OpenTK.Vector3();
            mState = State.PickOrigin;
        }

        protected void buildCyl()
        {
            if (orientation.Length < .000001)
                return;
            //vrToPlatformPoint did the unit conversion as well
            Rhino.Geometry.Point3d center_point = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene,origin));
            Rhino.Geometry.Vector3d zaxis = Util.openTkToRhinoVector(Util.vrToPlatformVector(ref mScene,orientation));
            //Rhino.Geometry.Plane plane = new Rhino.Geometry.Plane(center_point, zaxis);
            Rhino.Geometry.Plane plane = new Rhino.Geometry.Plane(center_point, Util.openTkToRhinoVector(Util.vrToPlatformVector(ref mScene, new OpenTK.Vector3(0, 1, 0))));
            Rhino.Geometry.Circle circle = new Rhino.Geometry.Circle(plane, radius);
            Rhino.Geometry.Cylinder cylinder = new Rhino.Geometry.Cylinder(circle, zaxis.Length);
            Rhino.Geometry.Brep brep = cylinder.ToBrep(true, true);
            
            //render in VR
            Material.Material mesh_m = new Material.RGBNormalMaterial(1);
            Util.addSceneNode(ref mScene, brep, ref mesh_m, "a1");


        }

        protected void advanceState(uint trackedDeviceIndex)
        {
            switch (mState)
            {
                case State.PickOrigin:
                    origin = Util.getTranslationVector3(Util.getControllerTipPosition(ref mScene, trackedDeviceIndex == mScene.leftControllerIdx));
                    mState = State.PickOrientation;
                    mPrimaryDevice = trackedDeviceIndex;
                    Util.MarkPoint(ref mScene.tableGeometry, origin, 1, 1, 0);
                    
                    break;
                case State.PickOrientation:
                    if (mPrimaryDevice != trackedDeviceIndex)
                        return;
                    orientation = Util.getTranslationVector3(Util.getControllerTipPosition(ref mScene, trackedDeviceIndex == mScene.leftControllerIdx));
                    Util.MarkPoint(ref mScene.tableGeometry, orientation, 1, 1, 0);
                    orientation -= origin;
                    mState = State.PickRadius;                 
                    break;
                case State.PickRadius:
                    if (mPrimaryDevice != trackedDeviceIndex)
                        return;
                    OpenTK.Vector3 radial = Util.getTranslationVector3(Util.getControllerTipPosition(ref mScene, trackedDeviceIndex == mScene.leftControllerIdx));
                    Util.MarkPoint(ref mScene.tableGeometry, radial, 1, 1, 0);
                    radial -= OpenTK.Vector3.Dot(orientation.Normalized(), radial) * orientation.Normalized();
                    radial = Util.vrToPlatformVector(ref mScene, radial);
                    radius = radial.Length;
                    //rhino test
                    OpenTK.Vector3 r = Util.vrToPlatformPoint(ref mScene, Util.getTranslationVector3(Util.getControllerTipPosition(ref mScene, trackedDeviceIndex == mScene.leftControllerIdx)));
                    OpenTK.Vector3 o = Util.vrToPlatformPoint(ref mScene, origin);
                    radius = (float)Math.Sqrt(Math.Pow((r.X - o.X), 2) + Math.Pow((r.Y - o.Y), 2) + Math.Pow((r.Z - o.Z), 2));
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
