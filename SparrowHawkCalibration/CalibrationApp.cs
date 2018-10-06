﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using Valve.VR;
using SparrowHawk;
using SparrowHawk.Calibration;
using OpenTK.Input;
using System.Xml.Serialization;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using SparrowHawk.Renderer;


namespace SparrowHawkCalibration
{
    public class CalibrationApp : OpenTK.GameWindow
    {
        // VR Handling
        CVRSystem mHMD;
        int mLeftControllerIdx, mRightControllerIdx;
        TrackedDevicePose_t[] mGamePoseArray = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        TrackedDevicePose_t[] mTrackedDevices = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        char[] mDeviceClassChar = new Char[OpenVR.k_unMaxTrackedDeviceCount];
        FramebufferDesc mLeftEyeDesc, mRightEyeDesc;
        Random mRand;
        SparrowHawk.Geometry.Geometry bunny;
        SparrowHawk.Material.Material bunnyMat;
        SparrowHawk.Geometry.Geometry rightCursor;
        SparrowHawk.Material.Material cursorMat;

        // Callibration Machinery
        int mPointIndex = 0;
        static int numCalibrationPoints = 8;
        CalibrationProcedure calibrationProcedure = new StereoSpaamCalibrationProcedure(numCalibrationPoints);
        Vector3 controllerOffset = Vector3.Zero;
        MetaTwoCalibrationData calibrationData = new MetaTwoCalibrationData();

        // Debug info
        private bool debug = true;
        private SparrowHawk.Geometry.Geometry debugPoint;
        SparrowHawk.Material.Material pointMaterial;

        /// <summary>
        /// Updates matrix poses. Should probably inherit from VrGame
        /// </summary>
        protected void UpdateMatrixPose()
        {
            if (mHMD == null)
                return;
            OpenVR.Compositor.WaitGetPoses(mTrackedDevices, mGamePoseArray);
            for (uint i = 0; i < mTrackedDevices.Length; i++)
            {
                var device = mGamePoseArray[i];
                if (!device.bPoseIsValid)
                    continue;
                switch (mHMD.GetTrackedDeviceClass(i))
                {
                    case ETrackedDeviceClass.Controller:
                        mDeviceClassChar[i] = 'C';
                        string name = SparrowHawk.UtilOld.GetTrackedDeviceString(ref mHMD, i, ETrackedDeviceProperty.Prop_RenderModelName_String);
                        if (name.ToLower().Contains("left"))
                            mLeftControllerIdx = (int)i;
                        else if (name.ToLower().Contains("right"))
                            mRightControllerIdx = (int)i;
                        else if (mLeftControllerIdx < 0)
                            mLeftControllerIdx = (int)i;
                        else if (mRightControllerIdx < 0)
                           mRightControllerIdx = (int)i;
                        break;
                    case ETrackedDeviceClass.HMD: mDeviceClassChar[i] = 'H'; break;
                    case ETrackedDeviceClass.Invalid: mDeviceClassChar[i] = 'I'; break;
                    case ETrackedDeviceClass.GenericTracker: mDeviceClassChar[i] = 'G'; break;
                    case ETrackedDeviceClass.TrackingReference: mDeviceClassChar[i] = 'T'; break;
                    default: mDeviceClassChar[i] = '?'; break;
                }

            }
        }


        public CalibrationApp()
        {
            Init();
        }

        protected void Init()
        {

            // Init VR
            EVRInitError eError = EVRInitError.None;
            mHMD = OpenVR.Init(ref eError, EVRApplicationType.VRApplication_Scene);
            if (eError != EVRInitError.None)
                Console.WriteLine("Initialized. Error code: " + eError.ToString());

            // Setup window
            uint pnWidth = 1280, pnHeight = 1440;
            if (debug && mHMD != null)
                mHMD.GetRecommendedRenderTargetSize(ref pnWidth, ref pnHeight);
            Width = (int) pnWidth * 2;
            Height = (int) pnHeight;    
            Title = "SPAAM Calibration App";
            MakeCurrent();

            // Create rendering scaffolding.
            FramebufferDesc.CreateFrameBuffer(Width / 2, Height, out mLeftEyeDesc);
            FramebufferDesc.CreateFrameBuffer(Width / 2, Height, out mRightEyeDesc);

            debugPoint = new SparrowHawk.Geometry.DrawPointMarker(new Vector3(0,0,0));//new SparrowHawk.Geometry.PointMarker(new Vector3(0,0,0));
            bunny = new SparrowHawk.Geometry.Geometry("D:\\workspace\\SparrowHawk\\src\\resources\\bunny.obj");
            pointMaterial = new SparrowHawk.Material.SingleColorMaterial(1,1,0,1);
            bunnyMat = new SparrowHawk.Material.RGBNormalMaterial(1);
            rightCursor = new SparrowHawk.Geometry.DrawPointMarker(new Vector3(0, 0, 0));
            cursorMat = new SparrowHawk.Material.SingleColorMaterial(1, 0, 0, 1);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            MakeCurrent();
            UpdateMatrixPose();

            HandleInput();
            calibrationProcedure.UpdateData();

            if (debug)
                RenderDebugScene();

            if (calibrationProcedure.IsComplete())
            {
                calibrationData = calibrationProcedure.GetCalibrationData();
                RenderTestScene(!debug);
            } else
            {
                calibrationProcedure.RenderCalibrationOverlay(ref mLeftEyeDesc, ref mRightEyeDesc, debug);
            }
                            
            if (debug)
                SubmitToCompositor();
            RenderMetaWindow();
        }

