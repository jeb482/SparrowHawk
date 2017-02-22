using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    class Stroke : Interaction
    {
        public enum State
        {
            READY = 0, PAINT = 1
        };

        private State currentState;
        private Geometry.Geometry target;
        uint primaryDeviceIndex;

        public Stroke(ref Scene s)
        {
            mScene = s;
            target = new Geometry.GeometryStroke();
            activate();
           
        }

        public void activate()
        {
            currentState = State.READY;
        }

        public void deactivate()
        {
            currentState = State.READY;

        }

        public void draw(bool inFront, uint trackedDeviceIndex)
        {
            
            if (currentState != State.PAINT)
            {
                return;
            }

            Vector3 pos = Util.getTranslationVector3(mScene.mDevicePose[trackedDeviceIndex]);
            ((Geometry.GeometryStroke)target).addPoint(pos);
        }

        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("oculus grip click event test");
            if (currentState == State.READY)
            {
                primaryDeviceIndex = vrEvent.trackedDeviceIndex;
                target = new Geometry.GeometryStroke();
                Material.Material stroke_m = new Material.SingleColorMaterial(0, 1, 0, 1);
                SceneNode stroke = new SceneNode("Stroke", ref target, ref stroke_m);
                mScene.staticGeometry.add(ref stroke);
                currentState = State.PAINT;
            }
            draw(true, primaryDeviceIndex);

        }

        protected override void onReleaseOculusGrip(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("oculus grip release event test");
            if (currentState == State.PAINT)
            {
                target = new Geometry.GeometryStroke();
                currentState = State.READY;
            }
        }

    }
}
