using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;
using OpenTK;

namespace SparrowHawk.Interaction
{
    class CreatePlaneA: Interaction
    {
        public enum State { Ready, PlaneCreated, PlaneSelected };
        protected State mState; 

        public CreatePlaneA(ref Scene scene)
        {
            mScene = scene;
            mState = State.Ready;
        }

        protected override void onClickViveTrigger(ref VREvent_t vrEvent)
        {

            switch (mState)
            {
                case State.Ready:
                    Matrix4 M = mScene.mDevicePose[vrEvent.trackedDeviceIndex];
                    Vector3 origin = Util.transformPoint(M, new Vector3(0, 0, -0.12f));
                    Vector3 norm = Util.transformVec(M, new Vector3(0, 0, 1));

                    Geometry.Geometry planeRep = new Geometry.PlaneRep();
                    Material.Material m = new Material.SingleColorMaterial(1, .8f, .8f, 1);
                    SceneNode n = new SceneNode("Plane", ref planeRep, ref m);
                    n.transform = new Matrix4();
                    mScene.staticGeometry.add(ref n);
                    break;

                case State.PlaneCreated:
                    // Select plane
                    break;

                case State.PlaneSelected:
                    // Drag the thangs.


            }


        }


    }
}
