using OpenTK;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    class Sweep : Interaction
    {
        public enum State
        {
            READY = 0, PAINT = 1
        };

        private State currentState;
        public Geometry.Geometry target;
        public Geometry.Geometry meshStroke_g;
        uint primaryDeviceIndex;

        Material.Material stroke_m;
        Material.Material mesh_m;
        Rhino.Geometry.NurbsCurve closedCurve;

        public Sweep(ref Scene s)
        {
            mScene = s;
            target = new Geometry.GeometryStroke();
            activate();
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.SingleColorMaterial(0, 1, 0, 1);
        }

        public Sweep(ref Scene s, ref Rhino.Geometry.NurbsCurve curve)
        {
            mScene = s;
            target = new Geometry.GeometryStroke();
            activate();
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.SingleColorMaterial(0, 1, 0, 1);

            closedCurve = curve;
        }

        public void activate()
        {
            currentState = State.READY;
        }

        public void deactivate()
        {
            currentState = State.READY;

        }

        public void draw(bool inFront, int trackedDeviceIndex)
        {

            if (currentState != State.PAINT)
            {
                return;
            }

            Vector3 pos = Util.getTranslationVector3(mScene.mDevicePose[trackedDeviceIndex]);
            ((Geometry.GeometryStroke)target).addPoint(pos);

            if (((Geometry.GeometryStroke)target).mNumPrimitives == 1)
            {
                SceneNode stroke = new SceneNode("Stroke", ref target, ref stroke_m);
                mScene.staticGeometry.add(ref stroke);
            }

        }


        List<Point3d> curvePoints = new List<Point3d>();
        public void renderMesh()
        {

            //reduce the points in the curve first
            List<Vector3> mPoints = ((Geometry.GeometryStroke)(target)).mPoints;
            List<Vector3> reducePoints = new List<Vector3>();
            float pointReductionTubeWidth = 0.004f;
            reducePoints = DouglasPeucker(ref mPoints, 0, mPoints.Count - 1, pointReductionTubeWidth);
            Rhino.RhinoApp.WriteLine("reduce points from" + mPoints.Count + " to " + curvePoints.Count);

            foreach (OpenTK.Vector3 point in reducePoints)
            {
                // -y_rhino = z_gl, z_rhino = y_gl
                curvePoints.Add(new Point3d(point.X, -point.Z, point.Y));
            }

            //Rhino curve and extrude test
            if (curvePoints.Count >= 2)
            {
                //Rhino mesh test
                Rhino.Geometry.Curve rail = Rhino.Geometry.Curve.CreateInterpolatedCurve(curvePoints.ToArray(), 3);
                //Rhino.Geometry.BrepFace face = brep.Faces[0];
                //Rhino.Geometry.Brep brep2 = face.CreateExtrusion(curve, true);

                //create surface from rails, curve ans sweep. Note that sweep = new Rhino.Geometry.SweepOneRail() has more attributes
                Brep[] breps = Brep.CreateFromSweep(rail, closedCurve, false, mScene.rhinoDoc.ModelAbsoluteTolerance);
                Brep brep = breps[0];


                Mesh base_mesh = new Mesh();
                if (brep != null)
                {
                    Mesh[] meshes = Mesh.CreateFromBrep(brep, MeshingParameters.Default);

                    foreach (Mesh mesh in meshes)
                        base_mesh.Append(mesh);

                    mScene.rhinoDoc.Objects.AddMesh(base_mesh);
                    mScene.rhinoDoc.Views.Redraw();
                }

                meshStroke_g = new Geometry.RhinoMesh();
                ((Geometry.RhinoMesh)meshStroke_g).setMesh(ref base_mesh);
                SceneNode strokeMesh = new SceneNode("MeshStroke", ref meshStroke_g, ref mesh_m);
                mScene.staticGeometry.add(ref strokeMesh);

            }
        }

        protected override void onClickOculusTrigger(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("oculus button click event test");
        }

        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("oculus grip click event test");
            primaryDeviceIndex = vrEvent.trackedDeviceIndex;
            if (currentState == State.READY)
            {
                target = new Geometry.GeometryStroke();
                //SceneNode stroke = new SceneNode("Stroke", ref target, ref stroke_m);
                //mScene.staticGeometry.add(ref stroke);
                currentState = State.PAINT;

            }

        }

        protected override void onReleaseOculusGrip(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("oculus grip release event test");
            if (currentState == State.PAINT)
            {
                //target = new Geometry.GeometryStroke();
                renderMesh();
                currentState = State.READY;

            }
        }

        //Quick test about Douglas-Peucker for rhino points, return point3d with rhino coordinate system
        public List<Vector3> DouglasPeucker(ref List<Vector3> points, int startIndex, int lastIndex, float epsilon)
        {
            float dmax = 0f;
            int index = startIndex;

            for (int i = index + 1; i < lastIndex; ++i)
            {
                float d = PointLineDistance(points[i], points[startIndex], points[lastIndex]);
                if (d > dmax)
                {
                    index = i;
                    dmax = d;
                }
            }

            if (dmax > epsilon)
            {
                List<Vector3> res1 = DouglasPeucker(ref points, startIndex, index, epsilon);
                List<Vector3> res2 = DouglasPeucker(ref points, index, lastIndex, epsilon);

                //watch out the coordinate system
                List<Vector3> finalRes = new List<Vector3>();
                for (int i = 0; i < res1.Count - 1; ++i)
                {
                    finalRes.Add(res1[i]);
                }

                for (int i = 0; i < res2.Count; ++i)
                {
                    finalRes.Add(res2[i]);
                }

                return finalRes;
            }
            else
            {
                return new List<Vector3>(new Vector3[] { points[startIndex], points[lastIndex] });
            }
        }

        public float PointLineDistance(Vector3 point, Vector3 start, Vector3 end)
        {

            if (start == end)
            {
                return (float)Math.Sqrt(Math.Pow(point.X - start.X, 2) + Math.Pow(point.Y - start.Y, 2) + Math.Pow(point.Z - start.Z, 2));
            }

            Vector3 u = new Vector3(end.X - start.X, end.Y - start.Y, end.Z - start.Z);
            Vector3 pq = new Vector3(point.X - start.X, point.Y - start.Y, point.Z - start.Z);

            return Vector3.Cross(pq, u).Length / u.Length;


        }

    }
}
