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
        private uint indexBufferId;
        private uint vertexBufferId;
        private uint colorBufferId;

        public SingleColorMaterial(Rhino.RhinoDoc doc, float r, float g, float b, float a)
        {
            mShader = new GLShader(doc);
            mShader.init("SingleColorMaterial", ShaderSource.SingleColorVertShader, ShaderSource.SingleColorFragShader);

            uint[] indices = new uint[] {0,1,2,3,2,1};
            GL.GenBuffers(1, out indexBufferId);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBufferId);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                (IntPtr)(indices.Length * sizeof(ushort)),
                indices,
                BufferUsageHint.StaticDraw);

            // Set-up vertex buffer:
            float[] vertexData = new float[] {
            50.0f, 50.0f,
            100.0f, 50.0f,
            100.0f, 100.0f,
            50.0f, 100.0f };

            GL.GenBuffers(1, out vertexBufferId);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferId);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(vertexData.Length * sizeof(float)),
                vertexData,
                BufferUsageHint.StaticDraw);

            // Set-up color buffer:
            float[] colorData = new float[] {
            1.0f, 1.0f, 1.0f, 1.0f,
            1.0f, 1.0f, 1.0f, 1.0f,
            1.0f, 1.0f, 1.0f, 1.0f,
            1.0f, 1.0f, 1.0f, 1.0f
        };

            GL.GenBuffers(1, out colorBufferId);
            GL.BindBuffer(BufferTarget.ArrayBuffer, colorBufferId);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                (IntPtr)(colorData.Length * sizeof(float)),
                colorData,
                BufferUsageHint.StaticDraw);

        }

        public override void draw(ref Geometry.Geometry g, ref Matrix4 model, ref Matrix4 vp)
        {
            mShader.bind();
            GL.Disable(EnableCap.Blend);


           // GL.BindVertexBuffer(vertexBufferId, )

    
                }
    }
}
