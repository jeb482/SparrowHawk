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
    public struct GLBuffer
    {
        public int id;
        public int version;
        public int size;
        public int compSize;
        public VertexAttribPointerType glType;
        public int dim;

    }

    public class GLShader
    {

        string mName;
        int mVAO = 0;
        int mVertexShader;
        int mFragmentShader;
        public int mProgramShader;
        public bool isInitialized = false;
        SortedDictionary<string, GLBuffer> mBufferObjects;
        Rhino.RhinoDoc mDoc = null;

        public GLShader(Rhino.RhinoDoc doc)
        {
            mDoc = doc;
            mBufferObjects = new SortedDictionary<string, GLBuffer>();
        }

        public void initFromFile(string name, string vertexFileName, string fragmentFileName)
        {
            throw new NotImplementedException();
        }

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
            int status;
            GL.GetProgram(mProgramShader, GetProgramParameterName.LinkStatus, out status);
            // if status !+ glTrue, let us know. 

            if (status == 0)
                Rhino.RhinoApp.WriteLine("Linker Error: " + GL.GetProgramInfoLog(mProgramShader));

            isInitialized = true;
            return true;
        }



        private int createShaderHelper(ShaderType type, string name, string shaderSource)
        {
            if (shaderSource == "")
                return 0;

            int id = GL.CreateShader(type);
            GL.ShaderSource(id, shaderSource);
            GL.CompileShader(id);

            int[] status = new int[1];
            GL.GetShader(id, ShaderParameter.CompileStatus, status);

            if (status[0] != 1)
            {
                Rhino.RhinoApp.WriteLine("Error while compiling shader " + name + ".");
                if (type == ShaderType.VertexShader)
                    Rhino.RhinoApp.WriteLine("Error in vertex shader.");
                else if (type == ShaderType.FragmentShader)
                    Rhino.RhinoApp.WriteLine("Error in fragment shader.");
            }

            return id;
        }

        public void bind()
        {
            GL.UseProgram(mProgramShader);
            GL.BindVertexArray(mVAO);
        }

        public int attrib(string name, bool warn)
        {
            int id = GL.GetAttribLocation(mProgramShader, name);
            if (id == -1 && warn)
                Rhino.RhinoApp.WriteLine(mName + ": warning could not find attrib " + name);
            return id;
        }

        public int uniform(string name, bool warn = false)
        {
            int id = GL.GetUniformLocation(mProgramShader, name);
            if (id == -1 && warn)
                Rhino.RhinoApp.WriteLine(mName + ": warning could not find uniform " + name);
            return id;
        }

        public void uploadAttrib<T>(string name, int size, int dim, int compSize, VertexAttribPointerType glType, bool integral, ref T[] data, int version) where T : struct
        {
            int attribID = 0;
            if (name != "indices")
            {
                attribID = attrib(name,false);
                if (attribID < 0)
                    return;
            }

            int bufferID;
            GLBuffer buf;
            if (mBufferObjects.TryGetValue(name, out buf))
            {
                bufferID = buf.id;
                buf.version = version;
                buf.size = size;
                buf.compSize = compSize;
            } else
            {
                buf = new GLBuffer();
                bufferID = GL.GenBuffer();
                buf.id = bufferID;
                buf.glType = glType;
                buf.dim = dim;
                buf.compSize = compSize;
                buf.size = size;
                buf.version = version;
                mBufferObjects[name] = buf;
            }
            // TODO: do I have to multiply by size?
            int totalSize = size * compSize;
            

            if (name == "indices") {
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, buf.id);
                GL.BufferData(BufferTarget.ElementArrayBuffer, totalSize,  data, BufferUsageHint.DynamicDraw);
            } else
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, bufferID);
                GL.BufferData(BufferTarget.ArrayBuffer, totalSize,  data, BufferUsageHint.DynamicDraw);
                if (size == 0)
                    GL.DisableVertexAttribArray(attribID);
                else
                {
                    GL.EnableVertexAttribArray(attribID);
                    GL.VertexAttribPointer(attribID, buf.dim,  buf.glType, true, 0, 0);
                }
            }
        }

        //void setUniform(string name)
        //{
        //    int blockIndex =  GL.GetUniformBlockIndex(mProgramShader, name);
        //    GL.UniformBlockBinding(mProgramShader, blockIndex,  blockIndex,);
            
        //}

        void downloadAttrib<T>(string name, int size, int dim, int compSize, VertexAttribPointerType glType, ref T data) where T : struct
        {
            throw new NotImplementedException();
        }
     
        // shareAttrib. Not sure if useful. Leaving unimplemented
        public void shareAttrib(ref GLShader other, string attribName, string asparam)
        {
            throw new NotImplementedException();
        }

        public void freeAttrib(string name)
        {
            GLBuffer buf;
            if (mBufferObjects.TryGetValue(name, out buf))
            {
                GL.DeleteBuffer(buf.id);
                mBufferObjects.Remove(name);
            }
        }

        //public void drawIndexed(PrimitiveType type, int offset, int count)
        //{
        //    if (count == 0)
        //        return;
        //    switch(type)
        //    {
        //        case PrimitiveType.Triangles: offset *= 3; count *= 3; break;
        //        case PrimitiveType.Lines: offset *= 3, count *= 2; break;
        //    }
        //    // TODO: do I have to multiple by size?
        //    GL.DrawElements(type, count, DrawElementsType.UnsignedInt, offset);
        //}

        public void drawArray(PrimitiveType type, int offset, int count)
        {
            if (count == 0)
                return;
            ErrorCode error = GL.GetError();
            GL.DrawArrays(type, offset, count);
            error = GL.GetError();
        }

        public void drawIndexed(BeginMode type, int offset, int count)
        {
            if (count == 0)
                return;

            switch (type)
            {
                case BeginMode.Triangles:
                    offset *= 3; count *= 3; break;
                case BeginMode.Lines:
                    offset *= 2; count *= 2; break;
            }

              GL.DrawElements(type, count, DrawElementsType.UnsignedInt, offset);

        }

        public void free()
        {
            foreach (KeyValuePair<string,GLBuffer> buf in mBufferObjects)
            {
                GL.DeleteBuffer(buf.Value.id);
            }
            mBufferObjects.Clear();

            if (mVAO != 0)
            {
                GL.DeleteVertexArray(mVAO);
            }

            if (mProgramShader != 0)
                GL.DeleteProgram(mProgramShader);
            if (mVertexShader != 0)
                GL.DeleteShader(mVertexShader);
            if (mFragmentShader != 0)
                GL.DeleteShader(mFragmentShader);
             
            isInitialized = false;
        }
    }
}
