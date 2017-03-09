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
            Rhino.Geometry.Point3d center_point = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene,origin));
            center_point *= 1000;
            Rhino.Geometry.Vector3d zaxis = Util.openTkToRhinoVector(Util.vrToPlatformVector(ref mScene,orientation));//*1000);
            //Rhino.Geometry.Plane plane = new Rhino.Geometry.Plane(center_point, zaxis);
            Rhino.Geometry.Plane plane = new Rhino.Geometry.Plane(center_point, new Rhino.Geometry.Vector3d(0,0,1));

            Rhino.Geometry.Circle circle = new Rhino.Geometry.Circle(plane, radius*1000);// *1000);
            Rhino.Geometry.Cylinder cylinder = new Rhino.Geometry.Cylinder(circle, zaxis.Length*1000);
            Rhino.Geometry.Brep brep = cylinder.ToBrep(true, true);

            //render in VR
            Material.Material mesh_m = new Material.RGBNormalMaterial(1);
            Util.addSceneNode(ref mScene, brep, ref mesh_m);

            //var normal = Util.openTkToRhinoVector(Util.vrToPlatformVector(ref mScene, orientation).Normalized());
            //var circle = new Rhino.Geometry.Circle(new Rhino.Geometry.Plane(Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, origin)), normal), radius*1000);
            //var cyl = new Rhino.Geometry.Cylinder(circle, orientation.Length * 1000);
            //var brep = cyl.ToBrep(true, true);
            //mScene.rhinoDoc.Objects.AddBrep(brep);
            //mScene.rhinoDoc.Views.Redraw();

        }

        protected void advanceState(uint trackedDeviceIndex)
        {
            switch (mState)
            {
                case State.PickOrigin:
                    origin = Util.getTranslationVector3(Util.getControllerTipPosition(ref mScene, trackedDeviceIndex == mScene.leftControllerIdx));
                    mState = State.PickOrientation;
                    mPrimaryDevice = trackedDeviceIndex;
                    Util.MarkPoint(ref mScene.staticGeometry, origin, 1, 1, 0);

                    break;
                case State.PickOrientation:
                    if (mPrimaryDevice != trackedDeviceIndex)
                        return;
                    orientation = Util.getTranslationVector3(Util.getControllerTipPosition(ref mScene, trackedDeviceIndex == mScene.leftControllerIdx));
                    Util.MarkPoint(ref mScene.staticGeometry, orientation, 1, 1, 0);
                    orientation -= origin;
                    mState = State.PickRadius;                 
                    break;
                case State.PickRadius:
                    if (mPrimaryDevice != trackedDeviceIndex)
                        return;
                    OpenTK.Vector3 radial = Util.getTranslationVector3(Util.getControllerTipPosition(ref mScene, trackedDeviceIndex == mScene.leftControllerIdx));
                    Util.MarkPoint(ref mScene.staticGeometry, radial, 1, 1, 0);
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
