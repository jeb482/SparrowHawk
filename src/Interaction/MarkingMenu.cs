using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    class MarkingMenu : Interaction
    {
        SceneNode mSceneNode;
        Material.Material radialMenuMat;
        int mNumSectors = 8;
        float mFirstSectorOffsetAngle = 0f;

        public MarkingMenu(ref Scene scene)
        {
            mScene = scene;
        }

        protected override void onClickViveTrigger(ref VREvent_t vrEvent)
        {
            Rhino.RhinoApp.WriteLine("Pulled the Vive trigger");
        }

        protected override void onClickViveTouchpad(ref VREvent_t vrEvent)
        {
            if (vrEvent.trackedDeviceIndex != mScene.leftControllerIdx)
                return;
            float r, theta;
            getViveTouchpadPoint(vrEvent.trackedDeviceIndex, out r, out theta);
            launchInteraction(r, theta);
        }

        public override void draw(bool isTop) {
            float r = 0;
            float theta = 0;
            if (mScene.isOculus)
            {
                getOculusJoystickPoint((uint) mScene.leftControllerIdx, out r, out theta);
                if (r > 0.1)
                    launchInteraction(r, theta);
            } else
            {
                getViveTouchpadPoint((uint)mScene.leftControllerIdx, out r, out theta);
            }
            ((Material.RadialMenuMaterial) radialMenuMat).setHighlightedSector(mNumSectors, mFirstSectorOffsetAngle, theta);
            
        }

        public override void activate()
        {
            Geometry.Geometry g = new Geometry.Geometry("C:\\workspace\\SparrowHawk\\src\\resources\\circle.obj");
            //Material.Material m = new Material.RGBNormalMaterial(1);
            //Material.Material m = new Material.TextureMaterial(mScene.rhinoDoc, "C:\\workspace\\SparrowHawk\\src\\resources\\mmenu1.png");
            radialMenuMat = new Material.RadialMenuMaterial(mScene.rhinoDoc, "C:\\workspace\\SparrowHawk\\src\\resources\\mmenu1.png");
            mSceneNode = new SceneNode("MarkingMenu", ref g, ref radialMenuMat);
            mSceneNode.transform = new OpenTK.Matrix4(1, 0,  0, 0,
                                                          0, 0,  -1, 0,
                                                          0, 1,  0, 0,
                                                          0, 0,  0, 1);
            mScene.leftControllerNode.add(ref mSceneNode);
        }

        public override void deactivate()
        {
            mScene.leftControllerNode.remove(ref mSceneNode);
        }

        protected override void onClickViveAppMenu(ref VREvent_t vrEvent)
        {
            terminate();
        }

        protected override void onClickOculusStick(ref VREvent_t vrEvent)
        {
            terminate();
        }

        private void terminate() {
            Rhino.RhinoApp.WriteLine("Quitting marking menu");
            mScene.popInteraction();
        }

        // TODO: Need to account for offset. Next
        private void launchInteraction(float r, float theta)
        {
            int interactionNumber = ((int) Math.Floor(mNumSectors * theta / (2 * Math.PI)));
            if (interactionNumber < 0) interactionNumber += (int)mNumSectors;
            mScene.popInteraction();
            Rhino.RhinoApp.WriteLine("Selected Interaction " + interactionNumber);
            switch (interactionNumber)
            {
                case 0:
                    mScene.pushInteraction(new PickPoint(ref mScene));
                    break;
                case 1:
                    mScene.pushInteraction(new Stroke(ref mScene));
                    break;
                case 2:
                    mScene.pushInteraction(new Closedcurve(ref mScene));
                    break;
                case 3:
                    mScene.pushInteraction(new Sweep(ref mScene));
                    break;
                case 4:
                    mScene.pushInteraction(new Loft(ref mScene));
                    break;
                case 5:
                    mScene.pushInteraction(new Selection(ref mScene));
                    break;
                case 6:
                    mScene.pushInteraction(new CreatePlaneA(ref mScene));
                    break;
                case 7:
                    mScene.pushInteraction(new Delete(ref mScene));
                    break;
            }
        }
    }
}
