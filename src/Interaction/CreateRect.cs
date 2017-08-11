using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparrowHawk.Interaction
{
    class CreateRect : Interaction
    {
        private Material.Material mesh_m;
        private Rhino.Geometry.NurbsCurve rectCurve;
        private Rhino.Geometry.Brep rectBrep;

        public CreateRect(ref Scene scene) : base(ref scene)
        {
            mesh_m = new Material.SingleColorMaterial(0, 1.0f, 0, 0.8f);
        }

        public override void init()
        {
            renderRect();
            mScene.popInteraction();
            mScene.peekInteraction().init();
        }

        //Curve-EditPoint-Revolve
        private void renderRect()
        {
            Point3d topLeftP = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, mScene.iPointList[0]));
            Point3d bottomRightP = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, mScene.iPointList[1]));

            //plane - testing Rectangle3d
            Rectangle3d rect = new Rectangle3d(mScene.iPlaneList[mScene.iPlaneList.Count - 1], topLeftP, bottomRightP);

            //testing transfrom
            /*
            Point3d c = rect.Center;
            OpenTK.Vector3 normal = new OpenTK.Vector3((float)mScene.iPlaneList[mScene.iPlaneList.Count - 1].Normal.X,
                                                       (float)mScene.iPlaneList[mScene.iPlaneList.Count - 1].Normal.Y,
                                                       (float)mScene.iPlaneList[mScene.iPlaneList.Count - 1].Normal.Z);

            OpenTK.Matrix4 transM = Util.getCoordinateTransM(new OpenTK.Vector3((float)c.X,(float)c.Y,(float)c.Z), new OpenTK.Vector3(0, 0, 0), normal, new OpenTK.Vector3(0,1,0));
            Transform t = Util.OpenTKToRhinoTransform(transM);
            rect.Transform(t);
            */

            rectCurve = rect.ToNurbsCurve();
            Brep[] shapes = Brep.CreatePlanarBreps(rectCurve);
            rectBrep = shapes[0];

            Util.addSceneNode(ref mScene, rectBrep, ref mesh_m, "rect");

            //add curve to mScene.iCurveList   
            mScene.iCurveList.Add(rectCurve);

            // Don't cleardrawing now
            // TODO-add editSize feature then clearDrawing
            clearDrawing();
            Util.clearPlanePoints(ref mScene);

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
