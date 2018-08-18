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
        public OpenTK.Graphics.Color4 mColor;

        public SingleColorMaterial(float r, float g, float b, float a)
        {
            mColor = new OpenTK.Graphics.Color4(r, g, b, a);
            mShader = new GLShader();
            mShader.init("SingleColorMaterial", ShaderSource.SingleColorVertShader, ShaderSource.SingleColorFragShader);
        }


        public override void setAlpha(float alpha)
        {
            mColor = new OpenTK.Graphics.Color4(mColor.R, mColor.G, mColor.B, alpha);
        }

        public void changeColor(float r, float g, float b, float a)
        {
            mColor = new OpenTK.Graphics.Color4(r, g, b, a);
        }

        public override void draw(ref Geometry.Geometry g, ref Matrix4 model, ref Matrix4 vp)
        {
            GL.Disable(EnableCap.DepthTest);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Blend);
            int dim;
            if (g.primitiveType == BeginMode.Lines) dim = 2; else dim = 3;
                mShader.bind();
            mShader.uploadAttrib<int>("indices", g.mGeometryIndices.Length, dim, 4, VertexAttribPointerType.UnsignedInt, false, ref g.mGeometryIndices, 0);
            mShader.uploadAttrib<float>("position", g.mGeometry.Count(), 3, 4, VertexAttribPointerType.Float, false, ref g.mGeometry, 0);
            GL.Uniform4(mShader.uniform("color"), mColor);
            GL.UniformMatrix4(mShader.uniform("modelTransform"), true, ref model);
            GL.UniformMatrix4(mShader.uniform("viewProjTransform"), false, ref vp); // TODO: Fix this 
            mShader.drawIndexed(g.primitiveType, 0, g.mNumPrimitives);
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);
        }
    }
}
