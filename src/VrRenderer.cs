using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK.Graphics.OpenGL4;
using OpenTK;
using System.Drawing;
using System.Drawing.Imaging;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Threading;
using System.Runtime.InteropServices;
using Valve.VR;
using SparrowHawk.Ovrvision;

namespace SparrowHawk
{
    public struct FramebufferDesc
    {
        public int depthBufferId;
        public int renderTextureId;
        public int renderFramebufferId;
        public int resolveTextureId;
        public int resolveFramebufferId;
        public int Width;
        public int Height;
    }

    public class VrRenderer
    {
        protected uint vrRenderWidth;
        protected uint vrRenderHeight;
        protected float mNearClip = 0.1f;
        protected float mFarClip = 90.0f;
        protected Matrix4 mEyeProjLeft;
        protected Matrix4 mEyeProjRight;
        protected Matrix4 mEyePosLeft;
        protected Matrix4 mEyePosRight;
        protected FramebufferDesc leftEyeDesc;
        protected FramebufferDesc rightEyeDesc;
        protected Valve.VR.CVRSystem mHMD;
        protected Scene mScene;
        public OvrvisionController ovrvision_controller;
        protected bool enableAR = false;
        protected Geometry.Geometry fullscreenQuad;


        public VrRenderer(ref Valve.VR.CVRSystem HMD, ref Scene scene, uint mRenderWidth, uint mRenderHeight)
        {
            mHMD = HMD;
            mScene = scene;
            SetupStereoRenderTargets(ref mHMD);
            //1344 * 1600
            vrRenderWidth = mRenderWidth;
            vrRenderHeight = mRenderHeight;
            //ovrvision
            if (enableAR)
            {
                ovrvision_controller = new OvrvisionController(ref mHMD, ref mScene);
                ovrvision_controller.initOVrvision();
            }
        }