        protected void SubmitToCompositor()
        {
            // Submit to SteamVR compositor if running in debug mode.
            if (debug && mHMD != null)
            {
                mLeftEyeDesc.BlitToResolve();
                mRightEyeDesc.BlitToResolve();
                RenderMetaWindow();
                VrRenderer.SubmitToHmd(mLeftEyeDesc, mRightEyeDesc);
            }
        }

        protected void RenderMetaWindow()
        {
            MakeCurrent();
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, mLeftEyeDesc.renderFramebufferId);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            GL.BlitFramebuffer(0,0,Width/2,Height, Width / 2, Height, 0, 0, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);

            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, mRightEyeDesc.renderFramebufferId);
            GL.BlitFramebuffer(0, 0, Width / 2, Height, Width, Height, Width/2, 0, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            GL.Flush();
            GL.Finish();

            SwapBuffers();
        }

        protected void RenderTestScene(bool clear)
        {
            Matrix4 modelTransform = Matrix4.CreateTranslation(calibrationProcedure.GetKnownPoint().Xyz);
            modelTransform.Transpose();
            Vector4 cursorPos = UtilOld.steamVRMatrixToMatrix4(mGamePoseArray[mRightControllerIdx].mDeviceToAbsoluteTracking) * new Vector4(controllerOffset, 1);
            Matrix4 rightControllerM = Matrix4.CreateTranslation(cursorPos.Xyz);
            rightControllerM.Transpose();
            GL.Viewport(0, 0, Width / 2, Height);
            GL.ClearColor(0.1f, 0, 0.1f, 1);

            Matrix4 viewProject = calibrationData.leftEyeProjection * UtilOld.steamVRMatrixToMatrix4(mGamePoseArray[mLeftControllerIdx].mDeviceToAbsoluteTracking).Inverted();
            viewProject.Transpose();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, mLeftEyeDesc.renderFramebufferId);
            if (clear)
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            bunnyMat.draw(ref bunny, ref modelTransform , ref viewProject);
            cursorMat.draw(ref rightCursor, ref rightControllerM, ref viewProject);

            viewProject = calibrationData.rightEyeProjection * UtilOld.steamVRMatrixToMatrix4(mGamePoseArray[mLeftControllerIdx].mDeviceToAbsoluteTracking).Inverted();
            viewProject.Transpose();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, mRightEyeDesc.renderFramebufferId);
            if (clear)
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            bunnyMat.draw(ref bunny, ref modelTransform, ref viewProject);
            cursorMat.draw(ref rightCursor, ref rightControllerM, ref viewProject);
        }


        protected void RenderDebugScene()
        {
            //(vp = mEyeProjRight * mEyePosRight * mScene.mHMDPose).T
            Matrix4 modelTransform = Matrix4.CreateTranslation(calibrationProcedure.GetKnownPoint().Xyz);
            modelTransform.Transpose();
            GL.Viewport(0, 0, Width / 2, Height);
            GL.ClearColor(0.1f, 0, 0.1f, 1);

            Matrix4 viewProject = VrRenderer.GetHMDMatrixProjectionEye(ref mHMD, EVREye.Eye_Left, 0.01f, 10f) * VrRenderer.GetHMDMatrixPoseEye(ref mHMD, EVREye.Eye_Left) * UtilOld.steamVRMatrixToMatrix4(mGamePoseArray[OpenVR.k_unTrackedDeviceIndex_Hmd].mDeviceToAbsoluteTracking).Inverted();
            viewProject.Transpose();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, mLeftEyeDesc.renderFramebufferId);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            pointMaterial.draw(ref debugPoint, ref modelTransform, ref viewProject);

            viewProject = VrRenderer.GetHMDMatrixProjectionEye(ref mHMD, EVREye.Eye_Right, 0.01f, 10f) * VrRenderer.GetHMDMatrixPoseEye(ref mHMD, EVREye.Eye_Right) * UtilOld.steamVRMatrixToMatrix4(mGamePoseArray[OpenVR.k_unTrackedDeviceIndex_Hmd].mDeviceToAbsoluteTracking).Inverted();
            viewProject.Transpose();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, mRightEyeDesc.renderFramebufferId);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            pointMaterial.draw(ref debugPoint, ref modelTransform, ref viewProject);
        }

        protected void HandleInput()
        {
            VREvent_t vrEvent  = new VREvent_t();
            unsafe
            {
                while (mHMD != null && mHMD.PollNextEvent(ref vrEvent, (uint)sizeof(VREvent_t)))
                {
                    if (vrEvent.eventType == (int) EVREventType.VREvent_ButtonPress
                        && vrEvent.data.controller.button == (uint)UtilOld.OculusButtonId.k_EButton_Oculus_Trigger)
                    {
                        RegisterPoint();
                    }
                }
            }
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.Key) {
                case Key.S:
                    SaveMatrices();
                    break;
                case Key.P:
                    RegisterPoint();
                    break;
                default:
                    break;
            }
        }

        protected void RegisterPoint()
        {
            if (mHMD != null)
            {
                calibrationProcedure.RegisterPoint(
                    mGamePoseArray[mLeftControllerIdx].mDeviceToAbsoluteTracking,
                    mGamePoseArray[mRightControllerIdx].mDeviceToAbsoluteTracking,
                    controllerOffset);
            }
        }

        protected void SaveMatrices()
        {
            Console.WriteLine("Saving calibration data to " + Spaam.CalibrationPath);
            System.IO.Directory.CreateDirectory(Spaam.CalibrationDir);
            XmlSerializer xmlf = new XmlSerializer(typeof(MetaTwoCalibrationData));
            FileStream file = File.Open(Spaam.CalibrationPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            xmlf.Serialize(file, calibrationData);
            file.Close();
        }

        static void Main(string[] args)
        {
            new CalibrationApp().Run();
        }
    }
}
