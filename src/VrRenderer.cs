using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK.Graphics.OpenGL4;
using OpenTK;

namespace SparrowHawk
{
    public struct FramebufferDesc
    {
        public int depthBufferId;
        public int renderTextureId;
        public int renderFramebufferId;
        public int resolveTextureId;
        public int resolveFramebufferId;
    }

    public class VrRenderer
    {
        uint vrRenderWidth;
        uint vrRenderHeight;
        float mNearClip = 0.1f;
        float mFarClip = 30.0f;
        FramebufferDesc leftEyeDesc;
        FramebufferDesc rightEyeDesc;
        Valve.VR.CVRSystem mHMD;
        Scene mScene;

        public VrRenderer(ref Valve.VR.CVRSystem HMD, ref Scene scene, uint mRenderWidth, uint mRenderHeight)
        {
            mHMD = HMD;
            mScene = scene;
            SetupStereoRenderTargets(ref mHMD);
            SetupDistortion();
            vrRenderWidth = mRenderWidth;
            vrRenderHeight = mRenderHeight;
        }

        /**
         * Generates a framebuffer object on the GPU. Taken directly from the OpenGL demo
         * that comes with the openvr project.
         * 
         */
        bool CreateFrameBuffer(int width, int height, out FramebufferDesc framebufferDesc)
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

        bool SetupStereoRenderTargets(ref Valve.VR.CVRSystem HMD)
        {
            if (HMD == null)
                return false;
            HMD.GetRecommendedRenderTargetSize(ref vrRenderWidth, ref vrRenderHeight);
            CreateFrameBuffer((int) vrRenderWidth, (int) vrRenderHeight, out leftEyeDesc);
            CreateFrameBuffer((int) vrRenderWidth, (int) vrRenderHeight, out rightEyeDesc);
            return true;
        }

        // TODO: Handle this shit.
        bool SetupDistortion()
        {
            return true;
        }

        Matrix4 GetHMDMatrixProjectionEye(ref Valve.VR.CVRSystem HMD, Valve.VR.EVREye eye)
        {
            if (HMD == null)
                return new Matrix4();
            Valve.VR.HmdMatrix44_t M = HMD.GetProjectionMatrix(eye, mNearClip, mFarClip);
            return Util.steamVRMatrixToMatrix4(M);
        }

        Matrix4 GetHMDMatrixPoseEye(ref Valve.VR.CVRSystem HMD, Valve.VR.EVREye eye)
        {
            if (HMD == null)
                return new Matrix4();
            Valve.VR.HmdMatrix34_t M = HMD.GetEyeToHeadTransform(eye);
            return Util.steamVRMatrixToMatrix4(M).Inverted();
        }




        // TODO: Generate Shaders
        void generateShaders()
        {
        }



        public void RenderScene(Valve.VR.EVREye eye)
        {

            Matrix4 p = Matrix4.CreatePerspectiveFieldOfView(1.2f, 1280f / 760, .01f, 30);
            Matrix4 v =  Matrix4.LookAt(new Vector3(-5, -5, -5), new Vector3(0, 0, 0), new Vector3(0, 1, 0));
            Matrix4 vp = p * v;
            Vector4 t1 = v*new Vector4(1,1,1,1);
            Vector4 t2 = vp * new Vector4(1,1,1,1);
            Vector4 o1 = v * new Vector4(0, 0, 0, 1);
            Vector4 o2 = vp * new Vector4(0,0,0, 1);
            //GL.ClearColor(1,0,0,1);
            //GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            mScene.render(ref vp);
            
        }

        // TODO: CreateShaderProgram
        // TODO: specifyScreenVertexAttributes


        protected void RenderStereoTargets()
        {
            GL.Enable(EnableCap.Multisample);
            // some ovrcamera stuff here

            // Left Eye. Notably the original openvr code has some lame code duplication here.
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, leftEyeDesc.renderFramebufferId);
            GL.Viewport(0, 0, (int) vrRenderWidth, (int) vrRenderHeight);
            RenderScene(Valve.VR.EVREye.Eye_Left);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Disable(EnableCap.Multisample);

            // BLit Left Eye to Resolve buffer
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, leftEyeDesc.renderFramebufferId);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, leftEyeDesc.resolveFramebufferId);
            GL.BlitFramebuffer(0, 0, (int) vrRenderWidth, (int) vrRenderHeight, 0, 0, (int) vrRenderWidth, (int) vrRenderHeight, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);

            // Right Eye.
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, rightEyeDesc.renderFramebufferId);
            GL.Viewport(0, 0, (int)vrRenderWidth, (int)vrRenderHeight);
            RenderScene(Valve.VR.EVREye.Eye_Right);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Disable(EnableCap.Multisample);

            // BLit Right Eye to Resolve buffer
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rightEyeDesc.renderFramebufferId);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, rightEyeDesc.resolveFramebufferId);
            GL.BlitFramebuffer(0, 0, (int) vrRenderWidth, (int) vrRenderHeight, 0, 0, (int) vrRenderWidth, (int) vrRenderHeight, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);



        }
        
            // TODO: RenderDistortion
        
        // TODO: RenderFrame
        public void renderFrame()
        {
            if (mHMD != null)
            {
                // DrawControllers
                RenderStereoTargets();
                //RenderDistortion();

                Valve.VR.Texture_t leftEyeTexture, rightEyeTexture;
                leftEyeTexture.handle = new IntPtr(leftEyeDesc.resolveTextureId);
                rightEyeTexture.handle = new IntPtr(rightEyeDesc.resolveTextureId);
                leftEyeTexture.eType = Valve.VR.ETextureType.OpenGL;
                rightEyeTexture.eType = Valve.VR.ETextureType.OpenGL;
                leftEyeTexture.eColorSpace = Valve.VR.EColorSpace.Gamma;
                rightEyeTexture.eColorSpace = Valve.VR.EColorSpace.Gamma;
                Valve.VR.VRTextureBounds_t pBounds = new Valve.VR.VRTextureBounds_t();
                pBounds.uMax = 1; pBounds.uMin = 0; pBounds.vMax = 1; pBounds.uMin = 0;
                Valve.VR.OpenVR.Compositor.Submit(Valve.VR.EVREye.Eye_Left, ref leftEyeTexture, ref pBounds, Valve.VR.EVRSubmitFlags.Submit_Default); // TODO: There's a distortion already applied flag.
                Valve.VR.OpenVR.Compositor.Submit(Valve.VR.EVREye.Eye_Right, ref leftEyeTexture, ref pBounds, Valve.VR.EVRSubmitFlags.Submit_Default);

               
                
                GL.Finish();
            }
        }

        // TODO: UpdateHMDMatrixPose
    }
}
