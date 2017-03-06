﻿using OpenTK;
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
        private Rhino.Geometry.Brep closedCurveBrep;
        List<Point3d> curvePoints = new List<Point3d>();

        public Closedcurve(ref Scene s)
        {
            mScene = s;
            stroke_g = new Geometry.GeometryStroke();
            stroke_m = new Material.SingleColorMaterial(1, 0, 0, 1);
            mesh_m = new Material.RGBNormalMaterial(1);
            currentState = State.READY;

        }

        public override void draw(bool inTop)
        {
            base.draw(inTop);
        }


        public void renderPlanarShape()
        {
            //reduce the points in the curve first
            simplifyCurve(ref ((Geometry.GeometryStroke)(stroke_g)).mPoints);

            foreach (OpenTK.Vector3 point in reducePoints)
            {
                // -y_rhino = z_gl, z_rhino = y_gl
                OpenTK.Vector3 p = Util.transformPoint(Util.mGLToRhino, point);
                curvePoints.Add(new Point3d(p.X, p.Y, p.Z));
            }

            //Rhino CreateInterpolatedCurve and CreatePlanarBreps
            if (curvePoints.Count >= 8)
            {
                //Rhino closed curve through NURBS curve
                closedCurve = Rhino.Geometry.NurbsCurve.Create(true, 3, curvePoints.ToArray());
                //Rhino.Geometry.Curve nc = Curve.CreateInterpolatedCurve(curvePoints.ToArray(), 3);
                //nc.SetEndPoint(nc.PointAtStart);

                Plane proj_plane = new Plane();
                Plane.FitPlaneToPoints(curvePoints.ToArray(), out proj_plane);
                Curve proj_curve = Curve.ProjectToPlane(closedCurve, proj_plane);
                
                //TODO: make sure the proj_curve is on the same plane ? or it's beacuse not enough points
                Brep[] shapes = Brep.CreatePlanarBreps(proj_curve);
                Brep curve_s = shapes[0];
                closedCurveBrep = curve_s;

                Util.addSceneNode(ref mScene, curve_s, ref mesh_m);
                mScene.rhinoDoc.Views.Redraw();

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
                foreach (SceneNode sn in mScene.staticGeometry.children)
                {
                    if (sn.guid == strokeId)
                    {
                        mScene.staticGeometry.children.Remove(sn);
                        break;
                    }
                }

                renderPlanarShape();
                currentState = State.READY;
            }
        }

    }
}