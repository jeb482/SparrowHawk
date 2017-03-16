using OpenTK;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    class CreatePatch : Stroke
    {

        private Material.Material mesh_m;
        private Rhino.Geometry.NurbsCurve curve;
        private Rhino.Geometry.Brep closedCurveBrep;
        List<Point3d> curvePoints = new List<Point3d>();
        List<NurbsCurve> curvelist = new List<NurbsCurve>();
        List<Guid> curveGuids = new List<Guid>();

        public CreatePatch(ref Scene s)
        {
            mScene = s;
            stroke_g = new Geometry.GeometryStroke();
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.RGBNormalMaterial(1);
            currentState = State.READY;

        }

        public CreatePatch(ref Scene s, bool drawOnP)
        {
            mScene = s;
            stroke_g = new Geometry.GeometryStroke();
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.RGBNormalMaterial(1);
            currentState = State.READY;

            onPlane = drawOnP;

            if (onPlane)
            {
                Geometry.Geometry geo = new Geometry.PointMarker(new OpenTK.Vector3(0, 0, 0));
                Material.Material m = new Material.SingleColorMaterial(250 / 255, 128 / 255, 128 / 255, 1);
                drawPoint = new SceneNode("Point", ref geo, ref m);
                drawPoint.transform = new OpenTK.Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
                mScene.staticGeometry.add(ref drawPoint);

                //TODO-support both controllers
                primaryDeviceIndex = (uint)mScene.leftControllerIdx;
            }

        }

        public override void draw(bool inTop)
        {
            base.draw(inTop);

        }

        public void renderPatch()
        {
            Brep patchSurface = Brep.CreatePatch(curvelist, 4, 4, mScene.rhinoDoc.ModelAbsoluteTolerance);

            Util.addSceneNode(ref mScene, patchSurface, ref mesh_m);
            mScene.rhinoDoc.Views.Redraw();

            foreach (Guid id in curveGuids)
            {
                foreach (SceneNode sn in mScene.staticGeometry.children)
                {
                    if (sn.guid == id)
                    {
                        mScene.staticGeometry.children.Remove(sn);
                        break;
                    }
                }
            }

            curvelist.Clear();
        }

        public void renderCurve()
        {
            //reduce the points in the curve first
            simplifyCurve(ref ((Geometry.GeometryStroke)(stroke_g)).mPoints);

            foreach (OpenTK.Vector3 point in reducePoints)
            {
                // -y_rhino = z_gl, z_rhino = y_gl and unit conversion
                // OpenTK.Vector3 p = Util.transformPoint(Util.mGLToRhino, point*1000);              
                //curvePoints.Add(new Point3d(p.X, p.Y, p.Z));
                curvePoints.Add(Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, point)));
            }

            //Rhino CreateInterpolatedCurve and CreatePlanarBreps
            if (curvePoints.Count >= 2)
            {
                curve = Rhino.Geometry.NurbsCurve.Create(true, 3, curvePoints.ToArray());
                curvelist.Add(curve);

            }
        }

        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            curvePoints = new List<Point3d>();
            base.onClickOculusGrip(ref vrEvent);


        }

        protected override void onReleaseOculusGrip(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("oculus grip release event test");
            if (currentState == State.PAINT)
            {
                //clear the stroke
                /*
                foreach (SceneNode sn in mScene.staticGeometry.children)
                {
                    if (sn.guid == strokeId)
                    {
                        mScene.staticGeometry.children.Remove(sn);
                        break;
                    }
                }*/

                curveGuids.Add(strokeId);
                renderCurve();
                if (curvelist.Count == 8)
                {
                    renderPatch();
                }
                currentState = State.READY;           
            }
        }

    }
}