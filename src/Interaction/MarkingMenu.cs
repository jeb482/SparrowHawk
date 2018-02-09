using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.VR;
using static SparrowHawk.Scene;

namespace SparrowHawk.Interaction
{

    //update selectionDic, mInteractionChain
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
        private bool isVisiable = false;
        private bool isShowing = false;

        float thetaDebug = 0;

        private SelectionKey curSelectionKey = SelectionKey.Null;
        private Stack<Interaction> interactionChain = new Stack<Interaction>();


        public override void init()
        {
            // Set initial timeout that cannot be skipped to prevent double selections.
            mInitialSelectOKTime = mScene.gameTime + defaultInitialDelay;

            //TODO-fix, C2D2 situation, only remore one key now
            if (mScene != null && curSelectionKey != SelectionKey.Null)
            {
                if (curSelectionKey == SelectionKey.CurveOn) //Loft,Sweep,Extrude C2D2
                {
                    mScene.selectionDic.Remove(SelectionKey.Profile2Shape);
                    mScene.selectionDic.Remove(SelectionKey.Profile2On);
                }
                else if (curSelectionKey == SelectionKey.ShapeOnPlanes) //revolveC1D1
                {
                    mScene.selectionDic.Remove(SelectionKey.Profile1Shape);
                    mScene.selectionDic.Remove(SelectionKey.Profile1On);
                }
                else
                {
                    mScene.selectionDic.Remove(curSelectionKey);
                }

                if (curSelectionKey == SelectionKey.Profile1On || curSelectionKey == SelectionKey.Profile2On ||
                    curSelectionKey == SelectionKey.CurveOn || curSelectionKey == SelectionKey.ShapeOnPlanes)
                {
                    interactionChain.Clear();
                }
            }

            if (isVisiable)
                showMenu(true);
        }

        public void setCurSelectionKey(SelectionKey lastKey)
        {
            curSelectionKey = lastKey;
        }

        public int getNumSectors(MenuLayout2 layout)
        {
            //TODO- support different layout

            switch (layout)
            {
                case MenuLayout2.MainMenu: return 5;
                case MenuLayout2.SweepC1: return 2;
                case MenuLayout2.SweepD1: return 3;
                case MenuLayout2.SweepC2D2: return 2;
                case MenuLayout2.ExtrudeC1: return 2;
                case MenuLayout2.ExtrudeD1: return 3;
                case MenuLayout2.ExtrudeC2D2: return 2;
                case MenuLayout2.LoftC1: return 3;
                case MenuLayout2.LoftD1: return 3;
                case MenuLayout2.LoftC2D2: return 3;
                case MenuLayout2.LoftC2: return 2;
                case MenuLayout2.LoftD2: return 3;
                case MenuLayout2.RevolveC1D1: return 3;
                case MenuLayout2.RevolveAxis: return 3;
            }
            return 0;

        }

        public string getTexturePath(MenuLayout2 layout)
        {
            switch (layout)
            {
                //testing C:\\Users\\ericw\\Documents at home, C:\workspace at lab
                case MenuLayout2.MainMenu: return @"C:\workspace\SparrowHawk\src\resources\menus\new3\MainMenu.png";
                case MenuLayout2.SweepC1: return @"C:\workspace\SparrowHawk\src\resources\menus\new3\SweepC1.png";
                case MenuLayout2.SweepD1: return @"C:\workspace\SparrowHawk\src\resources\menus\new3\SweepD1.png";
                case MenuLayout2.SweepC2D2: return @"C:\workspace\SparrowHawk\src\resources\menus\new3\SweepC2D2.png";
                case MenuLayout2.ExtrudeC1: return @"C:\workspace\SparrowHawk\src\resources\menus\new3\ExtrudeC1.png";
                case MenuLayout2.ExtrudeD1: return @"C:\workspace\SparrowHawk\src\resources\menus\new3\ExtrudeD1.png";
                case MenuLayout2.ExtrudeC2D2: return @"C:\workspace\SparrowHawk\src\resources\menus\new3\ExtrudeC2D2.png";
                case MenuLayout2.LoftC1: return @"C:\workspace\SparrowHawk\src\resources\menus\new3\LoftC1.png";
                case MenuLayout2.LoftD1: return @"C:\workspace\SparrowHawk\src\resources\menus\new3\LoftD1.png";
                case MenuLayout2.LoftC2D2: return @"C:\workspace\SparrowHawk\src\resources\menus\new3\LoftC2D2.png";
                case MenuLayout2.LoftC2: return @"C:\workspace\SparrowHawk\src\resources\menus\new3\LoftC2.png";
                case MenuLayout2.LoftD2: return @"C:\workspace\SparrowHawk\src\resources\menus\new3\LoftD2.png";
                case MenuLayout2.RevolveC1D1: return @"C:\workspace\SparrowHawk\src\resources\menus\new3\RevolveC1D1.png";
                case MenuLayout2.RevolveAxis: return @"C:\workspace\SparrowHawk\src\resources\menus\new3\RevolveAxis.png";

            }
            return "";

        }

