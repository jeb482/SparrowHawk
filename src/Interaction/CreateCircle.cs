using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    class CreateCircle: Interaction
    {
        enum State { PickOrigin, PickRadius };
        State mState;
        OpenTK.Vector3 origin;
        OpenTK.Vector3 radius_point;
        float radius;
        uint mPrimaryDevice;

        private Material.Material mesh_m;
        private Rhino.Geometry.NurbsCurve circleCurve;
        private Rhino.Geometry.Brep circleBrep;

        public CreateCircle(ref Scene scene)
        {
            mScene = scene;
            origin = new OpenTK.Vector3();
            mState = State.PickOrigin;
            mesh_m = new Material.RGBNormalMaterial(1);
        }

        public override void draw(bool inTop)
        {
           
        }

        protected void buildCircle()
        {

            //Rhino.Geometry.Point3d center_point = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, origin));
            Rhino.Geometry.Point3d center_point = Util.openTkToRhinoPoint(Util.transformPoint(Util.mGLToRhino, origin));
            Rhino.Geometry.Plane plane = new Rhino.Geometry.Plane(center_point, new Rhino.Geometry.Vector3d(0,0,1));

            Rhino.Geometry.Circle circle = new Rhino.Geometry.Circle(plane, radius);// *1000);
            circleCurve = circle.ToNurbsCurve();
            Brep[] shapes = Brep.CreatePlanarBreps(circleCurve);
            Brep circle_s = shapes[0];
            circleBrep = circle_s;

            Util.addSceneNode(ref mScene, circleBrep, ref mesh_m);
            mScene.rhinoDoc.Views.Redraw();

        }

        protected void advanceState(uint trackedDeviceIndex)
        {
            switch (mState)
            {
                case State.PickOrigin:
                    origin = Util.getTranslationVector3(mScene.mDevicePose[trackedDeviceIndex]);
                    mState = State.PickRadius;
                    mPrimaryDevice = trackedDeviceIndex;
                    break;
                case State.PickRadius:
                    if (mPrimaryDevice != trackedDeviceIndex)
                        return;
                    //TODO: change to platform
                    //OpenTK.Vector3 r = Util.vrToPlatformPoint(ref mScene, Util.getTranslationVector3(mScene.mDevicePose[trackedDeviceIndex]));
                    //OpenTK.Vector3 o = Util.vrToPlatformPoint(ref mScene, origin);
                    OpenTK.Vector3 r = Util.transformPoint(Util.mGLToRhino, Util.getTranslationVector3(mScene.mDevicePose[trackedDeviceIndex]));
                    OpenTK.Vector3 o = Util.transformPoint(Util.mGLToRhino, origin);

                    radius = (float)Math.Sqrt(Math.Pow((r.X - o.X), 2) + Math.Pow((r.Y - o.Y), 2) + Math.Pow((r.Z - o.Z), 2));
                    buildCircle();
                    mState = State.PickOrigin;
                    break;
            }
        }

        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            advanceState(vrEvent.trackedDeviceIndex);
        }
    }
}
