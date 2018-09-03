using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using Valve.VR;

namespace SparrowHawk.Renderer
{
    /// <summary>
    /// A wrapper for a OpenGL Framebuffer object with double buffering
    /// </summary>
    public class FramebufferDesc
    {
        public int depthBufferId;
        public int renderTextureId;
        public int renderFramebufferId;
        public int resolveTextureId;
        public int resolveFramebufferId;
        public int Width;
        public int Height;

        /// <summary>
        /// Copies the data in the render framebuffer to the resolve framebuffer
        /// </summary>
        /// <param name="desc"></param>
        public void BlitToResolve()
        {
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, this.renderFramebufferId);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, this.resolveFramebufferId);
            GL.BlitFramebuffer(0, 0, this.Width, this.Height, 0, 0, this.Width, this.Height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        }

        /**
         * Generates a framebuffer object on the GPU. Taken directly from the OpenGL demo
         * that comes with the openvr project.
         */
        public static bool CreateFrameBuffer(int width, int height, out FramebufferDesc framebufferDesc)
        {
            framebufferDesc = new FramebufferDesc();
            framebufferDesc.Width = width;
            framebufferDesc.Height = height;
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
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, new IntPtr()); // Hoping this is a nullptr
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, framebufferDesc.resolveTextureId, 0);


            FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
                return false;

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            return true;
        }
    }


    public abstract class AbstractRenderer
    {
        protected Scene mScene;
        protected FramebufferDesc leftEyeDesc;
        protected FramebufferDesc rightEyeDesc;
        protected CVRSystem mHMD;

        /// <summary>
        /// Renders the current scene to the HMD. [Abstract]
        /// </summary>
        public abstract void RenderFrame();

        /// <summary>
        /// Submits two appropriately sized framebuffers to SteamVr for native rendering to viewport.
        /// </summary>
        /// <param name="leftEyeDesc"></param>
        /// <param name="rightEyeDesc"></param>
        public static void SubmitToHmd(FramebufferDesc leftEyeDesc, FramebufferDesc rightEyeDesc)
        {
            Valve.VR.Texture_t leftEyeTexture, rightEyeTexture;
            leftEyeTexture.handle = new IntPtr(leftEyeDesc.resolveTextureId);
            rightEyeTexture.handle = new IntPtr(rightEyeDesc.resolveTextureId);
            leftEyeTexture.eType = Valve.VR.ETextureType.OpenGL;
            rightEyeTexture.eType = Valve.VR.ETextureType.OpenGL;
            leftEyeTexture.eColorSpace = Valve.VR.EColorSpace.Gamma;
            rightEyeTexture.eColorSpace = Valve.VR.EColorSpace.Gamma;
            Valve.VR.VRTextureBounds_t pBounds = new Valve.VR.VRTextureBounds_t();
            pBounds.uMax = 1; pBounds.uMin = 0; pBounds.vMax = 1; pBounds.uMin = 0;
            Valve.VR.OpenVR.Compositor.Submit(Valve.VR.EVREye.Eye_Left, ref leftEyeTexture, ref pBounds, Valve.VR.EVRSubmitFlags.Submit_LensDistortionAlreadyApplied); // TODO: There's a distortion already applied flag.
            Valve.VR.OpenVR.Compositor.Submit(Valve.VR.EVREye.Eye_Right, ref rightEyeTexture, ref pBounds, Valve.VR.EVRSubmitFlags.Submit_LensDistortionAlreadyApplied);
            GL.Finish();
        }
    }
}
