using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparrowHawk.Interaction
{
    class CreateCircle2 : Interaction
    {
        private Material.Material mesh_m;
        private Rhino.Geometry.NurbsCurve circleCurve;
        private Rhino.Geometry.Brep circleBrep;

        public CreateCircle2(ref Scene scene) : base(ref scene)
        {
            mesh_m = new Material.SingleColorMaterial(0, 1.0f, 0, 0.8f);
        }

        public override void init()
        {
            renderCircle();
            mScene.popInteraction();
            mScene.peekInteraction().init();
        }

        //Curve-EditPoint-Revolve
        private void renderCircle()
        {
            Point3d origin = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, mScene.iPointList[0]));
            Point3d circleP = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, mScene.iPointList[1]));
            float radius = (float)Math.Sqrt(Math.Pow(circleP.X - origin.X, 2) + Math.Pow(circleP.Y - origin.Y, 2) + Math.Pow(circleP.Z - origin.Z, 2));
            Rhino.Geometry.Circle circle = new Rhino.Geometry.Circle(mScene.iPlaneList[mScene.iPlaneList.Count - 1], origin, radius);
            circleCurve = circle.ToNurbsCurve();
            Brep[] shapes = Brep.CreatePlanarBreps(circleCurve);
            Brep circle_s = shapes[0];
            circleBrep = circle_s;

            Util.addSceneNode(ref mScene, circleBrep, ref mesh_m, "circle");

            //add curve to mScene.iCurveList
            mScene.iCurveList.Add(circleCurve);

            // Don't cleardrawing now
            // TODO-add editSize feature then clearDrawing
            clearDrawing();

        }

        private void clearDrawing()
        {
            //clear the curve and points
            if (mScene.tableGeometry.children.Count > 0)
            {
                // need to remove rerverse since the list update dynamically
                foreach (SceneNode sn in mScene.tableGeometry.children.Reverse<SceneNode>())
                {
                    
                }
            }
        }
    }
}
