/*
 * GLShader.cs is adapted from Wenzel Jakob's Nanogui renderering platform
 * Available at https://github.com/wjakob/nanogui
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL4;

namespace SparrowHawk
{
    class GLShader
    {
        string mName;
        int mVAO;
        int mVertexShader;
        int mFragmentShader;
        int mProgramShader;

        public bool init(string name, string vertexSource, string fragmentSource)
        {
            // defines?
            mVAO = GL.GenVertexArray();
            mName = name;
            mVertexShader = createShaderHelper(ShaderType.VertexShader, name, vertexSource);
            mFragmentShader = createShaderHelper(ShaderType.FragmentShader, name, fragmentSource);

            if (mVertexShader == 0 || mFragmentShader == 0)
                return false;

            mProgramShader = GL.CreateProgram();
            GL.AttachShader(mProgramShader, mVertexShader);
            GL.AttachShader(mProgramShader, mFragmentShader);

            GL.LinkProgram(mProgramShader);
            int[] status = new int[0];
            GL.GetProgram(mProgramShader, GetProgramParameterName.LinkStatus, status);
            // if status !+ glTrue, let us know. 

            return true;
        }

        private static int createShaderHelper(ShaderType type, string name, string shaderSource)
        {
            if (shaderSource == "")
                return 0;

            int id = GL.CreateShader(type);
            GL.ShaderSource(id, shaderSource);
            GL.CompileShader(id);

            int[] status = new int[1];
            GL.GetShader(id, ShaderParameter.CompileStatus, status);

            //if (status[0] != 1)
            //"Error while compiling shader.";

            return id;
        }

        public void bind()
        {
            GL.UseProgram(mProgramShader);
            GL.BindVertexArray(mVAO);
        }
    }
}