        public float getAngularMenuOffset(int numOptions)
        {
            if (numOptions <= 1) return 0;

            if (numOptions == 2)
            {
                return -(float)(Math.PI / 2);
            }
            else
            {
                //aligin with 12oclock direction looks better 
                return -(float)((Math.PI / 2) - (Math.PI / numOptions));
            }

        }

        MenuLayout2 mLayout;
        SceneNode mSceneNode;
        Material.Material radialMenuMat;
        int mNumSectors;
        float mFirstSectorOffsetAngle;

        public MarkingMenu(ref Scene scene, MenuLayout2 layout = MenuLayout2.MainMenu) : base(ref scene)
        {
            mScene = scene;
            mLayout = layout;
            mNumSectors = getNumSectors(layout);
            mFirstSectorOffsetAngle = getAngularMenuOffset(mNumSectors);
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

            UtilOld.showLaser(ref mScene, false);
        }

        public override void draw(bool isTop)
        {
            if (!isShowing)
                return;

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

            //int sector = (int)Math.Floor((((theta + 2 * Math.PI) % (2 * Math.PI)) - mFirstSectorOffsetAngle) * mNumSectors / (2 * Math.PI));

            float thetaOffset;
            if (theta < 0)
            {
                thetaOffset = (theta + (float)(2 * Math.PI)) - mFirstSectorOffsetAngle; //mFirstSectorOffsetAngle <0 so the result might greater than 360
            }
            else
            {
                thetaOffset = (theta) - mFirstSectorOffsetAngle;
            }

            if (thetaOffset < 0)
            {
                thetaOffset = thetaOffset + (float)(2 * Math.PI);
            }
            else if (thetaOffset > (float)(2 * Math.PI))
            {
                thetaOffset = thetaOffset - (float)(2 * Math.PI);
            }

            float sectorAngle = (float)(2 * Math.PI) / (float)mNumSectors;
            int sector = (int)Math.Floor(thetaOffset / sectorAngle);
            thetaDebug = theta;
            //Rhino.RhinoApp.WriteLine("theta= "+ thetaOffset * 180.0/ Math.PI + ", offset= " + mFirstSectorOffsetAngle * 180.0 / Math.PI + ", sector = " + sector);

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
            /*
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
            }*/


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

        private void showMenu(bool vis)
        {
            if (mSceneNode != null)
            {
                if (vis)
                {
                    isShowing = true;
                    if (mScene.mIsLefty)
                        mScene.leftControllerNode.add(ref mSceneNode);
                    else
                        mScene.rightControllerNode.add(ref mSceneNode);
                }
                else
                {
                    isShowing = false;
                    if (mScene.mIsLefty)
                        mScene.leftControllerNode.remove(ref mSceneNode);
                    else
                        mScene.rightControllerNode.remove(ref mSceneNode);
                }
            }
        }

        public void setVisible(bool visible)
        {
            this.isVisiable = visible;
        }

        //TODO- seperate activate and visible
        public override void activate()
        {

        }
        public override void deactivate()
        {
            if (isShowing)
            {
                showMenu(false);
            }
        }

        public override void leaveTop()
        {
            if (isShowing)
            {
                showMenu(false);
            }
        }

        protected override void onClickViveAppMenu(ref VREvent_t vrEvent)
        {
            if (!isShowing)
                showMenu(true);
            //terminate();
        }

        protected override void onClickOculusStick(ref VREvent_t vrEvent)
        {
            if (!isShowing)
                showMenu(true);
            //terminate();
        }

        protected override void onClickOculusAX(ref VREvent_t vrEvent)
        {
            //temporary testing patch
            if (vrEvent.trackedDeviceIndex == mScene.leftControllerIdx)
            {
                mScene.popInteraction();
                mScene.pushInteraction(new Cut(ref mScene));
            }
            else
            {
                /*
                mScene.popInteraction();
                mScene.pushInteraction(new CreatePatch(ref mScene));
                */
            }

        }

        private void terminate()
        {
            Rhino.RhinoApp.WriteLine("Quitting marking menu");
            mScene.popInteraction();
        }

        // TODO: Need to account for offset. Next
        // TODO: after add a new menulayout to the list, newing a new marking menu interaction and set approiate visiable attribute. it's easier for the undo function.
        private void launchInteraction(float r, float theta)
        {
            //int interactionNumber = ((int) Math.Floor((mNumSectors * theta - mFirstSectorOffsetAngle) / (2 * Math.PI)));
            mScene.vibrateController(0.1, (uint)primaryControllerIdx);
            int interactionNumber;
            /*
            if (theta < 0) { theta += (float)(2 * Math.PI); }
            interactionNumber = (int)Math.Ceiling((theta - (Math.PI / mNumSectors)) / (Math.PI / 2));
            if (interactionNumber >= mNumSectors) { interactionNumber = 0; }
            */
            float thetaOffset;
            if (theta < 0)
            {
                thetaOffset = (theta + (float)(2 * Math.PI)) - mFirstSectorOffsetAngle; //mFirstSectorOffsetAngle <0 so the result might greater than 360
            }
            else
            {
                thetaOffset = (theta) - mFirstSectorOffsetAngle;
            }

            if (thetaOffset < 0)
            {
                thetaOffset = thetaOffset + (float)(2 * Math.PI);
            }
            else if (thetaOffset > (float)(2 * Math.PI))
            {
                thetaOffset = thetaOffset - (float)(2 * Math.PI);
            }

            float sectorAngle = (float)(2 * Math.PI) / (float)mNumSectors;
            interactionNumber = (int)Math.Floor(thetaOffset / sectorAngle);

            Rhino.RhinoApp.WriteLine("interactionNumber: " + interactionNumber);
            MarkingMenu nextMenu = null;
            switch (mLayout)
            {
                case MenuLayout2.MainMenu:
                    switch (interactionNumber)
                    {
                        //loft
                        case 0:
                            mScene.selectionDic.Add(SelectionKey.ModelFun, FunctionType.Loft);
                            setCurSelectionKey(SelectionKey.ModelFun);
                            nextMenu = new MarkingMenu(ref mScene, MenuLayout2.LoftC1);
                            //Rhino.RhinoApp.WriteLine("section 0 : " + thetaDebug / Math.PI * 180);
                            if (nextMenu != null)
                            {
                                //this.setVisible(false);
                                nextMenu.setVisible(true);
                                mScene.pushInteraction(nextMenu);
                            }
                            break;
                        //Extrude
                        case 1:
                            mScene.selectionDic.Add(SelectionKey.ModelFun, FunctionType.Extrude);
                            setCurSelectionKey(SelectionKey.ModelFun);
                            nextMenu = new MarkingMenu(ref mScene, MenuLayout2.ExtrudeC1);
                            //Rhino.RhinoApp.WriteLine("section 3: "+ thetaDebug / Math.PI * 180);
                            if (nextMenu != null)
                            {
                                //this.setVisible(false);
                                nextMenu.setVisible(true);
                                mScene.pushInteraction(nextMenu);
                            }
                            break;
                        //Sweep
                        case 2:
                            mScene.selectionDic.Add(SelectionKey.ModelFun, FunctionType.Sweep);
                            setCurSelectionKey(SelectionKey.ModelFun);
                            nextMenu = new MarkingMenu(ref mScene, MenuLayout2.SweepC1);
                            //Rhino.RhinoApp.WriteLine("section 1 : " + thetaDebug / Math.PI * 180);
                            if (nextMenu != null)
                            {
                                //this.setVisible(false);
                                nextMenu.setVisible(true);
                                mScene.pushInteraction(nextMenu);
                            }
                            break;
                        //Revolve
                        case 3:
                            mScene.selectionDic.Add(SelectionKey.ModelFun, FunctionType.Revolve);
                            setCurSelectionKey(SelectionKey.ModelFun);
                            nextMenu = new MarkingMenu(ref mScene, MenuLayout2.RevolveAxis);
                            //Rhino.RhinoApp.WriteLine("section 2 : "+ thetaDebug / Math.PI * 180);
                            if (nextMenu != null)
                            {
                                //this.setVisible(false);
                                nextMenu.setVisible(true);
                                mScene.pushInteraction(nextMenu);
                            }
                            break;
                        //Patch
                        case 4:
                            mScene.popInteraction();
                            mScene.pushInteraction(new CreatePatch(ref mScene));
                            break;

                    }
                    break;
                case MenuLayout2.LoftC1:
                    switch (interactionNumber)
                    {
                        //Circle
                        case 0:
                            mScene.selectionDic.Add(SelectionKey.Profile1Shape, ShapeType.Circle);
                            setCurSelectionKey(SelectionKey.Profile1Shape);
                            nextMenu = new MarkingMenu(ref mScene, MenuLayout2.LoftD1);
                            nextMenu.setVisible(true);
                            mScene.pushInteraction(nextMenu);
                            break;
                        //Rect
                        case 1:
                            mScene.selectionDic.Add(SelectionKey.Profile1Shape, ShapeType.Rect);
                            setCurSelectionKey(SelectionKey.Profile1Shape);
                            nextMenu = new MarkingMenu(ref mScene, MenuLayout2.LoftD1);
                            nextMenu.setVisible(true);
                            mScene.pushInteraction(nextMenu);
                            break;
                        //Curve
                        case 2:
                            mScene.selectionDic.Add(SelectionKey.Profile1Shape, ShapeType.Curve);
                            setCurSelectionKey(SelectionKey.Profile1Shape);
                            nextMenu = new MarkingMenu(ref mScene, MenuLayout2.LoftD1);
                            nextMenu.setVisible(true);
                            mScene.pushInteraction(nextMenu);
                            break;

                    }
                    break;
                case MenuLayout2.LoftD1:
                    switch (interactionNumber)
                    {
                        //Surface
                        case 0:
                            mScene.selectionDic.Add(SelectionKey.Profile1On, DrawnType.Surface);
                            setCurSelectionKey(SelectionKey.Profile1On);
                            initInteractionChain(CurveID.ProfileCurve1);
                            mScene.pushInteractionFromChain();
                            break;
                        //Plane
                        case 1:
                            mScene.selectionDic.Add(SelectionKey.Profile1On, DrawnType.Plane);
                            setCurSelectionKey(SelectionKey.Profile1On);
                            initInteractionChain(CurveID.ProfileCurve1);
                            mScene.pushInteractionFromChain();
                            break;
                        //In3D
                        case 2:
                            mScene.selectionDic.Add(SelectionKey.Profile1On, DrawnType.In3D);
                            setCurSelectionKey(SelectionKey.Profile1On);
                            initInteractionChain(CurveID.ProfileCurve1);
                            mScene.pushInteractionFromChain();
                            break;

                    }
                    break;
                case MenuLayout2.LoftC2:
                    switch (interactionNumber)
                    {
                        //Circle
                        case 0:
                            mScene.selectionDic.Add(SelectionKey.Profile2Shape, ShapeType.Circle);
                            setCurSelectionKey(SelectionKey.Profile2Shape);
                            nextMenu = new MarkingMenu(ref mScene, MenuLayout2.LoftD2);
                            nextMenu.setVisible(true);
                            mScene.pushInteraction(nextMenu);
                            break;
                        //Rect
                        case 1:
                            mScene.selectionDic.Add(SelectionKey.Profile2Shape, ShapeType.Rect);
                            setCurSelectionKey(SelectionKey.Profile2Shape);
                            nextMenu = new MarkingMenu(ref mScene, MenuLayout2.LoftD2);
                            nextMenu.setVisible(true);
                            mScene.pushInteraction(nextMenu);
                            break;

                    }
                    break;
                case MenuLayout2.LoftD2:
                    switch (interactionNumber)
                    {
                        //Surface
                        case 0:
                            mScene.selectionDic.Add(SelectionKey.Profile2On, DrawnType.Surface);
                            setCurSelectionKey(SelectionKey.Profile2On);
                            initInteractionChain(CurveID.ProfileCurve2);
                            mScene.pushInteractionFromChain();
                            break;
                        //Plane
                        case 1:
                            mScene.selectionDic.Add(SelectionKey.Profile2On, DrawnType.Plane);
                            setCurSelectionKey(SelectionKey.Profile2On);
                            initInteractionChain(CurveID.ProfileCurve2);
                            mScene.pushInteractionFromChain();
                            break;
                        //In3D
                        case 2:
                            mScene.selectionDic.Add(SelectionKey.Profile2On, DrawnType.In3D);
                            setCurSelectionKey(SelectionKey.Profile2On);
                            initInteractionChain(CurveID.ProfileCurve2);
                            mScene.pushInteractionFromChain();
                            break;

                    }
                    break;
                case MenuLayout2.LoftC2D2:
                    switch (interactionNumber)
                    {
                        //Curve on Surface
                        case 0:
                            /*
                            mScene.selectionDic.Add(SelectionKey.CurveOn, DrawnType.Surface);
                            setCurSelectionKey(SelectionKey.CurveOn);
                            */
                            mScene.selectionDic.Add(SelectionKey.Profile2Shape, ShapeType.Curve);
                            mScene.selectionDic.Add(SelectionKey.Profile2On, DrawnType.Surface);
                            setCurSelectionKey(SelectionKey.CurveOn);

                            initInteractionChain(CurveID.ProfileCurve2);
                            mScene.pushInteractionFromChain();
                            break;
                        // Curve on Plane
                        case 1:
                            //mScene.selectionDic.Add(SelectionKey.CurveOn, DrawnType.Plane);
                            //setCurSelectionKey(SelectionKey.CurveOn);
                            mScene.selectionDic.Add(SelectionKey.Profile2Shape, ShapeType.Curve);
                            mScene.selectionDic.Add(SelectionKey.Profile2On, DrawnType.Plane);
                            setCurSelectionKey(SelectionKey.CurveOn);
                            initInteractionChain(CurveID.ProfileCurve2);
                            mScene.pushInteractionFromChain();
                            break;
                        //Curve in 3D
                        case 2:
                            //mScene.selectionDic.Add(SelectionKey.CurveOn, DrawnType.In3D);
                            //setCurSelectionKey(SelectionKey.CurveOn);
                            mScene.selectionDic.Add(SelectionKey.Profile2Shape, ShapeType.Curve);
                            mScene.selectionDic.Add(SelectionKey.Profile2On, DrawnType.In3D);
                            setCurSelectionKey(SelectionKey.CurveOn);
                            initInteractionChain(CurveID.ProfileCurve2);
                            mScene.pushInteractionFromChain();
                            break;

                    }
                    break;

                case MenuLayout2.ExtrudeC1:
                    switch (interactionNumber)
                    {
                        //Circle
                        case 0:
                            mScene.selectionDic.Add(SelectionKey.Profile1Shape, ShapeType.Circle);
                            setCurSelectionKey(SelectionKey.Profile1Shape);
                            nextMenu = new MarkingMenu(ref mScene, MenuLayout2.ExtrudeD1);
                            nextMenu.setVisible(true);
                            mScene.pushInteraction(nextMenu);
                            break;
                        //Rect
                        case 1:
                            mScene.selectionDic.Add(SelectionKey.Profile1Shape, ShapeType.Rect);
                            setCurSelectionKey(SelectionKey.Profile1Shape);
                            nextMenu = new MarkingMenu(ref mScene, MenuLayout2.ExtrudeD1);
                            nextMenu.setVisible(true);
                            mScene.pushInteraction(nextMenu);
                            break;
                    }
                    break;
                case MenuLayout2.ExtrudeD1:
                    switch (interactionNumber)
                    {
                        //Surface
                        case 0:
                            mScene.selectionDic.Add(SelectionKey.Profile1On, DrawnType.Surface);
                            setCurSelectionKey(SelectionKey.Profile1On);
                            initInteractionChain(CurveID.ProfileCurve1);
                            mScene.pushInteractionFromChain();
                            break;
                        //Plane
                        case 1:
                            mScene.selectionDic.Add(SelectionKey.Profile1On, DrawnType.Plane);
                            setCurSelectionKey(SelectionKey.Profile1On);
                            initInteractionChain(CurveID.ProfileCurve1);
                            mScene.pushInteractionFromChain();
                            break;
                        //In3D
                        case 2:
                            mScene.selectionDic.Add(SelectionKey.Profile1On, DrawnType.In3D);
                            setCurSelectionKey(SelectionKey.Profile1On);
                            initInteractionChain(CurveID.ProfileCurve1);
                            mScene.pushInteractionFromChain();
                            break;

                    }
                    break;
                case MenuLayout2.ExtrudeC2D2:
                    switch (interactionNumber)
                    {
                        //Curve in 3D
                        case 0:
                            //mScene.selectionDic.Add(SelectionKey.CurveOn, DrawnType.Surface);
                            //setCurSelectionKey(SelectionKey.CurveOn);
                            mScene.selectionDic.Add(SelectionKey.Profile2Shape, ShapeType.Curve);
                            mScene.selectionDic.Add(SelectionKey.Profile2On, DrawnType.In3D);
                            setCurSelectionKey(SelectionKey.CurveOn);
                            initInteractionChain(CurveID.ProfileCurve2);
                            mScene.pushInteractionFromChain();
                            break;
                        // Curve on normal plane
                        case 1:
                            //mScene.selectionDic.Add(SelectionKey.CurveOn, DrawnType.Reference);
                            mScene.selectionDic.Add(SelectionKey.Profile2Shape, ShapeType.Curve);
                            mScene.selectionDic.Add(SelectionKey.Profile2On, DrawnType.Reference);
                            setCurSelectionKey(SelectionKey.CurveOn);
                            initInteractionChain(CurveID.ProfileCurve2);
                            mScene.pushInteractionFromChain();
                            break;

                    }
                    break;
                case MenuLayout2.SweepC1:
                    switch (interactionNumber)
                    {
                        //Circle
                        case 0:
                            mScene.selectionDic.Add(SelectionKey.Profile1Shape, ShapeType.Circle);
                            setCurSelectionKey(SelectionKey.Profile1Shape);
                            nextMenu = new MarkingMenu(ref mScene, MenuLayout2.SweepD1);
                            nextMenu.setVisible(true);
                            mScene.pushInteraction(nextMenu);
                            break;
                        //Rect
                        case 1:
                            mScene.selectionDic.Add(SelectionKey.Profile1Shape, ShapeType.Rect);
                            setCurSelectionKey(SelectionKey.Profile1Shape);
                            nextMenu = new MarkingMenu(ref mScene, MenuLayout2.SweepD1);
                            nextMenu.setVisible(true);
                            mScene.pushInteraction(nextMenu);
                            break;

                    }
                    break;
                case MenuLayout2.SweepD1:
                    switch (interactionNumber)
                    {
                        //Surface
                        case 0:
                            mScene.selectionDic.Add(SelectionKey.Profile1On, DrawnType.Surface);
                            setCurSelectionKey(SelectionKey.Profile1On);
                            initInteractionChain(CurveID.ProfileCurve1);
                            mScene.pushInteractionFromChain();
                            break;
                        //Plane
                        case 1:
                            mScene.selectionDic.Add(SelectionKey.Profile1On, DrawnType.Plane);
                            setCurSelectionKey(SelectionKey.Profile1On);
                            initInteractionChain(CurveID.ProfileCurve1);
                            mScene.pushInteractionFromChain();
                            break;
                        //In3D
                        case 2:
                            mScene.selectionDic.Add(SelectionKey.Profile1On, DrawnType.In3D);
                            setCurSelectionKey(SelectionKey.Profile1On);
                            initInteractionChain(CurveID.ProfileCurve1);
                            mScene.pushInteractionFromChain();
                            break;

                    }
                    break;
                case MenuLayout2.SweepC2D2:
                    switch (interactionNumber)
                    {
                        //Curve in 3D
                        case 0:
                            //mScene.selectionDic.Add(SelectionKey.CurveOn, DrawnType.Surface);
                            mScene.selectionDic.Add(SelectionKey.Profile2Shape, ShapeType.Curve);
                            mScene.selectionDic.Add(SelectionKey.Profile2On, DrawnType.In3D);
                            setCurSelectionKey(SelectionKey.CurveOn);
                            initInteractionChain(CurveID.ProfileCurve2);
                            mScene.pushInteractionFromChain();
                            break;
                        // Curve on normal plane
                        case 1:
                            mScene.selectionDic.Add(SelectionKey.Profile2Shape, ShapeType.Curve);
                            mScene.selectionDic.Add(SelectionKey.Profile2On, DrawnType.Reference);
                            setCurSelectionKey(SelectionKey.CurveOn);
                            initInteractionChain(CurveID.ProfileCurve2);
                            mScene.pushInteractionFromChain();
                            break;

                    }
                    break;
                case MenuLayout2.RevolveAxis:
                    switch (interactionNumber)
                    {
                        //x-axis
                        case 0:
                            mScene.selectionDic.Add(SelectionKey.RevolveAxis, AxisType.worldY);
                            setCurSelectionKey(SelectionKey.RevolveAxis);
                            nextMenu = new MarkingMenu(ref mScene, MenuLayout2.RevolveC1D1);
                            nextMenu.setVisible(true);
                            mScene.pushInteraction(nextMenu);
                            break;
                        //z-axis
                        case 1:
                            mScene.selectionDic.Add(SelectionKey.RevolveAxis, AxisType.worldZ);
                            setCurSelectionKey(SelectionKey.RevolveAxis);
                            nextMenu = new MarkingMenu(ref mScene, MenuLayout2.RevolveC1D1);
                            nextMenu.setVisible(true);
                            mScene.pushInteraction(nextMenu);
                            break;
                        //y-axis
                        case 2:
                            mScene.selectionDic.Add(SelectionKey.RevolveAxis, AxisType.worldX);
                            setCurSelectionKey(SelectionKey.RevolveAxis);
                            nextMenu = new MarkingMenu(ref mScene, MenuLayout2.RevolveC1D1);
                            nextMenu.setVisible(true);
                            mScene.pushInteraction(nextMenu);
                            break;

                    }
                    break;
                case MenuLayout2.RevolveC1D1:
                    switch (interactionNumber)
                    {
                        //Circle on planes
                        case 0:
                            mScene.selectionDic.Add(SelectionKey.Profile1Shape, ShapeType.Circle);
                            mScene.selectionDic.Add(SelectionKey.Profile1On, DrawnType.Plane);
                            setCurSelectionKey(SelectionKey.ShapeOnPlanes);
                            initInteractionChain(CurveID.ProfileCurve1);
                            mScene.pushInteractionFromChain();
                            break;
                        //Rect on plane
                        case 1:
                            mScene.selectionDic.Add(SelectionKey.Profile1Shape, ShapeType.Rect);
                            mScene.selectionDic.Add(SelectionKey.Profile1On, DrawnType.Plane);
                            setCurSelectionKey(SelectionKey.ShapeOnPlanes);
                            initInteractionChain(CurveID.ProfileCurve1);
                            mScene.pushInteractionFromChain();
                            break;
                        //Curve on plane
                        case 2:
                            mScene.selectionDic.Add(SelectionKey.Profile1Shape, ShapeType.Curve);
                            mScene.selectionDic.Add(SelectionKey.Profile1On, DrawnType.Plane);
                            setCurSelectionKey(SelectionKey.ShapeOnPlanes);
                            initInteractionChain(CurveID.ProfileCurve1);
                            mScene.pushInteractionFromChain();
                            break;

                    }
                    break;


            }
        }

        private void pushInteractionChain(Interaction i)
        {
            i.setInChain(true);
            interactionChain.Push(i);
        }

        private void initInteractionChain(CurveID curveID)
        {
            //0-Surface, 1-3D, 2-Plane, 3-Reference
            interactionChain.Clear();
            //always remove from the last
            if (mScene.mIChainsList.Count > 0)
                mScene.mIChainsList.RemoveAt(mScene.mIChainsList.Count - 1);

            FunctionType modelFun = (FunctionType)mScene.selectionDic[SelectionKey.ModelFun];
            ShapeType shapeType = ShapeType.None;
            DrawnType drawnType = DrawnType.None;
            AxisType axisType = AxisType.None;
            MarkingMenu nextMenu = null;

            if (curveID == CurveID.ProfileCurve1)
            {

                if (modelFun == FunctionType.Loft)
                {
                    shapeType = (ShapeType)mScene.selectionDic[SelectionKey.Profile1Shape];
                    drawnType = (DrawnType)mScene.selectionDic[SelectionKey.Profile1On];
                    if (shapeType == ShapeType.Curve)
                    {
                        nextMenu = new MarkingMenu(ref mScene, MenuLayout2.LoftC2D2);
                    }
                    else
                    {
                        nextMenu = new MarkingMenu(ref mScene, MenuLayout2.LoftC2);
                    }

                    nextMenu.setVisible(true);
                    pushInteractionChain(nextMenu);
                }
                else if (modelFun == FunctionType.Sweep)
                {
                    shapeType = (ShapeType)mScene.selectionDic[SelectionKey.Profile1Shape];
                    drawnType = (DrawnType)mScene.selectionDic[SelectionKey.Profile1On];
                    nextMenu = new MarkingMenu(ref mScene, MenuLayout2.SweepC2D2);
                    nextMenu.setVisible(true);
                    pushInteractionChain(nextMenu);
                }
                else if (modelFun == FunctionType.Extrude)
                {
                    shapeType = (ShapeType)mScene.selectionDic[SelectionKey.Profile1Shape];
                    drawnType = (DrawnType)mScene.selectionDic[SelectionKey.Profile1On];
                    nextMenu = new MarkingMenu(ref mScene, MenuLayout2.ExtrudeC2D2);
                    nextMenu.setVisible(true);
                    pushInteractionChain(nextMenu);
                }
                else if (modelFun == FunctionType.Revolve)
                {
                    shapeType = (ShapeType)mScene.selectionDic[SelectionKey.Profile1Shape];
                    drawnType = (DrawnType)mScene.selectionDic[SelectionKey.Profile1On];
                    axisType = (AxisType)mScene.selectionDic[SelectionKey.RevolveAxis];
                }

            }
            else if (curveID == CurveID.ProfileCurve2)
            {

                if (modelFun == FunctionType.Loft)
                {
                    /*
                    if (mScene.selectionDic.ContainsKey(SelectionKey.CurveOn))
                    {
                        shapeType = ShapeType.Curve;
                        drawnType = (DrawnType)mScene.selectionDic[SelectionKey.CurveOn];
                    }
                    else
                    {
                        shapeType = (ShapeType)mScene.selectionDic[SelectionKey.Profile2Shape];
                        drawnType = (DrawnType)mScene.selectionDic[SelectionKey.Profile2On];

                    }*/
                    shapeType = (ShapeType)mScene.selectionDic[SelectionKey.Profile2Shape];
                    drawnType = (DrawnType)mScene.selectionDic[SelectionKey.Profile2On];
                }
                else if (modelFun == FunctionType.Sweep || modelFun == FunctionType.Extrude)
                {
                    ShapeType shapeType1 = (ShapeType)mScene.selectionDic[SelectionKey.Profile1Shape];
                    DrawnType drawnType1 = (DrawnType)mScene.selectionDic[SelectionKey.Profile1On];

                    shapeType = ShapeType.Curve;
                    drawnType = (DrawnType)mScene.selectionDic[SelectionKey.Profile2On];
                    //add the editing end cap interactaion here
                    if (shapeType1 == ShapeType.Circle || shapeType1 == ShapeType.Rect)
                        pushInteractionChain(new EditPoint(ref mScene, CurveID.EndCapCurve));
                }

            }

            if (drawnType == DrawnType.Surface)
            {
                if (shapeType == ShapeType.Rect)
                {
                    pushInteractionChain(new EditPoint(ref mScene, curveID));
                    pushInteractionChain(new AddPoint(ref mScene, 2, curveID));
                }
                else if (shapeType == ShapeType.Circle)
                {
                    pushInteractionChain(new EditPoint(ref mScene, curveID));
                    pushInteractionChain(new AddPoint(ref mScene, 2, curveID));
                }
                else if (shapeType == ShapeType.Curve)
                {
                    pushInteractionChain(new EditPoint(ref mScene, curveID));
                    pushInteractionChain(new CreateCurve(ref mScene, false, curveID));
                }
                /*
                else if (mScene.selectionList[index] == "Patch")
                {
                    mScene.mInteractionChian.Push(new CreatePatch(ref mScene));
                }*/
            }
            else if (drawnType == DrawnType.In3D)
            {
                if (shapeType == ShapeType.Circle)
                {
                    pushInteractionChain(new EditPoint(ref mScene, curveID));
                    pushInteractionChain(new CreatePlane(ref mScene, curveID));
                }
                else if (shapeType == ShapeType.Rect)
                {
                    pushInteractionChain(new EditPoint(ref mScene, curveID));
                    pushInteractionChain(new CreatePlane(ref mScene, curveID));
                }
                else if (shapeType == ShapeType.Curve)
                {
                    pushInteractionChain(new EditPoint(ref mScene, curveID));
                    pushInteractionChain(new CreateCurve(ref mScene, false, curveID));
                }

            }
            else if (drawnType == DrawnType.Plane)
            {

                if (shapeType == ShapeType.Rect)
                {
                    pushInteractionChain(new EditPoint(ref mScene, curveID));
                    pushInteractionChain(new AddPoint(ref mScene, 2, curveID));
                }
                else if (shapeType == ShapeType.Circle)
                {
                    pushInteractionChain(new EditPoint(ref mScene, curveID));
                    pushInteractionChain(new AddPoint(ref mScene, 2, curveID));
                }
                else if (shapeType == ShapeType.Curve)
                {
                    pushInteractionChain(new EditPoint(ref mScene, curveID));
                    pushInteractionChain(new CreateCurve(ref mScene, false, curveID));
                }
                /*
                else if (mScene.selectionList[index] == "Patch")
                {
                    mScene.mInteractionChian.Push(new CreatePatch(ref mScene, true));
                }*/
            }
            else if (drawnType == DrawnType.Reference)
            {
                if (shapeType == ShapeType.Rect)
                {
                    pushInteractionChain(new EditPoint(ref mScene, curveID));
                    //mScene.mInteractionChian.Push(new AddPoint(ref mScene, 2, curveID));
                    pushInteractionChain(new CreatePlane(ref mScene, curveID));
                }
                else if (shapeType == ShapeType.Circle)
                {
                    pushInteractionChain(new EditPoint(ref mScene, curveID));
                    //mScene.mInteractionChian.Push(new AddPoint(ref mScene, 2, curveID));
                    pushInteractionChain(new CreatePlane(ref mScene, curveID));
                }
                else if (shapeType == ShapeType.Curve)
                {
                    pushInteractionChain(new EditPoint(ref mScene, curveID));
                    pushInteractionChain(new CreateCurve(ref mScene, false, curveID));
                }
                /*
                else if (mScene.selectionList[index] == "Patch")
                {
                    mScene.mInteractionChian.Push(new CreatePatch(ref mScene));
                }*/

            }

            mScene.mIChainsList.Add(interactionChain);

        }

    }
}