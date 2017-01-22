using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using OpenTK;

namespace SparrowHawk.Material
{
    class RGBNormalMaterial : Material
    {
        float mAlpha;
        

        public RGBNormalMaterial(float alpha, Rhino.RhinoDoc doc)
        {
            // Init Shader?
            mShader = new GLShader(doc);
            mShader.init("RGBNormal", ShaderSource.RGBNormalVertShader, ShaderSource.RGBNormalFragShader);
            mAlpha = alpha;
        }

        override public void draw(ref Geometry.Geometry g, ref Matrix4 model, ref Matrix4 vp)
        {
            // bind shader
            mShader.bind();
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            mShader.uploadAttrib("indices", g.mGeometryIndices.Count, 3, 4, VertexAttribPointerType.Int, true, g.mGeometryIndices, 0);
            mShader.uploadAttrib("position", g.mGeometry.Count, 3, 4, VertexAttribPointerType.Float, false, g.mGeometry, 0);
            mShader.uploadAttrib("normal", g.mNormals.Count, 3, 4, VertexAttribPointerType.Float, false, g.mNormals, 0);
            ErrorCode error = GL.GetError();


            int uni = mShader.uniform("modelTransform", false);
            error = GL.GetError();
            GL.UniformMatrix4(uni, false, ref model);
            error = GL.GetError();
            GL.UniformMatrix4(mShader.uniform("viewProjTransform", false), false, ref vp);
            error = GL.GetError();
            GL.Uniform1(mShader.uniform("alpha"), mAlpha);
            error = GL.GetError();
            mShader.drawArray(g.primitiveType, 0, g.mNumPrimitives);
        }

    }
}
