using OpenTK;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    class Sweep : Stroke
    {

        public Geometry.Geometry meshStroke_g;
        Material.Material mesh_m;
        //Rhino.Geometry.NurbsCurve closedCurve;
        Rhino.Geometry.Curve closedCurve;
        List<Point3d> curvePoints = new List<Point3d>();

        public Sweep(ref Scene s)
        {

            mScene = s;
            stroke_g = new Geometry.GeometryStroke(ref mScene);
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.RGBNormalMaterial(.5f);
            currentState = State.READY;

        }

        public Sweep(ref Scene s, ref Rhino.Geometry.Brep brep)
        {
            mScene = s;
            stroke_g = new Geometry.GeometryStroke(ref mScene);
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.SingleColorMaterial(0, 1, 0, 1);
            closedCurve = brep.Curves3D.ElementAt(0);
            currentState = State.READY;
        }

        public override void draw(bool isTop)
        {
            base.draw(isTop);
        }


        public void renderSweep()
        {

            //reduce the points in the curve first
            simplifyCurve(ref ((Geometry.GeometryStroke)(stroke_g)).mPoints);

            foreach (OpenTK.Vector3 point in reducePoints)
            {
                // -y_rhino = z_gl, z_rhino = y_gl
                //OpenTK.Vector3 p = Util.transformPoint(Util.mGLToRhino, point);
                ///curvePoints.Add(new Point3d(p.X, p.Y, p.Z));
                curvePoints.Add(Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, point)));
            }

            //Rhino curve and extrude test
            if (curvePoints.Count >= 2)
            {
                Rhino.Geometry.Curve rail = Rhino.Geometry.Curve.CreateInterpolatedCurve(curvePoints.ToArray(), 3);
                //get the shape  first
                Curve[] overlap_curves;
                Point3d[] inter_points;
                Rhino.DocObjects.ObjectEnumeratorSettings settings = new Rhino.DocObjects.ObjectEnumeratorSettings();
                settings.ObjectTypeFilter = Rhino.DocObjects.ObjectType.Brep;
                foreach (Rhino.DocObjects.RhinoObject rhObj in mScene.rhinoDoc.Objects.GetObjectList(settings))
                {
                    if (rhObj.Attributes.Name == "plane")
                        continue;

                    if (Intersection.CurveBrep(rail, rhObj.Geometry as Brep, mScene.rhinoDoc.ModelAbsoluteTolerance, out overlap_curves, out inter_points))
                    {
                        if (overlap_curves.Length > 0 || inter_points.Length > 0)
                        {
                            closedCurve = ((Brep)rhObj.Geometry).Curves3D.ElementAt(0);
                            //testing open/close curve
                            closedCurve.SetEndPoint(closedCurve.PointAtStart);

                            Brep[] breps = Brep.CreateFromSweep(rail, closedCurve, false, mScene.rhinoDoc.ModelAbsoluteTolerance);
                            Brep brep = breps[0];

                            if (brep != null)
                            {
                                Util.addSceneNode(ref mScene, brep, ref mesh_m, "aprint");
                                Util.removeSceneNode(ref mScene, rhObj.Id);
                                mScene.rhinoDoc.Views.Redraw();
                            }
                            break;
                        }
                    }
                }

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
                foreach (SceneNode sn in mScene.tableGeometry.children)
                {
                    if (sn.guid == strokeId)
                    {
                        mScene.tableGeometry.children.Remove(sn);
                        break;
                    }
                }

                renderSweep();
                currentState = State.READY;

            }
        }

    }
}