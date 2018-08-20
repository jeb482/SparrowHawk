using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using Valve.VR;
using SparrowHawk;
using SparrowHawk.Calibration;

namespace SparrowHawkCalibration
{




    public class CalibrationApp : OpenTK.GameWindow
    {
        CVRSystem mHMD;
        String mStrDriver = "No Driver";
        String mStrDisplay = "No Display";
        String mTitleBase;
        //uint mRenderWidth = 1280;
        //uint mRenderHeight = 720;
        int mLeftControllerIdx, mRightControllerIdx;
        TrackedDevicePose_t[] mGamePoseArray = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        TrackedDevicePose_t[] mTrackedDevices = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        char[] mDeviceClassChar = new Char[OpenVR.k_unMaxTrackedDeviceCount];
        FramebufferDesc mLeftEyeDesc, mRightEyeDesc;
        Random mRand;
        
        

        int mPointIndex = 0;
        bool calibrateLeft = true;
        bool hasKnownPoint = false;
        bool hasOffset = false;
        bool calibrationDone = false;
        Vector4 knownPoint = Vector4.Zero;
        Vector3 controllerOffset = Vector3.Zero;
        List<Vector2> mScreenPoints;
        List<Matrix4> mLeftHeadPoses;
        List<Matrix4> mRightHeadPoses;
        Matrix4 leftProj = Matrix4.Identity;
        Matrix4 rightProj = Matrix4.Identity;

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
                mScreenPoints.Add(new Vector2(mRand.Next(Width / 2), mRand.Next(Width / 2)));
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
                    leftProj = p4x4;
                    calibrateLeft = false;
                    mPointIndex = 0;
                } else
                {
                    rightProj = p4x4;
                    calibrationDone = true;
                }
                SaveMatrices();
            }

            // Render the active point.
            if (!calibrationDone) {
                FramebufferDesc fb = (calibrateLeft) ? mLeftEyeDesc : mRightEyeDesc;
                Spaam.RenderCrosshairs(mScreenPoints[mPointIndex], new OpenTK.Graphics.Color4(1, 1, 1, 1), fb);
            } else
            {
                // TODO: Render a debug scene.
            }
                

            


            //SparrowHawk.Calibration.Spaam
        }

        protected void HandleInput()
        {
            

            VREvent_t vrEvent  = new VREvent_t();
            unsafe
            {
                while (mHMD.PollNextEvent(ref vrEvent, (uint)sizeof(VREvent_t)))
                {
                    if (vrEvent.eventType == (int) EVREventType.VREvent_ButtonPress
                        && vrEvent.data.controller.button == (uint)UtilOld.OculusButtonId.k_EButton_Oculus_Trigger)
                    {
                        RegisterPoint();
                    }
                }
            }
        }

        protected void RegisterPoint()
        {
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
            // #TODO:
        }

        static void Main(string[] args)
        {

        }
    }
}
