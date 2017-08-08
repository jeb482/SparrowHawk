using System;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using Rhino.Geometry;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    public delegate void revolveFun(ref Scene mScene, ref Material.Material mesh_m, ref NurbsCurve curve);

    class Revolve : Stroke
    {
        public Geometry.Geometry meshStroke_g;
        protected Material.Material mesh_m;
        private Rhino.Geometry.NurbsCurve revolveCurve;
        List<Point3d> curvePoints = new List<Point3d>();


        public Revolve(ref Scene scene) : base(ref scene)
        {
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.RGBNormalMaterial(0.5f);

        }

        public Revolve(ref Scene scene, bool drawOnP) : base(ref scene)
        {
            stroke_g = new Geometry.GeometryStroke(ref mScene);
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.RGBNormalMaterial(0.5f);
            currentState = State.READY;

            onPlane = drawOnP;

            if (onPlane)
            {
                //clear previous drawpoint
                foreach (SceneNode sn in mScene.tableGeometry.children)
                {
                    if (sn.name == "drawPoint")
                    {
                        mScene.tableGeometry.children.Remove(sn);
                        break;
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

                revolveFun rf = revolveF;
            }

        }

        private void renderRevolve()
        {
            //reduce the points in the curve first
            if(((Geometry.GeometryStroke)(stroke_g)).mPoints.Count >= 2){
                simplifyCurve(ref ((Geometry.GeometryStroke)(stroke_g)).mPoints);

                foreach (OpenTK.Vector3 point in reducePoints)
                {
                    // -y_rhino = z_gl, z_rhino = y_gl and unit conversion
                    // OpenTK.Vector3 p = Util.transformPoint(Util.mGLToRhino, point*1000);              
                    //curvePoints.Add(new Point3d(p.X, p.Y, p.Z));
                    //TODO: point is not the real position we want, we want rotation * point
                    //testing
                    //OpenTK.Vector3 p = Util.transformPoint(mScene.tableGeometry.transform, point);
                    curvePoints.Add(Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, point)));
                }

                //Rhino CreateInterpolatedCurve and CreatePlanarBreps
                if (curvePoints.Count >= 4)
                {
                    revolveCurve = Rhino.Geometry.NurbsCurve.Create(false, 3, curvePoints.ToArray());
                    //do it after edit point interaction
                    //revolveF(ref mScene, ref mesh_m, ref revolveCurve);
                }
            }

        }

        public void revolveF(ref Scene mScene, ref Material.Material mesh_m, ref NurbsCurve curve)
        {
            Line axis = new Line(new Point3d(0, 0, 0), new Point3d(0, 0, 1));
            RevSurface revsrf = RevSurface.Create(curve, axis);

            Brep brepRevolve = Brep.CreateFromRevSurface(revsrf, false, false);
            Util.addSceneNode(ref mScene, brepRevolve, ref mesh_m, "aprint");
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
                foreach (SceneNode sn in mScene.tableGeometry.children)
                {
                    if (sn.guid == strokeId)
                    {
                        mScene.tableGeometry.children.Remove(sn);
                        break;
                    }
                }
                //render after edit point interaction
                renderRevolve();
                //testing edit point interaction
                if (onPlane && revolveCurve!=null && targetPRhObj != null)
                {
                    List<Curve> curveL = new List<Curve>();
                    curveL.Add(revolveCurve);
                    mScene.popInteraction();
                    mScene.pushInteraction(new EditPoint(ref mScene, ref targetPRhObj, true, curveL, Guid.Empty, "Revolve"));
                }
                currentState = State.READY;
            }
        }


    }
}
