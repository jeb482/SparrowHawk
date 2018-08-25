using System;
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

namespace SparrowHawkCalibration
{




    public class CalibrationApp : OpenTK.GameWindow
    {
        CVRSystem mHMD;
        int mLeftControllerIdx, mRightControllerIdx;
        TrackedDevicePose_t[] mGamePoseArray = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        TrackedDevicePose_t[] mTrackedDevices = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        char[] mDeviceClassChar = new Char[OpenVR.k_unMaxTrackedDeviceCount];
        FramebufferDesc mLeftEyeDesc, mRightEyeDesc;
        Random mRand;
        
        

        int mPointIndex = 0;
        bool calibrateLeft = false;
        bool hasKnownPoint = true;
        bool hasOffset = false;
        bool calibrationDone = false;
        Vector4 knownPoint = Vector4.Zero;
        Vector3 controllerOffset = Vector3.Zero;
        List<Vector2> mScreenPoints;
        List<Matrix4> mLeftHeadPoses;
        List<Matrix4> mRightHeadPoses;
        MetaTwoCalibrationData calibrationData;
        string CalibrationPath = "meta_calibration.xml";


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
            mHMD = OpenVR.Init(ref eError, EVRApplicationType.VRApplication_Overlay);

            // Setup window
            Width = 2560;
            Height = 1440;
            Console.WriteLine("Initialized. Error code: " + eError.ToString());
            Title = "SPAAM Calibration App";
            MakeCurrent();

            // Create rendering scaffolding.
            VrRenderer.CreateFrameBuffer(Width / 2, Height, out mLeftEyeDesc);
            VrRenderer.CreateFrameBuffer(Width / 2, Height, out mRightEyeDesc);
            mRand = new Random();
            mScreenPoints = new List<Vector2>();
            for (int i = 0; i < 6; i++)
            {
                mScreenPoints.Add(new Vector2((float) mRand.NextDouble() * 2 - 1, (float) mRand.NextDouble() * 2 - 1));
            }
            mLeftHeadPoses = new List<Matrix4>();
            mRightHeadPoses = new List<Matrix4>();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            MakeCurrent();
            UpdateMatrixPose();

            // Handle Input; Update state 
            HandleInput();
            if (!calibrationDone && mPointIndex >= 6)
            {
                var poses = (calibrateLeft) ? mLeftHeadPoses : mRightHeadPoses;
                var p3x4 = Spaam.estimateProjectionMatrix3x4(poses, mScreenPoints, knownPoint);
                var p4x4 = Spaam.constructProjectionMatrix4x4(p3x4, 0.02f, 5, Width / 4, Width / 4, Height / 2, Height / 2);
                if (calibrateLeft)
                {
                    calibrationData.leftEyeProjection = p4x4;
                    calibrateLeft = false;
                    mPointIndex = 0;
                } else
                {
                    calibrationData.rightEyeProjection = p4x4;
                    calibrationDone = true;
                }
            }

            // Render the active point.
            if (!calibrationDone) {
                FramebufferDesc fb = (calibrateLeft) ? mLeftEyeDesc : mRightEyeDesc;
                Spaam.RenderCrosshairs(mScreenPoints[mPointIndex], new OpenTK.Graphics.Color4(1, 1, 1, 1), fb);
            } else
            {
                // TODO: Render a debug scene.
            }

            RenderMetaWindow();
            


            //SparrowHawk.Calibration.Spaam
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
            if (mHMD == null)
            {
                mPointIndex += 1;
                return;
            }
            if (!hasKnownPoint)
            {
                var controllerPose = UtilOld.steamVRMatrixToMatrix4(mGamePoseArray[mRightControllerIdx].mDeviceToAbsoluteTracking);
                knownPoint = controllerPose * new Vector4(controllerOffset, 1);
                hasKnownPoint = true;
            } else if (calibrateLeft)
            {
                mLeftHeadPoses[mPointIndex] = UtilOld.steamVRMatrixToMatrix4(mGamePoseArray[mLeftControllerIdx].mDeviceToAbsoluteTracking);
                mPointIndex += 1;
            } else
            {
                mRightHeadPoses[mPointIndex] = UtilOld.steamVRMatrixToMatrix4(mGamePoseArray[mLeftControllerIdx].mDeviceToAbsoluteTracking);
                mPointIndex += 1;
            }
        }

        protected void SaveMatrices()
        {
            Console.WriteLine("Saving calibration data to " + CalibrationPath);
            XmlSerializer xmlf = new XmlSerializer(typeof(MetaTwoCalibrationData));
            FileStream file = File.Open(CalibrationPath, FileMode.OpenOrCreate);
            xmlf.Serialize(file, calibrationData);
            file.Close();
        }

        static void Main(string[] args)
        {
            new CalibrationApp().Run();
        }
    }
}
