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

        // opencv for ovrvision
        private Mat _frame_L = new Mat();
        private Mat outFrame_L = new Mat();
        private readonly Mat _grayFrame_L = new Mat();
        private Mat _frame_R = new Mat();
        private Mat outFrame_R = new Mat();
        private readonly Mat _grayFrame_R = new Mat();
        int _width = 9; //width of chessboard no. squares in width - 1
        int _height = 6; // heght of chess board no. squares in heigth - 1
        private float _squareSize = 1.0f;
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
        Mat _cameraMatrix_new = new Mat(3, 3, DepthType.Cv64F, 1);
        Mat _distCoeffs_new = new Mat(4, 1, DepthType.Cv64F, 1);


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
            initOvrvisoin();
            initCameraPara();
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

            /*
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
            */

            switch (eye)
            {
                case Valve.VR.EVREye.Eye_Left:
                    if (foundMarker_L)
                    {
                        drawCubeGL(0);
                        
                    }
                    drawController(0);
                    break;
                default:
                    if (foundMarker_R)
                    {
                        drawCubeGL(1);
                        
                    }
                    drawController(1);
                    break;
            }

            

        }

        //ovrvision stuff
        Geometry.Geometry marker_cube_g;
        Material.Material marker_cube_m;

        Geometry.Geometry controller_cube_g;
        Material.Material controller_cube_m;

        Geometry.Geometry fs_quad_g;
        Material.TextureMaterial fs_quad_m_L;
        Material.TextureMaterial fs_quad_m_R;

        private void initCameraPara()
        {
 
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

        }

        private void initARscene()
        {
            //TODO- rewrite with geometry and material
            // depth is negtive due to the marker coordinate system
            marker_cube_g = new Geometry.CubeGeometry(3.0f,3.0f,-3.0f);
            marker_cube_m = new Material.TextureMaterial(mScene.rhinoDoc, "texture.jpg", false);

            controller_cube_g = new Geometry.CubeGeometry(0.05f, 0.05f, -0.05f);
            controller_cube_m = new Material.TextureMaterial(mScene.rhinoDoc, "texture.jpg", false);
            // since markercube use different view matrix with other scene objects, we don't add it to the scene.
            // instead we use marker_cube_m.draw(ref geometry, ref model, ref vp) to draw it. 

        }

        //TODO- create a class and texture material
        private void initOvrvisoin()
        {
            Ovrvision = new COvrvision();
            Ovrvision.useProcessingQuality = 0;	//DEMOSAIC & REMAP

            if (Ovrvision.Open(COvrvision.OV_CAMVR_FULL))
            {
                //create the fullscreen quad here
                fs_quad_g = new Geometry.Geometry();
                fs_quad_g.mGeometry = new float[12] {-1.0f, 1.0f, 0.0f,
                                                             1.0f,  1.0f, 0.0f,
                                                             1.0f, -1.0f, 0.0f,
                                                             -1.0f, -1.0f, 0.0f};

                fs_quad_g.mGeometryIndices = new int[6]{ 0, 1, 2,
                                                             2, 3, 0};
                fs_quad_g.mUvs = new float[8] { 0.0f, 0.0f,
                                                1.0f, 0.0f,
                                                1.0f, 1.0f,
                                                0.0f, 1.0f};

                fs_quad_g.mNumPrimitives = 2;
                fs_quad_g.primitiveType = BeginMode.Triangles;

                //left eye texture material
                fs_quad_m_L = new Material.TextureMaterial(mScene.rhinoDoc, Ovrvision.imageSizeW, Ovrvision.imageSizeH, OpenTK.Graphics.OpenGL4.PixelFormat.Bgr, PixelType.UnsignedByte);
                fs_quad_m_R = new Material.TextureMaterial(mScene.rhinoDoc, Ovrvision.imageSizeW, Ovrvision.imageSizeH, OpenTK.Graphics.OpenGL4.PixelFormat.Bgr, PixelType.UnsignedByte);

                BuildProjectionMatrix(0.1f, 30, 0);
                BuildModelMatrix();

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
                //Util.WriteLine(ref mScene.rhinoDoc, "waiting camera views");
                return;
            }
            
            _frame_L = Ovrvision.imageDataLeft_Mat;
            _frame_R = Ovrvision.imageDataRight_Mat;

            CvInvoke.CvtColor(_frame_L, _grayFrame_L, ColorConversion.Bgr2Gray);
            CvInvoke.CvtColor(_frame_R, _grayFrame_R, ColorConversion.Bgr2Gray);

            //calculate view and projection matrix for opengl CalibCbType.AdaptiveThresh | CalibCbType.FastCheck | CalibCbType.NormalizeImag
            _find = CvInvoke.FindChessboardCorners(_grayFrame_L, _patternSize, _corners, CalibCbType.AdaptiveThresh | CalibCbType.FastCheck | CalibCbType.NormalizeImage);

            //we use this loop so we can show a colour image rather than a gray:
            if (_find) //chess board found
            {
                //Util.WriteLine(ref mScene.rhinoDoc, "left marker found");
                //make mesurments more accurate by using FindCornerSubPixel
                CvInvoke.CornerSubPix(_grayFrame_L, _corners, new Size(11, 11), new Size(-1, -1),
                    new MCvTermCriteria(20, 0.001));
                CvInvoke.SolvePnP(objectList.ToArray(), _corners.ToArray(), _cameraMatrix_new, _distCoeffs_new, _rvecAR, _tvecAR);

                // drawing axis points or cubePoints
                imagePoints_L = new PointF[cubePoints.Count];
                imagePoints_L = CvInvoke.ProjectPoints(cubePoints.ToArray(), _rvecAR, _tvecAR, _cameraMatrix_new, _distCoeffs_new);

                imagePoints_axis_L = new PointF[axisPoints.Count];
                imagePoints_axis_L = CvInvoke.ProjectPoints(axisPoints.ToArray(), _rvecAR, _tvecAR, _cameraMatrix_new, _distCoeffs_new);

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
                //Util.WriteLine(ref mScene.rhinoDoc, "right marker found");
                //make mesurments more accurate by using FindCornerSubPixel
                CvInvoke.CornerSubPix(_grayFrame_R, _corners, new Size(11, 11), new Size(-1, -1),
                    new MCvTermCriteria(20, 0.001));
                CvInvoke.SolvePnP(objectList.ToArray(), _corners.ToArray(), _cameraMatrix_new, _distCoeffs_new, _rvecAR, _tvecAR);

                // drawing axis points or cubePoints
                imagePoints_R = new PointF[cubePoints.Count];
                imagePoints_R = CvInvoke.ProjectPoints(cubePoints.ToArray(), _rvecAR, _tvecAR, _cameraMatrix_new, _distCoeffs_new);

                imagePoints_axis_R = new PointF[axisPoints.Count];
                imagePoints_axis_R = CvInvoke.ProjectPoints(axisPoints.ToArray(), _rvecAR, _tvecAR, _cameraMatrix_new, _distCoeffs_new);

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

            // using the new camrea matrix that we passed in when we do the initundistortremap in ovrvision.dll
            float[] camMatirx_array = new float[9];
            IntPtr ptr = Ovrvision.GetCameraMatrix(0);
            Marshal.Copy(ptr, camMatirx_array, 0, 9);

            /*
            float alpha = 428.0f;
            float beta = 428.0f;
            float x0 = (Ovrvision.imageSizeW / 2);
            float y0 = (Ovrvision.imageSizeH / 2);
            */

            //init new cameraMatrix
            _cameraMatrix_new.SetValue(0, 0, camMatirx_array[0]);
            _cameraMatrix_new.SetValue(0, 1, camMatirx_array[1]);
            _cameraMatrix_new.SetValue(0, 2, camMatirx_array[2]);
            _cameraMatrix_new.SetValue(1, 0, camMatirx_array[3]);
            _cameraMatrix_new.SetValue(1, 1, camMatirx_array[4]);
            _cameraMatrix_new.SetValue(1, 2, camMatirx_array[5]);
            _cameraMatrix_new.SetValue(2, 0, camMatirx_array[6]);
            _cameraMatrix_new.SetValue(2, 1, camMatirx_array[7]);
            _cameraMatrix_new.SetValue(2, 2, camMatirx_array[8]);

            _distCoeffs_new.SetValue(0, 0, 0);
            _distCoeffs_new.SetValue(1, 0, 0);
            _distCoeffs_new.SetValue(2, 0, 0);
            _distCoeffs_new.SetValue(3, 0, 0);

            float alpha = camMatirx_array[0];
            float beta = camMatirx_array[4];
            float x0 = camMatirx_array[2];
            float y0 = camMatirx_array[5];


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
            glModelMatrix_R = new Matrix4(Vector4.UnitX, Vector4.UnitY, Vector4.UnitZ, Vector4.UnitW);
        }

        Matrix4 mHeadtoCam_L = new Matrix4();
        Matrix4 mHeadtoCam_R = new Matrix4();

        private void BuildViewMatrix(int eye)
        {
            Mat rotation = new Mat(3, 3, DepthType.Cv64F, 1);
            rotation.SetTo(new MCvScalar(0));
            CvInvoke.Rodrigues(_rvecAR, rotation, null);

            //using OpenTK Matrix4
            OpenTK.Matrix4 tempViewMatrix = new Matrix4(
               (float)rotation.GetValue(0,0), (float)rotation.GetValue(0, 1), (float)rotation.GetValue(0, 2), (float)_tvecAR.Data[0, 0],
               (float)rotation.GetValue(1,0), (float)rotation.GetValue(1, 1), (float)rotation.GetValue(1, 2), (float)_tvecAR.Data[1, 0],
               (float)rotation.GetValue(2,0), (float)rotation.GetValue(2, 1), (float)rotation.GetValue(2, 2), (float)_tvecAR.Data[2, 0],
                0, 0, 0, 1
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
                if(calib_status == 1)
                {
                    Matrix4 mHeadInvert = new Matrix4();
                    Matrix4.Invert(ref mScene.mHMDPose, out mHeadInvert);
                    mHeadtoCam_L = tempViewMatrix * glmVRtoMarker * mHeadInvert;
                    calib_status = 2;

                    Util.WriteLine(ref mScene.rhinoDoc,"left eye calibrated");
                    Util.WriteLine(ref mScene.rhinoDoc, mHeadtoCam_L.ToString());
                }
                //Util.WriteLine(ref mScene.rhinoDoc, glViewMatrix.ToString());

               
            }
            else
            {

                OpenTK.Matrix4.Transpose(ref tempViewMatrix, out glViewMatrix_R);
                if (calib_status == 2)
                {
                    Matrix4 mHeadInvert = new Matrix4();
                    Matrix4.Invert(ref mScene.mHMDPose, out mHeadInvert);
                    mHeadtoCam_R = tempViewMatrix * glmVRtoMarker * mHeadInvert;
                    calib_status = 3;
                    Util.WriteLine(ref mScene.rhinoDoc, "right eye calibrated");
                    Util.WriteLine(ref mScene.rhinoDoc, mHeadtoCam_R.ToString());

                }
                //debug
                //calculatePosition();
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
            OpenTK.Matrix4.Transpose(ref glProjectionMatrix_R, out ProjectionMatrix_RowMajor);
            OpenTK.Matrix4.Transpose(ref glViewMatrix_R, out ViewMatrix_RowMajor);
            OpenTK.Matrix4.Transpose(ref glModelMatrix_R, out ModelMatrix_RowMajor);

            new_point = ProjectionMatrix_RowMajor * ViewMatrix_RowMajor * ModelMatrix_RowMajor * point;
            new_point /= new_point.W;

            Util.WriteLine(ref mScene.rhinoDoc, new_point.ToString());
        }


        List<MCvPoint3D32f> vr_points = new List<MCvPoint3D32f>();
        List<MCvPoint3D32f> marker_points = new List<MCvPoint3D32f>();
        Matrix<double> mVRtoMarker;
        Matrix4 glmVRtoMarker;
        byte[] inliers;
        int calib_status = 0; // 0 no cailb, 1 vr points added, 2 left eye matrix rdy , 3 righrt eye matrix rdy

        public void setDefaultMatrixHC()
        {
            //(-13.86312, -2.31339, 41.60522, -15.28837)
            //(-40.69572, 2.709207, -13.83543, 13.93765)
            //(-1.969063, -40.31068, -1.043179, 32.76967)
            //(0, 0, 0, 1)
            glmVRtoMarker = new Matrix4(
                    -13.86312f, -2.31339f, 41.60522f, -15.28837f,
                    -40.69572f, 2.709207f, -13.83543f, 13.93765f,
                    -1.969063f, -40.31068f, -1.043179f, 32.76967f,
                    0, 0, 0, 1
           );

            mHeadtoCam_L = new Matrix4(
                    42.96452f, 0.6706083f, 1.155278f, 1.186528f,
                    -1.254154f, 44.08835f, 1.114768f, 0.1481566f,
                    -2.066973f, -1.235542f, 40.16489f, 4.631462f,
                    0, 0, 0, 1
           );

            mHeadtoCam_R = new Matrix4(
                    42.99606f, 0.6534817f, 0.4786577f, -1.238245f,
                    -1.223896f, 44.08417f, 1.241231f, 0.2366943f,
                    -1.33017f, -1.375824f, 40.17358f, 4.709026f,
                    0, 0, 0, 1
           );
            calib_status = 3;
        }

        public void getMatrixHeadtoCamera()
        {
            Vector4 center = new Vector4(0, 0, 0, 1);
            if(vr_points.Count < 8 )
            {
                // find the pose of the controllers
                for (uint nDevice = OpenVR.k_unTrackedDeviceIndex_Hmd + 1; nDevice < OpenVR.k_unMaxTrackedDeviceCount; ++nDevice)
                {
                    if (!mHMD.IsTrackedDeviceConnected(nDevice))
                        continue;

                    if (mHMD.GetTrackedDeviceClass(nDevice) != ETrackedDeviceClass.Controller)
                        continue;

                    if (!mScene.mTrackedDevices[nDevice].bPoseIsValid)
                        continue;


                    Matrix4 mControllerPose = mScene.m_rmat4DevicePose[nDevice];
                    center = mControllerPose * new Vector4(0, 0, 0, 1);
                    Util.WriteLine(ref mScene.rhinoDoc, center.ToString());
                }

                vr_points.Add(new MCvPoint3D32f(center.X, center.Y, center.Z));
                calib_status = 0; 
            }
            else
            {
                marker_points.Add(new MCvPoint3D32f(0, 0, 0));
                marker_points.Add(new MCvPoint3D32f(3, 0, 0));
                marker_points.Add(new MCvPoint3D32f(3, 3, 0));
                marker_points.Add(new MCvPoint3D32f(0, 3, 0));

                marker_points.Add(new MCvPoint3D32f(0, 0, -2.17f));
                marker_points.Add(new MCvPoint3D32f(2.25f, 0, -2.17f));
                marker_points.Add(new MCvPoint3D32f(2.25f, 2.25f, -2.17f));
                marker_points.Add(new MCvPoint3D32f(0, 2.25f, -2.17f));

                CvInvoke.EstimateAffine3D(vr_points.ToArray(), marker_points.ToArray(), out mVRtoMarker, out inliers, 3, 0.99);

                glmVRtoMarker = new Matrix4(
                    (float)mVRtoMarker[0, 0], (float)mVRtoMarker[0, 1], (float)mVRtoMarker[0, 2], (float)mVRtoMarker[0, 3],
                    (float)mVRtoMarker[1, 0], (float)mVRtoMarker[1, 1], (float)mVRtoMarker[1, 2], (float)mVRtoMarker[1, 3],
                    (float)mVRtoMarker[2, 0], (float)mVRtoMarker[2, 1], (float)mVRtoMarker[2, 2], (float)mVRtoMarker[2, 3],
                    0, 0, 0, 1
                );

                calib_status = 1;
                Util.WriteLine(ref mScene.rhinoDoc, "VRtoMarker matrix");
                Util.WriteLine(ref mScene.rhinoDoc, glmVRtoMarker.ToString());

                vr_points.Clear();
                marker_points.Clear();
            }

            
        }


        private void drawController(int eye)
        {

            Matrix4 glControllerPose = new Matrix4();
            Matrix4 glViewMatrix_L = new Matrix4();
            Matrix4 glViewMatrix_R = new Matrix4();

            if (calib_status == 3 )
            {
               
               glViewMatrix_L = mHeadtoCam_L * mScene.mHMDPose;
               glViewMatrix_L.Transpose();

               glViewMatrix_R = mHeadtoCam_R * mScene.mHMDPose;
               glViewMatrix_R.Transpose();

            }
            else
            {
                glViewMatrix_L = mEyePosLeft * mScene.mHMDPose;
                glViewMatrix_L.Transpose();

                glViewMatrix_R = mEyePosRight * mScene.mHMDPose;
                glViewMatrix_R.Transpose();
            }

            //testing drawing at the controller position, mEyeProjLeft * mEyePosLeft * mScene.mHMDPose * mControllerPose;
            // find the pose of the controllers
            for (uint nDevice = OpenVR.k_unTrackedDeviceIndex_Hmd + 1; nDevice < OpenVR.k_unMaxTrackedDeviceCount; ++nDevice)
            {
                if (!mHMD.IsTrackedDeviceConnected(nDevice))
                    continue;

                if (mHMD.GetTrackedDeviceClass(nDevice) != ETrackedDeviceClass.Controller)
                    continue;

                if (!mScene.mTrackedDevices[nDevice].bPoseIsValid)
                    continue;


                Matrix4 mControllerPose = mScene.m_rmat4DevicePose[nDevice]; 
                Matrix4.Transpose(ref mControllerPose, out glControllerPose);

            }

            Matrix4 vp = new Matrix4();
            if (eye == 0)
            {
                //it's already transposed (column-major) so the order is v * p
                vp = glViewMatrix_L * glProjectionMatrix;

            }
            else
            {
                //it's already transposed (column-major) so the order is v * p
                vp = glViewMatrix_R * glProjectionMatrix_R;

            }

            GL.Enable(EnableCap.DepthTest);
            // glControllerPose pass as model since model matrix is identity matrix
            controller_cube_m.draw(ref controller_cube_g, ref glControllerPose, ref vp);
        }


        private void drawCubeGL(int eye)
        {
            Matrix4 vp = new Matrix4();

            if (eye == 0)
            {
               //watch out the matrix order since we already tranpose the matraix when we create the glMatrix(column-major)
               vp = glViewMatrix * glProjectionMatrix;
            }
            else
            {
                vp = glViewMatrix_R * glProjectionMatrix_R;
            }

            marker_cube_m.draw(ref marker_cube_g, ref glModelMatrix, ref vp);
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
            //camera draw on a full screen quad so we don't need model, vp matrix.
            Matrix4 model = new Matrix4(Vector4.UnitX, Vector4.UnitY, Vector4.UnitZ, Vector4.UnitW);
            Matrix4 vp = new Matrix4(Vector4.UnitX, Vector4.UnitY, Vector4.UnitZ, Vector4.UnitW);

            if (eye == 0)
            {

                if (foundMarker_L)
                {
                    drawCubeOpenCV(0);
                }

                fs_quad_m_L.updateTexture(Ovrvision.imageDataLeft_Mat.DataPointer);
                GL.Disable(EnableCap.DepthTest);
                fs_quad_m_L.draw(ref fs_quad_g, ref model, ref vp);

            }
            else
            {

                if (foundMarker_R)
                {
                    drawCubeOpenCV(1);
                }

                fs_quad_m_R.updateTexture(Ovrvision.imageDataRight_Mat.DataPointer);
                GL.Disable(EnableCap.DepthTest);
                fs_quad_m_R.draw(ref fs_quad_g, ref model, ref vp);

            }

            //set back to enable depth test after drawing
            GL.Enable(EnableCap.DepthTest);

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


        protected void RenderStereoTargets()
        {
            GL.Enable(EnableCap.Multisample);
            // some ovrcamera stuff here

            // Left Eye. Notably the original openvr code has some lame code duplication here.
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, leftEyeDesc.renderFramebufferId);
            int ox = ((int)vrRenderWidth - Ovrvision.imageSizeW) / 2;
            int oy = ((int)vrRenderHeight - Ovrvision.imageSizeH) / 2;
            //ox+100 to deal with blur issue
            GL.Viewport(ox+100, oy, Ovrvision.imageSizeW, Ovrvision.imageSizeH);
            //GL.Viewport(0, 0, (int) vrRenderWidth, (int) vrRenderHeight);
            RenderScene_AR(0);
            //GL.Viewport(0, 0, (int)vrRenderWidth, (int)vrRenderHeight);
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
            //ox-100 to deal with the blur issue
            GL.Viewport(ox-100, oy, Ovrvision.imageSizeW, Ovrvision.imageSizeH);
            //GL.Viewport(0, 0, (int)vrRenderWidth, (int)vrRenderHeight);
            RenderScene_AR(1);
            //GL.Viewport(0, 0, (int)vrRenderWidth, (int)vrRenderHeight);
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
