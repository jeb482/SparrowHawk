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
        List<Point> allPoints = new List<Point>();
        SceneNode planeSN;
        private bool lockPlane = false;

        public CreatePatch(ref Scene s) : base(ref s)
        {
            mScene = s;
            stroke_g = new Geometry.GeometryStroke(ref mScene);
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.RGBNormalMaterial(.5f);
            currentState = State.READY;

            //TODO-support both controllers
            if (mScene.mIsLefty)
                primaryDeviceIndex = (uint)mScene.leftControllerIdx;
            else
                primaryDeviceIndex = (uint)mScene.rightControllerIdx;

            UtilOld.showLaser(ref mScene, false);

        }

        public CreatePatch(ref Scene s, bool drawOnP) : base(ref s)
        {
            stroke_g = new Geometry.GeometryStroke(ref mScene);
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.RGBNormalMaterial(.5f);
            currentState = State.READY;

            onPlane = drawOnP;

            if (onPlane)
            {
                Geometry.Geometry geo = new Geometry.PointMarker(new OpenTK.Vector3(0, 0, 0));
                Material.Material m = new Material.SingleColorMaterial(250 / 255, 128 / 255, 128 / 255, 1);
                drawPoint = new SceneNode("Point", ref geo, ref m);
                drawPoint.transform = new OpenTK.Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
                mScene.tableGeometry.add(ref drawPoint);

                //TODO-support both controllers
                if (mScene.mIsLefty)
                    primaryDeviceIndex = (uint)mScene.leftControllerIdx;
                else
                    primaryDeviceIndex = (uint)mScene.rightControllerIdx;

            }

        }

        public override void draw(bool inTop)
        {
            base.draw(inTop);

        }

        public void renderPatch()
        {
            //Brep patchSurface = Brep.CreatePatch(curvelist, 4, 4, mScene.rhinoDoc.ModelAbsoluteTolerance);
            Brep patchSurface = Brep.CreatePatch(allPoints, 10, 10, mScene.rhinoDoc.ModelAbsoluteTolerance);          
            Guid planGuid = UtilOld.addRhinoObjectSceneNode(ref mScene, ref patchSurface, ref mesh_m, "patchSurface", out planeSN);

            //clear profile curves
            foreach (Guid id in curveGuids)
            {
                foreach (SceneNode sn in mScene.tableGeometry.children)
                {
                    if (sn.guid == id)
                    {
                        mScene.tableGeometry.children.Remove(sn);
                        break;
                    }
                }
            }

            allPoints.Clear();
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
                curvePoints.Add(UtilOld.openTkToRhinoPoint(UtilOld.vrToPlatformPoint(ref mScene, point)));
                allPoints.Add(new Point(UtilOld.openTkToRhinoPoint(UtilOld.vrToPlatformPoint(ref mScene, point))));
            }

            //Rhino CreateInterpolatedCurve and CreatePlanarBreps
            if (curvePoints.Count >= 2)
            {
                curve = Rhino.Geometry.NurbsCurve.Create(true, 3, curvePoints.ToArray());
                curvelist.Add(curve);
            }
        }

        protected override void onClickOculusAX(ref VREvent_t vrEvent)
        {
            renderPatch();
            currentState = State.READY;
            //TODO support undo funtion
            mScene.popInteraction();
        }

        protected override void onClickOculusTrigger(ref VREvent_t vrEvent)
        {
            curvePoints = new List<Point3d>();
            if (currentState == State.READY)
            {
                lockPlane = true;
                stroke_g = new Geometry.GeometryStroke(ref mScene);
                reducePoints = new List<Vector3>();
                currentState = State.PAINT;
            }

        }

        protected override void onReleaseOculusTrigger(ref VREvent_t vrEvent)
        {
            if (currentState == State.PAINT)
            {
                lockPlane = false;
                currentState = State.READY;
                curveGuids.Add(strokeId); //for clear curve after patch created
                renderCurve(); //update points
            }
        }

    }
}