using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using SparrowHawk.Geometry;
using OpenTK.Graphics.OpenGL4;

namespace SparrowHawk.Material
{

    class SingleColorMaterial : Material
    {
        OpenTK.Graphics.Color4 mColor; 

        public SingleColorMaterial(Rhino.RhinoDoc doc, float r, float g, float b, float a)
        {
            mColor = new OpenTK.Graphics.Color4(r, g, b, a);
            mShader = new GLShader(doc);
            mShader.init("SingleColorMaterial", ShaderSource.SingleColorVertShader, ShaderSource.SingleColorFragShader);


        }

        public override void draw(ref Geometry.Geometry g, ref Matrix4 model, ref Matrix4 vp)
        {
            mShader.bind();
            mShader.uploadAttrib<int>("indices", 3, 3, 4, VertexAttribPointerType.UnsignedInt, false, ref g.mGeometryIndices, 0);
            mShader.uploadAttrib<float>("position", 12, 3, 4, VertexAttribPointerType.Float, false, ref g.mGeometry, 0);
            GL.Uniform4(mShader.uniform("color"), mColor);
            GL.UniformMatrix4(mShader.uniform("modelTransform"), false, ref model);
            GL.UniformMatrix4(mShader.uniform("viewProjTransform"), false, ref vp);
        }
    }
}
