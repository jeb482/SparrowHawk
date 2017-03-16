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

        public Selection()
        {

        }

        public Selection(ref Scene s)
        {
            mScene = s;
            currentState = State.READY;
        }

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
                foreach (Rhino.DocObjects.RhinoObject rhObj in mScene.rhinoDoc.Objects.GetObjectList(settings))
                {
                    //if (rhObj.Attributes.Name == "plane")
                    //continue;
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