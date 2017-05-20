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
        protected double mInitialSelectOKTime = 0;
        protected double mSelectOKTime = 0;
        double markingMenuFeedbackDelay = .2;
        double markingMenuSelectionDelay = .85f;
        double defaultInitialDelay = .2;
        float mMinSelectionRadius;
        float mOuterSelectionRadius;
        float mCurrentRadius;

        public enum MenuLayout {RootMenu, CalibrationMenu, TwoDMenu,
                                ThreeDMenu,NavMenu, PlaneMenu, PlanarMenu,
                                NonPlanarMenu };

        public int getNumSectors(MenuLayout layout)
        {
            switch (layout)
            {
                case MenuLayout.RootMenu: return 4;
                case MenuLayout.CalibrationMenu: return 4;
                case MenuLayout.TwoDMenu: return 4;
                case MenuLayout.ThreeDMenu: return 4;
                case MenuLayout.NavMenu: return 4;
                case MenuLayout.PlaneMenu: return 4;
                case MenuLayout.PlanarMenu: return 4;
                case MenuLayout.NonPlanarMenu: return 4;
            }
            return 0;
        }

        public string getTexturePath(MenuLayout layout)
        {
            switch (layout)
            {
                case MenuLayout.RootMenu: return @"C:\workspace\SparrowHawk\src\resources\menus\homemenu.png";
                case MenuLayout.CalibrationMenu: return @"C:\workspace\SparrowHawk\src\resources\menus\calmenu2.png";
                case MenuLayout.TwoDMenu: return @"C:\workspace\SparrowHawk\src\resources\menus\2dgeo2.png";
                case MenuLayout.ThreeDMenu: return @"C:\workspace\SparrowHawk\src\resources\menus\3dgeo1.png";
                case MenuLayout.NavMenu: return @"C:\workspace\SparrowHawk\src\resources\menus\navmenu_plane2.png";
                case MenuLayout.PlaneMenu: return @"C:\workspace\SparrowHawk\src\resources\menus\planesmenu2.png";
                case MenuLayout.PlanarMenu: return @"";
                case MenuLayout.NonPlanarMenu: return @"";
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

        public MarkingMenu(ref Scene scene, MenuLayout layout = MenuLayout.RootMenu) : base(ref scene)
        {

            mLayout = layout;
            mNumSectors = getNumSectors(layout);
            mFirstSectorOffsetAngle = getAngularMenuOffset(mNumSectors);
            mScene = scene;
            mCurrentSelection = -1;
            
            if (scene.isOculus)
            {
                mMinSelectionRadius = 0.2f;
                mOuterSelectionRadius = 0.8f;
            }
            else { 
                mMinSelectionRadius = 0.4f;
                mOuterSelectionRadius = 0.6f;
            }
        }



        public override void draw(bool isTop) {
            // get R and Theta and the associated sector
            float theta = 0;
            float mLastRadius = mCurrentRadius;
            if (mScene.isOculus)
            {
                getOculusJoystickPoint((uint) primaryControllerIdx, out mCurrentRadius, out theta);
            } else {
                getViveTouchpadPoint((uint)primaryControllerIdx, out mCurrentRadius, out theta);
            }
            int sector = (int)Math.Floor((((theta + 2*Math.PI) % (2*Math.PI)) - mFirstSectorOffsetAngle) * mNumSectors / (2 * Math.PI));
            ;

            Rhino.RhinoApp.WriteLine("r = " + mCurrentRadius);

            // Update the shader
            if (mCurrentRadius > mMinSelectionRadius)
                ((Material.RadialMenuMaterial)radialMenuMat).setHighlightedSector(mNumSectors, mFirstSectorOffsetAngle, theta);
            if (mSelectOKTime - mScene.gameTime < markingMenuFeedbackDelay)
                ((Material.RadialMenuMaterial)radialMenuMat).setIsSelected(1);
            else
                ((Material.RadialMenuMaterial)radialMenuMat).setIsSelected(0);
            
            // Enforce initial delay
            if (mScene.gameTime < this.mInitialSelectOKTime)
            {
                if (mCurrentSelection != sector)
                {
                    mCurrentSelection = sector;
                    mSelectOKTime = mScene.gameTime + markingMenuSelectionDelay;
                }
                return;
            }
            
            // If you're in the outer ring, select immediately
            if (mCurrentRadius >= mOuterSelectionRadius )
            {
                if (mLastRadius < mOuterSelectionRadius)
                {
                    mCurrentSelection = sector;
                    mSelectOKTime = mScene.gameTime + markingMenuFeedbackDelay;
                }
                else
                {
                    if (mScene.gameTime > mSelectOKTime)
                    {
                        launchInteraction(mCurrentRadius, theta);
                    }
                }
                return;
            }


            // If in midlle selection ring, check delay
            if (mCurrentRadius > mMinSelectionRadius)
            {
                //Rhino.RhinoApp.WriteLine(r + ", " + theta);
                if (mCurrentSelection != sector)
                {
                    mCurrentSelection = sector;
                    mSelectOKTime = mScene.gameTime + markingMenuSelectionDelay;
                }
                else
                {
                    if (mScene.gameTime > mSelectOKTime)
                    {
                        launchInteraction(mCurrentRadius, theta);
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
            mSceneNode.transform = new OpenTK.Matrix4(2, 0,  0, 0,
                                                          0, 0,  -2, 0,
                                                          0, 2,  0, 0,
                                                          0, 0,  0, 1);
            if (mScene.mIsLefty)
                mScene.leftControllerNode.add(ref mSceneNode);
            else
                mScene.rightControllerNode.add(ref mSceneNode);
            // Set initial timeout that cannot be skipped to prevent double selections.
            mInitialSelectOKTime = mScene.gameTime + defaultInitialDelay;
        }

        public override void deactivate()
        {
            if (mScene.mIsLefty)
                mScene.leftControllerNode.remove(ref mSceneNode);
            else
                mScene.rightControllerNode.remove(ref mSceneNode);

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
            //int interactionNumber = ((int) Math.Floor((mNumSectors * theta - mFirstSectorOffsetAngle) / (2 * Math.PI)));
            mScene.vibrateController(0.1, (uint) primaryControllerIdx);
            int interactionNumber;
            if (theta < 0) { theta += (float)(2 * Math.PI); }
            interactionNumber = (int) Math.Ceiling((theta - (Math.PI / mNumSectors)) / (Math.PI / 2));
            if (interactionNumber >= mNumSectors) { interactionNumber = 0; }
            switch(mLayout)
            { 
                case MenuLayout.RootMenu:
                    switch (interactionNumber)
                    {
                        case 0:
                            mScene.popInteraction();
                            mScene.pushInteraction(new MarkingMenu(ref mScene, MenuLayout.TwoDMenu));
                            break;
                        case 1:
                            mScene.popInteraction();
                            mScene.pushInteraction(new MarkingMenu(ref mScene, MenuLayout.NavMenu));
                            break;
                        case 2:
                            mScene.popInteraction();
                            mScene.pushInteraction(new MarkingMenu(ref mScene, MenuLayout.ThreeDMenu));
                            break;
                        case 3:
                            mScene.popInteraction();
                            mScene.pushInteraction(new MarkingMenu(ref mScene, MenuLayout.CalibrationMenu));
                            break;
                    } break;

                case MenuLayout.CalibrationMenu:
                    switch (interactionNumber)
                    {
                        case 0:
                            mScene.popInteraction();
                            break;
                        case 1:
                            mScene.popInteraction();
                            break;
                        case 2:
                            mScene.popInteraction();
                            break;
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
                            mScene.pushInteraction(new MarkingMenu(ref mScene, MenuLayout.PlaneMenu));
                            break;
                        case 2:
                            mScene.popInteraction();
                            mScene.pushInteraction(new Grip(ref mScene));
                            break;
                    }
                    break;
                case MenuLayout.NonPlanarMenu:
                    switch (interactionNumber)
                    {
                        case 0:
                            mScene.popInteraction();
                            break;
                        case 1:
                            mScene.popInteraction();
                            break;
                        case 2:
                            mScene.popInteraction();
                            break;
                    }
                    break;
                case MenuLayout.PlanarMenu:
                    switch (interactionNumber)
                    {
                        case 0:
                            mScene.popInteraction();
                            break;
                        case 1:
                            mScene.popInteraction();
                            break;
                        case 2:
                            mScene.popInteraction();
                            break;
                    }
                    break;
                case MenuLayout.PlaneMenu:
                    switch (interactionNumber)
                    {
                        case 0:
                            mScene.popInteraction();
                            break;
                        case 1:
                            mScene.popInteraction();
                            break;
                        case 2:
                            mScene.popInteraction();
                            break;
                    }
                    break;
                case MenuLayout.ThreeDMenu:
                    switch (interactionNumber)
                    {
                        case 0:
                            mScene.popInteraction();
                            mScene.pushInteraction(new CreatePatch(ref mScene));
                            break;
                        case 1:
                            mScene.popInteraction();
                            mScene.pushInteraction(new Sweep(ref mScene));
                            break;
                        case 2:
                            mScene.popInteraction();
                            mScene.pushInteraction(new Revolve(ref mScene, true));
                            break;
                        case 3:
                            mScene.popInteraction();
                            mScene.pushInteraction(new Sweep2(ref mScene, true));
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
                            mScene.pushInteraction(new CreateCircle(ref mScene));
                            break;
                    }
                    break;
            }
        }
    }
}
