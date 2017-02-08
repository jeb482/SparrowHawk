﻿using System;
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
        Matrix4 mEyeProjLeft;
        Matrix4 mEyeProjRight;
        Matrix4 mEyePosLeft;
        Matrix4 mEyePosRight;
        FramebufferDesc leftEyeDesc;
        FramebufferDesc rightEyeDesc;
        Valve.VR.CVRSystem mHMD;
        Scene mScene;

        //ovrvision camera
        //Thread
        Thread UpdateThread = null;
        bool ThreadEnd = false;
        private volatile bool UPDATE_LEFTDATA = false;
        private volatile bool UPDATE_RIGHTDATA = false;

        COvrvision Ovrvision = null;
        int[] _cameraTextures = new int[2];
        int cameraTexturesLeft;
        int cameraTexturesRight;
        int cubeTexture;
        int vaoDepth, vaoQuad, vaoCube;
        int vboDepth, vboQuad, vboCube;
        // Create shader programs
        int screenVertexShader, screenFragmentShader, screenShaderProgram;
        int cubeVertexShader, cubeFragmentShader, cubeShaderProgram;
        int depthVertexShader, depthFragmentShader, depthShaderProgram;

        float[] quadVertices = new float[24]{
        -1.0f,  1.0f, 0.0f, 0.0f,
        1.0f,  1.0f,  1.0f, 0.0f,
        1.0f, -1.0f,  1.0f, 1.0f,

        1.0f, -1.0f,  1.0f, 1.0f,
        -1.0f, -1.0f,  0.0f, 1.0f,
        -1.0f,  1.0f,  0.0f, 0.0f
        };

        float[] cubeVertices = new float[180]{
        // Positions          // Texture Coords
        0.0f, 0.0f, 0.0f,  0.0f, 0.0f,
              3.0f, 0.0f, 0.0f,  1.0f, 0.0f,
              3.0f,  3.0f, 0.0f,  1.0f, 1.0f,
              3.0f,  3.0f, 0.0f,  1.0f, 1.0f,
              0.0f,  3.0f, 0.0f,  0.0f, 1.0f,
              0.0f, 0.0f, 0.0f,  0.0f, 0.0f,

              0.0f, 0.0f,  -3.0f,  0.0f, 0.0f,
              3.0f, 0.0f,  -3.0f,  1.0f, 0.0f,
              3.0f,  3.0f,  -3.0f,  1.0f, 1.0f,
              3.0f,  3.0f,  -3.0f,  1.0f, 1.0f,
              0.0f,  3.0f,  -3.0f,  0.0f, 1.0f,
              0.0f, 0.0f,  -3.0f,  0.0f, 0.0f,

              0.0f,  3.0f,  -3.0f,  1.0f, 0.0f,
              0.0f,  3.0f, 0.0f,  1.0f, 1.0f,
              0.0f, 0.0f, 0.0f,  0.0f, 1.0f,
              0.0f, 0.0f, 0.0f,  0.0f, 1.0f,
              0.0f, 0.0f,  -3.0f,  0.0f, 0.0f,
              0.0f,  3.0f,  -3.0f,  1.0f, 0.0f,

              3.0f,  3.0f,  -3.0f,  1.0f, 0.0f,
              3.0f,  3.0f, 0.0f,  1.0f, 1.0f,
              3.0f, 0.0f, 0.0f,  0.0f, 1.0f,
              3.0f, 0.0f, 0.0f,  0.0f, 1.0f,
              3.0f, 0.0f,  -3.0f,  0.0f, 0.0f,
              3.0f,  3.0f,  -3.0f,  1.0f, 0.0f,

              0.0f, 0.0f, 0.0f,  0.0f, 1.0f,
              3.0f, 0.0f, 0.0f,  1.0f, 1.0f,
              3.0f, 0.0f,  -3.0f,  1.0f, 0.0f,
              3.0f, 0.0f,  -3.0f,  1.0f, 0.0f,
              0.0f, 0.0f,  -3.0f,  0.0f, 0.0f,
              0.0f, 0.0f, 0.0f,  0.0f, 1.0f,

              0.0f,  3.0f, 0.0f,  0.0f, 1.0f,
              3.0f,  3.0f, 0.0f,  1.0f, 1.0f,
              3.0f,  3.0f,  -3.0f,  1.0f, 0.0f,
              3.0f,  3.0f,  -3.0f,  1.0f, 0.0f,
              0.0f,  3.0f,  -3.0f,  0.0f, 0.0f,
              0.0f,  3.0f, 0.0f,  0.0f, 1.0f
          };

        string screenVertexSource = @"#version 150 core
in vec2 position;
in vec2 texcoord;
out vec2 Texcoord; // must match name in fragment shader
void main()
{
    // gl_Position is a special variable of OpenGL that must be set
	Texcoord = texcoord;
	gl_Position = vec4(position, 0.0, 1.0);
}";
        string screenFragmentSource = @"#version 150 core
in vec2 Texcoord; // must match name in vertex shader
out vec4 outColor; // first out variable is automatically written to the screen
uniform sampler2D texFramebuffer;
void main()
{
    outColor = texture(texFramebuffer, Texcoord);
}";

        string augmentedSceneSource_vs = @"#version 330 core
