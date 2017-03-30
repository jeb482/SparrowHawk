using OpenTK;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    class SweepShape : Stroke
    {

        private Material.Material mesh_m;
        private Rhino.Geometry.NurbsCurve closedCurve;
        private Rhino.Geometry.Brep closedCurveBrep;
        List<Point3d> curvePoints = new List<Point3d>();
        Rhino.Geometry.Curve railCurve;
        Guid sGuid, eGuid;
        private string type = "none";

        public SweepShape(ref Scene s)
        {
            mScene = s;
            stroke_g = new Geometry.GeometryStroke();
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.RGBNormalMaterial(1);
            currentState = State.READY;

        }

        public SweepShape(ref Scene s, bool drawOnP, Curve curve, Guid startGuid, Guid endGuid)
        {
            railCurve = curve;
            sGuid = startGuid;
            eGuid = endGuid;

            mScene = s;
            stroke_g = new Geometry.GeometryStroke();
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.RGBNormalMaterial(1);
            currentState = State.READY;

            onPlane = drawOnP;

            if (onPlane)
            {
                Geometry.Geometry geo = new Geometry.PointMarker(new OpenTK.Vector3(0, 0, 0));
                Material.Material m = new Material.SingleColorMaterial(250 / 255, 128 / 255, 128 / 255, 1);
                drawPoint = new SceneNode("Point", ref geo, ref m);
                drawPoint.transform = new OpenTK.Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
                mScene.tableGeometry.add(ref drawPoint);

                //TODO-support both controllers
                primaryDeviceIndex = (uint)mScene.leftControllerIdx;
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
                if (rayIntersectionsS != null)
                {
                    projectP = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)rayIntersectionsS[0].X, (float)rayIntersectionsS[0].Y, (float)rayIntersectionsS[0].Z));
                    OpenTK.Matrix4 t = OpenTK.Matrix4.CreateTranslation(projectP);
                    t.Transpose();
                    drawPoint.transform = t;
                    targetPSN = mScene.brepToSceneNodeDic[rhObjS.Id];
                    targetPRhObj = rhObjS;
                    type = "start";
                }
                else if (rayIntersectionsE != null)
                {
                    projectP = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3((float)rayIntersectionsE[0].X, (float)rayIntersectionsE[0].Y, (float)rayIntersectionsE[0].Z));
                    OpenTK.Matrix4 t = OpenTK.Matrix4.CreateTranslation(projectP);
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

            if (currentState != State.PAINT)
            {
                return;
            }

            Vector3 pos = new Vector3();
            if (onPlane)
            {
                pos = projectP;
            }
            else
            {
                pos = Util.getTranslationVector3(Util.getControllerTipPosition(ref mScene, primaryDeviceIndex == mScene.leftControllerIdx));
            }

            ((Geometry.GeometryStroke)stroke_g).addPoint(pos);

            if (((Geometry.GeometryStroke)stroke_g).mNumPrimitives == 1)
            {
                SceneNode stroke = new SceneNode("Stroke", ref stroke_g, ref stroke_m);
                mScene.tableGeometry.add(ref stroke);
                strokeId = stroke.guid;
            }
        }


        public void renderSweep()
        {
            //reduce the points in the curve first
            simplifyCurve(ref ((Geometry.GeometryStroke)(stroke_g)).mPoints);

            foreach (OpenTK.Vector3 point in reducePoints)
            {
                // -y_rhino = z_gl, z_rhino = y_gl and unit conversion
                // OpenTK.Vector3 p = Util.transformPoint(Util.mGLToRhino, point*1000);              
                //curvePoints.Add(new Point3d(p.X, p.Y, p.Z));
                curvePoints.Add(Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, point)));
            }

            //Rhino CreateInterpolatedCurve and CreatePlanarBreps
            if (curvePoints.Count >= 3)
            {
                //Rhino closed curve through NURBS curve
                closedCurve = Rhino.Geometry.NurbsCurve.Create(true, 3, curvePoints.ToArray());
                //Rhino.Geometry.Curve nc = Curve.CreateInterpolatedCurve(curvePoints.ToArray(), 3);
                //nc.SetEndPoint(nc.PointAtStart);

                Plane proj_plane = new Plane();
                Plane.FitPlaneToPoints(curvePoints.ToArray(), out proj_plane);
                Curve proj_curve = Curve.ProjectToPlane(closedCurve, proj_plane);


                //TODO: make sure the proj_curve is on the same plane ? or it's beacuse not enough points
                Brep[] breps = null;
                if (type == "start")
                    breps = Brep.CreateFromSweep(railCurve, closedCurve, false, mScene.rhinoDoc.ModelAbsoluteTolerance);
                else if (type == "end")
                {
                    railCurve.Reverse();
                    breps = Brep.CreateFromSweep(railCurve, closedCurve, false, mScene.rhinoDoc.ModelAbsoluteTolerance);
                }
                Brep brep = breps[0];

                if (brep != null)
                {
                    Util.addSceneNode(ref mScene, brep, ref mesh_m, "aprint");
                   
                    //Util.removeSceneNode(ref mScene, sGuid);
                    //Util.removeSceneNode(ref mScene, eGuid);

                    mScene.rhinoDoc.Views.Redraw();

                    //mScene.popInteraction();
                    //mScene.pushInteraction(new Sweep2(ref mScene));
                }

            }

        }

        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {
            curvePoints = new List<Point3d>();
            base.onClickOculusGrip(ref vrEvent);


        }

        protected override void onReleaseOculusGrip(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("oculus grip release event test");
            if (currentState == State.PAINT)
            {
                //clear the stroke
                foreach (SceneNode sn in mScene.tableGeometry.children)
                {
                    if (sn.guid == strokeId)
                    {
                        mScene.tableGeometry.children.Remove(sn);
                        break;
                    }
                }

                renderSweep();
                currentState = State.READY;
            }
        }

    }
}