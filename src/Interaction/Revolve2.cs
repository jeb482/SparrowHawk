using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparrowHawk.Interaction
{
    class Revolve2 : Interaction
    {
        private Material.Material mesh_m;

        public Revolve2(ref Scene scene) : base(ref scene)
        {
            mesh_m = new Material.LambertianMaterial(.7f, .7f, .7f, .3f);
        }

        public override void init()
        {
            renderRevolve();
            mScene.popInteraction();
        }

        //Curve-EditPoint-Revolve
        private void renderRevolve()
        {
            Line axis = new Line(new Point3d(0, 0, 0), new Point3d(0, 0, 1));
            RevSurface revsrf = RevSurface.Create(mScene.iCurveList[0], axis);

            Brep brepRevolve = Brep.CreateFromRevSurface(revsrf, false, false);
            Util.addSceneNode(ref mScene, brepRevolve, ref mesh_m, "aprint");

            clearDrawing();
            Util.clearPlanePoints(ref mScene);
            Util.clearCurveTargetRhObj(ref mScene);
        }

        private void clearDrawing()
        {
            //clear the curve and points
            if (mScene.tableGeometry.children.Count > 0)
            {
                // need to remove rerverse since the list update dynamically
                foreach (SceneNode sn in mScene.tableGeometry.children.Reverse<SceneNode>())
                {
                    if (sn.name == "EditCurve")
                    {
                        mScene.tableGeometry.children.Remove(sn);
                    }
                }
            }
        }

    }
}
