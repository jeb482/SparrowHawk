using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    public class Delete : Grip
    {

        public Delete(ref Scene s)
        {
            mScene = s;
            currentState = State.READY;
        }

        protected override void onReleaseOculusGrip(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("Oculus grip release event");
            if (currentState == State.SELECTION)
            {
                //remove the sceneNode and RhiObj. Update the Dictionary
                Util.removeSceneNode(ref mScene, selectedRhObj.Id);
                currentState = State.READY;
                mScene.rhinoDoc.Views.Redraw();
            }
        }

    }
}