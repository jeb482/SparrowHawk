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
        protected bool enableAR = true;
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
        protected bool CreateFrameBuffer(int width, int height, out FramebufferDesc framebufferDesc)
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
            mEyeProjLeft = GetHMDMatrixProjectionEye(ref mHMD, Valve.VR.EVREye.Eye_Left);
            mEyeProjRight = GetHMDMatrixProjectionEye(ref mHMD, Valve.VR.EVREye.Eye_Right);
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

                renderCompanionWindow();
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



        public void renderCompanionWindow()
        {
            //// Temp: move it to a single startup method. 
            //Geometry.Geometry fs_quad_g = new Geometry.Geometry();
            //fs_quad_g.mGeometry = new float[12] {-1.0f, 1.0f, 0.0f,
            //                                                 1.0f,  1.0f, 0.0f,
            //                                                 1.0f, -1.0f, 0.0f,
            //                                                 -1.0f, -1.0f, 0.0f};
            //
            //fs_quad_g.mGeometryIndices = new int[6]{ 0, 1, 2,
            //                                                 2, 3, 0};
            //fs_quad_g.mUvs = new float[8] { 0.0f, 0.0f,
            //                                    1.0f, 0.0f,
            //                                    1.0f, 1.0f,
            //                                    0.0f, 1.0f};
            //
            //fs_quad_g.mNumPrimitives = 2;
            //fs_quad_g.primitiveType = BeginMode.Triangles;
            //
            //
            //
            //GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rightEyeDesc.renderFramebufferId);
            ////Material.TextureMaterial mat = new Material.TextureMaterial(mScene.rhinoDoc, rightEyeDesc.renderFramebufferId, (int) vrRenderWidth, (int) vrRenderHeight);
            //
            ////mat.updateTextureFromFramebuffer(rightEyeDesc.renderFramebufferId);
            ////GL.Disable(EnableCap.DepthTest);
            ////Matrix4 model = new Matrix4(Vector4.UnitX, Vector4.UnitY, Vector4.UnitZ, Vector4.UnitW);
            ////Matrix4 vp = new Matrix4(Vector4.UnitX, Vector4.UnitY, Vector4.UnitZ, Vector4.UnitW*.000000000000001f);
            ////Material.SingleColorMaterial mat = new Material.SingleColorMaterial(1, 1, 0, 1);
            //
            //var mat = new Material.NaiveMaterial(mScene.rhinoDoc);
            //GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            //GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            //GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            //GL.BindFramebuffer(FramebufferTarget.FramebufferExt, 0);
            //GL.ClearColor(0, 0, 1,1);
            //GL.Clear( ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            //var m = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
            //var vp = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
            //mat.draw(ref fs_quad_g, ref m, ref vp);
            
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, rightEyeDesc.renderFramebufferId);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            //if (enableAR)
            //{
            //    int ox = ((int)vrRenderWidth - ovrvision_controller.camWidth) / 2;
            //    int oy = ((int)vrRenderHeight - ovrvision_controller.camHeight) / 2;
            //    GL.BlitFramebuffer(ox + 100, oy, ovrvision_controller.camWidth, ovrvision_controller.camHeight, 0, 0, ovrvision_controller.camWidth, ovrvision_controller.camHeight, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            //} 
            //else
            //
            
            // THIS ONE IS WITHOUT ANY CLIPPING
            // 
            // int leftOffset = 100;
            // int botOffset = 415;
            // int rightOffset = 300;
            // int topOffset = 375;

            // THIS ONE CLIPS TO A NICE RECTANGLE
            int leftOffset = 183;
            int botOffset = 517;
            int rightOffset = 470;
            int topOffset = 392;


            GL.BlitFramebuffer(leftOffset, botOffset, (int)vrRenderWidth - rightOffset, (int)vrRenderHeight - topOffset, 0, 0, (int)vrRenderWidth - (leftOffset + rightOffset), (int)vrRenderHeight - (botOffset + topOffset), ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            
            //GL.BlitFramebuffer(100, 400, (int)vrRenderWidth - 100, (int)vrRenderHeight - 300 , 0, 0, (int)vrRenderWidth - 200, (int)vrRenderHeight -  700, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            
        }

        
    }
}
