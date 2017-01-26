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
        int vboPositions;
        int vboNormals;
        int ibo;
        int vao;        

        public RGBNormalMaterial(float alpha, Rhino.RhinoDoc doc)
        {
            // Init Shader?
            mShader = new GLShader(doc);
            mShader.init("RGBNormal", ShaderSource.RGBNormalVertShader, ShaderSource.RGBNormalFragShader);
            mAlpha = alpha;

            mShader.bind();
            vboPositions = GL.GenBuffer();
            float[] pos = { -1f, -1f, 0f, 1f, -1f, 0f, 0f, 1f, 0f };
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboPositions);
            GL.BufferData<float>(BufferTarget.ArrayBuffer, 12 * 4, pos, BufferUsageHint.DynamicDraw);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.EnableVertexAttribArray(0);
        }

        override public void draw(ref Geometry.Geometry g, ref Matrix4 model, ref Matrix4 vp)
        {
            // bind shader
            mShader.bind();



            // LALALA
           // GL.Enable(EnableCap.Blend);
           // GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

           // //GL.BindBuffer(BufferTarget.ElementArrayBuffer, in);
           // mShader.uploadAttrib("indices", g.mGeometryIndices.Count, 3, 4, VertexAttribPointerType.Int, true, g.mGeometryIndices, 0);
           // mShader.uploadAttrib("position", g.mGeometry.Count, 3, 4, VertexAttribPointerType.Float, false, g.mGeometry, 0);
           // mShader.uploadAttrib("normal", g.mNormals.Count, 3, 4, VertexAttribPointerType.Float, false, g.mNormals, 0);
           // ErrorCode error = GL.GetError();


           // Matrix4 mvp = model * vp;
           // Vector4 t = mvp * new Vector4(1,1,1,1);
           // Vector4 o = mvp * new Vector4(0, 0, 0, 1);

           // int uni = mShader.uniform("modelTransform", false);
           // error = GL.GetError();
           // GL.UniformMatrix4(uni, false, ref model);
           // error = GL.GetError();
           // GL.UniformMatrix4(mShader.uniform("viewProjTransform", false), true, ref vp);
           // error = GL.GetError();
           // GL.Uniform1(mShader.uniform("alpha"), mAlpha);
           // error = GL.GetError();
           // GL.DrawElements(PrimitiveType.Triangles, 24, DrawElementsType.UnsignedInt, 0);
           //// mShader.drawIndexed(g.primitiveType, 0, g.mNumPrimitives);
            //mShader.drawArray(g.GetType, 0, g.mNumPrimitives)
        }

    }
}
