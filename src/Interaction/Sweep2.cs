﻿using OpenTK;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    class Sweep2 : Stroke
    {
        public Geometry.Geometry meshStroke_g;
        Material.Material mesh_m;
        //Rhino.Geometry.NurbsCurve closedCurve;
        Rhino.Geometry.Curve closedCurve;
        List<Point3d> curvePoints = new List<Point3d>();
        Brep startPlane, endPlane;
        Guid sGuid, eGuid;

        public Sweep2(ref Scene s)
        {

            mScene = s;
            stroke_g = new Geometry.GeometryStroke();
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.RGBNormalMaterial(.5f);
            currentState = State.READY;

        }

        public Sweep2(ref Scene s, ref Rhino.Geometry.Brep brep)
        {
            mScene = s;
            stroke_g = new Geometry.GeometryStroke();
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.SingleColorMaterial(0, 1, 0, 1);
            closedCurve = brep.Curves3D.ElementAt(0);
            currentState = State.READY;
        }

        public override void draw(bool isTop)
        {
            base.draw(isTop);
        }


        public void renderSweep()
        {

            //reduce the points in the curve first
            simplifyCurve(ref ((Geometry.GeometryStroke)(stroke_g)).mPoints);

            foreach (OpenTK.Vector3 point in reducePoints)
            {
                // -y_rhino = z_gl, z_rhino = y_gl
                //OpenTK.Vector3 p = Util.transformPoint(Util.mGLToRhino, point);
                ///curvePoints.Add(new Point3d(p.X, p.Y, p.Z));
                curvePoints.Add(Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, point)));
            }

            //Rhino curve and extrude test
            if (curvePoints.Count >= 2)
            {
                Rhino.Geometry.Curve rail = Rhino.Geometry.Curve.CreateInterpolatedCurve(curvePoints.ToArray(), 3);

                //
                Plane planeStart = new Plane(rail.PointAtStart, rail.TangentAtStart);
                PlaneSurface planeStart_surface = new PlaneSurface(planeStart,
                  new Interval(-30, 30),
                  new Interval(-30, 30));

                Rhino.Geometry.Vector3d enormal = rail.TangentAtEnd;
                //enormal.Reverse();

                Plane planeEnd = new Plane(rail.PointAtEnd, rail.TangentAtEnd);
                PlaneSurface planeEnd_surface = new PlaneSurface(planeEnd,
                  new Interval(-30, 30),
                  new Interval(-30, 30));

                startPlane = Brep.CreateFromSurface(planeStart_surface);
                endPlane = Brep.CreateFromSurface(planeEnd_surface);

                if (startPlane != null && endPlane != null)
                {
                    sGuid = Util.addSceneNode(ref mScene, startPlane, ref mesh_m, "planeStart");
                    eGuid = Util.addSceneNode(ref mScene, endPlane, ref mesh_m, "planeEnd");
                }


                mScene.popInteraction();
                mScene.pushInteraction(new SweepShape(ref mScene, true, rail, sGuid, eGuid));

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