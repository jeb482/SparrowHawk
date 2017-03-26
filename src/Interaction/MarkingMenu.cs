using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;

namespace SparrowHawk.Interaction
{
    

    class MarkingMenu : Interaction
    {
        protected int mCurrentSelection = -1;
        protected double mSelectOKTime = 0;
        double markingMenuSelectionDelay = 1;
        float mMinSelectionRadius;

        public enum MenuLayout {RootMenu, CalibrationMenu, TwoDMenu,
                                ThreeDMenu,NavMenu, PlaneMenu, PlanarMenu,
                                NonPlanarMenu };

        public int getNumSectors(MenuLayout layout)
        {
            switch (layout)
            {
                case MenuLayout.RootMenu: return 4;
                case MenuLayout.CalibrationMenu: return 3;
                case MenuLayout.TwoDMenu: return 2;
                case MenuLayout.ThreeDMenu: return 4;
                case MenuLayout.NavMenu: return 2;
                case MenuLayout.PlaneMenu: return 3;
                case MenuLayout.PlanarMenu: return 3;
                case MenuLayout.NonPlanarMenu: return 2;
            }
            return 0;
        }

        public string getTexturePath(MenuLayout layout)
        {
            switch (layout)
            {
                case MenuLayout.RootMenu: return @"C:\workspace\SparrowHawk\src\resources\menus\homemenu.png";
                case MenuLayout.CalibrationMenu: return @"C:\workspace\SparrowHawk\src\resources\menus\3template.png";
                case MenuLayout.TwoDMenu: return @"C:\workspace\SparrowHawk\src\resources\menus\2dgeo1.png";
                case MenuLayout.ThreeDMenu: return @"C:\workspace\SparrowHawk\src\resources\menus\3dgeo1.png";
                case MenuLayout.NavMenu: return @"C:\workspace\SparrowHawk\src\resources\menus\navmenu.png";
                case MenuLayout.PlaneMenu: return @"C:\workspace\SparrowHawk\src\resources\menus\homemenu.png";
                case MenuLayout.PlanarMenu: return @"C:\workspace\SparrowHawk\src\resources\menus\homemenu.png";
                case MenuLayout.NonPlanarMenu: return @"C:\workspace\SparrowHawk\src\resources\menus\homemenu.png";
            }
            return "";

        }

        public float getAngularMenuOffset(int numOptions)
        {
            if (numOptions <= 1) return 0;
            return (float)(-2 * Math.PI) / (2 * numOptions);
        }

        MenuLayout mLayout;
        SceneNode mSceneNode;
        Material.Material radialMenuMat;
        int mNumSectors;
        float mFirstSectorOffsetAngle;

