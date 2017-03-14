using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    class Align : Selection
    {
        List<OpenTK.Vector3> vectors = new List<OpenTK.Vector3>();
        List<OpenTK.Vector3> snapPoints = new List<OpenTK.Vector3>();
        List<SceneNode> sceneNodes = new List<SceneNode>();
        List<RhinoObject> selectedRhObjs = new List<RhinoObject>();
        OpenTK.Matrix4 currentTransform;

        public Align()
        {

        }

        public Align(ref Scene s)
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

            //implement here
            if (vectors.Count == 2)
            {
                currentTransform = sceneNodes[0].transform;

                Rhino.RhinoApp.WriteLine("Snap event");
                //compute transforantion.
                //translate
                //OpenTK.Vector3 transV = new OpenTK.Vector3(snapPoints[1].X - snapPoints[0].X, snapPoints[1].Y - snapPoints[0].Y, snapPoints[1].Z - snapPoints[0].Z);
                //OpenTK.Matrix4 transMatrix = new OpenTK.Matrix4();
                //OpenTK.Matrix4.CreateTranslation(transV.X, transV.Y, transV.Z, out transMatrix);
                //transMatrix.Transpose();
              
                OpenTK.Matrix4 transToOrigin = new OpenTK.Matrix4();
                OpenTK.Matrix4.CreateTranslation(-snapPoints[0].X, -snapPoints[0].Y, -snapPoints[0].Z, out transToOrigin);
                transToOrigin.Transpose();

                OpenTK.Matrix4 transToTarget = new OpenTK.Matrix4();
                OpenTK.Matrix4.CreateTranslation(snapPoints[1].X, snapPoints[1].Y, snapPoints[1].Z, out transToTarget);
                transToTarget.Transpose();


                //rotation
                OpenTK.Vector3 rotation_axis = OpenTK.Vector3.Cross(vectors[0], vectors[1]);
                rotation_axis.Normalize();
                float rotation_angles = OpenTK.Vector3.CalculateAngle(vectors[0], vectors[1]);
                OpenTK.Matrix4 rotM = new OpenTK.Matrix4();
                OpenTK.Matrix4.CreateFromAxisAngle(rotation_axis, rotation_angles, out rotM);
                rotM.Transpose();

                //TODO: translate the point to the origin, rotate and translate to new point 
                //sceneNodes[0].transform = rotM * transMatrix * currentTransform;
                sceneNodes[0].transform = transToTarget * rotM * transToOrigin * currentTransform;

                //Watchout!! since the bug of transformation, we need to remove currentTransform first here
                OpenTK.Matrix4 transMRhino = Util.mGLToRhino * (sceneNodes[0].transform * currentTransform.Inverted()) * Util.mRhinoToGL;
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
                SceneNode sn = mScene.brepToSceneNodeDic[selectedRhObjs[0].Id];
                mScene.brepToSceneNodeDic.Remove(selectedRhObjs[0].Id);

                Guid newGuid = mScene.rhinoDoc.Objects.Transform(selectedRhObjs[0].Id, transM, true);
                mScene.rhinoDoc.Views.Redraw();

                //add reference SceneNode to brep and vice versa
                mScene.brepToSceneNodeDic.Add(newGuid, sn);
                mScene.SceneNodeToBrepDic[sn.guid] = mScene.rhinoDoc.Objects.Find(newGuid);



                snapPoints.Clear();
                vectors.Clear();
                sceneNodes.Clear();
            }

        }

        //only use ray casting and get the points
        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("Selcet event");
            primaryDeviceIndex = vrEvent.trackedDeviceIndex;
            if (currentState == State.READY)
            {
                //OpenTK.Vector4 controller_p = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, 0, 1);
                //OpenTK.Vector4 controller_pZ = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, -1000, 1);
                //OpenTK.Vector3 direction = new OpenTK.Vector3(controller_pZ.X - controller_p.X, controller_pZ.Y - controller_p.Y, controller_pZ.Z - controller_p.Z); // -y_rhino = z_gl, z_rhino = y_gl
                //Ray3d ray = new Ray3d(new Point3d(controller_p.X, -controller_p.Z, controller_p.Y), new Vector3d(direction.X, -direction.Z, direction.Y));

                //TOOD-use Utli function
                Point3d controller_p = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(0, 0, 0)));
                Point3d controller_pZ = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(0, 0, -1000)));
                Vector3d direction = new Vector3d(controller_pZ.X - controller_p.X, controller_pZ.Y - controller_p.Y, controller_pZ.Z - controller_p.Z);
                Ray3d ray = new Ray3d(controller_p, direction);

                Rhino.DocObjects.ObjectEnumeratorSettings settings = new Rhino.DocObjects.ObjectEnumeratorSettings();
                settings.ObjectTypeFilter = Rhino.DocObjects.ObjectType.Brep;
                foreach (Rhino.DocObjects.RhinoObject rhObj in mScene.rhinoDoc.Objects.GetObjectList(settings))
                {

                    List<GeometryBase> geometries = new List<GeometryBase>();
                    geometries.Add(rhObj.Geometry);
                    //must be a brep or surface, not mesh
                    Point3d[] rayIntersections = Rhino.Geometry.Intersect.Intersection.RayShoot(ray, geometries, 1);
                    if (rayIntersections != null)
                    {
                        //it should be only one point in the rayIntersections and one face in the Brep.
                        double u, v;
                        if (((Brep)rhObj.Geometry).Faces[0].ClosestPoint(rayIntersections[0], out u, out v))
                        {
                            Vector3d normal = ((Brep)rhObj.Geometry).Faces[0].NormalAt(u, v);
                            if (((Brep)rhObj.Geometry).Faces[0].OrientationIsReversed)
                                normal.Reverse();

                            //OpenTK.Vector3 normalV = Util.transformVec(Util.mRhinoToGL, new OpenTK.Vector3((float)normal.X, (float)normal.Y, (float)normal.Z));
                            OpenTK.Vector3 normalV = Util.platformToVRVec(ref mScene, new OpenTK.Vector3((float)normal.X, (float)normal.Y, (float)normal.Z));
                            normalV.Normalize();
                            vectors.Add(normalV);
                            OpenTK.Vector3 snapP = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)rayIntersections[0].X, (float)rayIntersections[0].Y, (float)rayIntersections[0].Z));

                            snapPoints.Add(snapP);
                            Rhino.RhinoApp.WriteLine(string.Format("Surface normal at uv({0:f},{1:f}) = ({2:f},{3:f},{4:f})", u, v, normal.X, normal.Y, normal.Z));



                        }

                        selectedSN = mScene.brepToSceneNodeDic[rhObj.Id];
                        selectedRhObj = rhObj;
                        currentState = State.SELECTION;

                        sceneNodes.Add(selectedSN);
                        selectedRhObjs.Add(selectedRhObj);
                        break;
                    }





                }

            }

        }



    }
}
