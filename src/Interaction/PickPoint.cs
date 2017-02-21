using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;
using OpenTK;

namespace SparrowHawk.Interaction
{
    class PickPoint : Interaction
    {
        public PickPoint(ref Scene s)
        {
            mScene = s;
        }

        protected override void onClickViveTrigger(ref VREvent_t vrEvent)
        {
            // Find position of controller
            Matrix4 M = mScene.mDevicePose[vrEvent.trackedDeviceIndex];
            Vector4 origin = M * new Vector4(0,0,0,1);



            // Add a Point Rep object to the mScene
            Geometry.Geometry point = new Geometry.PointMarker(new Vector3());
            Material.Material material = new Material.SingleColorMaterial(0f, .5f, 1f, 1f);
            SceneNode sceneNode = new SceneNode("a", ref point, ref material);
            
            sceneNode.transform = M;
            mScene.tableGeometry.add(ref sceneNode);
            // Clean up (if necessary)
        }
    }
}
