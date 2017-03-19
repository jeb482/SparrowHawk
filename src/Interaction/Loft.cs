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
    class Loft : Stroke
    {

        public Geometry.Geometry meshStroke_g;
        protected Material.Material mesh_m;
        List<Point3d> curvePoints = new List<Point3d>();
        List<Curve> loftcurves = new List<Curve>();
        List<Guid> loftObjsUID = new List<Guid>();


        public Loft(ref Scene s)
        {
            mScene = s;
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.RGBNormalMaterial(1); ;
        }

        public Loft(ref Scene s, ref Rhino.Geometry.Brep[] brep)
        {
            mScene = s;
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.RGBNormalMaterial(1); ;

        }

        public override void draw(bool isTop)
        {
            base.draw(isTop);
        }

        public void renderLoft()
        {

            //reduce the points in the curve first
            simplifyCurve(ref ((Geometry.GeometryStroke)(stroke_g)).mPoints);

            foreach (OpenTK.Vector3 point in reducePoints)
            {
                // -y_rhino = z_gl, z_rhino = y_gl
                //OpenTK.Vector3 p = Util.transformPoint(Util.mGLToRhino, point);
                //curvePoints.Add(new Point3d(p.X, p.Y, p.Z));
                curvePoints.Add(Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, point)));
            }

            //Rhino curve and extrude test
            if (curvePoints.Count >= 2)
            {
                //Rhino mesh test
                Rhino.Geometry.Curve rail = Rhino.Geometry.Curve.CreateInterpolatedCurve(curvePoints.ToArray(), 3);
                //Rhino.Geometry.BrepFace face = brep.Faces[0];
                //Rhino.Geometry.Brep brep2 = face.CreateExtrusion(curve, true);

                //get the shape curve first

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
                            //testing open/close curve
                            Curve loftcurve = ((Brep)rhObj.Geometry).Curves3D.ElementAt(0);
                            loftcurve.SetEndPoint(loftcurve.PointAtStart);
                            loftcurves.Add(loftcurve);
                            loftObjsUID.Add(rhObj.Id);
                        }
                    }
                }

                //Loft
                if (loftcurves.Count > 1)
                {
                    //check the direction of the curves
                    for (int i = 0; i < loftcurves.Count - 1; i++)
                    {
                        double t = 0;


                        if (!Curve.DoDirectionsMatch(loftcurves.ElementAt(i), loftcurves.ElementAt(i + 1)))
                        {
                            //whether curve is open or closed
                            loftcurves.ElementAt(i + 1).Reverse();

                        }

                        if (i == 0)
                        {
                            loftcurves.ElementAt(i).ClosestPoint(loftcurves.ElementAt(i).PointAtStart, out t);
                            loftcurves.ElementAt(i).ChangeClosedCurveSeam(t);
                        }

                        loftcurves.ElementAt(i + 1).ClosestPoint(loftcurves.ElementAt(i).PointAtStart, out t);
                        loftcurves.ElementAt(i + 1).ChangeClosedCurveSeam(t);
                        //loftcurves.ElementAt(i + 1).SetStartPoint(loftcurves.ElementAt(i + 1).PointAt(t));


                    }

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

                        Util.addSceneNode(ref mScene, brep, ref mesh_m);

                        //remove the shape surfaces of the loft
                        foreach (Guid id in loftObjsUID)
                        {
                            Util.removeSceneNode(ref mScene, id);
                        }
                        mScene.rhinoDoc.Views.Redraw();
                    }
                }

            }
        }


        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            curvePoints.Clear();
            loftcurves.Clear();
            loftObjsUID.Clear();
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

                renderLoft();
                currentState = State.READY;

            }
        }

    }
}