        /**
         * Generates a framebuffer object on the GPU. Taken directly from the OpenGL demo
         * that comes with the openvr project.
         * 
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

        bool SetupStereoRenderTargets(ref Valve.VR.CVRSystem HMD)
        {
            if (HMD == null)
                return false;
            HMD.GetRecommendedRenderTargetSize(ref vrRenderWidth, ref vrRenderHeight);
            CreateFrameBuffer((int) vrRenderWidth, (int) vrRenderHeight, out leftEyeDesc);
            CreateFrameBuffer((int) vrRenderWidth, (int) vrRenderHeight, out rightEyeDesc);
            return true;
        }

        public static Matrix4 GetHMDMatrixProjectionEye(ref Valve.VR.CVRSystem HMD, Valve.VR.EVREye eye, float near, float far)
        {
            if (HMD == null)
                return new Matrix4();
            Valve.VR.HmdMatrix44_t M = HMD.GetProjectionMatrix(eye, near, far);
            return UtilOld.steamVRMatrixToMatrix4(M);
        }

        public static Matrix4 GetHMDMatrixPoseEye(ref Valve.VR.CVRSystem HMD, Valve.VR.EVREye eye)
        {
            if (HMD == null)
                return new Matrix4();
            Valve.VR.HmdMatrix34_t M = HMD.GetEyeToHeadTransform(eye);
            return UtilOld.steamVRMatrixToMatrix4(M).Inverted();
        }


        // TODO: Generate Shaders
        void generateShaders()
        {
        }

        public void switchAR()
        {
            enableAR = !enableAR;
            if (enableAR)
                Rhino.RhinoApp.WriteLine("enable AR mode");
            else
                Rhino.RhinoApp.WriteLine("disable AR mode");
        }

        public void RenderScene(Valve.VR.EVREye eye)
        {
            // Clear the screen to white
            GL.ClearColor(.1f, 0f, .1f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // use ovrvision camera
            if (enableAR)
            {
                ovrvision_controller.updateCamera(eye);
                Matrix4 vp = new Matrix4();
                switch (eye)
                {
                    case Valve.VR.EVREye.Eye_Left:
                        if (ovrvision_controller.foundMarker_L)
                        {
                            ovrvision_controller.drawCubeGL(0);
                        }
                        //ovrvision_controller.drawController(0);

                        ovrvision_controller.getOVRVPMatrix(0, ref vp);
                        //Util.WriteLine(ref mScene.rhinoDoc, vp.ToString());
                        break;
                    default:
                        if (ovrvision_controller.foundMarker_R)
                        {
                            ovrvision_controller.drawCubeGL(1);

                        }
                       // ovrvision_controller.drawController(1);

                        ovrvision_controller.getOVRVPMatrix(1, ref vp);
                        break;
                }
                //already transpose
                //vp.Transpose();
                mScene.render(ref vp);
            }
            else
            {

                mHMD.GetEyeToHeadTransform(eye);

                Matrix4 vp;
                switch (eye)
                {
                    case Valve.VR.EVREye.Eye_Left:
                        //Util.WriteLine(ref mScene.rhinoDoc, mScene.mHMDPose.ToString());
                        vp = mEyeProjLeft * mEyePosLeft * mScene.mHMDPose;
                        break;
                    default:
                        vp = mEyeProjRight * mEyePosRight * mScene.mHMDPose;
                        break;
                }
                vp.Transpose();
                mScene.render(ref vp);
            }

        }


        protected void RenderStereoTargets()
        {
            GL.Enable(EnableCap.Multisample);
            // some ovrcamera stuff here

            // Left Eye. Notably the original openvr code has some lame code duplication here.
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, leftEyeDesc.renderFramebufferId);

            if (enableAR)
            {
                //1344 * 1600  <-> 960*950
                int ox = ((int)vrRenderWidth - ovrvision_controller.camWidth) / 2;
                int oy = ((int)vrRenderHeight - ovrvision_controller.camHeight) / 2;
                //ox+100 to deal with blur issue
                GL.Viewport(ox + 100, oy, ovrvision_controller.camWidth, ovrvision_controller.camHeight);
                //GL.Viewport(ox + 100, oy, (int)vrRenderWidth, (int)vrRenderHeight);
                //GL.Viewport(0, 0, (int)vrRenderWidth, (int)vrRenderHeight);
                //GL.Viewport(ox + 100, oy, (int)(ovrvision_controller.camHeight * 0.84), (int)ovrvision_controller.camHeight);
            }
            else
            {
                GL.Viewport(0, 0, (int)vrRenderWidth, (int)vrRenderHeight);
            }
            
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
            if (enableAR)
            {
                int ox = ((int)vrRenderWidth - ovrvision_controller.camWidth) / 2;
                int oy = ((int)vrRenderHeight - ovrvision_controller.camHeight) / 2;
                //ox-100 to deal with the blur issue
                GL.Viewport(ox - 100, oy, ovrvision_controller.camWidth, ovrvision_controller.camHeight);
                //GL.Viewport(ox - 100, oy, (int)vrRenderWidth, (int)vrRenderHeight);
                //GL.Viewport(0, 0, (int)vrRenderWidth, (int)vrRenderHeight);
                
            }
            else
            {
                GL.Viewport(0, 0, (int)vrRenderWidth, (int)vrRenderHeight);
            }  
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
        
        private void setupCameras()
        {
            mEyePosLeft = GetHMDMatrixPoseEye(ref mHMD, Valve.VR.EVREye.Eye_Left);
            mEyePosRight = GetHMDMatrixPoseEye(ref mHMD, Valve.VR.EVREye.Eye_Right);
            mEyeProjLeft = GetHMDMatrixProjectionEye(ref mHMD, Valve.VR.EVREye.Eye_Left, mNearClip, mFarClip);
            mEyeProjRight = GetHMDMatrixProjectionEye(ref mHMD, Valve.VR.EVREye.Eye_Right, mNearClip, mFarClip);
        }
        
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


        public void renderFrame()
        {
            if (mHMD != null)
            {
                GL.DepthFunc(DepthFunction.Less);
                GL.Enable(EnableCap.DepthTest);
                setupCameras();
                // DrawControllers
                RenderStereoTargets();
                //RenderDistortion();
                GL.Finish();
                GL.Flush();

                renderMetaWindow();
                SubmitToHmd(leftEyeDesc, rightEyeDesc);
            }
        }


        public void renderMetaWindow()
        {
            // Per eye oculus dims: 1080x1200
            // Meta 2 Dims: 2560 x 1440

            // Copy Right Eye
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rightEyeDesc.renderFramebufferId);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            int leftOffset = 183;
            int botOffset = 517;
            int rightOffset = 470;
            int topOffset = 392;
            GL.BlitFramebuffer(leftOffset, botOffset, (int)vrRenderWidth - rightOffset, (int)vrRenderHeight - topOffset, 0, (int)vrRenderHeight - (botOffset + topOffset), (int)2560 - (leftOffset + rightOffset), 0, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);


            // Copy Left Eye
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, leftEyeDesc.renderFramebufferId);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            leftOffset = 183 + 1280;
            botOffset = 517;
            rightOffset = 470;
            topOffset = 392;
            GL.BlitFramebuffer(leftOffset, botOffset, (int)vrRenderWidth - rightOffset, (int)vrRenderHeight - topOffset, 0, (int)vrRenderHeight - (botOffset + topOffset), (int)2560 - (leftOffset + rightOffset), 0, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);


        }
        public void renderCompanionWindow()
        {

            
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rightEyeDesc.renderFramebufferId);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);

            // THIS ONE CLIPS TO A NICE RECTANGLE
            int leftOffset = 183;
            int botOffset = 517;
            int rightOffset = 470;
            int topOffset = 392;


            GL.BlitFramebuffer(leftOffset, botOffset, (int)vrRenderWidth - rightOffset, (int)vrRenderHeight - topOffset, 0, (int)vrRenderHeight - (botOffset + topOffset), (int)vrRenderWidth - (leftOffset + rightOffset), 0, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            
        }

        
    }
}
