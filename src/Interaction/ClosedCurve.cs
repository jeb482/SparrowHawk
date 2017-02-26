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

        private State currentState;
        public Geometry.Geometry target;
        public Geometry.Geometry meshStroke_g;
        uint primaryDeviceIndex;

        Material.Material stroke_m;
        Material.Material mesh_m;
        public Rhino.Geometry.NurbsCurve closedCurve;

        public Closedcurve(ref Scene s)
        {
            mScene = s;
            currentState = State.Ready;
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.SingleColorMaterial(0, 1, 0, 1);
        }

    
        override public void draw(bool inFront)
        {
        }


        List<Point3d> curvePoints = new List<Point3d>();
        public void renderMesh()
        {

            //reduce the points in the curve first
            List<Vector3> mPoints = ((Geometry.GeometryStroke)(target)).mPoints;
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

                meshStroke_g = new Geometry.RhinoMesh();
                ((Geometry.RhinoMesh)meshStroke_g).setMesh(ref base_mesh);
                SceneNode strokeMesh = new SceneNode("MeshStroke", ref meshStroke_g, ref mesh_m);
                mScene.tableGeometry.add(ref strokeMesh);
                

            }
        }

        

        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            if (currentState == State.Ready)
                target = new Geometry.GeometryStroke();
                SceneNode node = new SceneNode("Closed curve stroke", ref target, ref stroke_m);
                mScene.mInteractionStack.Push(new Stroke(ref mScene, ref target, Stroke.State.Paint, vrEvent.trackedDeviceIndex));
                currentState = State.Paint;
        }

        protected override void onReleaseOculusGrip(ref VREvent_t vrEvent)
        {
            if (currentState == State.Paint)
            {
                renderMesh();
                currentState = State.Ready;
            }
        }

   
    }
}
