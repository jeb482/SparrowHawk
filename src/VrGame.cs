using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using Valve.VR;
using Rhino.Geometry;
using Emgu.CV.Structure;
using Emgu.CV;

namespace SparrowHawk
{
    public class VrGame : OpenTK.GameWindow
    {
        CVRSystem mHMD;
        CVRRenderModels mRenderModels;
        Scene mScene;
        VrRenderer mRenderer;
        Rhino.RhinoDoc mDoc;
        String mStrDriver = "No Driver";
        String mStrDisplay = "No Display";
        String mTitleBase;
        uint mRenderWidth = 1280;
        uint mRenderHeight = 720;
        bool mIsLefty;

        TrackedDevicePose_t[] renderPoseArray, gamePoseArray;
        DateTime mLastFrameTime;
        bool mSafeForRobot = false;
        bool isUserIn = false;
        DateTime mLastTrackingTime;
        int mFrameCount;
        double frameRateTimer = 1;

        // Callibration
        List<Vector3> robotCallibrationPoints = new List<Vector3>();
        List<Vector3> robotCallibrationPointsTest = new List<Vector3>();
        List<Vector3> vrCallibrationPoints = new List<Vector3>();
        //Testing controller origins
        List<Vector3> controllerP = new List<Vector3>();
        //using opencv by Eric
        List<MCvPoint3D32f> robotCallibrationPoints_cv = new List<MCvPoint3D32f>();
        List<MCvPoint3D32f> vrCallibrationPoints_cv = new List<MCvPoint3D32f>();
        int lastNumRightControllerPoses;
        int lastNumLeftControllerPoses;
        List<OpenTK.Matrix4> mRightControllerPoses = new List<OpenTK.Matrix4>();
        List<OpenTK.Matrix4> mLeftControllerPoses = new List<OpenTK.Matrix4>();
        Matrix<double> mVRtoRobot;
        Matrix4 glmVRtoMarker;
        byte[] inliers;

        bool manualCallibration = false;

        DesignPlane3 xzPlane, xyPlane, yzPlane;
        DesignPlane2 xzPlane2, xyPlane2, yzPlane2;

        Geometry.Geometry printStroke;
        Material.Material printStroke_m;

        Guid uGuid;
        Interaction.Interaction current_i,last_i;
        Guid cutPGuid;

        public VrGame(ref Rhino.RhinoDoc doc, bool isLefty = false)
        {
            mDoc = doc;
            Rhino.RhinoApp.WriteLine("The robot offset is: " + OfflineCalibration.solveForRobotOffsetVector(OfflineCalibration.getHuaishuRobotMeasurements()));
            Rhino.RhinoApp.WriteLine("Working Directory: " + System.IO.Directory.GetCurrentDirectory());
            mIsLefty = isLefty;

            if (init())
                Rhino.RhinoApp.WriteLine("Initialization complete!");

            mScene.pushInteraction(new Interaction.PickPoint(ref mScene, ref vrCallibrationPoints));
        }

        /**
         * Updates the poses of all tracked devices in the Matrix4 format. 
         * Also handles new tracked devices, setting them up in the scene
         * and loading their render models.
         */
        void updateMatrixPose()
        {
            if (mHMD == null)
                return;

            OpenVR.Compositor.WaitGetPoses(mScene.mTrackedDevices, gamePoseArray);

            for (uint i = 0; i < mScene.mTrackedDevices.Length; i++)
            {
                var device = gamePoseArray[i];
                if (device.bPoseIsValid)
                {
                    mScene.mDevicePose[i] = Util.steamVRMatrixToMatrix4(mScene.mTrackedDevices[i].mDeviceToAbsoluteTracking);
                    mHMD.GetTrackedDeviceClass(i);
                    if (mScene.mDeviceClassChar[i] == 0)
                    {
                        switch (mHMD.GetTrackedDeviceClass(i))
                        {
                            case ETrackedDeviceClass.Controller:
                                mScene.mDeviceClassChar[i] = 'C';
                                string name = Util.GetTrackedDeviceString(ref mHMD, i, ETrackedDeviceProperty.Prop_RenderModelName_String);
                                if (name.ToLower().Contains("left"))
                                {
                                    mScene.leftControllerIdx = (int)i;
                                    // Uncomment to show controller model.
                                    //Geometry.Geometry g = new Geometry.Geometry(@"C:/workspace/SparrowHawk/src/resources/external_controller01_left.obj");
                                    //Material.Material m = new Material.RGBNormalMaterial(.5f);
                                    //SceneNode s = new SceneNode("LeftControllerModel", ref g, ref m);
                                    //s.transform = Util.createTranslationMatrix(-mScene.mLeftControllerOffset.M14, -mScene.mLeftControllerOffset.M24, -mScene.mLeftControllerOffset.M34);
                                    //mScene.leftControllerNode.add(ref s);
                                }
                                else if (name.ToLower().Contains("right"))
                                    mScene.rightControllerIdx = (int)i;
                                else if (mScene.leftControllerIdx < 0)
                                    mScene.leftControllerIdx = (int)i;
                                else if (mScene.rightControllerIdx < 0)
                                    mScene.rightControllerIdx = (int)i;
                                break;
                            case ETrackedDeviceClass.HMD: mScene.mDeviceClassChar[i] = 'H'; break;
                            case ETrackedDeviceClass.Invalid: mScene.mDeviceClassChar[i] = 'I'; break;
                            case ETrackedDeviceClass.GenericTracker: mScene.mDeviceClassChar[i] = 'G'; break;
                            case ETrackedDeviceClass.TrackingReference: mScene.mDeviceClassChar[i] = 'T'; break;
                            default: mScene.mDeviceClassChar[i] = '?'; break;
                        }
                    }
                }

            }

            if (gamePoseArray[OpenVR.k_unTrackedDeviceIndex_Hmd].bPoseIsValid)
            {
                mScene.mHMDPose = Util.steamVRMatrixToMatrix4(gamePoseArray[OpenVR.k_unTrackedDeviceIndex_Hmd].mDeviceToAbsoluteTracking).Inverted();
            }

            if (mScene.leftControllerIdx > 0)
                mScene.leftControllerNode.transform = mScene.mDevicePose[mScene.leftControllerIdx] * mScene.mLeftControllerOffset;
            if (mScene.rightControllerIdx > 0)
                mScene.rightControllerNode.transform = mScene.mDevicePose[mScene.rightControllerIdx] * mScene.mRightControllerOffset;
        }

