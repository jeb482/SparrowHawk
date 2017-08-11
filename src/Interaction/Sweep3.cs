using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparrowHawk.Interaction
{
    class Sweep3 : Interaction
    {
        private Material.Material mesh_m;

        public Sweep3(ref Scene scene) : base(ref scene)
        {
            mesh_m = new Material.RGBNormalMaterial(0.5f);
        }

        public override void init()
        {
            renderSweep();
            mScene.popInteraction();
        }

        //Curve-EditPoint-Revolve
        private void renderSweep()
        {
            //compute the normal of the first point of the rail curve
            NurbsCurve rail = (NurbsCurve)mScene.iCurveList[1];
            PolylineCurve railPL = rail.ToPolyline(0, 0, 0, 0, 0, 1, 1, 0, true);
            OpenTK.Vector3 railStartPoint = new OpenTK.Vector3((float)railPL.PointAtStart.X, (float)railPL.PointAtStart.Y, (float)railPL.PointAtStart.Z);
            OpenTK.Vector3 railNormal = new OpenTK.Vector3((float)railPL.TangentAtStart.X, (float)railPL.TangentAtStart.Y, (float)railPL.TangentAtStart.Z);

            OpenTK.Vector3 shapeCenter = Util.vrToPlatformPoint(ref mScene, mScene.iPointList[0]);
            OpenTK.Vector3 shapeNormal = new OpenTK.Vector3((float)mScene.iPlaneList[0].Normal.X, (float)mScene.iPlaneList[0].Normal.Y, (float)mScene.iPlaneList[0].Normal.Z);

            OpenTK.Matrix4 transM = Util.getCoordinateTransM(shapeCenter, railStartPoint, shapeNormal, railNormal);
            Transform t = Util.OpenTKToRhinoTransform(transM);
            ((NurbsCurve)mScene.iCurveList[0]).Transform(t);
            NurbsCurve circleCurve = (NurbsCurve)mScene.iCurveList[0];

            //another solution-create a new circle at startpoint
            /*
            Plane plane = new Plane(railPL.PointAtStart, railPL.TangentAtStart);
            Point3d origin = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, mScene.iPointList[0]));
            Point3d circleP = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, mScene.iPointList[1]));
            float radius = (float)Math.Sqrt(Math.Pow(circleP.X - origin.X, 2) + Math.Pow(circleP.Y - origin.Y, 2) + Math.Pow(circleP.Z - origin.Z, 2));
            Rhino.Geometry.Circle circle = new Rhino.Geometry.Circle(plane, railPL.PointAtStart, radius);
            NurbsCurve circleCurve = circle.ToNurbsCurve();
            */

            //cruves coordinate are in rhino
            Brep[] breps = Brep.CreateFromSweep(mScene.iCurveList[1], circleCurve, true, mScene.rhinoDoc.ModelAbsoluteTolerance);
            Brep brep = breps[0];
            if (brep != null)
            {
                Util.addSceneNode(ref mScene, brep, ref mesh_m, "aprint");
            }

            //TODO- it's bad to clear panel, circle and points here
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
                    if (sn.name == "panel" || sn.name == "circle")
                    {
                        mScene.tableGeometry.children.Remove(sn);
                    }
                }
            }
        }
    }
}
