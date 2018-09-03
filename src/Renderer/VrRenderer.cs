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

namespace SparrowHawk.Renderer

{
    

    public class VrRenderer : AbstractRenderer
    {
        protected uint vrRenderWidth;
        protected uint vrRenderHeight;
        protected float mNearClip = 0.1f;
        protected float mFarClip = 90.0f;
        protected Matrix4 mEyeProjLeft;
        protected Matrix4 mEyeProjRight;
        protected Matrix4 mEyePosLeft;
        protected Matrix4 mEyePosRight;
        public OvrvisionController ovrvision_controller;
        protected bool enableAR = false;
        protected Geometry.Geometry fullscreenQuad;


        public VrRenderer(ref Valve.VR.CVRSystem HMD, ref Scene scene, uint renderWidth, uint renderHeight)
        {
            mHMD = HMD;
            mScene = scene;
            SetupStereoRenderTargets(ref mHMD);
            //1344 * 1600
            vrRenderWidth = renderWidth;
            vrRenderHeight = renderHeight;
            //ovrvision
            if (enableAR)
            {
                ovrvision_controller = new OvrvisionController(ref mHMD, ref mScene);
                ovrvision_controller.initOVrvision();
            }
        }
     

        protected bool SetupStereoRenderTargets(ref Valve.VR.CVRSystem HMD)
        {
            if (HMD == null)
                return false;
            HMD.GetRecommendedRenderTargetSize(ref vrRenderWidth, ref vrRenderHeight);
            FramebufferDesc.CreateFrameBuffer((int) vrRenderWidth, (int) vrRenderHeight, out leftEyeDesc);
            FramebufferDesc.CreateFrameBuffer((int) vrRenderWidth, (int) vrRenderHeight, out rightEyeDesc);
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

        public void switchAR()
        {
            enableAR = !enableAR;
            if (enableAR)
                Rhino.RhinoApp.WriteLine("enable AR mode");
            else
                Rhino.RhinoApp.WriteLine("disable AR mode");
        }

        public virtual void RenderScene(Valve.VR.EVREye eye)
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


        protected virtual void RenderStereoTargets()
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
            }
            else
            {
                GL.Viewport(0, 0, (int)vrRenderWidth, (int)vrRenderHeight);
            }
            
            RenderScene(Valve.VR.EVREye.Eye_Left);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Disable(EnableCap.Multisample);

            // BLit Left Eye to Resolve buffer
            leftEyeDesc.BlitToResolve();

            // Right Eye.
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, rightEyeDesc.renderFramebufferId);
            if (enableAR)
            {
                int ox = ((int)vrRenderWidth - ovrvision_controller.camWidth) / 2;
                int oy = ((int)vrRenderHeight - ovrvision_controller.camHeight) / 2;
                //ox-100 to deal with the blur issue
                GL.Viewport(ox - 100, oy, ovrvision_controller.camWidth, ovrvision_controller.camHeight);                
            }
            else
            {
                GL.Viewport(0, 0, (int)vrRenderWidth, (int)vrRenderHeight);
            }  
            RenderScene(Valve.VR.EVREye.Eye_Right);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Disable(EnableCap.Multisample);

            // BLit Right Eye to Resolve buffer
            rightEyeDesc.BlitToResolve();
        }
        
        private void SetupCameras()
        {
            mEyePosLeft = GetHMDMatrixPoseEye(ref mHMD, Valve.VR.EVREye.Eye_Left);
            mEyePosRight = GetHMDMatrixPoseEye(ref mHMD, Valve.VR.EVREye.Eye_Right);
            mEyeProjLeft = GetHMDMatrixProjectionEye(ref mHMD, Valve.VR.EVREye.Eye_Left, mNearClip, mFarClip);
            mEyeProjRight = GetHMDMatrixProjectionEye(ref mHMD, Valve.VR.EVREye.Eye_Right, mNearClip, mFarClip);
        }
        

        public override void RenderFrame()
        {
            if (mHMD != null)
            {
                GL.DepthFunc(DepthFunction.Less);
                GL.Enable(EnableCap.DepthTest);
                SetupCameras();
                // DrawControllers
                RenderStereoTargets();
                GL.Finish();
                GL.Flush();
                RenderCompanionWindow();
                SubmitToHmd(leftEyeDesc, rightEyeDesc);
            }
        }

        public void RenderCompanionWindow()
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
