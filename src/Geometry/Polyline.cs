using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparrowHawk.Geometry
{
    public class Polyline : Geometry
    {
        Polyline()
        {
            mGeometry = new float[3 * 1024];
            mGeometryIndices = new int[3 * 1024];
            primitiveType = OpenTK.Graphics.OpenGL4.BeginMode.Lines;
            mNumPrimitives = 0;
        }

        

    }
}
