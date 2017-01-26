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
        private int vboPositions;
        private int ibo;
        private int vao;
        Rhino.RhinoDoc mDoc;
        public NaiveMaterial(Rhino.RhinoDoc doc)
        {
            mDoc = doc;
            mShader = new GLShader(doc);
            mShader.init("DumbShader", ShaderSource.NaiveVertexShader, ShaderSource.NaiveVertexShader);

            mShader.bind();
            vboPositions = GL.GenBuffer();
            float[] pos = { -1f, -1f, 0f, 1f, -1f, 0f, 0f, 1f, 0f }; 
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboPositions);
            GL.BufferData<float>(BufferTarget.ArrayBuffer, 12 * 4, pos, BufferUsageHint.DynamicDraw);


            mShader.uploadAttrib<float>("position", 12, 3, 4, VertexAttribPointerType.Float, false, ref pos, 0);
            //GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            //GL.EnableVertexAttribArray(0);
            //GL.BindBuffer(BufferTarget.ArrayBuffer, vboPositions);
            //GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 12, 0);


            int[] idx = { 0, 1, 2 };
            ibo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ibo);
            GL.BufferData<int>(BufferTarget.ElementArrayBuffer, 3 * 4, idx, BufferUsageHint.DynamicDraw);


    //        GL.BindAttribLocation(mShader.mProgramShader, 0, "position");

            int zero_q = mShader.attrib("position", true);
        }

        public override void draw(ref Geometry.Geometry g, ref Matrix4 model, ref Matrix4 vp)
        {
            GL.DrawElements(BeginMode.Triangles, 3, DrawElementsType.UnsignedInt, 0);
        }
    }
}
