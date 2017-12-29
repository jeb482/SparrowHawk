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

            if (mScene != null && curSelectionKey != SelectionKey.Null)
            {
                mScene.selectionDic.Remove(curSelectionKey);

                if (curSelectionKey == SelectionKey.Profile1On || curSelectionKey == SelectionKey.Profile2On)
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

        public int getNumSectors(MenuLayout layout)
        {
            //TODO- support different layout
            /*
            switch (layout)
            {
                case MenuLayout.MainMenu: return 4;
                case MenuLayout.ExtrudeC1: return 4;
                case MenuLayout.ExtrudeD1Circle: return 4;
                case MenuLayout.ExtrudeD1Rect: return 4;
                case MenuLayout.ExtrudeD1Curve: return 4;
                case MenuLayout.ExtrudeC2: return 4;
                case MenuLayout.ExtrudeD2Circle: return 4;
                case MenuLayout.ExtrudeD2Rect: return 4;
                case MenuLayout.ExtrudeD2Curve: return 4;
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
            */
            return 4;
        }

        public string getTexturePath(MenuLayout layout)
        {
            switch (layout)
            {
                //testing C:\\Users\\ericw\\Documents at home, C:\workspace at lab
                case MenuLayout.MainMenu: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\Main.png";
                case MenuLayout.ExtrudeC1: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\Extrude1.png";
                case MenuLayout.ExtrudeD1Circle: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\Extrude2Circle.png";
                case MenuLayout.ExtrudeD1Rect: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\Extrude2Rectangle.png";
                case MenuLayout.ExtrudeD1Curve: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\Extrude2Curve.png";
                case MenuLayout.ExtrudeC2: return @"C:\workspace\SparrowHawk\src\resources\menus\new\Extrude1.png";
                case MenuLayout.ExtrudeD2Circle: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\Extrude2Circle.png";
                case MenuLayout.ExtrudeD2Rect: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\Extrude2Rectangle.png";
                case MenuLayout.ExtrudeD2Curve: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\Extrude2Curve.png";
                case MenuLayout.LoftC1: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\LoftA1.png";
                case MenuLayout.LoftD1Circle: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\LoftA2Circle.png";
                case MenuLayout.LoftD1Rect: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\LoftA2Rectangle.png";
                case MenuLayout.LoftD1Curve: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\LoftA2Curve.png";
                case MenuLayout.LoftC2: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\LoftB1.png";
                case MenuLayout.LoftD2Circle: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\LoftB2Circle.png";
                case MenuLayout.LoftD2Rect: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\LoftB2Rectangle.png";
                case MenuLayout.LoftD2Curve: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\LoftB2Curve.png";
                case MenuLayout.RevolveC1: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\Revolve1.png";
                case MenuLayout.RevolveD1Circle: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\Revovle2Circle.png";
                case MenuLayout.RevolveD1Rect: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\Revovle2Rectangle.png";
                case MenuLayout.RevolveD1Curve: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\Revovle2Curve.png";
                case MenuLayout.SweepC1: return @"C:\workspace\SparrowHawk\src\resources\menus\new\SweepA1.png";
                case MenuLayout.SweepD1Circle: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\SweepA2Circle.png";
                case MenuLayout.SweepD1Rect: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\SweepA2Rectangle.png";
                case MenuLayout.SweepD1Curve: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\SweepA2Curve.png";
                case MenuLayout.SweepC2: return @"C:\workspace\SparrowHawk\src\resources\menus\new\SweepB1.png";
                case MenuLayout.SweepD2Circle: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\SweepB2Circle.png";
                case MenuLayout.SweepD2Rect: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\SweepB2Rectangle.png";
                case MenuLayout.SweepD2Curve: return @"C:\workspace\SparrowHawk\src\resources\menus\new2\SweepB2Curve.png";

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
            showMenu(true);
            //terminate();
        }

        protected override void onClickOculusStick(ref VREvent_t vrEvent)
        {
            showMenu(true);
            //terminate();
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
            if (theta < 0) { theta += (float)(2 * Math.PI); }
            interactionNumber = (int)Math.Ceiling((theta - (Math.PI / mNumSectors)) / (Math.PI / 2));
            if (interactionNumber >= mNumSectors) { interactionNumber = 0; }
            MarkingMenu nextMenu = null;
            switch (mLayout)
            {
                case MenuLayout.MainMenu:
                    switch (interactionNumber)
                    {
                        //Loft
                        case 0:
                            mScene.selectionDic.Add(SelectionKey.ModelFun, FunctionType.Loft);
                            setCurSelectionKey(SelectionKey.ModelFun);
                            nextMenu = new MarkingMenu(ref mScene, MenuLayout.LoftC1);
                            //Rhino.RhinoApp.WriteLine("section 0 : " + thetaDebug / Math.PI * 180);
                            if (nextMenu != null)
                            {
                                //this.setVisible(false);
                                nextMenu.setVisible(true);
                                mScene.pushInteraction(nextMenu);
                            }
                            break;
                        //Sweep
                        case 1:
                            mScene.selectionDic.Add(SelectionKey.ModelFun, FunctionType.Sweep);
                            setCurSelectionKey(SelectionKey.ModelFun);
                            nextMenu = new MarkingMenu(ref mScene, MenuLayout.SweepC1);
                            //Rhino.RhinoApp.WriteLine("section 1 : " + thetaDebug / Math.PI * 180);
                            if (nextMenu != null)
                            {
                                //this.setVisible(false);
                                nextMenu.setVisible(true);
                                mScene.pushInteraction(nextMenu);
                            }
                            break;
                        //Revolve
                        case 2:
                            mScene.selectionDic.Add(SelectionKey.ModelFun, FunctionType.Revolve);
                            setCurSelectionKey(SelectionKey.ModelFun);
                            nextMenu = new MarkingMenu(ref mScene, MenuLayout.RevolveC1);
                            //Rhino.RhinoApp.WriteLine("section 2 : "+ thetaDebug / Math.PI * 180);
                            if (nextMenu != null)
                            {
                                //this.setVisible(false);
                                nextMenu.setVisible(true);
                                mScene.pushInteraction(nextMenu);
                            }
                            break;
                        //Extrude
                        case 3:
                            mScene.selectionDic.Add(SelectionKey.ModelFun, FunctionType.Extrude);
                            setCurSelectionKey(SelectionKey.ModelFun);
                            nextMenu = new MarkingMenu(ref mScene, MenuLayout.ExtrudeC1);
                            //Rhino.RhinoApp.WriteLine("section 3: "+ thetaDebug / Math.PI * 180);
                            if (nextMenu != null)
                            {
                                //this.setVisible(false);
                                nextMenu.setVisible(true);
                                mScene.pushInteraction(nextMenu);
                            }
                            break;
                    }
                    break;

                case MenuLayout.LoftC1:
                case MenuLayout.SweepC1:
                case MenuLayout.RevolveC1:
                case MenuLayout.ExtrudeC1:
                    mScene.selectionDic.Add(SelectionKey.Profile1Shape, (ShapeType)(interactionNumber));
                    setCurSelectionKey(SelectionKey.Profile1Shape);
                    nextMenu = new MarkingMenu(ref mScene, (MenuLayout)((int)mLayout + interactionNumber + 1));
                    nextMenu.setVisible(true);
                    mScene.pushInteraction(nextMenu);
                    break;

                case MenuLayout.LoftD1Circle:
                case MenuLayout.LoftD1Rect:
                case MenuLayout.LoftD1Curve:
                case MenuLayout.SweepD1Circle:
                case MenuLayout.SweepD1Rect:
                case MenuLayout.SweepD1Curve:
                case MenuLayout.ExtrudeD1Circle:
                case MenuLayout.ExtrudeD1Rect:
                case MenuLayout.ExtrudeD1Curve:
                    mScene.selectionDic.Add(SelectionKey.Profile1On, (DrawnType)(interactionNumber));
                    setCurSelectionKey(SelectionKey.Profile1On);
                    initInteractionChain(CurveID.ProfileCurve1);
                    mScene.pushInteractionFromChain();
                    break;

                case MenuLayout.RevolveD1Circle:
                case MenuLayout.RevolveD1Rect:
                case MenuLayout.RevolveD1Curve:
                    mScene.selectionDic.Add(SelectionKey.Profile1On, (DrawnType)(interactionNumber));
                    setCurSelectionKey(SelectionKey.Profile1On);
                    initInteractionChain(CurveID.ProfileCurve1);
                    mScene.pushInteractionFromChain();
                    break;

                case MenuLayout.LoftC2:
                case MenuLayout.SweepC2:
                case MenuLayout.ExtrudeC2:
                    mScene.selectionDic.Add(SelectionKey.Profile2Shape, (ShapeType)(interactionNumber));
                    setCurSelectionKey(SelectionKey.Profile2Shape);
                    nextMenu = new MarkingMenu(ref mScene, (MenuLayout)((int)mLayout + interactionNumber + 1));
                    nextMenu.setVisible(true);
                    mScene.pushInteraction(nextMenu);
                    break;

                case MenuLayout.LoftD2Circle:
                case MenuLayout.LoftD2Rect:
                case MenuLayout.LoftD2Curve:
                case MenuLayout.SweepD2Circle:
                case MenuLayout.SweepD2Rect:
                case MenuLayout.SweepD2Curve:
                case MenuLayout.ExtrudeD2Circle:
                case MenuLayout.ExtrudeD2Rect:
                case MenuLayout.ExtrudeD2Curve:
                    mScene.selectionDic.Add(SelectionKey.Profile2On, (DrawnType)(interactionNumber));
                    setCurSelectionKey(SelectionKey.Profile2On);
                    initInteractionChain(CurveID.ProfileCurve2);
                    mScene.pushInteractionFromChain();
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
            MarkingMenu nextMenu = null;

            if (curveID == CurveID.ProfileCurve1)
            {
                shapeType = (ShapeType)mScene.selectionDic[SelectionKey.Profile1Shape];
                drawnType = (DrawnType)mScene.selectionDic[SelectionKey.Profile1On];

                //create next menu interaction based on different modelFun, it will be at the bottom of the interactionChain
                switch (modelFun)
                {
                    case FunctionType.Extrude:
                        nextMenu = new MarkingMenu(ref mScene, MenuLayout.ExtrudeC2);
                        nextMenu.setVisible(true);
                        pushInteractionChain(nextMenu);
                        break;
                    case FunctionType.Sweep:
                        nextMenu = new MarkingMenu(ref mScene, MenuLayout.SweepC2);
                        nextMenu.setVisible(true);
                        pushInteractionChain(nextMenu);
                        break;
                    case FunctionType.Loft:
                        nextMenu = new MarkingMenu(ref mScene, MenuLayout.LoftC2);
                        nextMenu.setVisible(true);
                        pushInteractionChain(nextMenu);
                        break;
                }

            }
            else if (curveID == CurveID.ProfileCurve2)
            {
                ShapeType shapeType1 = (ShapeType)mScene.selectionDic[SelectionKey.Profile1Shape];
                DrawnType drawnType1 = (DrawnType)mScene.selectionDic[SelectionKey.Profile1On];

                //add the editing end cap interactaion here
                if (modelFun == FunctionType.Extrude || modelFun == FunctionType.Sweep)
                {
                    if (shapeType1 == ShapeType.Circle || shapeType1 == ShapeType.Rect)
                        pushInteractionChain(new EditPoint3(ref mScene, CurveID.EndCapCurve));
                }

                shapeType = (ShapeType)mScene.selectionDic[SelectionKey.Profile2Shape];
                drawnType = (DrawnType)mScene.selectionDic[SelectionKey.Profile2On];

            }

            if (drawnType == DrawnType.Surface)
            {
                if (shapeType == ShapeType.Rect)
                {
                    pushInteractionChain(new EditPoint3(ref mScene, curveID));
                    pushInteractionChain(new AddPoint(ref mScene, 2, curveID));
                }
                else if (shapeType == ShapeType.Circle)
                {
                    pushInteractionChain(new EditPoint3(ref mScene, curveID));
                    pushInteractionChain(new AddPoint(ref mScene, 2, curveID));
                }
                else if (shapeType == ShapeType.Curve)
                {
                    pushInteractionChain(new EditPoint3(ref mScene, curveID));
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
                    pushInteractionChain(new CreatePlane2(ref mScene, curveID));
                }
                else if (shapeType == ShapeType.Rect)
                {
                    pushInteractionChain(new CreatePlane2(ref mScene, curveID));
                }
                else if (shapeType == ShapeType.Curve)
                {
                    pushInteractionChain(new EditPoint3(ref mScene, curveID));
                    pushInteractionChain(new CreateCurve(ref mScene, false, curveID));
                }

            }
            else if (drawnType == DrawnType.Plane)
            {

                if (shapeType == ShapeType.Rect)
                {
                    pushInteractionChain(new EditPoint3(ref mScene, curveID));
                    pushInteractionChain(new AddPoint(ref mScene, 2, curveID));
                }
                else if (shapeType == ShapeType.Circle)
                {
                    pushInteractionChain(new EditPoint3(ref mScene, curveID));
                    pushInteractionChain(new AddPoint(ref mScene, 2, curveID));
                }
                else if (shapeType == ShapeType.Curve)
                {
                    pushInteractionChain(new EditPoint3(ref mScene, curveID));
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
                    pushInteractionChain(new EditPoint3(ref mScene, curveID));
                    //mScene.mInteractionChian.Push(new AddPoint(ref mScene, 2, curveID));
                    pushInteractionChain(new CreatePlane(ref mScene, curveID));
                }
                else if (shapeType == ShapeType.Circle)
                {
                    pushInteractionChain(new EditPoint3(ref mScene, curveID));
                    //mScene.mInteractionChian.Push(new AddPoint(ref mScene, 2, curveID));
                    pushInteractionChain(new CreatePlane(ref mScene, curveID));
                }
                else if (shapeType == ShapeType.Curve)
                {
                    pushInteractionChain(new EditPoint3(ref mScene, curveID));
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
