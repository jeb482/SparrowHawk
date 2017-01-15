using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK.Graphics.OpenGL4;

namespace SparrowHawk
{
    struct FramebufferDesc
    {
        public int depthBufferId;
        public int renderTextureId;
        public int renderFramebufferId;
        public int resolveTextureId;
        public int resolveFramebufferId;
    }

    class VrRenderer
    {
        /**
         * Generates a framebuffer object on the GPU. Taken directly from the OpenGL demo
         * that comes with the openvr project.
         * 
         */
        public bool CreateFrameBuffer(int width, int height, out FramebufferDesc framebufferDesc)
        {
            framebufferDesc = new FramebufferDesc();
            framebufferDesc.renderFramebufferId = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferDesc.renderFramebufferId);

            framebufferDesc.depthBufferId = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, framebufferDesc.depthBufferId);
            GL.RenderbufferStorageMultisample(RenderbufferTarget.Renderbuffer, 4, RenderbufferStorage.DepthComponent, width, height);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, framebufferDesc.depthBufferId);

            framebufferDesc.renderTextureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DMultisample, framebufferDesc.renderTextureId);
            GL.TexImage2DMultisample(TextureTargetMultisample.Texture2DMultisample, 4, PixelInternalFormat.Rgba8, width, height, true);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2DMultisample, framebufferDesc.renderTextureId, 0);

            framebufferDesc.resolveFramebufferId = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebufferDesc.resolveFramebufferId);

            framebufferDesc.resolveTextureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, framebufferDesc.resolveTextureId);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 0);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, new IntPtr()); // Hoping this is a nullptr
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, framebufferDesc.resolveTextureId, 0);

            FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
                return false;

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            return true;
        }

    }
}
