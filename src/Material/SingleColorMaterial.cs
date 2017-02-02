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
        Rhino.RhinoDoc mDoc;

        public SingleColorMaterial(Rhino.RhinoDoc doc, float r, float g, float b, float a)
        {
            mDoc = doc;
            mColor = new OpenTK.Graphics.Color4(r, g, b, a);
            mShader = new GLShader(doc);
            mShader.init("SingleColorMaterial", ShaderSource.SingleColorVertShader, ShaderSource.SingleColorFragShader);
            

        }

        public override void draw(ref Geometry.Geometry g, ref Matrix4 model, ref Matrix4 vp)
        {
            float[] fakeIndices = new float[] { -.5f, -.5f, 0, .5f, -.5f, 0, 0, .5f, 0 }; 
            ErrorCode e;
            mShader.bind();
           
            mShader.uploadAttrib<int>("indices",3, 3, 4, VertexAttribPointerType.UnsignedInt, false, ref g.mGeometryIndices, 0);
            e = GL.GetError();
            //mShader.uploadAttrib<float>("position", 9, 3, 4, VertexAttribPointerType.Float, false, ref fakeIndices, 0);
            mShader.uploadAttrib<float>("position", g.mGeometry.Count(), 3, 4, VertexAttribPointerType.Float, false, ref g.mGeometry, 0);
            e = GL.GetError();
            GL.Uniform4(mShader.uniform("color"), mColor);
            e = GL.GetError();
            GL.UniformMatrix4(mShader.uniform("modelTransform"), false, ref model);
            e = GL.GetError();

            GL.UniformMatrix4(mShader.uniform("viewProjTransform"), false, ref vp);
            float[] funMatrix = new float[16];
            GL.GetUniform(mShader.mProgramShader, mShader.uniform("viewProjTransform"), funMatrix);
            e = GL.GetError();
            mShader.drawIndexed(BeginMode.Triangles, 0, 3);
        }
    }
}
