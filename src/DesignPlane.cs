using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparrowHawk
{
    public class DesignPlane
    {
        private Scene mScene;
        private string type;
        public Guid guid;
        private Material.Material mesh_m;
        private Brep desingPlane;
        //define in the platform(Rhino) coordinate 
        public OpenTK.Vector3 origin;
        public OpenTK.Vector3 normal;
        private OpenTK.Vector3 xaxis;
        private OpenTK.Vector3 yaxis;
        public OpenTK.Matrix4 planeToVR;


        public DesignPlane()
        {

        }

        public DesignPlane(ref Scene scene, string t)
        {
            mScene = scene;
            type = t;
            // we use VR coordinate for convevnience
            if (type == "XZ")
            {
                origin = new OpenTK.Vector3(0, 0, 0);
                xaxis = new OpenTK.Vector3(1, 0, 0);
                yaxis = new OpenTK.Vector3(0, 0, 1);
                normal = new OpenTK.Vector3(0, 1, 0);
                mesh_m = new Material.SingleColorMaterial(0, 1, 0, 0.5f);

            }
            else if (type == "XY")
            {
                origin = new OpenTK.Vector3(0, 0, 0);
                xaxis = new OpenTK.Vector3(1, 0, 0);
                yaxis = new OpenTK.Vector3(0, 1, 0);
                normal = new OpenTK.Vector3(0, 0, 1);

                mesh_m = new Material.SingleColorMaterial(1, 0, 0, 0.5f);
            }
            else if (type == "YZ")
            {
                origin = new OpenTK.Vector3(0, 0, 0);
                xaxis = new OpenTK.Vector3(0, 1, 0);
                yaxis = new OpenTK.Vector3(0, 0, 1);
                normal = new OpenTK.Vector3(1, 0, 0);

                mesh_m = new Material.SingleColorMaterial(0, 0, 1, 0.5f);
            }

            createRhinoBrep();

            //compute initial planeToVR trasform, use VR coordinate system
            OpenTK.Vector3 planeO = Util.platformToVRPoint(ref mScene, new OpenTK.Vector3(0, 0, 0));

            //get translation vector
            OpenTK.Matrix4 translationM = OpenTK.Matrix4.CreateTranslation(planeO - origin);
            translationM.Transpose();

            planeToVR = translationM;

        }

        public void updateCoordinate(OpenTK.Matrix4 transform)
        {
            planeToVR =  planeToVR * transform;

            origin = Util.transformPoint(transform, origin);
            xaxis = Util.transformVec(transform, xaxis);
            xaxis.Normalize();
            yaxis = Util.transformVec(transform, yaxis);
            yaxis.Normalize();
            normal = Util.transformVec(transform, normal);
            normal.Normalize();

        }

        private void createRhinoBrep()
        {

            Rhino.Geometry.Vector3d rhinoNormal = Util.openTkToRhinoVector(Util.vrToPlatformVector(ref mScene, normal));
            //origin is the (0,0,0) in rhino
            Plane plane = new Plane(new Point3d(0,0,0), rhinoNormal);

            PlaneSurface plane_surface = new PlaneSurface(plane,
              new Interval(-150, 150),
              new Interval(-150, 150));

            desingPlane = Brep.CreateFromSurface(plane_surface);

            if (desingPlane != null)
            {
                guid = Util.addSceneNode(ref mScene, desingPlane, ref mesh_m, "plane" + type);
            }
        }

    }
}
