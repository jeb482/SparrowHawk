using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    class Cut : Interaction
    {

        private int numP;
        private Guid cutPGuid;


        public Cut(ref Scene scene) : base(ref scene)
        {
            mScene = scene;
            //sending cut postion
            Rhino.DocObjects.ObjectAttributes attr2 = new Rhino.DocObjects.ObjectAttributes();
            attr2.Name = "cut:";
            Point3d cutP = new Point3d(0, 0, 0);
            cutPGuid = mScene.rhinoDoc.Objects.AddPoint(cutP, attr2);

            //hiding the aprint model only show the printStroke
            
            Rhino.DocObjects.ObjectEnumeratorSettings settings = new Rhino.DocObjects.ObjectEnumeratorSettings();
            settings.ObjectTypeFilter = Rhino.DocObjects.ObjectType.Brep;
            foreach (Rhino.DocObjects.RhinoObject rhObj in mScene.rhinoDoc.Objects.GetObjectList(settings))
            {
                if (rhObj.Attributes.Name.Contains("aprint")){
                    SceneNode sn = mScene.brepToSceneNodeDic[rhObj.Id];
                    Material.LambertianMaterial hide_m = new Material.LambertianMaterial(((Material.LambertianMaterial)sn.material).mColor.R, ((Material.LambertianMaterial)sn.material).mColor.G, ((Material.LambertianMaterial)sn.material).mColor.B, 0);
                    sn.material = hide_m;
                }
            }

            //clear the stroke
            foreach (SceneNode sn in mScene.tableGeometry.children)
            {
                if (sn.name == "PrintStroke")
                {
                    mScene.tableGeometry.children.Remove(sn);
                    break;
                }
            }
        }

        protected override void onClickOculusTrigger(ref VREvent_t vrEvent)
        {
            OpenTK.Vector4 controller_p = Util.getLeftControllerTipPosition(ref mScene, true) * new OpenTK.Vector4(0, 0, 0, 1);
            Point3d controller_pRhino = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)));

            Rhino.DocObjects.RhinoObject rhobj = mScene.rhinoDoc.Objects.Find(cutPGuid);
            rhobj.Attributes.Name = "cut:" + controller_pRhino.X + ":" + controller_pRhino.Y + ":" + controller_pRhino.Z;
            rhobj.CommitChanges();
            Rhino.RhinoApp.WriteLine(rhobj.Attributes.Name);
        }
    }
}
