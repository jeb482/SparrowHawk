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
    public class Selection : Interaction
    {
        protected State currentState;
        protected SceneNode selectedSN;
        protected RhinoObject selectedRhObj;
        protected uint primaryDeviceIndex;

        protected enum State
        {
            READY = 0, SELECTION = 1
        };

        public Selection(ref Scene scene) : base (ref scene)
        {
            currentState = State.READY;
        }

        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("Selcet event");
            primaryDeviceIndex = vrEvent.trackedDeviceIndex;
            if (currentState == State.READY)
            {

                OpenTK.Vector4 controller_p = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, 0, 1);
                OpenTK.Vector3 tipDir = Util.transformPoint(mScene.mLeftControllerOffset, new OpenTK.Vector3(0, 0, 0));
                OpenTK.Vector4 controller_pZ = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, -1, 1);
                //OpenTK.Vector3 controller_pZ = tipDir;
                Point3d controller_pRhino = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)));
                Point3d controller_pZRhin = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_pZ.X, controller_pZ.Y, controller_pZ.Z)));

                Vector3d direction = new Vector3d(controller_pZRhin.X - controller_pRhino.X, controller_pZRhin.Y - controller_pRhino.Y, controller_pZRhin.Z - controller_pRhino.Z);
                Ray3d ray = new Ray3d(controller_pRhino, direction);

                Rhino.DocObjects.ObjectEnumeratorSettings settings = new Rhino.DocObjects.ObjectEnumeratorSettings();
                settings.ObjectTypeFilter = Rhino.DocObjects.ObjectType.Brep;
                float mimD = 1000000f;
                foreach (Rhino.DocObjects.RhinoObject rhObj in mScene.rhinoDoc.Objects.GetObjectList(settings))
                {
                    if (rhObj.Attributes.Name.Contains("plane"))
                        continue;
                    else if (rhObj.Attributes.Name.Contains("brepMesh") || rhObj.Attributes.Name.Contains("aprint"))
                    {
                        //grip selection
                        if (rhObj.Geometry.GetBoundingBox(false).Contains(controller_pRhino))
                        {
                            //get the nearest one

                            OpenTK.Vector3 centerBox = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)rhObj.Geometry.GetBoundingBox(false).Center.X, (float)rhObj.Geometry.GetBoundingBox(false).Center.Y, (float)rhObj.Geometry.GetBoundingBox(false).Center.Z));
                            float distance = (float)Math.Sqrt(Math.Pow(centerBox.X - controller_p.X, 2) + Math.Pow(centerBox.Y - controller_p.Y, 2) + Math.Pow(centerBox.Z - controller_p.Z, 2));

                            Rhino.RhinoApp.WriteLine("grip distance: " + distance);

                            if (distance < mimD)
                            {
                                selectedSN = mScene.brepToSceneNodeDic[rhObj.Id];
                                selectedRhObj = rhObj;
                                mimD = distance;
                            }
                            currentState = State.SELECTION;
                        }
                        else //ray casting selection
                        {
                            List<GeometryBase> geometries = new List<GeometryBase>();
                            geometries.Add(rhObj.Geometry);
                            //must be a brep or surface, not mesh
                            Point3d[] rayIntersections = Rhino.Geometry.Intersect.Intersection.RayShoot(ray, geometries, 1);
                            if (rayIntersections != null)
                            {
                                //get the nearest one
                                OpenTK.Vector3 projectP = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)rayIntersections[0].X, (float)rayIntersections[0].Y, (float)rayIntersections[0].Z));

                                float distance = (float)Math.Sqrt(Math.Pow(projectP.X - controller_p.X, 2) + Math.Pow(projectP.Y - controller_p.Y, 2) + Math.Pow(projectP.Z - controller_p.Z, 2));

                                if (distance < mimD)
                                {
                                    mimD = distance;
                                    selectedSN = mScene.brepToSceneNodeDic[rhObj.Id];
                                    selectedRhObj = rhObj;
                                }
                                currentState = State.SELECTION;

                            }
                        }

                    }
                }

            }

        }

        protected override void onReleaseOculusGrip(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("Oculus grip release event");
            if (currentState == State.SELECTION)
            {
                currentState = State.READY;
            }
        }

    }
}