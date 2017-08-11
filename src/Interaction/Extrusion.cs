using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparrowHawk.Interaction
{
    class Extrusion : Interaction
    {
        private Material.Material mesh_m;
        private Rhino.Geometry.NurbsCurve extrudeCurve;
        private Rhino.Geometry.Brep extrudeBrep;

        public Extrusion(ref Scene scene) : base(ref scene)
        {
            mesh_m = new Material.RGBNormalMaterial(0.5f);
        }

        public override void init()
        {
            renderExtrusion();
            mScene.popInteraction();
        }

        //Curve-EditPoint-Revolve
        private void renderExtrusion()
        {
            Curve railCurve = mScene.iCurveList[1];
            double height = Math.Abs(railCurve.PointAtStart.Z - railCurve.PointAtEnd.Z);

            Rhino.Geometry.Extrusion extrusion = Rhino.Geometry.Extrusion.Create(mScene.iCurveList[0], height, false);
            extrudeBrep = extrusion.ToBrep();

            Util.addSceneNode(ref mScene, extrudeBrep, ref mesh_m, "extrude");

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
