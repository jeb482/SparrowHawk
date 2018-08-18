using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparrowHawk.Geometry
{
    public class Polyline : Geometry
    {
        public Polyline()
        {
            mGeometry = new float[3 * 1024];
            mGeometryIndices = new int[2 * 1024];
            primitiveType = OpenTK.Graphics.OpenGL4.BeginMode.Lines;
            mNumPrimitives = 0;
        }

        /// <summary>
        /// Creates a polyline that matches the given array, indexed in consecutive pairs.
        /// </summary>
        /// <param name="vertices"></param>
        public Polyline(float[] vertices)
        {
            mGeometry = vertices;
            mNumPrimitives = vertices.Length / 3 / 2;
            mGeometryIndices = new int[mNumPrimitives * 2];
            for (int i = 0; i < mGeometryIndices.Length; i++)
                mGeometryIndices[i] = i;
            primitiveType = OpenTK.Graphics.OpenGL4.BeginMode.Lines;
        }

    }
}