layout (location = 0) in vec3 position;
layout (location = 1) in vec2 texcoord;

out vec2 Texcoord;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{

    gl_Position = projection * view * model * vec4(position, 1.0);
    Texcoord = texcoord;
}";

        string augmentedSceneSource_fs = @"#version 330 core
in vec2 Texcoord;

out vec4 outColor;

uniform sampler2D texFramebuffer;

void main()
{             
    outColor = texture(texFramebuffer, Texcoord);
}";

        // opencv for ovrvision
        private Mat _frame_L = new Mat();
        private Mat outFrame_L = new Mat();
        private readonly Mat _grayFrame_L = new Mat();
        private Mat _frame_R = new Mat();
        private Mat outFrame_R = new Mat();
        private readonly Mat _grayFrame_R = new Mat();
        int _width = 9; //width of chessboard no. squares in width - 1
        int _height = 6; // heght of chess board no. squares in heigth - 1
        private float _squareSize = 1.4f;
        private Size _patternSize = new Size(9, 6);  //size of chess board to be detected
        VectorOfPointF _corners = new VectorOfPointF(); //corners found from chessboard
        Mat[] _rvecs, _tvecs;
        Matrix<double> _rvecAR = new Matrix<double>(3, 1);
        Matrix<double> _tvecAR = new Matrix<double>(3, 1);
        Matrix<double> _tvecAR_debug = new Matrix<double>(3, 1);

        OpenTK.Matrix4 glProjectionMatrix = new Matrix4();
        OpenTK.Matrix4 glViewMatrix = new Matrix4();
        OpenTK.Matrix4 glModelMatrix = new Matrix4();
        OpenTK.Matrix4 glProjectionMatrix_R = new Matrix4();
        OpenTK.Matrix4 glViewMatrix_R = new Matrix4();
        OpenTK.Matrix4 glModelMatrix_R = new Matrix4();

        private bool _find;
        Matrix<double> ProjectionMatrix_debug = new Matrix<double>(4, 4);
        Matrix<double> ViewMatrix_debug = new Matrix<double>(4, 4);
        Matrix<double> ModelMatrix_debug = new Matrix<double>(4, 4);
        List<MCvPoint3D32f> objectList;
        List<MCvPoint3D32f> axisPoints;
        List<MCvPoint3D32f> cubePoints;
        Mat _cameraMatrix_left = new Mat(3, 3, DepthType.Cv64F, 1);
        Mat _distCoeffs_left = new Mat(4, 1, DepthType.Cv64F, 1);
        Mat _cameraMatrix_right = new Mat(3, 3, DepthType.Cv64F, 1);
        Mat _distCoeffs_right = new Mat(4, 1, DepthType.Cv64F, 1);


        public VrRenderer(ref Valve.VR.CVRSystem HMD, ref Scene scene, uint mRenderWidth, uint mRenderHeight)
        {
            mHMD = HMD;
            mScene = scene;
            SetupStereoRenderTargets(ref mHMD);
            SetupDistortion();
            vrRenderWidth = mRenderWidth;
            vrRenderHeight = mRenderHeight;

            //ovrvision
            initARscene();
            initCameraPara();
            //initOvrvisoin();
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
            //clear in the RenderScene_AR
            //GL.ClearColor(0,0,0,1);
            //GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            mHMD.GetEyeToHeadTransform(eye);

            Matrix4 vp;
            switch (eye)
            {
                case Valve.VR.EVREye.Eye_Left:
                    //Util.WriteLine(ref mScene.rhinoDoc, mScene.mHMDPose.ToString());
                    vp = mEyeProjLeft * mEyePosLeft * mScene.mHMDPose;
                    break;
                default:
                    vp = mEyeProjRight * mEyePosRight* mScene.mHMDPose;
                    break;
            }
            vp.Transpose();
            mScene.render(ref vp);
            
        }

        //ovrvision stuff

        private void initCameraPara()
        {
            //using default values for testing
            // Left CameraInstric   : (6.7970157255511901e+002, 0., 6.0536133655058381e+002, 0., 6.8042527485960966e+002, 5.5465392353996174e+002, 0., 0., 1.);
            // Right CameraInstric   : (6.8865122458251960e+002, 0., 6.1472005979575772e+002, 0., 6.8933034565503726e+002, 5.0684212440916116e+002, 0., 0., 1.);
            // Left Distortion : (-4.1335867833577783e-001, 2.1178505767332989e-001, -4.7504756919241204e-004, 3.2255999104604089e-003, 3.7887562626108255e-002, -9.2038953879423846e-002, 3.1299065818407031e-002, 1.3086219827422665e-001);
            // Right Distortion : (-3.0493990540623439e-001, 1.3662653080461551e-001, 1.0423167776015031e-003, 2.8491358735884113e-003, 5.9120577005421594e-002, 3.3803762878286243e-002, -5.1376486225192128e-002, 1.5379685303460996e-001);;
            _cameraMatrix_left.SetValue(0, 0, 6.7970157255511901e+002);
            _cameraMatrix_left.SetValue(0, 1, 0);
            _cameraMatrix_left.SetValue(0, 2, 6.0536133655058381e+002);
            _cameraMatrix_left.SetValue(1, 0, 0);
            _cameraMatrix_left.SetValue(1, 1, 6.8042527485960966e+002);
            _cameraMatrix_left.SetValue(1, 2, 5.5465392353996174e+002);
            _cameraMatrix_left.SetValue(2, 0, 0);
            _cameraMatrix_left.SetValue(2, 1, 0);
            _cameraMatrix_left.SetValue(2, 2, 1);

            _cameraMatrix_right.SetValue(0, 0, 6.8865122458251960e+002);
            _cameraMatrix_right.SetValue(0, 1, 0);
            _cameraMatrix_right.SetValue(0, 2, 6.1472005979575772e+002);
            _cameraMatrix_right.SetValue(1, 0, 0);
            _cameraMatrix_right.SetValue(1, 1, 6.8933034565503726e+002);
            _cameraMatrix_right.SetValue(1, 2, 5.0684212440916116e+002);
            _cameraMatrix_right.SetValue(2, 0, 0);
            _cameraMatrix_right.SetValue(2, 1, 0);
            _cameraMatrix_right.SetValue(2, 2, 1);

            _distCoeffs_left.SetValue(0, 0, -4.1335867833577783e-001);
            _distCoeffs_left.SetValue(1, 0, 2.1178505767332989e-001);
            _distCoeffs_left.SetValue(2, 0, -4.7504756919241204e-004);
            _distCoeffs_left.SetValue(3, 0, 3.2255999104604089e-003);

            _distCoeffs_right.SetValue(0, 0, -3.0493990540623439e-001);
            _distCoeffs_right.SetValue(1, 0, 1.3662653080461551e-001);
            _distCoeffs_right.SetValue(2, 0, 1.0423167776015031e-003);
            _distCoeffs_right.SetValue(3, 0, 2.8491358735884113e-003);

            objectList = new List<MCvPoint3D32f>();
            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    objectList.Add(new MCvPoint3D32f(j * _squareSize, i * _squareSize, 0.0F));
                }
            }


            axisPoints = new List<MCvPoint3D32f>();
            axisPoints.Add(new MCvPoint3D32f(0.0f, 0.0f, 0.0f));
            axisPoints.Add(new MCvPoint3D32f(3.0f, 0.0f, 0.0f));
            axisPoints.Add(new MCvPoint3D32f(0.0f, 3.0f, 0.0f));
            axisPoints.Add(new MCvPoint3D32f(0.0f, 0.0f, -3.0f));

            cubePoints = new List<MCvPoint3D32f>();
            cubePoints.Add(new MCvPoint3D32f(0.0f, 0.0f, 0.0f));
            cubePoints.Add(new MCvPoint3D32f(0.0f, 3.0f, 0.0f));
            cubePoints.Add(new MCvPoint3D32f(3.0f, 3.0f, 0.0f));
            cubePoints.Add(new MCvPoint3D32f(3.0f, 0.0f, 0.0f));
            cubePoints.Add(new MCvPoint3D32f(0.0f, 0.0f, -3.0f));
            cubePoints.Add(new MCvPoint3D32f(0.0f, 3.0f, -3.0f));
            cubePoints.Add(new MCvPoint3D32f(3.0f, 3.0f, -3.0f));
            cubePoints.Add(new MCvPoint3D32f(3.0f, 0.0f, -3.0f));

            BuildProjectionMatrix(0.1f, 30, 0);
            BuildModelMatrix();

        }

        private void initARscene()
        {
            //testing
            cubeTexture = LoadTexture("texture.jpg");

            // Create VAOs
            GL.GenVertexArrays(1, out this.vaoCube);

            // Load vertex data
            GL.GenBuffers(1, out this.vboCube);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboCube);
            GL.BufferData(BufferTarget.ArrayBuffer, cubeVertices.Length * sizeof(float), cubeVertices, BufferUsageHint.StaticDraw);

            createShaderProgram(augmentedSceneSource_vs, augmentedSceneSource_fs, out cubeVertexShader, out cubeFragmentShader, out cubeShaderProgram);

            // Specify the layout of the vertex data
            GL.BindVertexArray(vaoCube);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboCube);

            specifyCubeVertexAttributes();
        }

        private void initOvrvisoin()
        {
            Ovrvision = new COvrvision();
            Ovrvision.useProcessingQuality = 0;	//DEMOSAIC & REMAP

            if (Ovrvision.Open(COvrvision.OV_CAMVR_FULL))
            {
                GL.GenTextures(1, out cameraTexturesLeft);
                GL.BindTexture(TextureTarget.Texture2D, cameraTexturesLeft);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, Ovrvision.imageSizeW, Ovrvision.imageSizeH, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgr, PixelType.UnsignedByte, IntPtr.Zero);

                GL.GenTextures(1, out cameraTexturesRight);
                GL.BindTexture(TextureTarget.Texture2D, cameraTexturesRight);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, Ovrvision.imageSizeW, Ovrvision.imageSizeH, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgr, PixelType.UnsignedByte, IntPtr.Zero);

                GL.BindTexture(TextureTarget.Texture2D, 0);

                // Create VAOs
                GL.GenVertexArrays(1, out this.vaoQuad);

                // Load vertex data
                GL.GenBuffers(1, out this.vboQuad);
                GL.BindBuffer(BufferTarget.ArrayBuffer, vboQuad);
                GL.BufferData(BufferTarget.ArrayBuffer, quadVertices.Length * sizeof(float), quadVertices, BufferUsageHint.StaticDraw);

                createShaderProgram(screenVertexSource, screenFragmentSource, out screenVertexShader, out screenFragmentShader, out screenShaderProgram);

                // Specify the layout of the vertex data
                GL.BindVertexArray(vaoQuad);
                GL.BindBuffer(BufferTarget.ArrayBuffer, vboQuad);
                specifyScreenVertexAttributes(screenShaderProgram);

                // Load textures
                GL.UseProgram(screenShaderProgram);
                GL.Uniform1(GL.GetUniformLocation(screenShaderProgram, "texFramebuffer"), 0);

                //Thread start
                ThreadEnd = false;
                UpdateThread = new Thread(new ThreadStart(Camera_UpdateThread));
                UpdateThread.Start();
            }
            else
            {
                Console.WriteLine("State: Open Error.");
            }
        }


        //Update Thread
        private void Camera_UpdateThread()
        {
            while (!ThreadEnd)
            {
                ProcessFrame();
            }
        }

        private PointF[] imagePoints_L, imagePoints_axis_L, imagePoints_R, imagePoints_axis_R;
        private bool foundMarker_L = false;
        private bool foundMarker_R = false;

        private void ProcessFrame()
        {
            if (Ovrvision.imageDataLeft_Mat.Cols == 0 || Ovrvision.imageDataRight_Mat.Cols == 0)
            {
                Util.WriteLine(ref mScene.rhinoDoc, "waiting camera views");
                return;
            }
            
            _frame_L = Ovrvision.imageDataLeft_Mat;
            _frame_R = Ovrvision.imageDataRight_Mat;

            //CvInvoke.Undistort(_frame_L, outFrame_L, _cameraMatrix_left, _distCoeffs_left);
            //CvInvoke.Undistort(_frame_R, outFrame_R, _cameraMatrix_right, _distCoeffs_right);
            
            CvInvoke.CvtColor(_frame_L, _grayFrame_L, ColorConversion.Bgr2Gray);
            CvInvoke.CvtColor(_frame_R, _grayFrame_R, ColorConversion.Bgr2Gray);

            //calculate view and projection matrix for opengl CalibCbType.AdaptiveThresh | CalibCbType.FastCheck | CalibCbType.NormalizeImag
            _find = CvInvoke.FindChessboardCorners(_grayFrame_L, _patternSize, _corners, CalibCbType.AdaptiveThresh | CalibCbType.FastCheck | CalibCbType.NormalizeImage);

            //we use this loop so we can show a colour image rather than a gray:
            if (_find) //chess board found
            {
                Util.WriteLine(ref mScene.rhinoDoc, "left marker found");
                //make mesurments more accurate by using FindCornerSubPixel
                CvInvoke.CornerSubPix(_grayFrame_L, _corners, new Size(11, 11), new Size(-1, -1),
                    new MCvTermCriteria(20, 0.001));
                CvInvoke.SolvePnP(objectList.ToArray(), _corners.ToArray(), _cameraMatrix_left, _distCoeffs_left, _rvecAR, _tvecAR);

                // drawing axis points or cubePoints
                imagePoints_L = new PointF[cubePoints.Count];
                imagePoints_L = CvInvoke.ProjectPoints(cubePoints.ToArray(), _rvecAR, _tvecAR, _cameraMatrix_left, _distCoeffs_left);

                imagePoints_axis_L = new PointF[axisPoints.Count];
                imagePoints_axis_L = CvInvoke.ProjectPoints(axisPoints.ToArray(), _rvecAR, _tvecAR, _cameraMatrix_left, _distCoeffs_left);

                foundMarker_L = true;

                //calculate view matrix
                BuildViewMatrix(0);


            }
            else
            {

                if (imagePoints_L != null)
                    Array.Clear(imagePoints_L, 0, imagePoints_L.Length);
                if (imagePoints_axis_L != null)
                    Array.Clear(imagePoints_axis_L, 0, imagePoints_axis_L.Length);

                foundMarker_L = false;

            }

            //calculate view and projection matrix for opengl CalibCbType.AdaptiveThresh | CalibCbType.FastCheck | CalibCbType.NormalizeImag
            _find = CvInvoke.FindChessboardCorners(_grayFrame_R, _patternSize, _corners, CalibCbType.AdaptiveThresh | CalibCbType.FastCheck | CalibCbType.NormalizeImage);

            //we use this loop so we can show a colour image rather than a gray:
            if (_find) //chess board found
            {
                Util.WriteLine(ref mScene.rhinoDoc, "right marker found");
                //make mesurments more accurate by using FindCornerSubPixel
                CvInvoke.CornerSubPix(_grayFrame_R, _corners, new Size(11, 11), new Size(-1, -1),
                    new MCvTermCriteria(20, 0.001));
                CvInvoke.SolvePnP(objectList.ToArray(), _corners.ToArray(), _cameraMatrix_right, _distCoeffs_right, _rvecAR, _tvecAR);

                // drawing axis points or cubePoints
                imagePoints_R = new PointF[cubePoints.Count];
                imagePoints_R = CvInvoke.ProjectPoints(cubePoints.ToArray(), _rvecAR, _tvecAR, _cameraMatrix_right, _distCoeffs_right);

                imagePoints_axis_R = new PointF[axisPoints.Count];
                imagePoints_axis_R = CvInvoke.ProjectPoints(axisPoints.ToArray(), _rvecAR, _tvecAR, _cameraMatrix_right, _distCoeffs_right);

                foundMarker_R = true;

                //calculate view matrix
                BuildViewMatrix(1);


            }
            else
            {

                if (imagePoints_R != null)
                    Array.Clear(imagePoints_R, 0, imagePoints_R.Length);
                if (imagePoints_axis_R != null)
                    Array.Clear(imagePoints_axis_R, 0, imagePoints_axis_R.Length);

                foundMarker_R = false;
            }


        }

        private void BuildProjectionMatrix(float near, float far, int eye)
        {

            // using the new camrea matrix that we passed in when we do the initundistortremap
            //TODO: compile new ovrvision dll to get the correct one considering to the distortion. check ovrsetting.cpp file.

            float alpha = 428.0f;
            //697.85, 428.0f
            float beta = 428.0f;
            //443.92, g_pOvrvision->GetCamWidth() / 2
            float x0 = (Ovrvision.imageSizeW / 2);
            //541.14, g_pOvrvision->GetCamHeight() / 2;
            float y0 = (Ovrvision.imageSizeH / 2);

            //what should the uint of alpha be
            float left_modified = -(near / alpha) * x0;
            float right_modified = (near / alpha) * x0;
            float bottom_modified = -(near / beta) * y0;
            float top_modified = (near / beta) * y0;

            // OpenTK Matrix4 constructor is row-major
            OpenTK.Matrix4 projectionmatrix = new OpenTK.Matrix4(
                (float)2.0 * near / (right_modified - left_modified), 0, (right_modified + left_modified) / (right_modified - left_modified), 0,
                0, (float)2.0 * near / (top_modified - bottom_modified), (top_modified + bottom_modified) / (top_modified - bottom_modified), 0,
                0, 0, -(far + near) / (far - near), -2 * far * near / (far - near),
                0, 0, -1.0f, 0);

            //TODO: considering the distortion, use different matrix
            OpenTK.Matrix4.Transpose(ref projectionmatrix, out glProjectionMatrix);
            OpenTK.Matrix4.Transpose(ref projectionmatrix, out glProjectionMatrix_R);

        }

        private void BuildModelMatrix()
        {
            //Identity matrix doesn't need to transpose. 
            glModelMatrix = new Matrix4(Vector4.UnitX, Vector4.UnitY, Vector4.UnitZ, Vector4.UnitW);
        }

        private void BuildViewMatrix(int eye)
        {
            Mat rotation = new Mat(3, 3, DepthType.Cv64F, 1);
            rotation.SetTo(new MCvScalar(0));
            CvInvoke.Rodrigues(_rvecAR, rotation, null);

            //using OpenTK Matrix4
            OpenTK.Matrix4 tempViewMatrix = new Matrix4(
                rotation.GetValue(0,0), rotation.GetValue(0, 1), rotation.GetValue(0, 2), (float)_tvecAR.Data[0, 0],
                rotation.GetValue(1,0), rotation.GetValue(1, 1), rotation.GetValue(1, 2), (float)_tvecAR.Data[1, 0],
                rotation.GetValue(2,0), rotation.GetValue(2, 1), rotation.GetValue(2, 2), (float)_tvecAR.Data[2, 0],
                rotation.GetValue(3,0), rotation.GetValue(3, 1), rotation.GetValue(3, 2), (float)_tvecAR.Data[3, 0]
            );

            OpenTK.Matrix4 cvToOgl = new Matrix4(
                1.0f, 0, 0, 0,
                0, -1.0f, 0,0,
                0, 0, -1.0f, 0,
                0, 0, 0, 1.0f
            );

            tempViewMatrix = cvToOgl * tempViewMatrix;

            if (eye == 0)
            {
                OpenTK.Matrix4.Transpose( ref tempViewMatrix, out glViewMatrix);
                Util.WriteLine(ref mScene.rhinoDoc, glViewMatrix.ToString());

                //debug
                //calculatePosition();
            }
            else
            {

                OpenTK.Matrix4.Transpose(ref tempViewMatrix, out glViewMatrix_R);
            }


            
        }

        private void calculatePosition()
        {
            Vector4 point = new Vector4(3.0f, 3.0f, 0.0f, 1.0f);
            Vector4 new_point = new Vector4();

            OpenTK.Matrix4 ProjectionMatrix_RowMajor = new Matrix4();
            OpenTK.Matrix4 ViewMatrix_RowMajor = new Matrix4();
            OpenTK.Matrix4 ModelMatrix_RowMajor = new Matrix4();

            //need to transpose back to the row-major format
            OpenTK.Matrix4.Transpose(ref glProjectionMatrix, out ProjectionMatrix_RowMajor);
            OpenTK.Matrix4.Transpose(ref glViewMatrix, out ViewMatrix_RowMajor);
            OpenTK.Matrix4.Transpose(ref glModelMatrix, out ModelMatrix_RowMajor);

            new_point = ProjectionMatrix_RowMajor * ViewMatrix_RowMajor * ModelMatrix_RowMajor * point;

            Util.WriteLine(ref mScene.rhinoDoc, new_point.ToString());
        }

        //for GL render cube testing
        private int LoadTexture(string path, bool flip_y = false)
        {
            Bitmap bitmap = new Bitmap(path);
            //Flip the image
            if (flip_y)
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

            //Generate a new texture target in gl
            int texture = GL.GenTexture();

            //Will bind the texture newly/empty created with GL.GenTexture
            //All gl texture methods targeting Texture2D will relate to this texture
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmap.Width, bitmap.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

            //Load the data from are loaded image into virtual memory so it can be read at runtime
            BitmapData bitmap_data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                    ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, bitmap.Width, bitmap.Height, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, bitmap_data.Scan0);

            //Release from memory
            bitmap.UnlockBits(bitmap_data);

            //get rid of bitmap object its no longer needed in this method
            bitmap.Dispose();
            GL.BindTexture(TextureTarget.Texture2D, 0);

            return texture;
        }

        private void drawCubeGL(int eye)
        {
            GL.GetError();
            GL.UseProgram(cubeShaderProgram);
            GL.GetError();
            GL.Uniform1(GL.GetUniformLocation(cubeShaderProgram, "texFramebuffer"), 0);
            GL.GetError();

            if (eye == 0)
            {
                GL.UniformMatrix4(GL.GetUniformLocation(cubeShaderProgram, "model"), false, ref glModelMatrix);
                GL.GetError();
                GL.UniformMatrix4(GL.GetUniformLocation(cubeShaderProgram, "view"), false, ref glViewMatrix);
                GL.GetError();
                GL.UniformMatrix4(GL.GetUniformLocation(cubeShaderProgram, "projection"), false, ref glProjectionMatrix);
                GL.GetError();

            }else
            {
                GL.UniformMatrix4(GL.GetUniformLocation(cubeShaderProgram, "model"), false, ref glModelMatrix_R);
                GL.GetError();
                GL.UniformMatrix4(GL.GetUniformLocation(cubeShaderProgram, "view"), false, ref glViewMatrix_R);
                GL.GetError();
                GL.UniformMatrix4(GL.GetUniformLocation(cubeShaderProgram, "projection"), false, ref glProjectionMatrix_R);
                GL.GetError();
            }


            GL.Enable(EnableCap.DepthTest);
            GL.BindVertexArray(vaoCube);
            GL.BindTexture(TextureTarget.Texture2D, cubeTexture);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            GL.BindVertexArray(0);

            float[] uniform_values = new float[16];
            GL.GetUniform(cubeShaderProgram, GL.GetUniformLocation(cubeShaderProgram, "projection"), uniform_values);
            GL.GetError();
            GL.UseProgram(0);
        }


        private void drawCubeOpenCV(int eye)
        {
            if (imagePoints_L == null || imagePoints_R == null)
            {
                return;
            }

            if (eye == 0)
            {
                //draw cube on the imags
                VectorOfPoint ground = new VectorOfPoint(new Point[4] { new Point((int)imagePoints_L[0].X, (int)imagePoints_L[0].Y),
                        new Point((int)imagePoints_L[1].X, (int)imagePoints_L[1].Y), new Point((int)imagePoints_L[2].X, (int)imagePoints_L[2].Y), new Point((int)imagePoints_L[3].X, (int)imagePoints_L[3].Y) });
                VectorOfVectorOfPoint contours_ground = new VectorOfVectorOfPoint(ground);

                CvInvoke.DrawContours(Ovrvision.imageDataLeft_Mat, contours_ground, 0, new MCvScalar(0, 255, 255), -1);

                CvInvoke.Line(Ovrvision.imageDataLeft_Mat, new Point((int)imagePoints_L[0].X, (int)imagePoints_L[0].Y), new Point((int)imagePoints_L[4].X, (int)imagePoints_L[4].Y), new MCvScalar(0, 0, 255), 2);
                CvInvoke.Line(Ovrvision.imageDataLeft_Mat, new Point((int)imagePoints_L[1].X, (int)imagePoints_L[1].Y), new Point((int)imagePoints_L[5].X, (int)imagePoints_L[5].Y), new MCvScalar(0, 0, 255), 2);
                CvInvoke.Line(Ovrvision.imageDataLeft_Mat, new Point((int)imagePoints_L[2].X, (int)imagePoints_L[2].Y), new Point((int)imagePoints_L[6].X, (int)imagePoints_L[6].Y), new MCvScalar(0, 0, 255), 2);
                CvInvoke.Line(Ovrvision.imageDataLeft_Mat, new Point((int)imagePoints_L[3].X, (int)imagePoints_L[3].Y), new Point((int)imagePoints_L[7].X, (int)imagePoints_L[7].Y), new MCvScalar(0, 0, 255), 2);

                VectorOfPoint top = new VectorOfPoint(new Point[4] { new Point((int)imagePoints_L[4].X, (int)imagePoints_L[4].Y),
                        new Point((int)imagePoints_L[5].X, (int)imagePoints_L[5].Y), new Point((int)imagePoints_L[6].X, (int)imagePoints_L[6].Y), new Point((int)imagePoints_L[7].X, (int)imagePoints_L[7].Y) });
                VectorOfVectorOfPoint contours_top = new VectorOfVectorOfPoint(top);

                CvInvoke.DrawContours(Ovrvision.imageDataLeft_Mat, contours_top, 0, new MCvScalar(255, 0, 0), 2);

                //DRAWING
                //draw the axis on the image
                CvInvoke.Line(Ovrvision.imageDataLeft_Mat, new Point((int)imagePoints_axis_L[0].X, (int)imagePoints_axis_L[0].Y), new Point((int)imagePoints_axis_L[1].X, (int)imagePoints_axis_L[1].Y), new MCvScalar(0, 0, 255), 2);
                CvInvoke.Line(Ovrvision.imageDataLeft_Mat, new Point((int)imagePoints_axis_L[0].X, (int)imagePoints_axis_L[0].Y), new Point((int)imagePoints_axis_L[2].X, (int)imagePoints_axis_L[2].Y), new MCvScalar(0, 255, 0), 2);
                CvInvoke.Line(Ovrvision.imageDataLeft_Mat, new Point((int)imagePoints_axis_L[0].X, (int)imagePoints_axis_L[0].Y), new Point((int)imagePoints_axis_L[3].X, (int)imagePoints_axis_L[3].Y), new MCvScalar(255, 0, 0), 2);
            }
            else
            {
                //draw cube on the imags
                VectorOfPoint ground = new VectorOfPoint(new Point[4] { new Point((int)imagePoints_R[0].X, (int)imagePoints_R[0].Y),
                        new Point((int)imagePoints_R[1].X, (int)imagePoints_R[1].Y), new Point((int)imagePoints_R[2].X, (int)imagePoints_R[2].Y), new Point((int)imagePoints_R[3].X, (int)imagePoints_R[3].Y) });
                VectorOfVectorOfPoint contours_ground = new VectorOfVectorOfPoint(ground);

                CvInvoke.DrawContours(Ovrvision.imageDataRight_Mat, contours_ground, 0, new MCvScalar(0, 255, 255), -1);

                CvInvoke.Line(Ovrvision.imageDataRight_Mat, new Point((int)imagePoints_R[0].X, (int)imagePoints_R[0].Y), new Point((int)imagePoints_R[4].X, (int)imagePoints_R[4].Y), new MCvScalar(0, 0, 255), 2);
                CvInvoke.Line(Ovrvision.imageDataRight_Mat, new Point((int)imagePoints_R[1].X, (int)imagePoints_R[1].Y), new Point((int)imagePoints_R[5].X, (int)imagePoints_R[5].Y), new MCvScalar(0, 0, 255), 2);
                CvInvoke.Line(Ovrvision.imageDataRight_Mat, new Point((int)imagePoints_R[2].X, (int)imagePoints_R[2].Y), new Point((int)imagePoints_R[6].X, (int)imagePoints_R[6].Y), new MCvScalar(0, 0, 255), 2);
                CvInvoke.Line(Ovrvision.imageDataRight_Mat, new Point((int)imagePoints_R[3].X, (int)imagePoints_R[3].Y), new Point((int)imagePoints_R[7].X, (int)imagePoints_R[7].Y), new MCvScalar(0, 0, 255), 2);

                VectorOfPoint top = new VectorOfPoint(new Point[4] { new Point((int)imagePoints_R[4].X, (int)imagePoints_R[4].Y),
                        new Point((int)imagePoints_R[5].X, (int)imagePoints_R[5].Y), new Point((int)imagePoints_R[6].X, (int)imagePoints_R[6].Y), new Point((int)imagePoints_R[7].X, (int)imagePoints_R[7].Y) });
                VectorOfVectorOfPoint contours_top = new VectorOfVectorOfPoint(top);

                CvInvoke.DrawContours(Ovrvision.imageDataRight_Mat, contours_top, 0, new MCvScalar(255, 0, 0), 2);

                //DRAWING
                //draw the axis on the image
                CvInvoke.Line(Ovrvision.imageDataRight_Mat, new Point((int)imagePoints_axis_R[0].X, (int)imagePoints_axis_R[0].Y), new Point((int)imagePoints_axis_R[1].X, (int)imagePoints_axis_R[1].Y), new MCvScalar(0, 0, 255), 2);
                CvInvoke.Line(Ovrvision.imageDataRight_Mat, new Point((int)imagePoints_axis_R[0].X, (int)imagePoints_axis_R[0].Y), new Point((int)imagePoints_axis_R[2].X, (int)imagePoints_axis_R[2].Y), new MCvScalar(0, 255, 0), 2);
                CvInvoke.Line(Ovrvision.imageDataRight_Mat, new Point((int)imagePoints_axis_R[0].X, (int)imagePoints_axis_R[0].Y), new Point((int)imagePoints_axis_R[3].X, (int)imagePoints_axis_R[3].Y), new MCvScalar(255, 0, 0), 2);
            }
        }

        private void drawCameraView(int eye)
        {
            
            if (eye == 0)
            {

                //CvInvoke.Undistort(Ovrvision.imageDataLeft_Mat, outFrame_L, _cameraMatrix_left, _distCoeffs_left);

                if (foundMarker_L)
                {
                    drawCubeOpenCV(0);
                    drawCubeGL(0);
                }

                GL.BindTexture(TextureTarget.Texture2D, cameraTexturesLeft);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, Ovrvision.imageSizeW, Ovrvision.imageSizeH, OpenTK.Graphics.OpenGL4.PixelFormat.Bgr, PixelType.UnsignedByte, Ovrvision.imageDataLeft_Mat.DataPointer);
                //OVRVision Texture
                GL.BindTexture(TextureTarget.Texture2D, cameraTexturesLeft);

            }
            else{

                //CvInvoke.Undistort(Ovrvision.imageDataRight_Mat, outFrame_R, _cameraMatrix_right, _distCoeffs_right);

                if (foundMarker_R)
                {
                    drawCubeOpenCV(1);
                    drawCubeGL(1);
                }

                GL.BindTexture(TextureTarget.Texture2D, cameraTexturesRight);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, Ovrvision.imageSizeW, Ovrvision.imageSizeH, OpenTK.Graphics.OpenGL4.PixelFormat.Bgr, PixelType.UnsignedByte, Ovrvision.imageDataRight_Mat.DataPointer);
                //OVRVision Texture
                GL.BindTexture(TextureTarget.Texture2D, cameraTexturesRight);
            }

            GL.BindVertexArray(vaoQuad);
            GL.Disable(EnableCap.DepthTest);
            GL.UseProgram(screenShaderProgram);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.BindVertexArray(0);
        }

        private void RenderScene_AR(int eye)
        {
            Ovrvision.UpdateCamera();
            // Clear the screen to white
            GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (eye == 0)
            {
                Ovrvision.UpdateLeft();
                drawCameraView(0);

            }else
            {
                Ovrvision.UpdateRight();
                drawCameraView(1);

            }

        }


        private void createShaderProgram(string vertSrc, string fragSrc, out int vertexShader, out int fragmentShader, out int shaderProgram)
        {
            int statusCode;
            string info;

            // Create and compile the vertex shader
            vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertSrc);
            GL.CompileShader(vertexShader);
            // Check for compile time errors
            info = GL.GetShaderInfoLog(vertexShader);
            Console.Write(string.Format("triangle.vert compile: {0}", info));
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out statusCode);
            if (statusCode != 1) throw new ApplicationException(info);

            // Create and compile the fragment shader
            fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragSrc);
            GL.CompileShader(fragmentShader);
            info = GL.GetShaderInfoLog(fragmentShader);
            Console.Write(string.Format("triangle.frag compile: {0}", info));
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out statusCode);
            if (statusCode != 1) throw new ApplicationException(info);

            // Link the vertex and fragment shader into a shader program
            shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);
            GL.BindFragDataLocation(shaderProgram, 0, "outColor");
            GL.LinkProgram(shaderProgram);
        }

        private void specifyCubeVertexAttributes()
        {
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float,
               false, 5 * sizeof(float), 0);
            GL.GetError();

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float,
               false, 5 * sizeof(float), (3 * sizeof(float)));
            GL.GetError();

        }

        private void specifyScreenVertexAttributes(int shaderProgram)
        {
            int posAttrib = GL.GetAttribLocation(shaderProgram, "position");
            GL.EnableVertexAttribArray(posAttrib);
            GL.VertexAttribPointer(posAttrib, 2, VertexAttribPointerType.Float,
               false, 4 * sizeof(float), 0);

            int texAttrib = GL.GetAttribLocation(shaderProgram, "texcoord");
            GL.EnableVertexAttribArray(texAttrib);
            GL.VertexAttribPointer(texAttrib, 2, VertexAttribPointerType.Float,
               false, 4 * sizeof(float), (2 * sizeof(float)));

        }


        protected void RenderStereoTargets()
        {
            GL.Enable(EnableCap.Multisample);
            // some ovrcamera stuff here

            // Left Eye. Notably the original openvr code has some lame code duplication here.
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, leftEyeDesc.renderFramebufferId);
            int ox = ((int)vrRenderWidth - Ovrvision.imageSizeW) / 2;
            int oy = ((int)vrRenderHeight - Ovrvision.imageSizeH) / 2;
            GL.Viewport(ox+100, oy, Ovrvision.imageSizeW, Ovrvision.imageSizeH);
            //GL.Viewport(0, 0, (int) vrRenderWidth, (int) vrRenderHeight);
            RenderScene_AR(0);
            //GL.Viewport(0, 0, (int)vrRenderWidth, (int)vrRenderHeight);
            //RenderScene(Valve.VR.EVREye.Eye_Left);
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
            GL.Viewport(ox-100, oy, Ovrvision.imageSizeW, Ovrvision.imageSizeH);
            //GL.Viewport(0, 0, (int)vrRenderWidth, (int)vrRenderHeight);
            RenderScene_AR(1);
            //GL.Viewport(0, 0, (int)vrRenderWidth, (int)vrRenderHeight);
            //RenderScene(Valve.VR.EVREye.Eye_Right);
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
            // TODO: RenderDistortion
        
        // TODO: RenderFrame
        public void renderFrame()
        {
            if (mHMD != null)
            {

                setupCameras();
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
                Valve.VR.OpenVR.Compositor.Submit(Valve.VR.EVREye.Eye_Right, ref rightEyeTexture, ref pBounds, Valve.VR.EVRSubmitFlags.Submit_Default);

               
                
                GL.Finish();
            }
        }

        // TODO: UpdateHMDMatrixPose
    }
}
