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
        }

        protected override void onClickOculusTrigger(ref VREvent_t vrEvent)
        {
            OpenTK.Vector4 controller_p = Util.getLeftControllerTipPosition(ref mScene) * new OpenTK.Vector4(0, 0, 0, 1);
            Point3d controller_pRhino = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, new OpenTK.Vector3(controller_p.X, controller_p.Y, controller_p.Z)));

            Rhino.DocObjects.RhinoObject rhobj = mScene.rhinoDoc.Objects.Find(cutPGuid);
            rhobj.Attributes.Name = "cut:" + controller_pRhino.X + ":" + controller_pRhino.Y + ":" + controller_pRhino.Z;
            rhobj.CommitChanges();
            Rhino.RhinoApp.WriteLine(rhobj.Attributes.Name);
        }
    }
}
