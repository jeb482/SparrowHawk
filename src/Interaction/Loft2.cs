using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    class Loft2 : Interaction
    {
        private List<Curve> loftcurves = new List<Curve>();
        private Material.Material mesh_m;

        public Loft2(ref Scene scene) : base(ref scene)
        {
            //mesh_m = new Material.RGBNormalMaterial(0.5f);
            mesh_m = new Material.LambertianMaterial(.7f, .7f, .7f, .2f);
        }

        public override void init()
        {

            foreach (Curve curve in mScene.iCurveList)
            {
                loftcurves.Add(curve);
            }

            renderLoft();
            mScene.popInteraction();

        }

        //Curve-EditPoint-Revolve
        private void renderLoft()
        {
            Brep[] loftBreps = Brep.CreateFromLoft(loftcurves, Point3d.Unset, Point3d.Unset, LoftType.Tight, false);
            Brep brep = new Brep();
            foreach (Brep bp in loftBreps)
            {
                brep.Append(bp);
            }

            Mesh base_mesh = new Mesh();
            // TODO: fix the issue that sometimes the brep is empty. Check the directions of open curves or the seams of closed curves. 
            if (brep != null && brep.Edges.Count != 0)
            {
                Util.addSceneNode(ref mScene, brep, ref mesh_m, "aprint");
            }

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
