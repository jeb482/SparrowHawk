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

        OpenTK.Matrix4 mInitialControllerPose;
        OpenTK.Matrix4 mInitialSceneNodeTransform;

        OpenTK.Vector3 planeOrigin = new OpenTK.Vector3(0, 0, 0);
        OpenTK.Vector3 planeNormalV = new OpenTK.Vector3(0, 1, 0);
        OpenTK.Vector3 VRNormalV = new OpenTK.Vector3(0, 1, 0);
        OpenTK.Matrix4 transToPlane = new OpenTK.Matrix4();
        OpenTK.Matrix4 rotM = new OpenTK.Matrix4();

        private DesignPlane planeXY, planeXZ, planeYZ, selectedPlane;
        private string selectedPlaneName;
        private List<RhinoObject> planeList = new List<RhinoObject>();
        OpenTK.Matrix4 M_L;
        OpenTK.Matrix4 mAlignO;

        public EditPlane(ref Scene s, ref DesignPlane xy, ref DesignPlane xz, ref DesignPlane yz) : base(ref s)
        {
            currentState = State.READY;
            planeXY = xy;
            planeXZ = xz;
            planeYZ = yz;

        }

        public override void draw(bool isTop)
        {
            if (currentState != State.SELECTION || !isTop)
            {
                return;
            }

            //OpenTK.Matrix4 currentControllerPose = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx).Inverted();
            //selectedSN.transform = currentControllerPose * mInitialControllerPose.Inverted() * mInitialSceneNodeTransform;

            OpenTK.Matrix4 T = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * mInitialControllerPose.Inverted();
            OpenTK.Vector3 planeNormal = selectedPlane.getPlaneNormal();
            OpenTK.Vector3 translation = new OpenTK.Vector3(T.M14, T.M24, T.M34);
            float mag = OpenTK.Vector3.Dot(translation, planeNormal);
            OpenTK.Matrix4 transM = OpenTK.Matrix4.CreateTranslation(new OpenTK.Vector3(0,0, mag));
            transM.Transpose();

            selectedSN.transform = selectedPlane.planeToVr * transM;

        }

        //only detect the pre-defined plane breps
        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("Selcet event");
            primaryDeviceIndex = vrEvent.trackedDeviceIndex;
            if (currentState == State.READY)
            {

                //since guid will change after each transformation
                planeList.Add(mScene.rhinoDoc.Objects.Find(planeXY.guid));
                planeList.Add(mScene.rhinoDoc.Objects.Find(planeXZ.guid));
                planeList.Add(mScene.rhinoDoc.Objects.Find(planeYZ.guid));

                OpenTK.Vector4 controller_p = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, 0, 1);
                OpenTK.Vector4 controller_pZ = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, -1, 1);
                Point3d controller_pRhino = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)));
                Point3d controller_pZRhin = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_pZ.X, controller_pZ.Y, controller_pZ.Z)));

                Vector3d direction = new Vector3d(controller_pZRhin.X - controller_pRhino.X, controller_pZRhin.Y - controller_pRhino.Y, controller_pZRhin.Z - controller_pRhino.Z);
                Ray3d ray = new Ray3d(controller_pRhino, direction);

                for (int i = 0; i < planeList.Count; i++)
                {
                    //grip selection
                    RhinoObject rhObj = planeList.ElementAt(i);
                    if (rhObj.Geometry.GetBoundingBox(false).Contains(controller_pRhino))
                    {
                        selectedSN = mScene.brepToSceneNodeDic[rhObj.Id];
                        selectedRhObj = rhObj;
                        currentState = State.SELECTION;

                        if (i == 0)
                        {
                            selectedPlane = planeXY;
                            selectedPlaneName = "XY";
                        }
                        else if (i == 1)
                        {
                            selectedPlane = planeXZ;
                            selectedPlaneName = "XZ";
                        }
                        else if (i == 2)
                        {
                            selectedPlane = planeYZ;
                            selectedPlaneName = "YZ";
                        }

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

                            if (i == 0)
                                selectedPlane = planeXY;
                            else if (i == 1)
                                selectedPlane = planeXZ;
                            else if (i == 2)
                                selectedPlane = planeYZ;

                            break;
                        }
                    }
                    
                }

            }

            if (currentState == State.SELECTION)
            {
                mInitialControllerPose = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx);
                mInitialSceneNodeTransform = selectedSN.transform;
            }

        }

        protected override void onReleaseOculusGrip(ref VREvent_t vrEvent)
        {
            //Watchout!! since the bug of transformation, we need to remove currentTransform first here
            //OpenTK.Matrix4 transMRhino = Util.mGLToRhino * (selectedSN.transform * currentTransform.Inverted()) * Util.mRhinoToGL;
            OpenTK.Matrix4 transMRhino = Util.platformToVR(ref mScene).Inverted() * (selectedSN.transform * mInitialControllerPose.Inverted()) * Util.platformToVR(ref mScene);
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

            //update the guid on selectPlane
            selectedPlane.guid = newGuid;
            selectedPlane.updateCoordinate(selectedSN.transform);
            planeList.Clear();

            currentState = State.READY;
        }


    }
}