        public MarkingMenu(ref Scene scene, MenuLayout layout = MenuLayout.RootMenu, double delay=0, OpenTK.Vector3 offset = new OpenTK.Vector3())
        {
            mLayout = layout;
            mNumSectors = getNumSectors(layout);
            mFirstSectorOffsetAngle = getAngularMenuOffset(mNumSectors);
            mScene = scene;
            mCurrentSelection = -1;
            if (scene.isOculus)
                mMinSelectionRadius = 0.2f;
            else
                mMinSelectionRadius = 0.5f;
            if (delay > 0)
            {
                mSelectOKTime = mScene.gameTime + delay;
            }
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

        protected override void onClickOculusTrigger(ref VREvent_t vrEvent)
        {
        }

        protected override void onReleaseOculusStick(ref VREvent_t vrEvent)
        {
            float r = 0;
            float theta = 0;
            getOculusJoystickPoint((uint)mScene.leftControllerIdx, out r, out theta);
            if (r > 0.2)
                launchInteraction(r, theta);
            else
            {
                mScene.popInteraction();
            }
        }

        // TODO: This could use a lot of refactoring.
        public override void draw(bool isTop) {
            float r = 0;
            float theta = 0;
            if (mScene.isOculus)
            {
                getOculusJoystickPoint((uint) mScene.leftControllerIdx, out r, out theta);
            } else {
                getViveTouchpadPoint((uint)mScene.leftControllerIdx, out r, out theta);
            }
            int sector = (int)Math.Floor((theta - mFirstSectorOffsetAngle) * mNumSectors / (2 * Math.PI));
            if (r > mMinSelectionRadius) {
                ((Material.RadialMenuMaterial)radialMenuMat).setHighlightedSector(mNumSectors, mFirstSectorOffsetAngle, theta);
                if (mCurrentSelection != sector) {
                    mCurrentSelection = sector;
                    mSelectOKTime = mScene.gameTime + markingMenuSelectionDelay;
                } else
                {
                    if (mScene.gameTime > mSelectOKTime)
                    {
                        launchInteraction(r, theta);
                    }
                }

            }
            else
            {
                mCurrentSelection = -1;
                ((Material.RadialMenuMaterial)radialMenuMat).removeHighlight();
            }
        }

        public override void activate()
        {
            Geometry.Geometry g = new Geometry.Geometry("C:\\workspace\\SparrowHawk\\src\\resources\\circle.obj");
            switch (mLayout)
            {

            }
            radialMenuMat = new Material.RadialMenuMaterial(mScene.rhinoDoc, getTexturePath(mLayout));
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

            int interactionNumber = ((int) Math.Floor((mNumSectors * theta - mFirstSectorOffsetAngle) / (2 * Math.PI)));
            if (interactionNumber < 0) interactionNumber += (int)mNumSectors;
            Rhino.RhinoApp.WriteLine("Selected Interaction " + interactionNumber);
            switch(mLayout)
            {
                case MenuLayout.RootMenu:
                    switch (interactionNumber)
                    {
                        case 0:
                            mScene.popInteraction();
                            mScene.pushInteraction(new MarkingMenu(ref mScene, MenuLayout.TwoDMenu, 1, new OpenTK.Vector3(0,0,.005f)));
                            break;
                        case 1:
                            mScene.popInteraction();
                            mScene.pushInteraction(new MarkingMenu(ref mScene, MenuLayout.NavMenu, 1, new OpenTK.Vector3(0, 0, .005f)));
                            break;
                        case 2:
                            mScene.popInteraction();
                            mScene.pushInteraction(new MarkingMenu(ref mScene, MenuLayout.ThreeDMenu, 1, new OpenTK.Vector3(0, 0, .005f)));
                            break;
                        case 3:
                            mScene.popInteraction();
                            mScene.pushInteraction(new MarkingMenu(ref mScene, MenuLayout.CalibrationMenu, 1, new OpenTK.Vector3(0, 0, .005f)));
                            break;
                    } break;

                case MenuLayout.CalibrationMenu:
                    switch (interactionNumber)
                    {

                    }
                    break;
                case MenuLayout.NavMenu:
                    switch (interactionNumber)
                    {
                        case 0:
                            mScene.popInteraction();
                            mScene.pushInteraction(new Delete(ref mScene));
                            break;
                        case 1:
                            mScene.popInteraction();
                            mScene.pushInteraction(new Grip(ref mScene));
                            break;
                    }
                    break;
                case MenuLayout.NonPlanarMenu:
                    switch (interactionNumber)
                    {

                    }
                    break;
                case MenuLayout.PlanarMenu:
                    switch (interactionNumber)
                    {

                    }
                    break;
                case MenuLayout.PlaneMenu:
                    switch (interactionNumber)
                    {

                    }
                    break;
                case MenuLayout.ThreeDMenu:
                    switch (interactionNumber)
                    {
                        case 0:
                            mScene.popInteraction();
                            mScene.pushInteraction(new Loft(ref mScene));
                            break;
                        case 1:
                            mScene.popInteraction();
                            mScene.pushInteraction(new CreateCylinder(ref mScene));
                            break;
                        case 2:
                            mScene.popInteraction();
                            mScene.pushInteraction(new Loft(ref mScene));
                            break;
                        case 3:
                            mScene.popInteraction();
                            mScene.pushInteraction(new Sweep(ref mScene));
                            break;
                    }
                    break;
                case MenuLayout.TwoDMenu:
                    switch (interactionNumber)
                    {
                        case 0:
                            mScene.popInteraction();
                            mScene.pushInteraction(new Closedcurve(ref mScene));
                            break;
                        case 1:
                            mScene.popInteraction();
                            mScene.pushInteraction(new Stroke(ref mScene));
                            break;
                        case 2:
                            mScene.popInteraction();
                            mScene.pushInteraction(new Closedcurve(ref mScene));
                            break;
                    }
                    break;
            }
            
            //switch (interactionNumber)
            //{
            //    case 0:
            //        mScene.pushInteraction(new PickPoint(ref mScene));
            //        break;
            //    case 1:
            //        mScene.pushInteraction(new Stroke(ref mScene));
            //        break;
            //    case 2:
            //        mScene.pushInteraction(new Closedcurve(ref mScene));
            //        break;
            //    case 3:
            //        mScene.pushInteraction(new Sweep(ref mScene));
            //        break;
            //    case 4:
            //        mScene.pushInteraction(new Loft(ref mScene));
            //        break;
            //    case 5:
            //        mScene.pushInteraction(new Selection(ref mScene));
            //        break;
            //    case 6:
            //        mScene.pushInteraction(new CreatePlaneA(ref mScene));
            //        break;
            //    case 7:
            //        mScene.pushInteraction(new Delete(ref mScene));
            //        break;
            //}
        }
    }
}
