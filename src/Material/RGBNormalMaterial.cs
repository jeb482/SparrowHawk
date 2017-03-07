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

        public RGBNormalMaterial(float alpha)
        {
            mAlpha = alpha;
            mShader = new GLShader();
            mShader.init("RGBNormalMaterial", ShaderSource.RGBNormalVertShader, ShaderSource.RGBNormalFragShader);
        }

        override public void draw(ref Geometry.Geometry g, ref Matrix4 model, ref Matrix4 vp)
        {
            // bind shader
            mShader.bind();

            Matrix4 modelIT = model.Inverted();
            modelIT.Transpose();

           // GL.Enable(EnableCap.Blend);
            mShader.uploadAttrib<int>("indices", g.mGeometryIndices.Length, 3, 4, VertexAttribPointerType.UnsignedInt, false, ref g.mGeometryIndices, 0);
            mShader.uploadAttrib<float>("position", g.mGeometry.Count(), 3, 4, VertexAttribPointerType.Float, false, ref g.mGeometry, 0);
            if (g.mNormals == null)
                Util.addNormalsToMesh(g); // TODO: I literally hate this, but it was easier than patching Eric's code.
            if (g.mNormals != null)
                mShader.uploadAttrib<float>("normal", g.mNormals.Count(), 3, 4, VertexAttribPointerType.Float, false, ref g.mNormals, 0);
            GL.Uniform1(mShader.uniform("alpha"), mAlpha);
            GL.ProgramUniformMatrix4(mShader.mProgramShader ,mShader.uniform("viewProjTransform"), false, ref vp);
            GL.ProgramUniformMatrix4(mShader.mProgramShader, mShader.uniform("modelTransform"), true, ref model);
            GL.ProgramUniformMatrix4(mShader.mProgramShader, mShader.uniform("modelInvTrans"), true, ref modelIT);

            mShader.drawIndexed(g.primitiveType, 0, g.mNumPrimitives);
            GL.Disable(EnableCap.Blend);
            // LALALA

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
