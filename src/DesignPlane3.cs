using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparrowHawk
{

    public class DesignPlane3
    {

        private Scene mScene;
        private string type;
        public Guid guid;
        private Material.Material mesh_m;
        private Brep designPlane;
        private int size = 240;

        public DesignPlane3(ref Scene scene)
        {

        }

        public DesignPlane3(ref Scene scene, int axis)
        {
            mScene = scene;
            mesh_m = new Material.SingleColorMaterial(0, 0, 0, 0.0f);

            switch (axis)
            {
                //Rhino space
                case 0: // Platform-space x-axis, yz-plane
                    type = "YZ";
                    ((Material.SingleColorMaterial)mesh_m).mColor.R = .5f;
                    createRhinoBrep(type);
                    break;
                case 1: // Platform-space y-axis, xz-plane 
                    type = "XZ";
                    ((Material.SingleColorMaterial)mesh_m).mColor.G = .5f;
                    createRhinoBrep(type);
                    break;
                case 2: // Platform-space z-axis, xy-plane
                    type = "XY";
                    ((Material.SingleColorMaterial)mesh_m).mColor.B = .5f;
                    createRhinoBrep(type);
                    break;
            }


        }

        public void setAlpha(float alpha)
        {
            mesh_m = new Material.SingleColorMaterial(((Material.SingleColorMaterial)mesh_m).mColor.R, ((Material.SingleColorMaterial)mesh_m).mColor.G, ((Material.SingleColorMaterial)mesh_m).mColor.B, alpha);
            SceneNode planeSN = mScene.brepToSceneNodeDic[guid];
            planeSN.material = mesh_m;
        }

        private void createRhinoBrep(string type)
        {
            Plane plane = new Plane();
            if (type == "YZ")
            {
                plane = new Plane(new Point3d(0, 0, 0), new Vector3d(1, 0, 0));
            }
            else if (type == "XZ")
            {
                plane = new Plane(new Point3d(0, 0, 0), new Vector3d(0, 1, 0));
            }
            else if (type == "XY")
            {
                plane = new Plane(new Point3d(0, 0, 0), new Vector3d(0, 0, 1));
            }

            PlaneSurface plane_surface = new PlaneSurface(plane, new Interval(-size, size), new Interval(-size, size));
            designPlane = Brep.CreateFromSurface(plane_surface);

            if (designPlane != null)
            {
                guid = Util.addStaticNode(ref mScene, designPlane, ref mesh_m, "plane" + type);
            }

        }
    }
}
