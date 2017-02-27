using OpenTK;
using Rhino.Geometry;
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
            Ready = 0, Paint = 1
        };

        private State mCurrentState;
        public Geometry.Geometry mTarget;
        uint mPrimaryDeviceIndex;

        Material.Material stroke_m;
        Material.Material mesh_m;
        Rhino.Geometry.Brep brep;

        // Pops this interaction of the stack after releasing stroke if true.
        bool mPopAfterStroke = false;

        /// <summary>
        /// Default stroke interaction.
        /// </summary>
        /// <param name="s">The scene</param>
        public Stroke(ref Scene s)
        {
            mScene = s;
            mCurrentState = State.Ready;
            mTarget = new Geometry.GeometryStroke();
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.SingleColorMaterial(0, 1, 0, 1);
        }

        public Stroke(ref Scene s, ref Rhino.Geometry.Brep brepObj)
        {
            mScene = s;
            mCurrentState = State.Ready;
            mTarget = new Geometry.GeometryStroke();
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.SingleColorMaterial(0, 1, 0, 1);

            brep = brepObj;
        }

        /// <summary>
        /// Creates a stroke in order to populate an existent piece of geometry. 
        /// The interaction will be popped of the stack (and therefore disappear)
        /// after a stroke is completed. Can start in either state, but releasing
        /// the grip is the only thing that will complete the stroke.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="target">The geometry to populate. NOTE: This should be a 
        /// GeometryStroke but C# will not allow this because of type safety.</param>
        /// <param name="currentState">The starting state. Probably == State.Paint</param>
        /// <param name="devIndex">The controller index responsible for this interaction.</param>
        public Stroke(ref Scene s, ref Geometry.Geometry target, State currentState, uint devIndex)
        {
            mScene = s;
            mTarget = target;
            mCurrentState = currentState;
            mPrimaryDeviceIndex = devIndex;
            mPopAfterStroke = true;
        }


        public void draw(bool inFront, int trackedDeviceIndex)
        {
            if (mCurrentState != State.Paint) 
                return;
         
            Vector3 pos = Util.getTranslationVector3(mScene.mDevicePose[trackedDeviceIndex]);
            ((Geometry.GeometryStroke)mTarget).addPoint(pos);
        }

        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            if (mCurrentState == State.Ready)
            {
                // Make a new stroke if you don't already have to work with.
                if (mTarget == null)
                {
                    mTarget = new Geometry.GeometryStroke();
                    Material.Material m = new Material.SingleColorMaterial(.7f, .7f, .7f, 1);
                    SceneNode stroke = new SceneNode("Stroke", ref mTarget, ref m);
                    mScene.tableGeometry.add(ref stroke);
                }
                // Switch into draw mode.
                mPrimaryDeviceIndex = vrEvent.trackedDeviceIndex;
                mCurrentState = State.Paint;
            }
        }


        protected override void onReleaseOculusGrip(ref VREvent_t vrEvent)
        {
            if (mCurrentState == State.Paint)
            {
                if (mPopAfterStroke)
                {
                    mScene.mInteractionStack.Pop();
                    return;
                }
                mTarget = null;
                mCurrentState = State.Ready;
            }
        }
    }
}
