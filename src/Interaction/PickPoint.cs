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
        List<Matrix4> mPoses;
        public PickPoint(ref Scene scene) : base(ref scene)
        {
            mPoints = null;
            mPoses = null;
        }

        public PickPoint(ref Scene scene, ref List<Vector3> points) : base(ref scene)
        {
            mPoints = points;
            mPoses = null;
        }

        public PickPoint(ref Scene scene, ref List<Matrix4> poses) : base(ref scene)
        {
            mPoints = null;
            mPoses = poses;
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
            Matrix4 M = Util.getLeftControllerTipPosition(ref mScene, mScene.leftControllerIdx == deviceIndex);

            Vector3 origin = Util.transformPoint(M, new Vector3(0, 0, 0));
 
            if (mPoints != null)
            {
                mPoints.Add(origin);
            }

            if (mPoses != null)
            {
                mPoses.Add(M);
            }

            Rhino.RhinoApp.WriteLine("RegisterPoint: " + origin.ToString());

            // Add a Point Rep object to the mScene
            Geometry.Geometry point = new Geometry.PointMarker(new Vector3());
            Material.Material material = new Material.SingleColorMaterial(0f, .5f, 1f, 1f);
            SceneNode sceneNode = new SceneNode("point", ref point, ref material);
            sceneNode.transform = M;


            mScene.tableGeometry.add(ref sceneNode);
        }

    }
}
