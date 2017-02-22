using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparrowHawk.Geometry
{
    class GeometryStroke : Geometry
    {

        public int mNumPoints;
        public List<OpenTK.Vector3> mPoints = new List<OpenTK.Vector3>();
        public List<float> vertices_array = new List<float>();
        public List<int> indices_array = new List<int>();

        public GeometryStroke()
        {
            mNumPoints = 0;
            primitiveType = OpenTK.Graphics.OpenGL4.BeginMode.Lines;
        }

        public void addPoint(OpenTK.Vector3 p)
        {
            
            mPoints.Add(p);

            vertices_array.Add(p.X);
            vertices_array.Add(p.Y);
            vertices_array.Add(p.Z);

            indices_array.Add(2* mNumPoints);
            indices_array.Add(2 * mNumPoints + 1);

            mNumPoints++;

            mGeometry = vertices_array.ToArray();
            mGeometryIndices = indices_array.ToArray();
            mNumPrimitives = mNumPoints - 1;

        }

        public void truncate(int n)
        {
            if(n > mNumPoints)
            {
                Rhino.RhinoApp.WriteLine("no enough points");
            }else
            {
                mNumPoints = mNumPoints - n;
                vertices_array.RemoveRange(vertices_array.Count - mNumPoints * 3, mNumPoints * 3);
                indices_array.RemoveRange(indices_array.Count - mNumPoints * 2, mNumPoints * 2);

                mGeometry = vertices_array.ToArray();
                mGeometryIndices = indices_array.ToArray();
                mNumPrimitives = mNumPoints - 1;
            }
            
        }




    }
}
