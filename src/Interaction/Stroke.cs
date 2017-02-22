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
        public Geometry.Geometry target;
        uint primaryDeviceIndex;

        Material.Material stroke_m;

        public Stroke(ref Scene s)
        {
            mScene = s;
            target = new Geometry.GeometryStroke();
            activate();
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);

        }

        public void activate()
        {
            currentState = State.READY;
        }

        public void deactivate()
        {
            currentState = State.READY;

        }

        public void draw(bool inFront, int trackedDeviceIndex)
        {
            
            if (currentState != State.PAINT)
            {
                return;
            }


            Vector3 pos = Util.getTranslationVector3(mScene.mDevicePose[trackedDeviceIndex]);
            ((Geometry.GeometryStroke)target).addPoint(pos);
            //SceneNode stroke = new SceneNode("Stroke", ref target, ref stroke_m);
            //mScene.staticGeometry.add(ref stroke);
            
        }

        protected override void onClickOculusTrigger(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("oculus button click event test");
        }

        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("oculus grip click event test");
            primaryDeviceIndex = vrEvent.trackedDeviceIndex;
            if (currentState == State.READY)
            {             
                target = new Geometry.GeometryStroke();            
                SceneNode stroke = new SceneNode("Stroke", ref target, ref stroke_m);
                mScene.staticGeometry.add(ref stroke);
                currentState = State.PAINT;
            }
            
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
