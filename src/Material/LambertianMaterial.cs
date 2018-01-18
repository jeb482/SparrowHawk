using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using Math = SparrowHawk.Util.Math;

namespace SparrowHawk.Material
{
    class LambertianMaterial : Material
    {
        const int MAX_LIGHT_COUNT = 12;
        public OpenTK.Graphics.Color4 mColor;
        //public float[] lightPositions = new float[3 * MAX_LIGHT_COUNT];
        //public float[] lightIntensities = new float[3 * MAX_LIGHT_COUNT];
        public Vector3[] lightPositions = new Vector3[MAX_LIGHT_COUNT];
        public Vector3[] lightIntensities = new Vector3[MAX_LIGHT_COUNT];

        public LambertianMaterial(float r, float g, float b, float a)
        {
            mColor = new OpenTK.Graphics.Color4(r, g, b, a);
            mShader = new GLShader();
            mShader.init("LambertianMaterial", ShaderSource.LambertianVertShader, ShaderSource.LambertianFragShader);
            lightPositions[0] = new Vector3(0,3,0);
            lightIntensities[0] = new Vector3(5,5,5);
            lightPositions[1] = new Vector3(1, 2, 0);
            lightIntensities[1] = new Vector3(.5f, .5f, 1);
            lightPositions[2] = new Vector3(-1, 3, -1);
            lightIntensities[2] = new Vector3(0, 1f, 0);
            lightPositions[3] = new Vector3(-1, 2, 1);
            lightIntensities[3] = new Vector3(1, .5f, .5f);
            lightPositions[4] = new Vector3(-1, 3, 1);
            lightIntensities[4] = new Vector3(1, 0, 0);
        }

        public override void draw(ref Geometry.Geometry g, ref Matrix4 model, ref Matrix4 vp)
        {
            UtilOld.depthSort(model*vp,g);
            GL.Disable(EnableCap.DepthTest);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Blend);
            Matrix4 modelIT = model.Inverted();
            modelIT.Transpose();
            int dim;
            if (g.primitiveType == BeginMode.Lines) dim = 2; else dim = 3;
            mShader.bind();
            mShader.uploadAttrib<int>("indices", g.mGeometryIndices.Length, dim, 4, VertexAttribPointerType.UnsignedInt, false, ref g.mGeometryIndices, 0);
            mShader.uploadAttrib<float>("position", g.mGeometry.Count(), 3, 4, VertexAttribPointerType.Float, false, ref g.mGeometry, 0);
            if (g.mNormals == null)
                Math.addNormalsToMesh(g); 
            if (g.mNormals != null)
                mShader.uploadAttrib<float>("normal", g.mNormals.Count(), 3, 4, VertexAttribPointerType.Float, false, ref g.mNormals, 0);
            GL.Uniform4(mShader.uniform("color"), mColor);
            GL.UniformMatrix4(mShader.uniform("modelTransform"), true, ref model);
            GL.UniformMatrix4(mShader.uniform("viewProjTransform"), false, ref vp); // TODO: Fix this 

            GL.ProgramUniformMatrix4(mShader.mProgramShader, mShader.uniform("modelInvTrans"), true, ref modelIT);
            GL.Uniform3(mShader.uniform("lightInt"), 3*MAX_LIGHT_COUNT, ref lightIntensities[0].X);
            GL.Uniform3(mShader.uniform("lightPos"), 3*MAX_LIGHT_COUNT, ref lightPositions[0].X);
            mShader.drawIndexed(g.primitiveType, 0, g.mNumPrimitives);
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);
        }

    }
}
