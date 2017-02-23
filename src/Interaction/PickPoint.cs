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
        List<Vector3> mPoints;
        public PickPoint(ref Scene s)
        {
            mScene = s;
            mPoints = null;
        }

        public PickPoint(ref Scene s, ref List<Vector3> points)
        {
            mScene = s;
            mPoints = points;
            
        }

        protected override void onClickViveTrigger(ref VREvent_t vrEvent)
        {
            registerPositon(vrEvent.trackedDeviceIndex);
        }

        protected override void onClickOculusTrigger(ref VREvent_t vrEvent)
        {
            registerPositon(vrEvent.trackedDeviceIndex);
        }

        protected void registerPositon(uint deviceIndex)
        {
            // Find and record position of controller
            Matrix4 M = mScene.mDevicePose[deviceIndex];
            Vector4 origin = M * new Vector4(0, 0, 0, 1);
            Vector3 origin2 = Util.transformPoint(M, new Vector3(0, 0, 0));
            Vector4 rando = M * new Vector4(1, 2, 3, 0);
            Vector3 rando2 = Util.transformVec(M, new Vector3(1, 2, 3));
            if (mPoints != null)
            {
                mPoints.Add(origin2);
            }


            // Add a Point Rep object to the mScene
            Geometry.Geometry point = new Geometry.PointMarker(new Vector3());
            Material.Material material = new Material.SingleColorMaterial(0f, .5f, 1f, 1f);
            SceneNode sceneNode = new SceneNode("point", ref point, ref material);

            sceneNode.transform = M;
            mScene.tableGeometry.add(ref sceneNode);
        }

    }
}
