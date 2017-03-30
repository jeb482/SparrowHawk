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
        private Brep designPlane;
        //define in the platform(Rhino) coordinate 
        public OpenTK.Matrix4 planeToRhino;
        public OpenTK.Matrix4 planeToVr;


        public DesignPlane(ref Scene scene)
        {

        }

        public DesignPlane(ref Scene scene, int axis)
        {
            mScene = scene;
            planeToRhino = new OpenTK.Matrix4(); // maps the z axis in plane space to the right axis in rhino space
            mesh_m = new Material.SingleColorMaterial(0, 0, 0, 0.5f);
            
            switch (axis)
            {
                //Rhino space
                case 0: // Platform-space x-axis, yz-plane
                    type = "YZ";
                    ((Material.SingleColorMaterial)mesh_m).mColor.R = .5f;
                    planeToRhino = new OpenTK.Matrix4(0, 0, 1, 0,
                                                      1, 0, 0, 0,
                                                      0, 1, 0, 0,
                                                      0, 0, 0, 1);
                    break;
                case 1: // Platform-space y-axis, xz-plane 
                    type = "XZ";
                    ((Material.SingleColorMaterial)mesh_m).mColor.G = .5f;
                    planeToRhino = new OpenTK.Matrix4(0, 1, 0, 0,
                                                      0, 0, 1, 0,
                                                      1, 0, 0, 0,
                                                      0, 0, 0, 1);
                    break;
                case 2: // Platform-space z-axis, xy-plane
                    type = "XY";
                    ((Material.SingleColorMaterial)mesh_m).mColor.B = .5f;
                    planeToRhino = new OpenTK.Matrix4(1, 0, 0, 0,
                                                     0, 1, 0, 0,
                                                     0, 0, 1, 0,
                                                     0, 0, 0, 1);
                    break;
            }
              
            // 
            createRhinoBrep();
        }

        public void updateCoordinate(OpenTK.Matrix4 transform)
        {
            planeToVr = transform;            
        }

        public OpenTK.Vector3 getPlaneNormal()
        {
            return Util.transformVec(planeToVr, new OpenTK.Vector3(0, 0, 1)).Normalized();
        }

        public OpenTK.Vector3 getPlaneOrigin()
        {
            return Util.transformPoint(planeToVr, new OpenTK.Vector3(0, 0, 0));
        }

        private void createRhinoBrep()
        {
            Plane plane = new Plane(new Point3d(0,0,0), new Vector3d(0,0,1));
            Vector3d rhinoNormal = Util.openTkToRhinoVector(Util.transformVec(planeToRhino, new OpenTK.Vector3(0,0,1)));
            plane.Transform(Util.OpenTKToRhinoTransform(planeToRhino));

            //-150 150
            PlaneSurface plane_surface = new PlaneSurface(plane,
              new Interval(-40, 40),
              new Interval(-40, 40));
            
            designPlane = Brep.CreateFromSurface(plane_surface);
            
            if (designPlane != null)
            {
                guid = Util.addSceneNode(ref mScene, designPlane, ref mesh_m, "plane" + type, Util.OpenTKToRhinoTransform(planeToRhino));
            }

            SceneNode planeSN = mScene.brepToSceneNodeDic[guid];
            planeToVr = Util.platformToVR(ref mScene) * planeToRhino;
            planeSN.transform = planeToVr;
        }



    }
}
