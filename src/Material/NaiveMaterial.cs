using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using SparrowHawk.Geometry;
using OpenTK.Graphics.OpenGL4;

namespace SparrowHawk.Material
{

    class NaiveMaterial : Material
    {
        public NaiveMaterial(Rhino.RhinoDoc doc)
        {
            mDoc = doc;
            mShader = new GLShader(doc);
            mShader.init("DumbShader", ShaderSource.NaiveVertexShader, ShaderSource.NaiveVertexShader);
        }

        public override void draw(ref Geometry.Geometry g, ref Matrix4 model, ref Matrix4 vp)
        {
            mShader.bind();
            mShader.uploadAttrib<int>("indices", 3, 3, 4, VertexAttribPointerType.UnsignedInt, false, ref g.mGeometryIndices, 0);
            mShader.uploadAttrib<float>("position", 12, 3, 4, VertexAttribPointerType.Float, false, ref g.mGeometry, 0);
            mShader.drawIndexed(BeginMode.Triangles, 0, 3);
        }
    }
}
