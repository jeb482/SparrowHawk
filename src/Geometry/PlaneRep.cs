using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparrowHawk.Geometry
{
    class PlaneRep : Geometry
    {
        public PlaneRep()
        {
            mNumPrimitives = 2;
            primitiveType = OpenTK.Graphics.OpenGL4.BeginMode.Triangles;
            mGeometry = new float[] { -12.5f,12.5f,0,  12.5f,12.5f,0,  12.5f,-12.5f,0,   -12.5f,-12.5f,0};
            mGeometryIndices = new int[] { 2, 1, 0, 2, 3, 1 };
            mNormals = new float[] { 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1 };  
        }
    }
}
