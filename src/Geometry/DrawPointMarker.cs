using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace SparrowHawk.Geometry
{

    public class DrawPointMarker : Geometry
    {
        private const float l = .003f; //.005f
        private const float sqrt3 = 1.73205080757f;
        /**
         *  Creates a point marker centered at p, where p is in VR-World-space.
         */
        public DrawPointMarker(Vector3 p)
        {
            primitiveType = OpenTK.Graphics.OpenGL4.BeginMode.Lines;
            float c = l / sqrt3;
            mNumPrimitives = 7;
            mGeometry = new float[]
                    {-l,0,0,
                      l,0,0,
                      0,-l,0,
                      0,l,0,
                      0,0,-l,
                      0,0,l,
                      -c,-c,-c,
                      c,c,c,
                      -c,-c,c,
                      c,c,-c,
                      -c,c,-c,
                      c,-c,c,
                      c,-c,-c,
                      -c,c,c };


            mGeometryIndices = new int[2 * mNumPrimitives];
            for (int i = 0; i < 2 * mNumPrimitives; i++)
            {
                mGeometryIndices[i] = i;
            }
        }
    }
}
