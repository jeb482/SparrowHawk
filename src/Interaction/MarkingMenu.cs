﻿using System;
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

        public MarkingMenu(ref Scene scene, MenuLayout layout = MenuLayout.RootMenu)
        {
            mLayout = layout;
            mNumSectors = getNumSectors(layout);
            mFirstSectorOffsetAngle = getAngularMenuOffset(mNumSectors);
            mScene = scene;
            mCurrentSelection = -1;
            if (scene.isOculus)
            {
                mMinSelectionRadius = 0.2f;
                mOuterSelectionRadius = 0.9f;
            }
            else { 
                mMinSelectionRadius = 0.4f;
                mOuterSelectionRadius = 0.75f;
            }
        }

        //protected override void onUntouchOculusStick(ref VREvent_t vrEvent)
        //{
        //    float r = 0;
        //    float theta = 0;
        //    int sector = (int)Math.Floor((theta - mFirstSectorOffsetAngle) * mNumSectors / (2 * Math.PI));
        //    getOculusJoystickPoint((uint)mScene.leftControllerIdx, out r, out theta);
        //    if(r > 0.5)
        //    {
        //        ((Material.RadialMenuMaterial)radialMenuMat).setHighlightedSector(mNumSectors, mFirstSectorOffsetAngle, theta);
        //        if (this.mInitialSelectOKTime != 0)
        //        {
        //            if (mScene.gameTime > this.mInitialSelectOKTime)
        //            {
        //                mCurrentSelection = sector;
        //                launchInteraction(r, theta);
        //            }
        //        }
        //        else
        //        {
        //            mCurrentSelection = sector;
        //            launchInteraction(r, theta);
        //        }
        //    }
        //    else
        //    {
        //        mScene.popInteraction();
        //    }
        //}

        // TODO: This could use a lot of refactoring.
        public override void draw(bool isTop) {
            // get R and Theta and the associated sector
            float r = 0;
            float theta = 0;
            if (mScene.isOculus)
            {
                getOculusJoystickPoint((uint) mScene.leftControllerIdx, out r, out theta);
            } else {
                getViveTouchpadPoint((uint)mScene.leftControllerIdx, out r, out theta);
            }
            int sector = (int)Math.Floor((((theta + 2*Math.PI) % (2*Math.PI)) - mFirstSectorOffsetAngle) * mNumSectors / (2 * Math.PI));

            // Update the shader
            if (r > mMinSelectionRadius)
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
            /*
            // If you're in the outer ring, select immediately
            if (r >= mOuterSelectionRadius)
            {
                launchInteraction(r, theta);
                return;
            }
            */
            // If in midlle selection ring, check delay
            if (r > mMinSelectionRadius)
            {
                Rhino.RhinoApp.WriteLine(r + ", " + theta);
                if (mCurrentSelection != sector)
                {
                    mCurrentSelection = sector;
                    mSelectOKTime = mScene.gameTime + markingMenuSelectionDelay;
                }
                else
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

            // Set initial timeout that cannot be skipped to prevent double selections.
            mInitialSelectOKTime = mScene.gameTime + defaultInitialDelay;
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
                            mScene.pushInteraction(new Loft(ref mScene));
                            break;
                        case 1:
                            mScene.popInteraction();
                            mScene.pushInteraction(new CreateCylinder(ref mScene));
                            break;
                        case 2:
                            mScene.popInteraction();
                            mScene.pushInteraction(new Revolve(ref mScene));
                            break;
                        case 3:
                            mScene.popInteraction();
                            mScene.pushInteraction(new Sweep2(ref mScene));
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
