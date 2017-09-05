using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;
using static SparrowHawk.Scene;

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

        float thetaDebug = 0;

        public int getNumSectors(MenuLayout layout)
        {
            switch (layout)
            {
                case MenuLayout.MainMenu: return 4;
                case MenuLayout.ExtrudeC1: return 4;
                case MenuLayout.ExtrudeD1: return 4;
                case MenuLayout.ExtrudeC2: return 4;
                case MenuLayout.ExtrudeD2: return 4;
                case MenuLayout.LoftC1: return 4;
                case MenuLayout.LoftD1: return 4;
                case MenuLayout.LoftC2: return 4;
                case MenuLayout.LoftD2: return 4;
                case MenuLayout.RevolveC1: return 4;
                case MenuLayout.RevolveD1: return 4;
                case MenuLayout.SweepC1: return 4;
                case MenuLayout.SweepD1: return 4;
                case MenuLayout.SweepC2: return 4;
                case MenuLayout.SweepD2: return 4;
            }
            return 0;
        }

        public string getTexturePath(MenuLayout layout)
        {
            switch (layout)
            {
                case MenuLayout.MainMenu: return @"C:\workspace\SparrowHawk\src\resources\menus\new\Main.png";
                case MenuLayout.ExtrudeC1: return @"C:\workspace\SparrowHawk\src\resources\menus\new\Extrude1.png";
                case MenuLayout.ExtrudeD1: return @"C:\workspace\SparrowHawk\src\resources\menus\new\Extrude2.png";
                case MenuLayout.ExtrudeC2: return @"C:\workspace\SparrowHawk\src\resources\menus\new\Extrude1.png";
                case MenuLayout.ExtrudeD2: return @"C:\workspace\SparrowHawk\src\resources\menus\new\Extrude2.png";
                case MenuLayout.LoftC1: return @"C:\workspace\SparrowHawk\src\resources\menus\new\LoftA1.png";
                case MenuLayout.LoftD1: return @"C:\workspace\SparrowHawk\src\resources\menus\new\LoftA2.png";
                case MenuLayout.LoftC2: return @"C:\workspace\SparrowHawk\src\resources\menus\new\LoftB1.png";
                case MenuLayout.LoftD2: return @"C:\workspace\SparrowHawk\src\resources\menus\new\LoftB2.png";
                case MenuLayout.RevolveC1: return @"C:\workspace\SparrowHawk\src\resources\menus\new\Revolve1.png";
                case MenuLayout.RevolveD1: return @"C:\workspace\SparrowHawk\src\resources\menus\new\Revovle2.png";
                case MenuLayout.SweepC1: return @"C:\workspace\SparrowHawk\src\resources\menus\new\SweepA1.png";
                case MenuLayout.SweepD1: return @"C:\workspace\SparrowHawk\src\resources\menus\new\SweepA2.png";
                case MenuLayout.SweepC2: return @"C:\workspace\SparrowHawk\src\resources\menus\new\SweepB1.png";
                case MenuLayout.SweepD2: return @"C:\workspace\SparrowHawk\src\resources\menus\new\SweepB2.png";
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

        public MarkingMenu(ref Scene scene, MenuLayout layout = MenuLayout.MainMenu) : base(ref scene)
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
            else
            {
                mMinSelectionRadius = 0.4f;
                mOuterSelectionRadius = 0.6f;
            }
        }



        public override void draw(bool isTop)
        {
            // get R and Theta and the associated sector
            float theta = 0;
            float mLastRadius = mCurrentRadius;
            if (mScene.isOculus)
            {
                getOculusJoystickPoint((uint)primaryControllerIdx, out mCurrentRadius, out theta);
            }
            else
            {
                getViveTouchpadPoint((uint)primaryControllerIdx, out mCurrentRadius, out theta);
            }
            int sector = (int)Math.Floor((((theta + 2 * Math.PI) % (2 * Math.PI)) - mFirstSectorOffsetAngle) * mNumSectors / (2 * Math.PI));
            ;

            //Rhino.RhinoApp.WriteLine("theta = " + theta);
            thetaDebug = theta;

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
            if (mCurrentRadius >= mOuterSelectionRadius)
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
            mSceneNode.transform = new OpenTK.Matrix4(2, 0, 0, 0,
                                                          0, 0, -2, 0,
                                                          0, 2, 0, 0,
                                                          0, 0, 0, 1);
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

        private void terminate()
        {
            Rhino.RhinoApp.WriteLine("Quitting marking menu");
            mScene.popInteraction();
        }

        // TODO: Need to account for offset. Next
        private void launchInteraction(float r, float theta)
        {
            //int interactionNumber = ((int) Math.Floor((mNumSectors * theta - mFirstSectorOffsetAngle) / (2 * Math.PI)));
            mScene.vibrateController(0.1, (uint)primaryControllerIdx);
            int interactionNumber;
            if (theta < 0) { theta += (float)(2 * Math.PI); }
            interactionNumber = (int)Math.Ceiling((theta - (Math.PI / mNumSectors)) / (Math.PI / 2));
            if (interactionNumber >= mNumSectors) { interactionNumber = 0; }
            switch (mLayout)
            {
                case MenuLayout.MainMenu:
                    switch (interactionNumber)
                    {
                        //Loft
                        case 0:
                            //clear selectionList here to make sure Editpoint can access it

                            mScene.selectionList.Clear();
                            //deal with when user select wrong at the begining
                            while (!mScene.interactionStackEmpty())
                                mScene.popInteraction();

                            mScene.menuList.Add(MenuLayout.LoftC1); //index is 1, 0 is mainMenu
                            mScene.menuList.Add(MenuLayout.LoftD1);
                            mScene.menuList.Add(MenuLayout.LoftC2);
                            mScene.menuList.Add(MenuLayout.LoftD2);
                            mScene.selectionList.Add("Loft");
                            mScene.menuIndex++;

                            //Rhino.RhinoApp.WriteLine("section 0 : " + thetaDebug / Math.PI * 180);

                            break;
                        //Sweep
                        case 1:

                            mScene.selectionList.Clear();
                            while (!mScene.interactionStackEmpty())
                                mScene.popInteraction();

                            mScene.menuList.Add(MenuLayout.SweepC1);
                            mScene.menuList.Add(MenuLayout.SweepD1);
                            mScene.menuList.Add(MenuLayout.SweepC2);
                            mScene.menuList.Add(MenuLayout.SweepD2);
                            mScene.selectionList.Add("Sweep");
                            mScene.menuIndex++;

                            //Rhino.RhinoApp.WriteLine("section 1 : " + thetaDebug / Math.PI * 180);

                            break;
                        //Revolve
                        case 2:

                            mScene.selectionList.Clear();
                            while (!mScene.interactionStackEmpty())
                                mScene.popInteraction();

                            mScene.menuList.Add(MenuLayout.RevolveC1);
                            mScene.menuList.Add(MenuLayout.RevolveD1);
                            mScene.selectionList.Add("Revolve");
                            mScene.menuIndex++;

                            //Rhino.RhinoApp.WriteLine("section 2 : "+ thetaDebug / Math.PI * 180);
                            break;
                        //Extrude
                        case 3:

                            mScene.selectionList.Clear();
                            while (!mScene.interactionStackEmpty())
                                mScene.popInteraction();

                            mScene.menuList.Add(MenuLayout.ExtrudeC1);
                            mScene.menuList.Add(MenuLayout.ExtrudeD1);
                            mScene.menuList.Add(MenuLayout.ExtrudeC2);
                            mScene.menuList.Add(MenuLayout.ExtrudeD2);
                            mScene.selectionList.Add("Extrude");
                            mScene.menuIndex++;

                            //Rhino.RhinoApp.WriteLine("section 3: "+ thetaDebug / Math.PI * 180);
                            break;
                            /*
                        case 4:
                            Rhino.RhinoApp.WriteLine("section 4 : " + thetaDebug / Math.PI * 180);
                            break;*/

                    }
                    break;

                case MenuLayout.LoftC1:
                case MenuLayout.SweepC1:
                case MenuLayout.RevolveC1:
                case MenuLayout.ExtrudeC1:
                case MenuLayout.LoftC2:
                case MenuLayout.SweepC2:
                case MenuLayout.ExtrudeC2:

                    switch (interactionNumber)
                    {
                        //rect
                        case 0:
                            mScene.popInteraction();
                            mScene.menuIndex++;
                            mScene.selectionList.Add("Rect");
                            break;
                        //curve
                        case 1:
                            mScene.popInteraction();
                            mScene.menuIndex++;
                            mScene.selectionList.Add("Curve");
                            break;
                        //circle
                        case 2:
                            mScene.popInteraction();
                            mScene.menuIndex++;
                            mScene.selectionList.Add("Circle");
                            break;
                        //patch
                        case 3:
                            mScene.popInteraction();
                            mScene.menuIndex++;
                            mScene.selectionList.Add("Patch");
                            break;
                    }
                    break;

                case MenuLayout.LoftD1:
                case MenuLayout.SweepD1:
                case MenuLayout.ExtrudeD1:
                    switch (interactionNumber)
                    {
                        //surface
                        case 0:
                            mScene.popInteraction();
                            initInteractionChain(mScene.selectionList.Count - 1, "Surface");
                            mScene.menuIndex++;
                            break;
                        //3D
                        case 1:
                            mScene.popInteraction();
                            initInteractionChain(mScene.selectionList.Count - 1, "3D");
                            mScene.menuIndex++;
                            break;
                        //plane
                        case 2:
                            mScene.popInteraction();
                            initInteractionChain(mScene.selectionList.Count - 1, "Plane");
                            mScene.menuIndex++;
                            break;
                        //patch
                        case 3:
                            mScene.popInteraction();
                            initInteractionChain(mScene.selectionList.Count - 1, "Reference");
                            mScene.menuIndex++;
                            break;
                    }
                    break;
                case MenuLayout.RevolveD1:
                case MenuLayout.LoftD2:
                case MenuLayout.SweepD2:
                case MenuLayout.ExtrudeD2:
                    switch (interactionNumber)
                    {
                        //surface
                        case 0:
                            mScene.popInteraction();
                            renderModel(mScene.selectionList.Count - 1, "Surface");
                            mScene.menuIndex = 0;
                            mScene.menuList.Clear();
                            break;
                        //3D
                        case 1:
                            mScene.popInteraction();
                            renderModel(mScene.selectionList.Count - 1, "3D");
                            mScene.menuIndex = 0;
                            mScene.menuList.Clear();
                            break;
                        //plane
                        case 2:
                            mScene.popInteraction();
                            renderModel(mScene.selectionList.Count - 1, "Plane");
                            mScene.menuIndex = 0;
                            mScene.menuList.Clear();
                            break;
                        //patch
                        case 3:
                            mScene.popInteraction();
                            renderModel(mScene.selectionList.Count - 1, "Reference");
                            mScene.menuIndex = 0;
                            mScene.menuList.Clear();
                            break;
                    }
                    break;
            }
        }

        private void initInteractionChain(int index, string type)
        {
            Util.setPlaneAlpha(ref mScene, 0.0f);
            if (type == "Surface")
            {
                if (mScene.selectionList[index] == "Rect")
                {
                    mScene.pushInteraction(new EditPoint3(ref mScene, true, "Rect"));
                    mScene.pushInteraction(new AddPoint(ref mScene, 2, 2, "Rect"));
                }
                else if (mScene.selectionList[index] == "Circle")
                {
                    mScene.pushInteraction(new EditPoint3(ref mScene, true, "Circle"));
                    mScene.pushInteraction(new AddPoint(ref mScene, 2, 2, "Circle"));
                }
                else if (mScene.selectionList[index] == "Curve")
                {
                    mScene.pushInteraction(new EditPoint3(ref mScene, true));
                    mScene.pushInteraction(new CreateCurve(ref mScene, 2, false));
                }
                else if (mScene.selectionList[index] == "Patch")
                {
                    mScene.pushInteraction(new CreatePatch(ref mScene));
                }
            }
            else if (type == "3D")
            {
                if (mScene.selectionList[index] == "Rect")
                {
                    mScene.pushInteraction(new EditPoint3(ref mScene, true, "Rect"));
                    //mScene.pushInteraction(new AddPoint(ref mScene, 3, 2));
                    mScene.pushInteraction(new CreatePlane(ref mScene, "Rect"));
                }
                else if (mScene.selectionList[index] == "Circle")
                {
                    mScene.pushInteraction(new EditPoint3(ref mScene, true, "Circle"));
                    //mScene.pushInteraction(new AddPoint(ref mScene, 3, 2));
                    mScene.pushInteraction(new CreatePlane(ref mScene, "Circle"));
                }
                else if (mScene.selectionList[index] == "Curve")
                {
                    mScene.pushInteraction(new EditPoint3(ref mScene, false));
                    mScene.pushInteraction(new CreateCurve(ref mScene, 0, false));
                }
                else if (mScene.selectionList[index] == "Patch")
                {
                    mScene.pushInteraction(new CreatePatch(ref mScene));
                }
            }
            else if (type == "Plane")
            {
                Util.setPlaneAlpha(ref mScene, 0.4f);

                if (mScene.selectionList[index] == "Rect")
                {
                    mScene.pushInteraction(new EditPoint3(ref mScene, true, "Rect"));
                    mScene.pushInteraction(new AddPoint(ref mScene, 1, 2, "Rect"));
                }
                else if (mScene.selectionList[index] == "Circle")
                {
                    mScene.pushInteraction(new EditPoint3(ref mScene, true, "Circle"));
                    mScene.pushInteraction(new AddPoint(ref mScene, 1, 2, "Circle"));
                }
                else if (mScene.selectionList[index] == "Curve")
                {
                    mScene.pushInteraction(new EditPoint3(ref mScene, true));
                    mScene.pushInteraction(new CreateCurve(ref mScene, 1, false));
                }
                else if (mScene.selectionList[index] == "Patch")
                {
                    mScene.pushInteraction(new CreatePatch(ref mScene, true));
                }
            }
            else if (type == "Reference")
            {
                if (mScene.selectionList[index] == "Circle")
                {
                    mScene.pushInteraction(new CreatePlane2(ref mScene, "Circle"));
                }else if (mScene.selectionList[index] == "Rect")
                {
                    mScene.pushInteraction(new CreatePlane2(ref mScene, "Rect"));
                }
                
            }

        }

        private void renderModel(int index, string type)
        {
            string renderType = mScene.selectionList[0];
            Util.setPlaneAlpha(ref mScene, 0.0f);

            if (type == "Surface")
            {
                if (mScene.selectionList[index] == "Rect")
                {
                    mScene.pushInteraction(new EditPoint3(ref mScene, true, renderType));
                    mScene.pushInteraction(new AddPoint(ref mScene, 2, 2, "Rect"));
                }
                else if (mScene.selectionList[index] == "Circle")
                {
                    mScene.pushInteraction(new EditPoint3(ref mScene, true, renderType));
                    mScene.pushInteraction(new AddPoint(ref mScene, 2, 2, "Circle"));
                }
                else if (mScene.selectionList[index] == "Curve")
                {
                    mScene.pushInteraction(new EditPoint3(ref mScene, true, renderType));
                    mScene.pushInteraction(new CreateCurve(ref mScene, 2, false));
                }
                else if (mScene.selectionList[index] == "Patch")
                {
                    mScene.pushInteraction(new CreatePatch(ref mScene));
                }
            }
            else if (type == "3D")
            {
                if (mScene.selectionList[index] == "Rect")
                {
                    mScene.pushInteraction(new EditPoint3(ref mScene, true, renderType));
                    //mScene.pushInteraction(new AddPoint(ref mScene, 3, 2));
                    mScene.pushInteraction(new CreatePlane(ref mScene, "Rect"));
                }
                else if (mScene.selectionList[index] == "Circle")
                {
                    mScene.pushInteraction(new EditPoint3(ref mScene, true, renderType));
                    //mScene.pushInteraction(new AddPoint(ref mScene, 3, 2));
                    mScene.pushInteraction(new CreatePlane(ref mScene, "Circle"));
                }
                else if (mScene.selectionList[index] == "Curve")
                {
                    mScene.pushInteraction(new EditPoint3(ref mScene, false, renderType));
                    //mScene.pushInteraction(new CreateCurve(ref mScene, 0, false));
                    mScene.pushInteraction(new CreateCurve(ref mScene, 0, false, renderType));
                }
                else if (mScene.selectionList[index] == "Patch")
                {
                    mScene.pushInteraction(new EditPoint3(ref mScene, false, renderType));
                    mScene.pushInteraction(new CreatePatch(ref mScene));
                }
            }
            else if (type == "Plane")
            {

                Util.setPlaneAlpha(ref mScene, 0.4f);
                if (mScene.selectionList[index] == "Rect")
                {
                    mScene.pushInteraction(new EditPoint3(ref mScene, true, renderType));
                    mScene.pushInteraction(new AddPoint(ref mScene, 1, 2, "Rect")); ;
                }
                else if (mScene.selectionList[index] == "Circle")
                {
                    mScene.pushInteraction(new EditPoint3(ref mScene, true, renderType));
                    mScene.pushInteraction(new AddPoint(ref mScene, 1, 2, "Circle"));
                }
                else if (mScene.selectionList[index] == "Curve")
                {
                    mScene.pushInteraction(new EditPoint3(ref mScene, true, renderType));
                    mScene.pushInteraction(new CreateCurve(ref mScene, 1, false, renderType));
                    //mScene.pushInteraction(new CreateCurve(ref mScene, 1, false));
                }
                else if (mScene.selectionList[index] == "Patch")
                {
                    mScene.pushInteraction(new EditPoint3(ref mScene, false, renderType));
                    mScene.pushInteraction(new CreatePatch(ref mScene, true));
                }
            }
            else if (type == "Reference")
            {
                if (mScene.selectionList[index] == "Curve")
                {
                    mScene.pushInteraction(new EditPoint3(ref mScene, true, renderType));
                    mScene.pushInteraction(new CreateCurve(ref mScene, 3, false, renderType));
                }
            }
        }


    }
}
