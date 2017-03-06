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
        public enum State { Ready, PlaneCreated }; //,PlaneSelected };
        protected State mState; 
        protected Vector3 origin;
        protected Vector3 normal;
        protected SceneNode planeNode;

        public CreatePlaneA(ref Scene scene)
        {
            mScene = scene;
            mState = State.Ready;
        }

        public override void activate()
        {
        }

        public override void deactivate()
        {
        }
        
       

        protected override void onClickViveTrigger(ref VREvent_t vrEvent)
        {

            switch (mState)
            {
                case State.Ready:
                    // Create the plane in front of the controller
                    Matrix4 M = mScene.mDevicePose[vrEvent.trackedDeviceIndex];
                    origin = Util.transformPoint(M, new Vector3(0, 0, -0.12f));
                    normal = Util.transformVec(M, new Vector3(0, 0, 1));
                    Geometry.Geometry planeRep = new Geometry.PlaneRep();
                    Material.Material m = new Material.SingleColorMaterial(1, .8f, .8f, 1);
                    planeNode = new SceneNode("Plane", ref planeRep, ref m);
                    planeNode.parentToChild = new Matrix4(1, 0, 0, 0,
                                              0, 1, 0, 0,
                                              0, 0, 1, -0.12f,
                                              0, 0, 0, 1);

                    if (vrEvent.trackedDeviceIndex == mScene.leftControllerIdx)
                    {
                        mScene.leftControllerNode.add(ref planeNode);
                    } else
                    {
                        mScene.rightControllerNode.add(ref planeNode);
                    }
                        

                    //mScene.staticGeometry.add(ref n);
                    //Rhino.Geometry.Brep(rhinoPlane);
                    //mScene.rhinoDoc.Objects.Add(rhinoPlane)
                    // TODO: We can't see it yet!

                    mState = State.PlaneCreated;
                    break;

                case State.PlaneCreated:
                    // Leave the graphical rep in the scene
                    Matrix4 accumulatedTransform = planeNode.accumulateTransform();

                    //
                    planeNode.parent.remove(ref planeNode);
                    mScene.staticGeometry.add(ref planeNode);
                    planeNode.parentToChild = accumulatedTransform;

                    // Build in rhino
                    Rhino.Geometry.Plane rhinoPlane = new Rhino.Geometry.Plane(Util.openTkToRhinoPoint(origin), Util.openTkToRhinoVector(normal));

                    break;
            }


        }


    }
}
