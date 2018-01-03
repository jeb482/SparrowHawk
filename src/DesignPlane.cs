using OpenTK;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static SparrowHawk.Scene;

namespace SparrowHawk
{

    public class DesignPlane
    {

        private Scene mScene;
        private XYZPlanes type;
        public Rhino.DocObjects.ObjRef planeObjRef;
        private SceneNode planeSN;
        private Material.Material mesh_m;
        private int size = 240;
        private Point3d origin = new Point3d(0, 0, 0);

        public DesignPlane(ref Scene scene)
        {

        }

        public DesignPlane(ref Scene scene, XYZPlanes axis, int s = 240)
        {
            mScene = scene;
            size = s;
            type = axis;
            mesh_m = new Material.SingleColorMaterial(0, 0, 0, 0.0f);

            switch (type)
            {
                //Rhino space
                case XYZPlanes.YZ: // Platform-space x-axis, yz-plane
                    ((Material.SingleColorMaterial)mesh_m).mColor.R = .5f;                   
                    break;
                case XYZPlanes.XZ: // Platform-space y-axis, xz-plane 
                    ((Material.SingleColorMaterial)mesh_m).mColor.G = .5f;                   
                    break;
                case XYZPlanes.XY: // Platform-space z-axis, xy-plane
                    ((Material.SingleColorMaterial)mesh_m).mColor.B = .5f;                 
                    break;
            }
            origin = new Point3d(0, 0, 0);
            createPlaneBrep();

        }


        public void setAlpha(float alpha)
        {
            planeSN = mScene.BiDictionaryRhinoVR.GetByFirst(planeObjRef.ObjectId);
            ((Material.SingleColorMaterial)planeSN.material).setAlpha(alpha);
        }

        public void resetOrgin()
        {
            Point3d newOrigin = planeObjRef.Object().Geometry.GetBoundingBox(true).Center;
            Matrix4 transMInvert = Matrix4.CreateTranslation(new Vector3(0f - (float)newOrigin.X, 0f - (float)newOrigin.Y, 0f - (float)newOrigin.Z));
            transMInvert.Transpose();
            Util.updateRhinoObjectSceneNode(ref mScene, ref planeObjRef, Util.OpenTKToRhinoTransform(transMInvert));
        }

        public void applyTrasform(Rhino.Geometry.Transform transM)
        {         
            Util.updateRhinoObjectSceneNode(ref mScene, ref planeObjRef, transM);
        }

        private void createPlaneBrep()
        {
            Plane plane = new Plane();
            if (type == XYZPlanes.YZ)
            {
                plane = new Plane(origin, new Rhino.Geometry.Vector3d(1, 0, 0));
            }
            else if (type == XYZPlanes.XZ)
            {
                plane = new Plane(origin, new Rhino.Geometry.Vector3d(0, 1, 0));
            }
            else if (type == XYZPlanes.XY)
            {
                plane = new Plane(origin, new Rhino.Geometry.Vector3d(0, 0, 1));
            }

            PlaneSurface plane_surface = new PlaneSurface(plane, new Interval(-size, size), new Interval(-size, size));
            Brep planeBrep = Brep.CreateFromSurface(plane_surface);

            if (planeBrep != null)
            {
                Guid guid = Util.addRhinoObjectSceneNode(ref mScene, ref planeBrep, ref mesh_m, "plane" + type.ToString(), out planeSN, true);
                planeObjRef = new Rhino.DocObjects.ObjRef(guid);
            }

        }
    }
}
