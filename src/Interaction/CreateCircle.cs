using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    class CreateCircle : Interaction
    {
        enum State { PickOrigin, PickRadius };
        State mState;
        OpenTK.Vector3 origin;
        OpenTK.Vector3 radius_point;
        float radius;
        uint mPrimaryDevice;

        private Material.Material mesh_m;
        private Rhino.Geometry.NurbsCurve circleCurve;
        private Rhino.Geometry.Brep circleBrep;

        public bool onPlane = false;
        protected int primaryDeviceIndex;
        protected SceneNode targetPSN;
        protected RhinoObject targetPRhObj;
        SceneNode drawPoint;
        OpenTK.Vector3 projectP;
        Point3d planeO;

        public CreateCircle(ref Scene scene) : base(ref scene)
        {
            origin = new OpenTK.Vector3();
            mState = State.PickOrigin;
            mesh_m = new Material.RGBNormalMaterial(.5f);
        }

        public CreateCircle(ref Scene scene, bool drawOnP) : base(ref scene)
        {
            origin = new OpenTK.Vector3();
            mState = State.PickOrigin;
            mesh_m = new Material.RGBNormalMaterial(.5f);

            onPlane = drawOnP;
            if (onPlane)
            {
                Geometry.Geometry geo = new Geometry.PointMarker(new OpenTK.Vector3(0, 0, 0));
                Material.Material m = new Material.SingleColorMaterial(250 / 255, 128 / 255, 128 / 255, 1);
                drawPoint = new SceneNode("Point", ref geo, ref m);
                drawPoint.transform = new OpenTK.Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
                mScene.tableGeometry.add(ref drawPoint);

                //TODO-support both controllers
                primaryDeviceIndex = mScene.leftControllerIdx;
            }


        }

        public override void draw(bool inTop)
        {
            if (onPlane)
            {
                //ray casting to the pre-defind planes
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

                    List<GeometryBase> geometries = new List<GeometryBase>();
                    geometries.Add(rhObj.Geometry);
                    //must be a brep or surface, not mesh
                    Point3d[] rayIntersections = Rhino.Geometry.Intersect.Intersection.RayShoot(ray, geometries, 1);
                    if (rayIntersections != null)
                    {
                        projectP = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)rayIntersections[0].X, (float)rayIntersections[0].Y, (float)rayIntersections[0].Z));
                        BoundingBox planeBB = ((Brep)rhObj.Geometry).GetBoundingBox(false);
                        planeO = planeBB.Center;
                        OpenTK.Vector3 planeOVR = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)planeO.X, (float)planeO.Y, (float)planeO.Z));

                        //snap to origin
                        if (Math.Sqrt(Math.Pow(projectP.X - planeOVR.X, 2) + Math.Pow(projectP.Y - planeOVR.Y, 2) + Math.Pow(projectP.Z - planeOVR.Z, 2)) < 0.03)
                        {
                            projectP = planeOVR;
                        }

                        OpenTK.Matrix4 t = OpenTK.Matrix4.CreateTranslation(projectP);
                        t.Transpose();
                        drawPoint.transform = t;
                        targetPSN = mScene.brepToSceneNodeDic[rhObj.Id];
                        targetPRhObj = rhObj;
                        break;
                    }

                }
            }
        }

        protected void buildCircle()
        {

            Rhino.Geometry.Point3d center_point = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, origin));
            Rhino.Geometry.Plane plane;
            if (onPlane)
            {
                //get the normal of the plane
                BrepFace face = ((Brep)targetPRhObj.Geometry).Faces[0];
                double u, v;
                Vector3d normal = new Vector3d();
                if (face.ClosestPoint(planeO, out u, out v))
                {
                    normal = face.NormalAt(u, v);
                    if (face.OrientationIsReversed)
                        normal.Reverse();
                }

                plane = new Rhino.Geometry.Plane(center_point, normal);
            }
            else
            {
                //xy plane as default plane
                plane = new Rhino.Geometry.Plane(center_point, new Rhino.Geometry.Vector3d(0, 0, 1));
            }

            Rhino.Geometry.Circle circle = new Rhino.Geometry.Circle(plane, radius);// *1000);
            circleCurve = circle.ToNurbsCurve();
            Brep[] shapes = Brep.CreatePlanarBreps(circleCurve);
            Brep circle_s = shapes[0];
            circleBrep = circle_s;

            Util.addSceneNode(ref mScene, circleBrep, ref mesh_m);
            mScene.rhinoDoc.Views.Redraw();

        }

        protected void advanceState(uint trackedDeviceIndex)
        {
            switch (mState)
            {
                case State.PickOrigin:
                    if (onPlane)
                    {
                        origin = projectP;
                    }
                    else
                    {
                        origin = Util.getTranslationVector3(Util.getControllerTipPosition(ref mScene, trackedDeviceIndex == mScene.leftControllerIdx));
                    }

                    mState = State.PickRadius;
                    mPrimaryDevice = trackedDeviceIndex;
                    break;
                case State.PickRadius:
                    if (mPrimaryDevice != trackedDeviceIndex)
                        return;

                    OpenTK.Vector3 r = new OpenTK.Vector3();
                    if (onPlane)
                    {
                        r = Util.vrToPlatformPoint(ref mScene, projectP);
                    }
                    else
                    {
                        r = Util.vrToPlatformPoint(ref mScene, Util.getTranslationVector3(Util.getControllerTipPosition(ref mScene, trackedDeviceIndex == mScene.leftControllerIdx)));
                    }

                    OpenTK.Vector3 o = Util.vrToPlatformPoint(ref mScene, origin);
                    radius = (float)Math.Sqrt(Math.Pow((r.X - o.X), 2) + Math.Pow((r.Y - o.Y), 2) + Math.Pow((r.Z - o.Z), 2));
                    buildCircle();
                    mState = State.PickOrigin;
                    break;
            }
        }

        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            advanceState(vrEvent.trackedDeviceIndex);
        }
    }
}
