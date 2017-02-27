using OpenTK;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    class Closedcurve : Interaction
    {
        public enum State
        {
            Ready = 0, Paint = 1
        };

        private State mCurrentState;
        public Geometry.Geometry mStrokeGeometry;
        public Geometry.Geometry mMeshGeometry;
        uint primaryDeviceIndex;
        List<Point3d> curvePoints = new List<Point3d>();

        Material.Material strokeMaterial;
        Material.Material meshMaterial;
        public Rhino.Geometry.NurbsCurve closedCurve;

        public Closedcurve(ref Scene s)
        {
            mScene = s;
            mCurrentState = State.Ready;
            strokeMaterial = new Material.SingleColorMaterial(1, 0, 0, 1);
            meshMaterial = new Material.SingleColorMaterial(0, 1, 0, 1);
        }

        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            if (mCurrentState == State.Ready)
                mStrokeGeometry = new Geometry.GeometryStroke();
                SceneNode node = new SceneNode("Closed curve stroke", ref mStrokeGeometry, ref strokeMaterial);
                mScene.mInteractionStack.Push(new Stroke(ref mScene, ref mStrokeGeometry, Stroke.State.Paint, vrEvent.trackedDeviceIndex));
                mCurrentState = State.Paint;
        }

        protected override void onReleaseOculusGrip(ref VREvent_t vrEvent)
        {
            if (mCurrentState == State.Paint)
            {
                renderMesh();
                mCurrentState = State.Ready;
            }
        }

        public void renderMesh()
        {

            //reduce the points in the curve first
            List<Vector3> mPoints = ((Geometry.GeometryStroke)(mStrokeGeometry)).mPoints;
            List<Vector3> reducePoints = new List<Vector3>();
            float pointReductionTubeWidth = 0.004f;
            reducePoints = Util.DouglasPeucker(ref mPoints, 0, mPoints.Count - 1, pointReductionTubeWidth);
            Rhino.RhinoApp.WriteLine("reduce points from" + mPoints.Count + " to " + curvePoints.Count);

            foreach (OpenTK.Vector3 point in reducePoints)
            {
                // -y_rhino = z_gl, z_rhino = y_gl
                curvePoints.Add(new Point3d(point.X, -point.Z, point.Y));
            }

            //Rhino curve and extrude test
            if (curvePoints.Count >= 2)
            {
                //Rhino closed curve through NURBS curve
                closedCurve = Rhino.Geometry.NurbsCurve.Create(true, 3, curvePoints.ToArray());
                //Rhino.Geometry.Curve nc = Curve.CreateInterpolatedCurve(curvePoints.ToArray(), 3);
                //nc.SetEndPoint(nc.PointAtStart);

                // TODO: find the right plane that we should project
                Plane proj_plane = new Plane();
                Plane.FitPlaneToPoints(curvePoints.ToArray(), out proj_plane);
                Curve proj_curve = Curve.ProjectToPlane(closedCurve, proj_plane);

                Brep[] shapes = Brep.CreatePlanarBreps(proj_curve);
                Brep curve_s = shapes[0];

                Mesh base_mesh = new Mesh();
                if (curve_s != null)
                {
                    Mesh[] meshes = Mesh.CreateFromBrep(curve_s, MeshingParameters.Default);

                    foreach (Mesh mesh in meshes)
                        base_mesh.Append(mesh);

                    mScene.rhinoDoc.Objects.AddMesh(base_mesh);
                    mScene.rhinoDoc.Views.Redraw();
                }

                mMeshGeometry = new Geometry.RhinoMesh();
                ((Geometry.RhinoMesh)mMeshGeometry).setMesh(ref base_mesh);
                SceneNode strokeMesh = new SceneNode("MeshStroke", ref mMeshGeometry, ref meshMaterial);
                mScene.tableGeometry.add(ref strokeMesh);
            }
        }


    }
}
