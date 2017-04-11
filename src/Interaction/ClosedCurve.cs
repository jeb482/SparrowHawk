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
    class Closedcurve : Stroke
    {

        private Material.Material mesh_m;
        private Rhino.Geometry.NurbsCurve closedCurve;
        private Rhino.Geometry.NurbsCurve editCurve;
        private Rhino.Geometry.Brep closedCurveBrep;
        private Plane proj_plane;
        List<Point3d> curvePoints = new List<Point3d>();
        List<SceneNode> pointMarkers = new List<SceneNode>();
        private Guid surfaceID;

        public Closedcurve(ref Scene s)
        {
            mScene = s;
            stroke_g = new Geometry.GeometryStroke();
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.RGBNormalMaterial(.5f);
            currentState = State.READY;

        }

        public Closedcurve(ref Scene s, bool drawOnP)
        {
            mScene = s;
            stroke_g = new Geometry.GeometryStroke();
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.RGBNormalMaterial(.5f);
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
            base.draw(inTop);

        }


        public void renderPlanarShape()
        {
            //reduce the points in the curve first
            simplifyCurve(ref ((Geometry.GeometryStroke)(stroke_g)).mPoints);

            //intialize the rhino points of curve
            foreach (OpenTK.Vector3 point in reducePoints)
            {
                curvePoints.Add(Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, point)));
                //Util.MarkPoint(ref mScene.staticGeometry, new Vector3(point.X, point.Y, point.Z), 1, 1, 1);
            }

            //Rhino NurbsCurve.Create or CreateInterpolatedCurve and CreatePlanarBreps
            if (curvePoints.Count >= 8) // tricky way to deal with Loft error
            {
                //Rhino closed curve through NURBS curve
                closedCurve = Rhino.Geometry.NurbsCurve.Create(true, 3, curvePoints.ToArray());
                //Rhino.Geometry.Curve nc = Curve.CreateInterpolatedCurve(curvePoints.ToArray(), 3);
                //nc.SetEndPoint(nc.PointAtStart);

                proj_plane = new Plane();
                Plane.FitPlaneToPoints(curvePoints.ToArray(), out proj_plane);
                Curve proj_curve = Curve.ProjectToPlane(closedCurve, proj_plane);
                editCurve = proj_curve.ToNurbsCurve();

                //TODO: make sure the proj_curve is on the same plane ? or it's beacuse not enough points
                Brep[] shapes = Brep.CreatePlanarBreps(editCurve);
                Brep curve_s = shapes[0];
                closedCurveBrep = curve_s;

                surfaceID = Util.addSceneNode(ref mScene, curve_s, ref mesh_m);

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

                renderPlanarShape();
                currentState = State.READY;

                //testing edit point interaction
                if (onPlane && editCurve != null)
                {
                    List<NurbsCurve> curveL = new List<NurbsCurve>();
                    curveL.Add(editCurve);
                    mScene.popInteraction();
                    mScene.pushInteraction(new EditPoint(ref mScene, ref targetPRhObj, true, curveL, surfaceID, "ClosedCurve"));
                }
            }
        }

    }
}