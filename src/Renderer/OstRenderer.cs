﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;
using OpenTK.Graphics.OpenGL4;
using SparrowHawk;
using OpenTK;
using System.Xml.Serialization;
using System.IO;

namespace SparrowHawk.Renderer
{
    /// <summary>
    /// An augemented reality (AR) renderer for optical see-through (OST) HMDs,
    /// designed specifically to be compatible with a tracked Meta 2 Headset.
    /// </summary>
    public class OstRenderer : SparrowHawk.Renderer.AbstractRenderer
    {
        Calibration.MetaTwoCalibrationData CalibrationData;

        public OstRenderer (ref CVRSystem hmd, ref Scene scene, uint renderWidth, uint renderHeight) 
        {
            mHMD = hmd;
            mScene = scene;
            FramebufferDesc.CreateFrameBuffer((int) renderWidth / 2, (int) renderHeight, out leftEyeDesc);
            FramebufferDesc.CreateFrameBuffer((int) renderWidth / 2, (int) renderHeight, out rightEyeDesc);


            bool success = false;
            XmlSerializer xmlf = new XmlSerializer(typeof(Calibration.MetaTwoCalibrationData));
            using (FileStream file = File.Open(Calibration.Spaam.CalibrationPath, FileMode.Open))
            {
                CalibrationData = xmlf.Deserialize(file) as Calibration.MetaTwoCalibrationData;
                success = true;
            }
            if (!success)
                Console.WriteLine("Failed to read file " + Calibration.Spaam.CalibrationPath);
            

        }

        /// <summary>
        /// Given stereo pair in left and right, renders, flipped, to a window 
        /// assumed to be sent to the OST system. 
        /// Left and right assumed to be same height.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public void RenderOstWindow(FramebufferDesc left, FramebufferDesc right, bool flipY=true, bool flipX=false)
        {
            void swap(ref int a, ref int b)
            {
                int temp = a;
                a = b;
                b = temp;
            }
            int xMin = 0, yMin = 0, xMax = left.Width, yMax = left.Height;
            if (flipY) swap(ref yMax, ref yMin);
            if (flipX) swap(ref xMax, ref xMin);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, left.renderFramebufferId);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            GL.BlitFramebuffer(0, 0, left.Width, left.Height, xMin, yMin, xMax, yMax, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);

            xMin = left.Width;
            xMax = left.Width + right.Width; 
            if (flipX) swap(ref xMax, ref xMin);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, right.renderFramebufferId);
            GL.BlitFramebuffer(0, 0, left.Width, left.Height, xMin, yMin, xMax, yMax, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            GL.Flush();
            GL.Finish();
        }

        /// <summary>
        /// Renders everything in the current scene from the viewpoint of the given eye.
        /// </summary>
        /// <param name="eye"></param>
        protected void RenderScene(EVREye eye)
        {
            GL.ClearColor(0.1f, 0.0f, 0.1f, 1f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Matrix4 vp;
            switch (eye)
            {
                case Valve.VR.EVREye.Eye_Left:
                    vp = CalibrationData.leftEyeProjection * mScene.mDevicePose[mScene.leftControllerIdx].Inverted();
                    break;
                default:
                    vp = CalibrationData.rightEyeProjection * mScene.mDevicePose[mScene.leftControllerIdx].Inverted();
                    break;
            }
            vp.Transpose();
            mScene.render(ref vp);
        }

        /// <summary>
        /// Renders the scene to the left and right framebuffer objects.
        /// </summary>
        protected void RenderStereoTargets()
        {
            GL.Enable(EnableCap.Multisample);
            GL.Viewport(0, 0, leftEyeDesc.Width, leftEyeDesc.Height);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, leftEyeDesc.renderFramebufferId);
            RenderScene(Valve.VR.EVREye.Eye_Left);
            leftEyeDesc.BlitToResolve();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, rightEyeDesc.renderFramebufferId);
            RenderScene(Valve.VR.EVREye.Eye_Right);
            rightEyeDesc.BlitToResolve();
        }

        public override void RenderFrame()
        {
            if (mHMD != null)
            {
                GL.DepthFunc(DepthFunction.Less);
                GL.Disable(EnableCap.DepthTest);
                RenderStereoTargets();
                GL.Finish();
                GL.Flush();

                RenderOstWindow(leftEyeDesc, rightEyeDesc);
                SubmitToHmd(leftEyeDesc, rightEyeDesc);

            }
        }
    }
}
