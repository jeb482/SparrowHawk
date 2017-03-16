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
    public class Grip : Selection
    {

        OpenTK.Matrix4 mVRtocontroller;
        OpenTK.Matrix4 currentTransform;

        public Grip()
        {

        }

        public Grip(ref Scene s)
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
            //controller pose map from controller coordinate system to VR world system. In order to use pose matrix, we first transform the select object to controller coordinate system.
            selectedSN.transform = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * mVRtocontroller * currentTransform;

        }

        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            base.onClickOculusGrip(ref vrEvent);

            if (currentState == State.SELECTION)
            {
                currentTransform = selectedSN.transform;
                mVRtocontroller = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx).Inverted();
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

        //Don't use this ! Already find a way to apply transform fro brep in Rhino
        private void updateVetexData()
        {
            float[] vertex_array = selectedSN.geometry.mGeometry;
            int[] indices_array = selectedSN.geometry.mGeometryIndices;
            OpenTK.Vector4 v = new OpenTK.Vector4();
            OpenTK.Vector4 new_v = new OpenTK.Vector4();
            Mesh mesh = new Mesh();
            for (int i = 0; i < vertex_array.Length; i += 3)
            {
                v.X = vertex_array[i];
                v.Y = vertex_array[i + 1];
                v.Z = vertex_array[i + 2];
                v.W = 1.0f;

                new_v = selectedSN.transform * v;
                vertex_array[i] = new_v.X;
                vertex_array[i + 1] = new_v.Y;
                vertex_array[i + 2] = new_v.Z;

                //rhino
                // -y_rhino = z_gl, z_rhino = y_gl
                Point3d rhino_p = new Point3d(new_v.X, -new_v.Z, new_v.Y);
                mesh.Vertices.Add(new_v.X, -new_v.Z, new_v.Y); //0

            }

            for (int i = 0; i < indices_array.Length; i += 3)
            {
                mesh.Faces.AddFace(indices_array[i], indices_array[i + 1], indices_array[i + 2], indices_array[i + 2]);
            }

            mesh.Normals.ComputeNormals();
            mesh.Compact();

            Brep brep = Brep.CreateFromMesh(mesh, false);
            mScene.rhinoDoc.Objects.Replace(selectedRhObj.Id, brep);

        }

    }
}