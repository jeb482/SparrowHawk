using OpenTK;
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
    class SweepShapeCircle : Interaction
    {

        enum State { PickOrigin, PickRadius, DrawCircle };
        State mState;
        OpenTK.Vector3 origin;
        OpenTK.Vector3 radius_point;
        float radius;

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

        Rhino.Geometry.NurbsCurve railCurve;
        Guid sGuid, eGuid;
        private string type = "none";

        public SweepShapeCircle(ref Scene s)
        {
            mScene = s;
            origin = new OpenTK.Vector3();
            mState = State.PickOrigin;
            mesh_m = new Material.RGBNormalMaterial(.5f);

        }

        public SweepShapeCircle(ref Scene s, bool drawOnP, Curve curve, Guid startGuid, Guid endGuid)
        {
            railCurve = curve.ToNurbsCurve();
            sGuid = startGuid;
            eGuid = endGuid;

            mScene = s;
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
            //visualize the point on the plane
            if (onPlane)
            {
                //ray casting to the pre-defind planes
                OpenTK.Vector4 controller_p = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, 0, 1);
                OpenTK.Vector4 controller_pZ = Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, -1, 1);
                Point3d controller_pRhino = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)));
                Point3d controller_pZRhin = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_pZ.X, controller_pZ.Y, controller_pZ.Z)));

                Rhino.Geometry.Vector3d direction = new Rhino.Geometry.Vector3d(controller_pZRhin.X - controller_pRhino.X, controller_pZRhin.Y - controller_pRhino.Y, controller_pZRhin.Z - controller_pRhino.Z);
                Ray3d ray = new Ray3d(controller_pRhino, direction);

                Rhino.DocObjects.RhinoObject rhObjS = mScene.rhinoDoc.Objects.Find(sGuid);
                Rhino.DocObjects.RhinoObject rhObjE = mScene.rhinoDoc.Objects.Find(eGuid);

                List<GeometryBase> geometriesS = new List<GeometryBase>();
                geometriesS.Add(rhObjS.Geometry);
                List<GeometryBase> geometriesE = new List<GeometryBase>();
                geometriesE.Add(rhObjE.Geometry);
                //must be a brep or surface, not mesh
                Point3d[] rayIntersectionsS = Rhino.Geometry.Intersect.Intersection.RayShoot(ray, geometriesS, 1);
                Point3d[] rayIntersectionsE = Rhino.Geometry.Intersect.Intersection.RayShoot(ray, geometriesE, 1);
                //TODO- fix when ray shoot both planes
                float mimD = 1000000f;
                int hitPlane = -1;
                if (rayIntersectionsS != null && rayIntersectionsE != null)
                {
                    //get the nearest one
                    OpenTK.Vector3 tmpS = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)rayIntersectionsS[0].X, (float)rayIntersectionsS[0].Y, (float)rayIntersectionsS[0].Z));
                    float distanceS = (float)Math.Sqrt(Math.Pow(tmpS.X - controller_p.X, 2) + Math.Pow(tmpS.Y - controller_p.Y, 2) + Math.Pow(tmpS.Z - controller_p.Z, 2));

                    OpenTK.Vector3 tmpE = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)rayIntersectionsE[0].X, (float)rayIntersectionsE[0].Y, (float)rayIntersectionsE[0].Z));
                    float distanceE = (float)Math.Sqrt(Math.Pow(tmpE.X - controller_p.X, 2) + Math.Pow(tmpE.Y - controller_p.Y, 2) + Math.Pow(tmpE.Z - controller_p.Z, 2));

                    if(distanceS < distanceE)
                    {
                        hitPlane = 0;
                    }
                    else
                    {
                        hitPlane = 1;
                    }

                }
                else if (rayIntersectionsS != null || hitPlane == 0)
                {
                    projectP = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)rayIntersectionsS[0].X, (float)rayIntersectionsS[0].Y, (float)rayIntersectionsS[0].Z));
                    
                    BoundingBox planeBB = ((Brep)rhObjS.Geometry).GetBoundingBox(false);
                    planeO = planeBB.Center;
                    OpenTK.Vector3 planeOVR = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)planeO.X, (float)planeO.Y, (float)planeO.Z));

                    //snap to origin
                    if (Math.Sqrt(Math.Pow(projectP.X - planeOVR.X, 2) + Math.Pow(projectP.Y - planeOVR.Y, 2) + Math.Pow(projectP.Z - planeOVR.Z, 2)) < 0.01)
                    {
                        projectP = planeOVR;
                    }

                    OpenTK.Matrix4 t = OpenTK.Matrix4.CreateTranslation(Util.transformPoint(mScene.tableGeometry.transform.Inverted(), projectP));
                    t.Transpose();
                    drawPoint.transform = t;
                    targetPSN = mScene.brepToSceneNodeDic[rhObjS.Id];
                    targetPRhObj = rhObjS;
                    type = "start";
                }
                else if (rayIntersectionsE != null ||  hitPlane == 1)
                {
                    projectP = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)rayIntersectionsE[0].X, (float)rayIntersectionsE[0].Y, (float)rayIntersectionsE[0].Z));

                    BoundingBox planeBB = ((Brep)rhObjE.Geometry).GetBoundingBox(false);
                    planeO = planeBB.Center;
                    OpenTK.Vector3 planeOVR = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)planeO.X, (float)planeO.Y, (float)planeO.Z));

                    //snap to origin
                    if (Math.Sqrt(Math.Pow(projectP.X - planeOVR.X, 2) + Math.Pow(projectP.Y - planeOVR.Y, 2) + Math.Pow(projectP.Z - planeOVR.Z, 2)) < 0.01)
                    {
                        projectP = planeOVR;
                    }

                    OpenTK.Matrix4 t = OpenTK.Matrix4.CreateTranslation(Util.transformPoint(mScene.tableGeometry.transform.Inverted(), projectP));
                    t.Transpose();
                    drawPoint.transform = t;
                    targetPSN = mScene.brepToSceneNodeDic[rhObjE.Id];
                    targetPRhObj = rhObjE;
                    type = "end";
                }
                else
                {
                    //make markerpoint invisible
                    OpenTK.Matrix4 t = OpenTK.Matrix4.CreateTranslation(new OpenTK.Vector3(100, 100, 100));
                    t.Transpose();
                    drawPoint.transform = t;
                    type = "none";
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
                Rhino.Geometry.Vector3d normal = new Rhino.Geometry.Vector3d();
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

            if (radius != 0)
            {
                Rhino.Geometry.Circle circle = new Rhino.Geometry.Circle(plane, radius);// *1000);
                circleCurve = circle.ToNurbsCurve();
                Brep[] shapes = Brep.CreatePlanarBreps(circleCurve);
                Brep circle_s = shapes[0];
                circleBrep = circle_s;

                Util.addSceneNode(ref mScene, circleBrep, ref mesh_m);
                mScene.rhinoDoc.Views.Redraw();
            }
            

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
                    primaryDeviceIndex = (int)trackedDeviceIndex;
                    break;
                case State.PickRadius:
                    if (primaryDeviceIndex != trackedDeviceIndex)
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
                    mState = State.DrawCircle;
                    break;
            }
        }



        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            //curvePoints = new List<Point3d>();
            advanceState(vrEvent.trackedDeviceIndex);
            //base.onClickOculusGrip(ref vrEvent);
        }

        protected override void onReleaseOculusGrip(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("oculus grip release event test");
            if (mState == State.DrawCircle)
            {

                List<Curve> curveL = new List<Curve>();
                curveL.Add(circleCurve);
                curveL.Add(railCurve);
                mScene.popInteraction();
                mScene.pushInteraction(new EditPoint(ref mScene, ref targetPRhObj, true, curveL, Guid.Empty, "Sweep2-" + type));


                mState = State.PickOrigin;
            }
        }

    }
}