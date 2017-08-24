using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;

namespace SparrowHawk.Geometry
{
    class RhinoMesh : Geometry
    {

        public Mesh triMesh;
        public bool isRotate = true;
        private Scene mScene;
        private OpenTK.Matrix4 transM;

        public RhinoMesh()
        {
            mNumPrimitives = 0;
            primitiveType = OpenTK.Graphics.OpenGL4.BeginMode.Triangles;
        }

        public RhinoMesh(ref Scene s)
        {
            mScene = s;
            mNumPrimitives = 0;
            primitiveType = OpenTK.Graphics.OpenGL4.BeginMode.Triangles;
        }

        public RhinoMesh(ref Mesh mesh)
        {
            triMesh = Triangulate(mesh);
            initMeshGeometry(ref triMesh);
        }

        public RhinoMesh(ref Scene s, ref Mesh mesh)
        {
            mScene = s;
            triMesh = Triangulate(mesh);
            initMeshGeometry(ref triMesh);
        }

        public RhinoMesh(ref Scene s, OpenTK.Matrix4 t)
        {
            mScene = s;
            mNumPrimitives = 0;
            primitiveType = OpenTK.Graphics.OpenGL4.BeginMode.Triangles;
            transM = t;
            transM = transM.Inverted();
        }

        public void setMesh(ref Mesh mesh)
        {
            triMesh = Triangulate(mesh);
            initMeshGeometry(ref triMesh);
        }

        public void getTriMesh(ref Mesh mesh)
        {
            mesh = triMesh;
        }

        private void initMeshGeometry(ref Mesh triMesh)
        {
            //get faces from mesh
            List<MeshFace> faces = new List<MeshFace>();
            List<int> indices_array = new List<int>();
            foreach (MeshFace face in triMesh.Faces)
            {
                faces.Add(face);
                //TODO-loop throgn the original mesh to optimize
                if (face.IsQuad)
                {
                    Rhino.RhinoApp.WriteLine("Triangulate error.");
                }
                else
                {
                    indices_array.Add(face.A);
                    indices_array.Add(face.B);
                    indices_array.Add(face.C);
                }

            }

            mGeometryIndices = indices_array.ToArray();

            //get vertices from mesh
            // rhino coordinate system is different from OpenGL
            List<Point3d> vertices = new List<Point3d>();
            List<float> vertices_array = new List<float>();
            OpenTK.Matrix4 transToOrigin = new OpenTK.Matrix4();


            foreach (Point3d vertex in triMesh.Vertices)
            {
                vertices.Add(vertex);
                OpenTK.Vector3 rp = new OpenTK.Vector3((float)vertex.X, (float)vertex.Y, (float)vertex.Z);   
                /*          
                if(transM != OpenTK.Matrix4.Identity)
                {                 
                    rp = Util.transformPoint(transM, rp);

                }*/

                //seperate the data and visualization
                OpenTK.Vector3 p = Util.platformToVRPoint(ref mScene, rp);
                if(isRotate)
                    p = Util.transformPoint(mScene.tableGeometry.transform.Inverted(), p);
                vertices_array.Add(p.X);
                vertices_array.Add(p.Y);
                vertices_array.Add(p.Z);

            }

            mGeometry = vertices_array.ToArray();

            mNumPrimitives = mGeometryIndices.Length / 3;
            primitiveType = OpenTK.Graphics.OpenGL4.BeginMode.Triangles;
        }

        //Triangulate for mesh data from Rhino
        public Mesh Triangulate(Mesh x)
        {
            int facecount = x.Faces.Count;
            for (int i = 0; i < facecount; i++)
            {
                var mf = x.Faces[i];
                if (mf.IsQuad)
                {
                    double dist1 = x.Vertices[mf.A].DistanceTo(x.Vertices[mf.C]);
                    double dist2 = x.Vertices[mf.B].DistanceTo(x.Vertices[mf.D]);
                    if (dist1 > dist2)
                    {
                        x.Faces.AddFace(mf.A, mf.B, mf.D);
                        x.Faces.AddFace(mf.B, mf.C, mf.D);
                    }
                    else
                    {
                        x.Faces.AddFace(mf.A, mf.B, mf.C);
                        x.Faces.AddFace(mf.A, mf.C, mf.D);
                    }
                }
            }

            var newfaces = new List<MeshFace>();
            foreach (var mf in x.Faces)
            {
                if (mf.IsTriangle) newfaces.Add(mf);
            }

            x.Faces.Clear();
            x.Faces.AddFaces(newfaces);
            return x;
        }
    }
}
