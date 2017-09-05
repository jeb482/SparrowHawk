using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using System.Drawing.Imaging;
using OpenTK;

namespace SparrowHawk.Material
{
    class TextureMaterial : Material
    {
        Rhino.RhinoDoc mDoc;
        int m_iTexture;
        int texWidth;
        int texHeight;

        public TextureMaterial(Rhino.RhinoDoc doc, string path, bool flip_y = false)
        {
            mDoc = doc;
            mShader = new GLShader();
            mShader.init("TextureMaterial", ShaderSource.TextureVertShader, ShaderSource.TextureFragShader);
            mShader.bind();

            Bitmap bitmap = new Bitmap(path);
            texWidth = bitmap.Width;
            texHeight = bitmap.Height;

            //Flip the image
            if (flip_y)
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

            GL.ActiveTexture(TextureUnit.Texture0);
            //Generate a new texture target in gl
            m_iTexture = GL.GenTexture();
            //bind the texture newly/empty created with GL.GenTexture
            GL.BindTexture(TextureTarget.Texture2D, m_iTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);

            //Load the data from are loaded image into virtual memory so it can be read at runtime
            BitmapData bitmap_data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                    ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmap.Width, bitmap.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, bitmap_data.Scan0);
            //Release from memory
            bitmap.UnlockBits(bitmap_data);
            //GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, bitmap.Width, bitmap.Height, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, bitmap_data.Scan0);
            //get rid of bitmap object its no longer needed in this method
            bitmap.Dispose();
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public TextureMaterial(Rhino.RhinoDoc doc, int width, int height, OpenTK.Graphics.OpenGL4.PixelFormat pixelFormat,
            OpenTK.Graphics.OpenGL4.PixelType pixelType)
        {
            texWidth = width;
            texHeight = height;

            mDoc = doc;
            mShader = new GLShader();
            mShader.init("TextureMaterial", ShaderSource.TextureVertShader, ShaderSource.TextureFragShader);
            mShader.bind();

            m_iTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, m_iTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, width, height, 0, pixelFormat, pixelType, IntPtr.Zero);

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        /**
         * Create a texture material from a framebuffer object. It will need to be updated
         */
        public TextureMaterial(Rhino.RhinoDoc doc, int framebufferId, int width, int height)
        {
            texWidth = width;
            texHeight = height;

            mDoc = doc;
            mShader = new GLShader();
            mShader.init("TextureMaterial", ShaderSource.TextureVertShader, ShaderSource.TextureFragShader);
            mShader.bind();

            m_iTexture = GL.GenTexture();
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, framebufferId);
            GL.BindTexture(TextureTarget.Texture2D, m_iTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.CopyTexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, 0, 0, width, height);

            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
        }

        public override void draw(ref Geometry.Geometry g, ref Matrix4 model, ref Matrix4 vp)
        {
            ErrorCode e;
            mShader.bind();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, m_iTexture);
            GL.DepthFunc(DepthFunction.Always);

            //TODO- what's is dim for? compSize is because UnsignedInt 4 bytes?
            mShader.uploadAttrib<int>("indices", g.mGeometryIndices.Length, 3, 4, VertexAttribPointerType.UnsignedInt, false, ref g.mGeometryIndices, 0);
            e = GL.GetError();
            mShader.uploadAttrib<float>("position", g.mGeometry.Count(), 3, 4, VertexAttribPointerType.Float, false, ref g.mGeometry, 0);
            e = GL.GetError();
            mShader.uploadAttrib<float>("uvs", g.mUvs.Count(), 2, 4, VertexAttribPointerType.Float, false, ref g.mUvs, 0);
            e = GL.GetError();
            GL.UniformMatrix4(mShader.uniform("modelTransform"), true, ref model);
            e = GL.GetError();
            GL.UniformMatrix4(mShader.uniform("viewProjTransform"), false, ref vp);
            //GL.UniformMatrix4(mShader.uniform("viewProjTransform"), false, ref vp);
            GL.Uniform1(mShader.uniform("tex"), 0);
            
            //for debugging
            float[] funMatrix = new float[16];
            GL.GetUniform(mShader.mProgramShader, mShader.uniform("viewProjTransform"), funMatrix);
            e = GL.GetError();
            
            mShader.drawIndexed(g.primitiveType, 0, g.mNumPrimitives);
            GL.DepthFunc(DepthFunction.Less);
        }

        public void updateTexture(IntPtr texture)
        {
            //	// Update the texture
            ErrorCode e;
            mShader.bind();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, m_iTexture);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, texWidth, texHeight, OpenTK.Graphics.OpenGL4.PixelFormat.Bgr, PixelType.UnsignedByte, texture);
        }

        public void updateTextureFromFramebuffer(int framebufferId)
        {
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, framebufferId);
            GL.BindTexture(TextureTarget.Texture2D, m_iTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.CopyTexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, 0, 0, texWidth, texHeight);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
        }

    }
}
