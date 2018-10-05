using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using OpenTK;
using Math = SparrowHawk.Util.Math;

namespace SparrowHawk.Material
{
    public class RGBNormalMaterial : Material
    {
        float mAlpha;
        int vboPositions;
        int vboNormals;
        int ibo;
        int vao;        

        public RGBNormalMaterial(float alpha)
        {
            //mAlpha = alpha;
            // Ignore and set a global alpha
            mAlpha = 1f;
            mShader = new GLShader();
            mShader.init("RGBNormalMaterial", ShaderSource.RGBNormalVertShader, ShaderSource.RGBNormalFragShader);
        }

        override public void draw(ref Geometry.Geometry g, ref Matrix4 model, ref Matrix4 vp)
        {
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            // bind shader
            GL.Disable(EnableCap.DepthTest);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            mShader.bind();

            Matrix4 modelIT = model.Inverted();
            modelIT.Transpose();

            GL.Enable(EnableCap.Blend);
            mShader.uploadAttrib<int>("indices", g.mGeometryIndices.Length, 3, 4, VertexAttribPointerType.UnsignedInt, false, ref g.mGeometryIndices, 0);
            mShader.uploadAttrib<float>("position", g.mGeometry.Count(), 3, 4, VertexAttribPointerType.Float, false, ref g.mGeometry, 0);
            if (g.mNormals == null)
                Math.addNormalsToMesh(g); // TODO: I literally hate this, but it was easier than patching Eric's code.
            if (g.mNormals != null)
                mShader.uploadAttrib<float>("normal", g.mNormals.Count(), 3, 4, VertexAttribPointerType.Float, false, ref g.mNormals, 0);
            GL.Uniform1(mShader.uniform("alpha"), mAlpha);
            GL.ProgramUniformMatrix4(mShader.mProgramShader ,mShader.uniform("viewProjTransform"), false, ref vp);
            GL.ProgramUniformMatrix4(mShader.mProgramShader, mShader.uniform("modelTransform"), true, ref model);
            GL.ProgramUniformMatrix4(mShader.mProgramShader, mShader.uniform("modelInvTrans"), true, ref modelIT);

            mShader.drawIndexed(g.primitiveType, 0, g.mNumPrimitives);
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);
        }

    }
}
