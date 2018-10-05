using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using Valve.VR;
using Rhino.Geometry;

namespace SparrowHawk
{
    public class RenderOrderComparer : IComparer<SceneNode>
    {
        private RenderOrderComparer() { }
        private static RenderOrderComparer instance;
        public static RenderOrderComparer Instance
        {
            get
            {
                if (instance == null)
                    RenderOrderComparer.instance = new RenderOrderComparer();
                return instance;
            }
        }
        public int Compare(SceneNode x, SceneNode y)
        {
            if (x.mRenderLate && !y.mRenderLate) return 1; else return 0;
        }
    }

    public class SceneNode
    {
        public string name;
        public Guid guid;
        public SceneNode parent = null;
        public List<SceneNode> children = new List<SceneNode>();
        public Geometry.Geometry geometry;
        public Material.Material material;
        public Matrix4 transform = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
        public bool mRenderLate;

        public SceneNode(string _name, ref Geometry.Geometry g, ref Material.Material m, bool renderLate = false)
        {
            name = _name;
            geometry = g;
            material = m;
            guid = Guid.NewGuid();
            mRenderLate = renderLate;
        }

        public SceneNode(string _name)
        {
            name = _name;
            geometry = null;
            material = null;
            guid = Guid.NewGuid();
            mRenderLate = false;
        }   

        public void render(ref Matrix4 vp, Matrix4 model)
        {
            model = model * transform;
            if (geometry != null && material != null)
            {
                material.draw(ref geometry, ref model, ref vp);
            }
            //testing
            foreach (SceneNode n in children.Reverse<SceneNode>())
            {
                n.render(ref vp, model);
            }
        }

        public Matrix4 accumulateTransform()
        {
            if (parent == null)
                return transform;
            return parent.accumulateTransform() * transform;
        }

        // TODO: Actually implement raycasting!
        public bool traceRay()
        {
            return false;
        }

        public void add(ref SceneNode child)
        {
            children.Add(child);
            child.parent = this;
            children.Sort(RenderOrderComparer.Instance);
        }

        public void remove(ref SceneNode child)
        {
            children.Remove(child);
            child.parent = null;
        }

    }

    public class Scene
    {
        // For renderring
        public bool useOvrVision;
        public SceneNode staticGeometry = new SceneNode("Static Scene");
        public SceneNode tableGeometry = new SceneNode("Encoder-Affected Geometry");
        public SceneNode leftControllerNode = new SceneNode("Right Controller Node");
        public SceneNode rightControllerNode = new SceneNode("Left Controller Node");
        public BiDictionaryOneToOne<Guid, SceneNode> BiDictionaryRhinoVR = new BiDictionaryOneToOne<Guid, SceneNode>();
        DateTime mLastFrameTime;
        DateTime mCurrentFrameTime;
        public double gameTime;

        // Companion window rendering
        protected int _windowWidth = 0;
        protected int _windowHeight = 0;
        public int windowWidth { get { return _windowWidth; } }
        public int windowHeight { get { return _windowHeight; } }

        // Camera data
        public Matrix4 mHMDPose;

        // tracked devices
        public Valve.VR.CVRSystem mHMD;
        public Valve.VR.TrackedDevicePose_t[] mTrackedDevices = new Valve.VR.TrackedDevicePose_t[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];
        public Matrix4[] mDevicePose = new Matrix4[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];
        public char[] mDeviceClassChar = new char[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];
        public int leftControllerIdx = -1;
        public int rightControllerIdx = -1;
        public Matrix4 mLeftControllerOffset = Util.Math.createTranslationMatrix(0.01451808f, -0.08065776f, 0.006754342f);
        public Matrix4 mRightControllerOffset = Util.Math.createTranslationMatrix(0.006479859f, -0.02640941f, 0.0007925751f);//Util.createTranslationMatrix(-0.03205855f+ 0.0001097674f, -0.02689967f+ -0.0008004899f, 0.006957637f+ -0.0005140541f);

        private double leftControllerEndVibrateTime;
        private double rightControllerEndVibrateTime;
        public bool mIsLefty;

        //for interaction chain
        //public List<Rhino.DocObjects.ObjRef> iRhObjList = new List<Rhino.DocObjects.ObjRef>();
        public List<Rhino.Geometry.Curve> iCurveList = new List<Rhino.Geometry.Curve>();
        public List<OpenTK.Vector3> iPointList = new List<OpenTK.Vector3>();
        //public List<Rhino.Geometry.Plane> iPlaneList = new List<Rhino.Geometry.Plane>(); //temporary solution for circle, rect
        public enum MenuLayout //make sure the order matches the selection numbers rect-curve-circle.
        {
            MainMenu, ExtrudeC1, ExtrudeD1Rect, ExtrudeD1Curve, ExtrudeD1Circle, ExtrudeC2, ExtrudeD2Rect, ExtrudeD2Curve, ExtrudeD2Circle,
            LoftC1, LoftD1Rect, LoftD1Curve, LoftD1Circle, LoftC2, LoftD2Rect, LoftD2Curve, LoftD2Circle, RevolveC1, RevolveD1Rect, RevolveD1Curve, RevolveD1Circle,
            SweepC1, SweepD1Rect, SweepD1Curve, SweepD1Circle, SweepC2, SweepD2Rect, SweepD2Curve, SweepD2Circle
        };
        public enum MenuLayout2 //make sure the order matches the selection numbers rect-curve-circle.
        {
            MainMenu, SweepC1, SweepD1, SweepC2D2, ExtrudeC1, ExtrudeD1, ExtrudeC2D2, LoftC1, LoftD1, LoftC2D2, LoftC2, LoftD2, RevolveC1D1, RevolveAxis, 
        };
        public enum XYZPlanes {YZ, XZ, XY};
        public enum FunctionType { None = -1, Loft, Sweep, Revolve, Extrude, Patch };
        public enum ShapeType { None = -1, Rect, Curve, Circle };
        public enum DrawnType { None = -1, Surface, In3D, Plane, Reference };
        public enum AxisType { None = -1, worldX, worldY, worldZ };
        public enum CurveID { ProfileCurve1, ProfileCurve2, EndCapCurve }
        public enum CurveData { CurveOnObj, PlaneOrigin, PlaneNormal };
        //public List<MenuLayout> menuList = new List<MenuLayout>();
        //public List<string> selectionList = new List<string>();
        public enum SelectionKey { Null = -1, ModelFun, Profile1Shape, Profile1On, Profile2Shape, Profile2On, CurveOn, RevolveAxis, ShapeOnPlanes };
        public Dictionary<SelectionKey, Object> selectionDic = new Dictionary<SelectionKey, Object>();
        //public int menuIndex = 0;

