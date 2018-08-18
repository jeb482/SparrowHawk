using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using Valve.VR;
using SparrowHawk;

namespace SparrowHawkCalibration
{




    public class CalibrationApp : OpenTK.GameWindow
    {
        CVRSystem mHMD;
        String mStrDriver = "No Driver";
        String mStrDisplay = "No Display";
        String mTitleBase;
        uint mRenderWidth = 1280;
        uint mRenderHeight = 720;
        int mLeftControllerIdx, mRightControllerIdx;
        TrackedDevicePose_t[] mGamePoseArray = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        TrackedDevicePose_t[] mTrackedDevices = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
        char[] mDeviceClassChar = new Char[OpenVR.k_unMaxTrackedDeviceCount];
        FramebufferDesc mLeftEyeDesc, mRightEyeDesc;
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
            SparrowHawk.VrRenderer.CreateFrameBuffer(Width / 2, Height, out mLeftEyeDesc);
            SparrowHawk.VrRenderer.CreateFrameBuffer(Width / 2, Height, out mRightEyeDesc);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            MakeCurrent();
            UpdateMatrixPose();
            //SparrowHawk.Calibration.Spaam
        }

        static void Main(string[] args)
        {

        }
    }
}
