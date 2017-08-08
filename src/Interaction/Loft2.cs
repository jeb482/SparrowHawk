using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    class Loft2 : Stroke
    {
        List<Point3d> curvePoints = new List<Point3d>();

        public Loft2(ref Scene scene) : base(ref scene)
        {
            stroke_g = new Geometry.GeometryStroke(ref mScene);
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);

            currentState = State.READY;

        }

        public Loft2(ref Scene scene, bool drawOnP) : base(ref scene)
        {
            stroke_g = new Geometry.GeometryStroke(ref mScene);
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            currentState = State.READY;

            onPlane = drawOnP;

            if (onPlane)
            {
                //clear previous drawpoint
                if (mScene.tableGeometry.children.Count > 0)
                {
                    foreach (SceneNode sn in mScene.tableGeometry.children)
                    {
                        if (sn.name == "drawPoint")
                        {
                            mScene.tableGeometry.children.Remove(sn);
                            break;
                        }
                    }
                }

                Geometry.Geometry geo = new Geometry.PointMarker(new OpenTK.Vector3(0, 0, 0));
                Material.Material m = new Material.SingleColorMaterial(250 / 255, 128 / 255, 128 / 255, 0.5f);
                drawPoint = new SceneNode("drawPoint", ref geo, ref m);
                drawPoint.transform = new OpenTK.Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
                mScene.tableGeometry.add(ref drawPoint);

                //TODO-support both controllers
                if (mScene.mIsLefty)
                    primaryDeviceIndex = (uint)mScene.leftControllerIdx;
                else
                    primaryDeviceIndex = (uint)mScene.rightControllerIdx;

            }

        }

        public override void draw(bool isTop)
        {
            base.draw(isTop);
        }

        protected override void onClickOculusTrigger(ref VREvent_t vrEvent)
        {
            curvePoints = new List<Point3d>();
            base.onClickOculusTrigger(ref vrEvent);

        }

        protected override void onReleaseOculusTrigger(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("oculus grip release event test");
            if (currentState == State.PAINT)
            {

                //clear the stroke
                /*
                foreach (SceneNode sn in mScene.tableGeometry.children)
                {
                    if (sn.guid == strokeId)
                    {
                        mScene.tableGeometry.children.Remove(sn);
                        break;
                    }
                }*/

                //renderSweep();
                currentState = State.READY;

            }
        }
    }
}