        protected void updateControllerCallibration()
        {
            if (mHMD == null)
                return;
            if (mLeftControllerPoses.Count >= 4 && mLeftControllerPoses.Count > lastNumLeftControllerPoses)
            {
                lastNumLeftControllerPoses = mLeftControllerPoses.Count;
                Vector3 x = Util.solveForOffsetVector(mLeftControllerPoses);
                Rhino.RhinoApp.WriteLine("Left controller offset: " + x);
            }
            if (mRightControllerPoses.Count >= 4 && mRightControllerPoses.Count > lastNumRightControllerPoses)
            {
                lastNumRightControllerPoses = mRightControllerPoses.Count;
                Vector3 x = Util.solveForOffsetVector(mRightControllerPoses);
                Rhino.RhinoApp.WriteLine("Right controller offset: " + x);
            }
        }

        protected void handleInteractions()
        {
            if (mHMD == null)
                return;
            //default interaction
            if (mScene.interactionStackEmpty())
                mScene.pushInteraction(new Interaction.PickPoint(ref mScene));

            if (current_i != null)
            {
                last_i = current_i;
                current_i = mScene.peekInteraction();

                //testing init()
                if (last_i.GetType() != current_i.GetType())
                {
                    current_i.init();
                }
            }else
            {
                current_i = mScene.peekInteraction();
                current_i.init();
            }

            current_i.handleInput();
            current_i.draw(true);

            if (manualCallibration && vrCallibrationPoints.Count == 8)
            {
                manualCallibration = false; // HACK to not reset my cylinder forever.
                //Util.solveForAffineTransform(vrCallibrationPoints, robotCallibrationPoints, ref mScene.vrToRobot);

                //using opencv by Eric                
                foreach (Vector3 p in vrCallibrationPoints)
                {
                    vrCallibrationPoints_cv.Add(new MCvPoint3D32f(p.X, p.Y, p.Z));
                }
                CvInvoke.EstimateAffine3D(vrCallibrationPoints_cv.ToArray(), robotCallibrationPoints_cv.ToArray(), out mVRtoRobot, out inliers, 3, 0.99);

                mScene.vrToRobot = new Matrix4(
                    (float)mVRtoRobot[0, 0], (float)mVRtoRobot[0, 1], (float)mVRtoRobot[0, 2], (float)mVRtoRobot[0, 3],
                    (float)mVRtoRobot[1, 0], (float)mVRtoRobot[1, 1], (float)mVRtoRobot[1, 2], (float)mVRtoRobot[1, 3],
                    (float)mVRtoRobot[2, 0], (float)mVRtoRobot[2, 1], (float)mVRtoRobot[2, 2], (float)mVRtoRobot[2, 3],
                    0, 0, 0, 1
                );

                mScene.popInteraction();
                mScene.pushInteraction(new Interaction.CreateCylinder(ref mScene));
            }

            //testing controller origins
            if (controllerP.Count == 2)
            {
                Vector3 p1 = controllerP[0];
                Vector3 p2 = controllerP[1];
                Rhino.RhinoApp.WriteLine("P1: " + p1.ToString());
                Rhino.RhinoApp.WriteLine("P2: " + p2.ToString());
                Rhino.RhinoApp.WriteLine("Distance: " + ((p1 - p2).Length).ToString());
                Rhino.RhinoApp.WriteLine("Vector: " + (p1 - p2).ToString());
                controllerP.Clear();
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            if (mFrameCount >= 30)
            {
                this.Title = mTitleBase + " - " + mFrameCount / ((TimeSpan)(System.DateTime.Now - mLastTrackingTime)).TotalSeconds + "fps.";
                mLastTrackingTime = DateTime.Now;
                mFrameCount = 0;
            }
            mFrameCount += 1;
            mScene.gameTime += e.Time;
            mLastFrameTime = DateTime.Now;

        }

        protected void handleSignals()
        {
            SparrowHawkSignal s = SparrowHawkEventListeners.Instance.getOneSignal();
            if (s == null)
                return;
            switch (s.type)
            {
                case SparrowHawkSignal.ESparrowHawkSigalType.InitType:
                    if (s.data.Length >= 3)
                    {
                        //Vector3 robotPoint = new Vector3(s.data[0] - 8, s.data[1], s.data[2] - 240);
                        Vector3 robotPoint = new Vector3(s.data[0], s.data[1], s.data[2]);
                        //robotPoint /= 1000;
                        robotCallibrationPoints.Add(robotPoint);
                        Rhino.RhinoApp.WriteLine("add robotPoint: " + robotPoint.ToString());
                        if (mIsLefty && mScene.leftControllerIdx < 0 || !mIsLefty && mScene.leftControllerIdx < 0)
                            break;
                        //adding vrCallibrationPoints by pressing v
                        //calibrationTest();
                    }
                    break;
                case SparrowHawkSignal.ESparrowHawkSigalType.LineType:
                    if(s.data.Length >= 4)
                    {

                        if (s.data[0] == 0)
                        {
                            printStroke = new Geometry.GeometryStroke(ref mScene);
                            OpenTK.Vector3 p1 = new Vector3(s.data[1], s.data[2], s.data[3]);
                            p1 = Util.platformToVRPoint(ref mScene, p1);
                            OpenTK.Vector3 p2 = new Vector3(s.data[4], s.data[5], s.data[6]);
                            p2 = Util.platformToVRPoint(ref mScene, p2);

                            ((Geometry.GeometryStroke)printStroke).addPoint(p1);
                            ((Geometry.GeometryStroke)printStroke).addPoint(p2);
                            SceneNode stroke = new SceneNode("PrintStroke", ref printStroke, ref printStroke_m);
                            mScene.tableGeometry.add(ref stroke);

                        }
                        else
                        {
                            OpenTK.Vector3 p1 = new Vector3(s.data[1], s.data[2], s.data[3]);
                            p1 = Util.platformToVRPoint(ref mScene, p1);
                            ((Geometry.GeometryStroke)printStroke).addPoint(p1);
                        }
                    }
                    break;
                case SparrowHawkSignal.ESparrowHawkSigalType.CutType:
                    string guidStr = s.strData;
                    Guid delId = new Guid(guidStr);
                    Util.removeSceneNode(ref mScene, delId);
                    mScene.rhinoDoc.Views.Redraw();
                    break;

                case SparrowHawkSignal.ESparrowHawkSigalType.EncoderType:

                    //for rhino object
                    OpenTK.Matrix4 currentRotation = mScene.platformRotation;
                    float theta = (float)(s.data[0] / 360f * 2 * Math.PI);
                    mScene.rhinoTheta = theta;
                    Rhino.RhinoApp.WriteLine("Theta = " + theta);
                    Matrix4.CreateRotationZ(-theta, out mScene.platformRotation);
                    mScene.platformRotation.Transpose();                

                    //rotate Rhino objects
                    OpenTK.Matrix4 rotMRhino = mScene.platformRotation * currentRotation.Inverted();
                    mScene.transM = new Transform();
                    for (int row = 0; row < 4; row++)
                    {
                        for (int col = 0; col < 4; col++)
                        {
                            mScene.transM[row, col] = rotMRhino[row, col];
                        }
                    }

                    /*
                    Rhino.DocObjects.ObjectEnumeratorSettings settings = new Rhino.DocObjects.ObjectEnumeratorSettings();
                    settings.ObjectTypeFilter = Rhino.DocObjects.ObjectType.Brep;
                    foreach (Rhino.DocObjects.RhinoObject rhObj in mScene.rhinoDoc.Objects.GetObjectList(settings))
                    {
                        if (mScene.brepToSceneNodeDic.ContainsKey(rhObj.Id) && !rhObj.Attributes.Name.Contains("planeXY") && !rhObj.Attributes.Name.Contains("planeXZ")
                                                                                && !rhObj.Attributes.Name.Contains("planeYZ"))
                        {
                            //SceneNode sn = mScene.brepToSceneNodeDic[rhObj.Id];
                            //mScene.brepToSceneNodeDic.Remove(rhObj.Id);

                            Guid newGuid = mScene.rhinoDoc.Objects.Transform(rhObj.Id, mScene.transM, true);
                            Rhino.RhinoApp.WriteLine("transM " + mScene.transM.ToString());
                            mScene.rhinoDoc.Views.Redraw();

                            //mScene.brepToSceneNodeDic.Add(newGuid, sn);
                            //mScene.SceneNodeToBrepDic[sn.guid] = mScene.rhinoDoc.Objects.Find(newGuid);
                        }

                    }*/

                    //rotate the current interaction curve as well
                    /*
                    foreach(Curve iCurve in mScene.iCurveList)
                    {
                        iCurve.Transform(transM);
                        
                    }
                    if (mScene.peekInteraction().GetType() == typeof(Interaction.EditPoint2))
                    {
                        mScene.peekInteraction().init();
                    }*/

                    break;
            }
        }

        private void calibrationTest()
        {

            Vector3 vrPoint = Util.getTranslationVector3(Util.getControllerTipPosition(ref mScene, true));
            vrCallibrationPoints.Add(vrPoint);
            Rhino.RhinoApp.WriteLine("add vrCallibrationPoints: " + vrPoint.ToString());
            Util.MarkPoint(ref mScene.staticGeometry, vrPoint, 1, 1, 0);
            if (vrCallibrationPoints.Count == 8)
            {
                Util.solveForAffineTransformOpenCV(vrCallibrationPoints, robotCallibrationPoints, ref mScene.vrToRobot);
                foreach (Vector3 v in robotCallibrationPoints)
                {
                    Vector4 v4 = new Vector4(v.X, v.Y, v.Z, 1);
                    v4 = mScene.vrToRobot.Inverted() * v4;
                    Util.MarkPoint(ref mScene.staticGeometry, new Vector3(v4.X, v4.Y, v4.Z), 0, 1, 0);
                }
                Rhino.RhinoApp.WriteLine(mScene.vrToRobot.ToString());
                robotCallibrationPoints.Clear();
                vrCallibrationPoints.Clear();
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            if (mScene.leftControllerShouldVibrate())
                mHMD.TriggerHapticPulse((uint) mScene.leftControllerIdx, 0, (char) 127);
            if (mScene.rightControllerShouldVibrate())
                mHMD.TriggerHapticPulse((uint)mScene.rightControllerIdx, 0, (char) 127);
            MakeCurrent();
            updateMatrixPose();
            notifyRobotIfSafe();
            handleSignals();
            handleInteractions();
            updateControllerCallibration();
            mRenderer.renderFrame();
            SwapBuffers();
        }

        protected override void Dispose(bool manual)
        {
            base.Dispose(manual);
        }

        protected void setupScene()
        {
            mScene = new Scene(ref mDoc, ref mHMD);
            mScene.setWindowSize(this.Width, this.Height);
            mScene.mIsLefty = mIsLefty;


            if (mStrDriver.Contains("oculus")) mScene.isOculus = true; else mScene.isOculus = false;

            if (mHMD != null)
                mHMD.GetRecommendedRenderTargetSize(ref mRenderWidth, ref mRenderHeight);

            //TODO: testing passing by ref bug of OpenGL
            /*
            Geometry.Geometry g = new Geometry.Geometry("C:/workspace/Kestrel/resources/meshes/bunny.obj");
            //Material.Material m = new Material.SingleColorMaterial(mDoc,1f,1f,1f,1f);
            Material.Material m = new Material.LambertianMaterial(1,1,1,.5f);
            //Material.Material m = new Material.RGBNormalMaterial(1);
            //Material.Material m = new Material.SingleColorMaterial(1, 0, 1, 1);
            SceneNode cube = new SceneNode("Triangle", ref g, ref m);
            cube.transform = new Matrix4(1, 0, 0, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1);
            mScene.tableGeometry.add(ref cube);

            g = new Geometry.PointMarker(new Vector3(0, 1, 0));
            m = new Material.SingleColorMaterial(1, 1, 1, 1);
            SceneNode point = new SceneNode("Point 1", ref g, ref m);
            mScene.staticGeometry.add(ref point);
            point.transform = new Matrix4(1, 0, 0, 0, 0, 1, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1);
            */

            //visualizing axises        
            OpenTK.Vector3 x0 = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3(-240, 0, 0));
            OpenTK.Vector3 x1 = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3(240, 0, 0));
            OpenTK.Vector3 y0 = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3(0, -240, 0));
            OpenTK.Vector3 y1 = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3(0, 240, 0));
            OpenTK.Vector3 z0 = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3(0, 0, -240));
            OpenTK.Vector3 z1 = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3(0, 0, 240));
            Geometry.Geometry xAxis_g = new Geometry.GeometryStroke(ref mScene);
            Material.Material xAxis_m = new Material.SingleColorMaterial(1, 1, 1, 0);
            ((Geometry.GeometryStroke)xAxis_g).addPoint(x0);
            ((Geometry.GeometryStroke)xAxis_g).addPoint(x1);
            mScene.xAxis = new SceneNode("xAxis", ref xAxis_g, ref xAxis_m);
            mScene.staticGeometry.add(ref mScene.xAxis);

            Geometry.Geometry yAxis_g = new Geometry.GeometryStroke(ref mScene);
            Material.Material yAxis_m = new Material.SingleColorMaterial(1, 1, 1, 0);
            ((Geometry.GeometryStroke)yAxis_g).addPoint(y0);
            ((Geometry.GeometryStroke)yAxis_g).addPoint(y1);
            mScene.yAxis = new SceneNode("yAxis", ref yAxis_g, ref yAxis_m);
            mScene.staticGeometry.add(ref mScene.yAxis);


            Geometry.Geometry zAxis_g = new Geometry.GeometryStroke(ref mScene);
            Material.Material zAxis_m = new Material.SingleColorMaterial(1, 1, 1, 0);
            ((Geometry.GeometryStroke)zAxis_g).addPoint(z0);
            ((Geometry.GeometryStroke)zAxis_g).addPoint(z1);
            mScene.zAxis = new SceneNode("zAxis", ref zAxis_g, ref zAxis_m);
            mScene.staticGeometry.add(ref mScene.zAxis);


            // LeftController Point and Laser
            //g = new Geometry.Geometry("C:/workspace/Kestrel/resources/meshes/bunny.obj");
            //m = new Material.RGBNormalMaterial(1);
            Geometry.Geometry controllerL_g = new Geometry.PointMarker(new Vector3(0, 0, 0));
            Material.Material controllerL_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            SceneNode controllerL_p = new SceneNode("Left Cursor", ref controllerL_g, ref controllerL_m);
            mScene.rightControllerNode.add(ref controllerL_p);
            controllerL_p.transform = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);//mScene.mLeftControllerOffset;

            Geometry.Geometry controllerR_g = new Geometry.PointMarker(new Vector3(0, 0, 0));
            Material.Material controllerR_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            SceneNode controllerR_p = new SceneNode("Right Cursor", ref controllerR_g, ref controllerR_m);
            mScene.rightControllerNode.add(ref controllerR_p);
            controllerR_p.transform = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

            Geometry.Geometry controllerLRay_g = new Geometry.GeometryStroke(ref mScene);
            Material.Material controllerLRay_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            ((Geometry.GeometryStroke)controllerLRay_g).addPoint(new Vector3(0, 0, 0));
            ((Geometry.GeometryStroke)controllerLRay_g).addPoint(new Vector3(0, 0, -1));
            SceneNode rayTraceL = new SceneNode("PrintStroke", ref controllerLRay_g, ref controllerLRay_m);
            if (mIsLefty)
                mScene.leftControllerNode.add(ref rayTraceL);
            else
                mScene.rightControllerNode.add(ref rayTraceL);
            rayTraceL.transform = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);//mScene.mLeftControllerOffset;


            mScene.xzPlane = new DesignPlane3(ref mScene, 1);
            mScene.xyPlane = new DesignPlane3(ref mScene, 2);
            mScene.yzPlane = new DesignPlane3(ref mScene, 0);


            /*
            List<Plane> testPlaneList = new List<Plane>();
            List<Point3d> testPointList = new List<Point3d>();
            testPointList.Add(new Point3d(0, 0, 0));
            testPointList.Add(new Point3d(1, 0, 0));
            List<Curve> testCurveList = new List<Curve>();
            PolylineCurve testCurve = new PolylineCurve(testPointList);
            testCurveList.Add(testCurve);
            Plane plane1 = new Plane(new Point3d(0, 0, 0), new Rhino.Geometry.Vector3d(0, 0, 1));
            testPlaneList.Add(plane1);
            Transform testRot = Transform.Rotation((30f / 360f) * 2 * Math.PI, plane1.Normal, plane1.Origin);
            //bool done = testPlaneList[0].Transform(testRot);
            testCurve.Transform(testRot);
            testCurveList[0].Transform(testRot);
            Plane plane2 = testPlaneList[0];
            plane2.Transform(testRot);
            testPlaneList[0] = plane2;
            */

            //xzPlane = new DesignPlane(ref mScene, 1);
            //xyPlane = new DesignPlane(ref mScene, 2);
            //yzPlane = new DesignPlane(ref mScene, 0);

            //xzPlane2 = new DesignPlane2(ref mScene, "XZ");
            //xyPlane2 = new DesignPlane2(ref mScene, "XY");
            //yzPlane2 = new DesignPlane2(ref mScene, "YZ");

            //Find the Rhino Object start with 'a' and render it
            //Material.Material mesh_m = new Material.RGBNormalMaterial(1); ;
            //Rhino.DocObjects.ObjectEnumeratorSettings settings = new Rhino.DocObjects.ObjectEnumeratorSettings();
            //settings.ObjectTypeFilter = Rhino.DocObjects.ObjectType.Brep
            //int obj_count = 0;
            //foreach (Rhino.DocObjects.RhinoObject rhObj in mScene.rhinoDoc.Objects.GetObjectList(settings))
            //{
            //    if (rhObj.Attributes.Name.StartsWith("a"))
            //    {
            //        Util.addSceneNode(ref mScene, ((Surface)rhObj.Geometry).ToBrep(), ref mesh_m, rhObj.Attributes.Name);
            //        mScene.rhinoDoc.Views.Redraw();
            //    }
            //    obj_count++;
            //}
            //Rhino.RhinoApp.WriteLine(obj_count + " breps found");
        }

        public bool init()
        {

            // Set up HMD
            EVRInitError eError = EVRInitError.None;
            mHMD = OpenVR.Init(ref eError, EVRApplicationType.VRApplication_Scene);

            if (eError == EVRInitError.None)
            {
                Rhino.RhinoApp.WriteLine("Booted VR System");
                renderPoseArray = new TrackedDevicePose_t[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];
                gamePoseArray = new TrackedDevicePose_t[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];
            }
            else
            {
                Rhino.RhinoApp.WriteLine("Failed to boot");
                mTitleBase = "SparrowHawk (No VR Detected)";
            }

            // // THIS IS FOR UNCLIPPED
            // Width = 864;
            // Height = 820; 

            // THIS IS FOR CLIPPED RECTANGLE
            Width = 691;
            Height = 692;
            
            // Window Setup Info
            mStrDriver = Util.GetTrackedDeviceString(ref mHMD, OpenVR.k_unTrackedDeviceIndex_Hmd, ETrackedDeviceProperty.Prop_TrackingSystemName_String);
            mStrDisplay = Util.GetTrackedDeviceString(ref mHMD, OpenVR.k_unTrackedDeviceIndex_Hmd, ETrackedDeviceProperty.Prop_SerialNumber_String);
            mTitleBase = "SparrowHawk - " + mStrDriver + " " + mStrDisplay;
            Title = mTitleBase;
            MakeCurrent();
            setupScene();


            if (eError == EVRInitError.None)
                mRenderer = new VrRenderer(ref mHMD, ref mScene, mRenderWidth, mRenderHeight);
            else
                mRenderer = new VrRenderer(ref mHMD, ref mScene, mRenderWidth, mRenderHeight);

            //use other 8 points for calibrartion
            robotCallibrationPointsTest.Add(new Vector3(22, 15, -100) / 1000);
            robotCallibrationPointsTest.Add(new Vector3(-10, 40, -153) / 1000);
            robotCallibrationPointsTest.Add(new Vector3(25, -25, -181) / 1000);

            foreach (Vector3 v in robotCallibrationPointsTest)
            {
                Vector4 v4 = new Vector4(v.X, v.Y, v.Z, 1);
                v4 = mScene.vrToRobot.Inverted() * v4;
                Util.MarkPoint(ref mScene.staticGeometry, new Vector3(v4.X, v4.Y, v4.Z), 1, 1, 1);
            }
            robotCallibrationPointsTest.Clear();

            //set default matrix
            if (mRenderer.ovrvision_controller != null)
                mRenderer.ovrvision_controller.setDefaultMatrixHC();

            //detecting whether users in control or left
            Rhino.DocObjects.ObjectAttributes attr = new Rhino.DocObjects.ObjectAttributes();
            attr.Name = "user:out";
            Point3d userP = new Point3d(0, 0, 0);
            uGuid = mScene.rhinoDoc.Objects.AddPoint(userP, attr);

            //testing - rotate rhino object as well
            /*                   
            Transform transM = new Transform();
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    transM[row, col] = mScene.platformRotation[row, col];
                }
            }
            Rhino.DocObjects.ObjectEnumeratorSettings settings = new Rhino.DocObjects.ObjectEnumeratorSettings();
            settings.ObjectTypeFilter = Rhino.DocObjects.ObjectType.Brep;
            foreach (Rhino.DocObjects.RhinoObject rhObj in mScene.rhinoDoc.Objects.GetObjectList(settings))
            {
                mDoc.Objects.Transform(rhObj.Id, transM, true);
            }
            mScene.rhinoDoc.Views.Redraw();
            */

            //testing visualize printStroke
            printStroke = new Geometry.GeometryStroke(ref mScene);
            printStroke_m = new Material.SingleColorMaterial(1, 1, 0, 0.95f);

            return (eError == EVRInitError.None);
        }

        List<Point3d> curvePoints = new List<Point3d>();
        Rhino.Geometry.Brep brep;
        protected override void OnKeyPress(OpenTK.KeyPressEventArgs e)
        {
            if (e.KeyChar == 'C' || e.KeyChar == 'c')
            {
                ((VrRenderer) mRenderer).ovrvision_controller.getMatrixHeadtoCamera(0);
                mScene.popInteraction();
                mScene.pushInteraction(new Interaction.CalibrationAR(ref mScene, ref mRenderer.ovrvision_controller));
            }

            if (e.KeyChar == 'D' || e.KeyChar == 'd')
                mRenderer.ovrvision_controller.setDefaultMatrixHC();

            if (e.KeyChar == 'S' || e.KeyChar == 's')
            {
                // i.GetType() == typeof(Interaction.Closedcurve) if we want to combine closedcurve and sweep as an interaction
                mScene.pushInteraction(new Interaction.Sweep3(ref mScene));
                mScene.pushInteraction(new Interaction.CreateCurve(ref mScene, 1, false));
                mScene.pushInteraction(new Interaction.CreateCircle2(ref mScene));
                mScene.pushInteraction(new Interaction.AddPoint(ref mScene, 3, 2));
                mScene.pushInteraction(new Interaction.CreatePlane(ref mScene));
                mScene.peekInteraction().init();
            }

            if (e.KeyChar == 'R' || e.KeyChar == 'r')
            {
                mScene.popInteraction();
                //mScene.pushInteraction(new Interaction.Revolve2(ref mScene));
                mScene.pushInteraction(new Interaction.EditPoint2(ref mScene, true, "Revolve"));
                mScene.pushInteraction(new Interaction.CreateCurve(ref mScene, 1, false));
            }

            if (e.KeyChar == 'E' || e.KeyChar == 'e')
            {
                mScene.popInteraction();
                mScene.pushInteraction(new Interaction.Extrusion(ref mScene));
                mScene.pushInteraction(new Interaction.CreateCurve(ref mScene, 0, false));
                mScene.pushInteraction(new Interaction.CreateRect(ref mScene));
                mScene.pushInteraction(new Interaction.AddPoint(ref mScene, 3, 2));
                mScene.pushInteraction(new Interaction.CreatePlane(ref mScene));
                mScene.peekInteraction().init();
            }

            if (e.KeyChar == 'G' || e.KeyChar == 'g')
            {
                mScene.popInteraction();
                mScene.pushInteraction(new Interaction.Grip(ref mScene));
            }

            if (e.KeyChar == 'H' || e.KeyChar == 'h')
            {
                mScene.popInteraction();
                //mScene.pushInteraction(new Interaction.EditPlane(ref mScene, ref xyPlane, ref xzPlane, ref yzPlane));
                //mScene.pushInteraction(new Interaction.EditPlane2(ref mScene, ref xyPlane2, ref xzPlane2, ref yzPlane2));
            }

            if (e.KeyChar == 'J' || e.KeyChar == 'j')
            {
                //mScene.popInteraction();
                //mScene.pushInteraction(new Interaction.EditPlane(ref mScene, ref xyPlane, ref xzPlane, ref yzPlane));
                //mScene.pushInteraction(new Interaction.RotatePlane(ref mScene, ref xyPlane2, ref xzPlane2, ref yzPlane2));

                //clear the stroke
                foreach (SceneNode sn in mScene.tableGeometry.children)
                {
                    if (sn.name == "PrintStroke")
                    {
                        mScene.tableGeometry.children.Remove(sn);
                        break;
                    }
                }
            }

            if (e.KeyChar == 'K' || e.KeyChar == 'k')
            {
                mScene.popInteraction();
                mScene.pushInteraction(new Interaction.Revolve(ref mScene, true));
            }

            if (e.KeyChar == 'L' || e.KeyChar == 'l')
            {
                mScene.popInteraction();
                mScene.pushInteraction(new Interaction.Loft2(ref mScene));
                mScene.pushInteraction(new Interaction.EditPoint2(ref mScene, false));
                mScene.pushInteraction(new Interaction.CreateCurve(ref mScene, 0, false));
                mScene.pushInteraction(new Interaction.EditPoint2(ref mScene, true));
                mScene.pushInteraction(new Interaction.CreateCurve(ref mScene, 2, false));
            }

            if (e.KeyChar == 'B' || e.KeyChar == 'b')
            {
                mScene.popInteraction();
                mScene.pushInteraction(new Interaction.Delete(ref mScene));
            }

            if (e.KeyChar == 'M' || e.KeyChar == 'm')
            {
                mScene.popInteraction();
                mScene.pushInteraction(new Interaction.Stroke(ref mScene));
            }

            if (e.KeyChar == 'N' || e.KeyChar == 'n')
            {
                mScene.popInteraction();
                //mScene.pushInteraction(new Interaction.Sweep2(ref mScene));
                mScene.pushInteraction(new Interaction.Sweep2(ref mScene,true));
            }

            if (e.KeyChar == 'V' || e.KeyChar == 'v')
            {
                calibrationTest();
            }

            if (e.KeyChar == 'P' || e.KeyChar == 'p')
            {
                mScene.popInteraction();
                controllerP = new List<Vector3>();
                mScene.pushInteraction(new Interaction.PickPoint(ref mScene, ref controllerP));
            }

            if (e.KeyChar == 'A' || e.KeyChar == 'a')
            {
                //mScene.popInteraction();
                //mScene.pushInteraction(new Interaction.Align(ref mScene));

                //for rhino object
                OpenTK.Matrix4 currentRotation =  mScene.platformRotation;

                float theta = (float)(90.0f / 360f * 2 * Math.PI);
                Rhino.RhinoApp.WriteLine("Theta = " + theta);
                Matrix4.CreateRotationZ(theta, out mScene.platformRotation);
                mScene.platformRotation.Transpose();

                //rotate Rhino objects
                OpenTK.Matrix4 rotMRhino =  mScene.platformRotation * currentRotation.Inverted();
                Rhino.DocObjects.ObjectEnumeratorSettings settings = new Rhino.DocObjects.ObjectEnumeratorSettings();
                mScene.transM = new Transform();
                for (int row = 0; row < 4; row++)
                {
                    for (int col = 0; col < 4; col++)
                    {
                        mScene.transM[row, col] = rotMRhino[row, col];
                    }
                }
                settings.ObjectTypeFilter = Rhino.DocObjects.ObjectType.Brep;
                foreach (Rhino.DocObjects.RhinoObject rhObj in mScene.rhinoDoc.Objects.GetObjectList(settings))
                {
                    if (mScene.brepToSceneNodeDic.ContainsKey(rhObj.Id) && !rhObj.Attributes.Name.Contains("planeXY") && !rhObj.Attributes.Name.Contains("planeXZ")
                                                                            && !rhObj.Attributes.Name.Contains("planeYZ"))
                    {
                        //SceneNode sn = mScene.brepToSceneNodeDic[rhObj.Id];
                        //mScene.brepToSceneNodeDic.Remove(rhObj.Id);
                        Guid newGuid = mScene.rhinoDoc.Objects.Transform(rhObj.Id, mScene.transM, true);
                        Rhino.RhinoApp.WriteLine("transM " + mScene.transM.ToString());
                        mScene.rhinoDoc.Views.Redraw();

                        //mScene.brepToSceneNodeDic.Add(newGuid, sn);
                        //mScene.SceneNodeToBrepDic[sn.guid] = mScene.rhinoDoc.Objects.Find(newGuid);
                    }
                    
                }
            }

            if (e.KeyChar == 'W' || e.KeyChar == 'w')
            {
                mScene.popInteraction();
                //mScene.pushInteraction(new Interaction.CreatePlane2(ref mScene, "circle"));
                mScene.selectionList.Add("Sweep");
                mScene.selectionList.Add("Circle");
                mScene.selectionList.Add("Curve");

                mScene.pushInteraction(new Interaction.EditPoint3(ref mScene, true, "Sweep"));
                mScene.pushInteraction(new Interaction.CreateCurve(ref mScene, 3, false, "Sweep"));
                mScene.pushInteraction(new Interaction.CreatePlane2(ref mScene, "Circle"));

            }

            if (e.KeyChar == 'O' || e.KeyChar == 'o')
            {
                mScene.popInteraction();
                Util.clearAllModel(ref mScene);
            }

            if (e.KeyChar == 'Q' || e.KeyChar == 'q')
            {
                mScene.popInteraction();
                mScene.pushInteraction(new Interaction.CreateCircle(ref mScene, true));
            }

            if (e.KeyChar == 'Z' || e.KeyChar == 'z')
            {
                mScene.popInteraction();
                mScene.pushInteraction(new Interaction.Closedcurve(ref mScene, true));
            }

            if (e.KeyChar == 'X' || e.KeyChar == 'x')
            {
                mScene.popInteraction();
                mScene.pushInteraction(new Interaction.CreatePatch(ref mScene));
            }

            if (e.KeyChar == '[' || e.KeyChar == '{')
            {
                mScene.popInteraction();
                mScene.pushInteraction(new Interaction.PickPoint(ref mScene, ref mLeftControllerPoses));
            }

            if (e.KeyChar == ']' || e.KeyChar == '}')
            {
                mScene.popInteraction();
                mScene.pushInteraction(new Interaction.PickPoint(ref mScene, ref mRightControllerPoses));
            }

        }

        protected void notifyRobotIfSafe()
        {
            //Vector3 tableOrigin = Util.transformPoint(mScene.vrToRobot.Inverted(), new Vector3(0, 0, 0));

            Vector3 tableOrigin = Util.platformToVRPoint(ref mScene, new Vector3(0, 0, 0));

            Vector3 headOrigin = Util.transformPoint(mScene.mHMDPose.Inverted(), new Vector3(0, 0, 0));

            Vector3 displacement = tableOrigin - headOrigin;

            displacement.Y = 0;

            if (displacement.Length > 1.0f && isUserIn == true)
            {
                Rhino.DocObjects.RhinoObject rhobj = mScene.rhinoDoc.Objects.Find(uGuid);
                rhobj.Attributes.Name = "user:out";
                rhobj.CommitChanges();

                isUserIn = false;
                Rhino.RhinoApp.WriteLine("User out. " + displacement.Length);
            }
            else if (displacement.Length < 1.0f && isUserIn == false)
            {
                Rhino.DocObjects.RhinoObject rhobj = mScene.rhinoDoc.Objects.Find(uGuid);
                rhobj.Attributes.Name = "user:in";
                rhobj.CommitChanges();

                isUserIn = true;
                Rhino.RhinoApp.WriteLine("User in. " + displacement.Length);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (mScene != null)
            {
                mScene.setWindowSize(Width, Height);
            }
        }
    }
}