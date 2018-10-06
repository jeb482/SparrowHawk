using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using SparrowHawk;
using SparrowHawk.Renderer;
using SparrowHawk.Calibration;
using OpenTK;
using Valve.VR;

namespace SparrowHawkCalibration
{
    public abstract class CalibrationProcedure
    {
        public abstract void RenderCalibrationOverlay(ref FramebufferDesc leftEyeDesc, ref FramebufferDesc rightEyeDesc, bool debug);
        public abstract void RegisterPoint(HmdMatrix34_t rightControllerPose, HmdMatrix34_t leftControllerPose, Vector3 rightControllerOffset);
        public abstract bool IsComplete();
        public abstract void UpdateData();
        public abstract MetaTwoCalibrationData GetCalibrationData();
        public abstract Vector4 GetKnownPoint();
    }





    public class SpaamCalibrationProcedure : CalibrationProcedure {

        protected bool CalibrateLeft = true;
        protected bool HasKnownPoint = false;
        protected int NumCalibrationPoints;
        bool CalibrationDone = false;
        protected int mPointIndex = 0;
        protected List<Vector2> mScreenPoints;
        Vector4 knownPoint = Vector4.Zero;
        List<Matrix4> mLeftHeadPoses;
        List<Matrix4> mRightHeadPoses;
        MetaTwoCalibrationData calibrationData = new MetaTwoCalibrationData();

        public SpaamCalibrationProcedure(int numCalibrationPoints)
        {
            NumCalibrationPoints = numCalibrationPoints;
            Random rand = new Random();
            mScreenPoints = new List<Vector2>();
            for (int i = 0; i < numCalibrationPoints; i++)
            {
                mScreenPoints.Add(new Vector2((float)rand.NextDouble() * 1f - .5f, (float)rand.NextDouble() * 1f - .5f));
            }
            mLeftHeadPoses = new List<Matrix4>();
            mRightHeadPoses = new List<Matrix4>();
        }

        public override Vector4 GetKnownPoint()
        {
            return knownPoint;
        }

        public override void RenderCalibrationOverlay(ref FramebufferDesc leftEyeDesc, ref FramebufferDesc rightEyeDesc, bool debug)
        {
            FramebufferDesc fb = (CalibrateLeft) ? leftEyeDesc : rightEyeDesc;
            Spaam.RenderCrosshairs(mScreenPoints[mPointIndex], new OpenTK.Graphics.Color4(1, 1, 1, 1), fb, !debug);
        }

        public override bool IsComplete()
        {
            return CalibrationDone;
        }

        public override MetaTwoCalibrationData GetCalibrationData()
        {
            return calibrationData;
        }

        public override void UpdateData()
        {
            if (!CalibrationDone && mPointIndex < NumCalibrationPoints - 1)
                return;

            var poses = (CalibrateLeft) ? mLeftHeadPoses : mRightHeadPoses;
            var p3x4 = Spaam.EstimateProjectionMatrix3x4(poses, mScreenPoints, knownPoint);
            var p4x4 = Spaam.ConstructProjectionMatrix4x4(p3x4, 0.01f, 10, 1, -1, 1, -1);
            if (CalibrateLeft)
            {
                calibrationData.leftEyeProjection = p4x4;
                Console.WriteLine(Spaam.CalculateProjectionError(p4x4, poses, mScreenPoints, knownPoint));
                CalibrateLeft = false;
                mPointIndex = 0;
            }
            else
            {
                calibrationData.rightEyeProjection = p4x4;
                Console.WriteLine(Spaam.CalculateProjectionError(p4x4, poses, mScreenPoints, knownPoint));
                CalibrationDone = true;
            }
            Console.WriteLine(p4x4);
        }

        public override void RegisterPoint(HmdMatrix34_t rightControllerPose, HmdMatrix34_t leftControllerPose, Vector3 rightControllerOffset)
        {
            Matrix4 lPose = UtilOld.steamVRMatrixToMatrix4(leftControllerPose);
            Vector4 rPoint = UtilOld.steamVRMatrixToMatrix4(rightControllerPose) * new Vector4(rightControllerOffset,1);
            if (!HasKnownPoint)
            {
                knownPoint = rPoint;
                HasKnownPoint = true;
            }
            else if (CalibrateLeft)
            {
                mLeftHeadPoses.Add(lPose.Inverted());
                mPointIndex += 1;
            }
            else
            {
                mRightHeadPoses.Add(lPose.Inverted());
                mPointIndex += 1;
            }
        }

    }


}


