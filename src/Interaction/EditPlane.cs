using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    public class EditPlane : Selection
    {

        OpenTK.Matrix4 mVRtocontroller;
        OpenTK.Matrix4 currentTransform;
        OpenTK.Vector3 planeOrigin = new OpenTK.Vector3(0, 0, 0);
        OpenTK.Vector3 planeNormalV = new OpenTK.Vector3(0, 1, 0);
        OpenTK.Vector3 VRNormalV = new OpenTK.Vector3(0, 1, 0);
        OpenTK.Matrix4 transToPlane = new OpenTK.Matrix4();
        OpenTK.Matrix4 rotM = new OpenTK.Matrix4();

        public EditPlane()
        {

        }

        public EditPlane(ref Scene s)
        {
            mScene = s;
            currentState = State.READY;
        }

        public override void draw(bool isTop)
        {
            if (currentState != State.SELECTION || !isTop)
            {
                return;
            }
            //selectedSN.transform = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * mVRtocontroller * currentTransform;

            //Todo: add contraints
            OpenTK.Matrix4 transM = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx);
            //ToDo-compute new transM in plane coordinate system
            //transM = transM * rotM * transToPlane;

            //get translation vector
            OpenTK.Vector3 translateV = new OpenTK.Vector3(0, transM.M24, 0);
            OpenTK.Matrix4 translationM = OpenTK.Matrix4.CreateTranslation(translateV);
            translationM.Transpose();
            //get rotation
            float thetaX = (float)Math.Atan2(transM.M32, transM.M33);
            float thetaY = (float)Math.Atan2(-transM.M31, Math.Sqrt(Math.Pow(transM.M32, 2) + Math.Pow(transM.M33, 2)));
            float thetaZ = (float)Math.Atan2(transM.M21, transM.M11);

            OpenTK.Matrix4 localRotM = OpenTK.Matrix4.CreateFromAxisAngle(new OpenTK.Vector3(0, 0, 1), thetaZ);
            localRotM.Transpose();

            //Check the formula - create a plane class with origin and axis data, keep traking the data.     M_ControllerPose * M_VR-Controller = M_L-VR * M_L
            //Calculate the M_L-VR by checking the origin position of the xy-plane (from platformToVR)
            //M_L * M_L-VR is the new M_L-VR that we need.
            //calculate the domonant axis and translation in M_L-VR  
            selectedSN.transform = translationM;

        }

        //only detect the pre-defined plane breps
        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("Selcet event");
            primaryDeviceIndex = vrEvent.trackedDeviceIndex;
            if (currentState == State.READY)
            {

                OpenTK.Vector4 controller_p = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, 0, 1);
                OpenTK.Vector4 controller_pZ = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, -1, 1);
                Point3d controller_pRhino = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)));
                Point3d controller_pZRhin = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_pZ.X, controller_pZ.Y, controller_pZ.Z)));

                Vector3d direction = new Vector3d(controller_pZRhin.X - controller_pRhino.X, controller_pZRhin.Y - controller_pRhino.Y, controller_pZRhin.Z - controller_pRhino.Z);
                Ray3d ray = new Ray3d(controller_pRhino, direction);

                Rhino.DocObjects.ObjectEnumeratorSettings settings = new Rhino.DocObjects.ObjectEnumeratorSettings();
                settings.ObjectTypeFilter = Rhino.DocObjects.ObjectType.Brep;
                //settings.NameFilter = "plane";
                foreach (Rhino.DocObjects.RhinoObject rhObj in mScene.rhinoDoc.Objects.GetObjectList(settings))
                {
                    //grip selection
                    //if (rhObj.Geometry.GetBoundingBox(false).Contains(controller_p))
                    if (rhObj.Geometry.GetBoundingBox(false).Contains(new Point3d(controller_p.X, -controller_p.Z, controller_p.Y)))
                    {
                        selectedSN = mScene.brepToSceneNodeDic[rhObj.Id];
                        selectedRhObj = rhObj;
                        currentState = State.SELECTION;
                        break;
                    }
                    else //ray casting selection
                    {
                        List<GeometryBase> geometries = new List<GeometryBase>();
                        geometries.Add(rhObj.Geometry);
                        //must be a brep or surface, not mesh
                        Point3d[] rayIntersections = Rhino.Geometry.Intersect.Intersection.RayShoot(ray, geometries, 1);
                        if (rayIntersections != null)
                        {
                            selectedSN = mScene.brepToSceneNodeDic[rhObj.Id];
                            selectedRhObj = rhObj;
                            currentState = State.SELECTION;
                            break;
                        }
                    }

                }

            }

            if (currentState == State.SELECTION)
            {
                currentTransform = selectedSN.transform;
                mVRtocontroller = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx).Inverted();
                //TODO: calculate the mVRtoPlane
                //align normal vector and origin
                planeOrigin = Util.transformPoint(currentTransform, new OpenTK.Vector3(0, 0, 0));
                planeNormalV = Util.transformVec(currentTransform, new OpenTK.Vector3(0, 1, 0));

                OpenTK.Matrix4.CreateTranslation(planeOrigin.X, planeOrigin.Y, planeOrigin.Z, out transToPlane);
                transToPlane.Transpose();

                //rotation
                OpenTK.Vector3 rotation_axis = OpenTK.Vector3.Cross(new OpenTK.Vector3(0, 1, 0), planeNormalV);
                rotation_axis.Normalize();
                float rotation_angles = OpenTK.Vector3.CalculateAngle(new OpenTK.Vector3(0, 1, 0), planeNormalV);
                OpenTK.Matrix4.CreateFromAxisAngle(rotation_axis, rotation_angles, out rotM);
                rotM.Transpose();

                //mVRtocontroller = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx);
                //mVRtocontroller = OpenTK.Matrix4.CreateTranslation(new OpenTK.Vector3(mVRtocontroller.M14, mVRtocontroller.M24, mVRtocontroller.M34)).Inverted();
                //mVRtocontroller.Transpose();


            }

        }

        protected override void onReleaseOculusGrip(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("Oculus grip release event");
            if (currentState == State.SELECTION)
            {

                //Watchout!! since the bug of transformation, we need to remove currentTransform first here
                //OpenTK.Matrix4 transMRhino = Util.mGLToRhino * (selectedSN.transform * currentTransform.Inverted()) * Util.mRhinoToGL;
                OpenTK.Matrix4 transMRhino = Util.platformToVR(ref mScene).Inverted() * (selectedSN.transform * currentTransform.Inverted()) * Util.platformToVR(ref mScene);
                Transform transM = new Transform();
                for (int row = 0; row < 4; row++)
                {
                    for (int col = 0; col < 4; col++)
                    {
                        transM[row, col] = transMRhino[row, col];
                    }
                }

                // BUG!!! apply the transform to the original one didn't work WHY??? need to delete first
                //((Brep)(gripRhObj.Geometry)).Transform(Transform.Translation(0,0,1));
                //Keep scene node reference before we delete the item
                SceneNode sn = mScene.brepToSceneNodeDic[selectedRhObj.Id];
                mScene.brepToSceneNodeDic.Remove(selectedRhObj.Id);

                Guid newGuid = mScene.rhinoDoc.Objects.Transform(selectedRhObj.Id, transM, true);
                mScene.rhinoDoc.Views.Redraw();

                //add reference SceneNode to brep and vice versa
                mScene.brepToSceneNodeDic.Add(newGuid, sn);
                mScene.SceneNodeToBrepDic[sn.guid] = mScene.rhinoDoc.Objects.Find(newGuid);

                currentState = State.READY;
                //gripSceneNode.transform = new OpenTK.Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
            }
        }


    }
}