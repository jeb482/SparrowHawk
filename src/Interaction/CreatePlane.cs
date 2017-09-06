using OpenTK;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    class CreatePlane : Interaction
    {
        private Material.Material mesh_m;
        private Brep designPlane;
        private Guid guid;
        private string renderType = "none";
        private SceneNode selectedSN;
        private NurbsCurve modelcurve;
        private Brep modelBrep;
        private Guid renderObjId = Guid.Empty;

        public CreatePlane(ref Scene scene) : base(ref scene)
        {
            mesh_m = new Material.SingleColorMaterial(0.5f, 0, 0, 0.4f);
        }

        public CreatePlane(ref Scene scene, string type) : base(ref scene)
        {
            mesh_m = new Material.SingleColorMaterial(0.5f, 0, 0, 0.4f);
            renderType = type;
        }

        public override void draw(bool isTop)
        {

        }

        public override void init()
        {

            //using controller's lazer direction
            Vector3 center = Util.getTranslationVector3(Util.getControllerTipPosition(ref mScene, primaryControllerIdx == mScene.leftControllerIdx));

            //offset the point a little bit to make the plane better
            OpenTK.Vector4 controller_p = Util.getControllerTipPosition(ref mScene, primaryControllerIdx == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, -0.05f, 1);
            OpenTK.Vector4 controller_pZ = Util.getControllerTipPosition(ref mScene, primaryControllerIdx == mScene.leftControllerIdx) * new OpenTK.Vector4(0, 0, -1, 1);
            Point3d controller_pRhino = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)));
            Point3d controller_pZRhin = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_pZ.X, controller_pZ.Y, controller_pZ.Z)));

            Rhino.Geometry.Vector3d normal = new Rhino.Geometry.Vector3d(controller_pZRhin.X - controller_pRhino.X, controller_pZRhin.Y - controller_pRhino.Y, controller_pZRhin.Z - controller_pRhino.Z);

            //project to xy plane in rhino
            normal = new Rhino.Geometry.Vector3d(normal.X, normal.Y, 0);
            Plane plane = new Plane(controller_pRhino, normal);


            //-150 150
            PlaneSurface plane_surface = new PlaneSurface(plane,
              new Interval(-120, 120),
              new Interval(-120, 120));

            designPlane = Brep.CreateFromSurface(plane_surface);

            if (designPlane != null)
            {
                guid = Util.addSceneNodeWithoutVR(ref mScene, designPlane, ref mesh_m, "panel");
                mScene.iRhObjList.Add(mScene.rhinoDoc.Objects.Find(guid));
                //TODO- bad solution. constriant the next interaction
                mScene.iPlaneList.Add(plane);

                if (renderType == "Circle")
                {

                    OpenTK.Vector3 origin = new Vector3(controller_p.X, controller_p.Y, controller_p.Z);
                    mScene.iPointList.Add(origin);
                    float radius = 20;
                    Rhino.Geometry.Circle circle = new Rhino.Geometry.Circle(plane, controller_pRhino, radius);
                    modelcurve = circle.ToNurbsCurve();


                    Point3d circleP = circle.ToNurbsCurve().PointAtStart;
                    mScene.iPointList.Add(Util.platformToVRPoint(ref mScene, new Vector3((float)circleP.X, (float)circleP.Y, (float)circleP.Z)));


                }
                else if (renderType == "Rect")
                {

                    float width = 40;
                    float height = 30;
                    //Rectangle3d rect = new Rectangle3d(plane, width, height);
                    Rectangle3d rect = new Rectangle3d(plane, new Interval(-width / 2, width / 2), new Interval(-height / 2, height / 2));
                    modelcurve = rect.ToNurbsCurve();

                    Point3d topLeftP = rect.Corner(3);
                    Point3d bottomRightP = rect.Corner(1);
                    Point3d rect_center = rect.Center;
                    //mScene.iPointList.Add(Util.platformToVRPoint(ref mScene, new Vector3((float)topLeftP.X, (float)topLeftP.Y, (float)topLeftP.Z)));
                    //changing to add center instead of topLeftP
                    mScene.iPointList.Add(Util.platformToVRPoint(ref mScene, new Vector3((float)rect_center.X, (float)rect_center.Y, (float)rect_center.Z)));
                    mScene.iPointList.Add(Util.platformToVRPoint(ref mScene, new Vector3((float)bottomRightP.X, (float)bottomRightP.Y, (float)bottomRightP.Z)));


                }

                Brep[] shapes = Brep.CreatePlanarBreps(modelcurve);
                modelBrep = shapes[0];
                renderObjId = Util.addSceneNode(ref mScene, modelBrep, ref mesh_m, renderType);
                //add icurveList since we don't use EditPoint2 for circle and rect
                mScene.iCurveList.Add(modelcurve);

                //now it will call the init() automatically
                mScene.popInteraction();
                mScene.peekInteraction().init();

            }


        }

        protected override void onClickOculusTrigger(ref VREvent_t vrEvent)
        {
            init();
        }

        protected override void onClickOculusGrip(ref VREvent_t vrEvent)
        {

        }

        protected override void onReleaseOculusTrigger(ref VREvent_t vrEvent)
        {
            mScene.popInteraction();
            mScene.peekInteraction().init();
        }
    }
}