        // For rhino positioning
        public Rhino.RhinoDoc rhinoDoc;
        public Matrix4 robotToPlatform = new Matrix4(1, 0, 0, 0,
                                                     0, -1, 0, 0,
                                                     0, 0, -1, 0,
                                                     0, 0, 0, 1);
        public Matrix4 platformRotation = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
        public Transform transM = new Transform();


        public Matrix4 vrToRobot = new Matrix4(-16.04682f, 10.11053f, -994.6521f, -412.0325f,
                                                994.7281f, -16.32391f, -23.68098f, -3.561577f,
                                                -13.79134f, -1002.81f, -30.55335f, 1444.57f,
                                                0f, 0f, 0f, 1f);


        // Set this to rotation you want (in radians)
        public Matrix4 laserBeamFrame = Matrix4.CreateRotationX(0);

        // Interactions
        private Stack<Interaction.Interaction> mInteractionStack = new Stack<Interaction.Interaction>();
        public List<Stack<Interaction.Interaction>> mIChainsList = new List<Stack<Interaction.Interaction>>();
        public bool isOculus = true;

        //SweepCapFun Debugging
        public float angleD;
        public string c1D, c2D, c3D;
        public Point3d sStartP, eStartP;

        //visiable and hide designPlane
        public DesignPlane xzPlane, xyPlane, yzPlane;
        public SceneNode xAxis, yAxis, zAxis;

        public float rhinoTheta = 0;

        public void pushInteractionToChain(Interaction.Interaction i)
        {
            i.setInChain(true);
            mIChainsList[mIChainsList.Count - 1].Push(i);
        }

        public void pushInteractionFromChain()
        {
            //get the interaction in the interactionChain
            for (int i = mIChainsList.Count - 1; i >= 0; i--)
            {
                if (mIChainsList[i].Count != 0)
                {
                    Interaction.Interaction nextI = mIChainsList[i].Pop();
                    pushInteraction(nextI);
                }
                else
                {
                    continue;
                }
            }

        }

        public void clearIChainsList()
        {
            mIChainsList.Clear();
        }

        public Interaction.Interaction popInteraction()
        {
            Interaction.Interaction i = mInteractionStack.Pop();
            if (i == null)
                return i;
            i.deactivate();
            return i;
        }

        public Interaction.Interaction peekInteraction()
        {
            return mInteractionStack.Peek();
        }

        public void pushInteraction(Interaction.Interaction i)
        {
            i.activate();
            mInteractionStack.Push(i);
        }

        public bool interactionStackEmpty()
        {
            return (mInteractionStack.Count == 0);
        }

        public void clearInteractionStack()
        {
            mInteractionStack.Clear();
        }

        public Scene(ref Rhino.RhinoDoc doc, ref Valve.VR.CVRSystem hmd)
        {
            rhinoDoc = doc;
            mHMD = hmd;
        }

        public void render(ref Matrix4 vp)
        {
            Matrix4 m = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
            //tableGeometry.transform = this.vrToRobot.Inverted()*this.platformRotation.Inverted()* this.vrToRobot;
            tableGeometry.transform = this.vrToRobot.Inverted() * this.robotToPlatform.Inverted() * this.platformRotation * this.robotToPlatform * this.vrToRobot;
            staticGeometry.render(ref vp, m);
            tableGeometry.render(ref vp, m);
            leftControllerNode.render(ref vp, m);
            rightControllerNode.render(ref vp, m);
        }

        public void vibrateController(double duration, uint deviceIndex)
        {
            if (deviceIndex == leftControllerIdx)
                leftControllerEndVibrateTime = Math.Max(leftControllerEndVibrateTime, gameTime + duration);
            else if (deviceIndex == rightControllerIdx)
                rightControllerEndVibrateTime = Math.Max(rightControllerEndVibrateTime, gameTime + duration);
        }

        public bool leftControllerShouldVibrate()
        {
            return (leftControllerEndVibrateTime > gameTime || false);
        }

        public bool rightControllerShouldVibrate()
        {
            return (rightControllerEndVibrateTime > gameTime || false);
        }

        // Should really only be called by the main game on resize.
        public void setWindowSize(int width, int height)
        {
            this._windowWidth = width;
            this._windowHeight = height;
        }
    }
}
