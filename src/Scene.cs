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
        public Dictionary<Guid, SceneNode> brepToSceneNodeDic = new Dictionary<Guid, SceneNode>();
        public Dictionary<Guid, Rhino.DocObjects.RhinoObject> SceneNodeToBrepDic = new Dictionary<Guid, Rhino.DocObjects.RhinoObject>();
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
        public Matrix4 mLeftControllerOffset = Util.createTranslationMatrix(0.01451808f, -0.08065776f, 0.006754342f);
        //public Matrix4 mLeftControllerOffset = Util.createTranslationMatrix(0.001885863f, -0.02479392f,-0.0003346408f);
        //public Matrix4 mLeftControllerOffset = Util.createTranslationMatrix(0.00134759f, -0.02559454f, -0.005455005f);//Util.createTranslationMatrix(0,0,0);//Util.createTranslationMatrix(0.0006068f, -.02383642f, -0.00026948f);
        public Matrix4 mRightControllerOffset = Util.createTranslationMatrix(0.006787707f + -0.0001619215f, -0.02603238f + -0.0003721698f, 0.002012544f + -0.0001412859f);//Util.createTranslationMatrix(-0.03205855f+ 0.0001097674f, -0.02689967f+ -0.0008004899f, 0.006957637f+ -0.0005140541f);
        //(0.005918316, -0.02670806, 0.001123522)
        //-0.0009147244f, -0.002345422f, -0.0006840917f
        //-5.139893E-05f, 0.0005281732f, 0.0001677993f
        //0.005918316f + -0.0003245298f, -0.02670806f+ 0.0002023121f, 0.001123522f + -0.0001561325f
        //0.005497481f, -0.02618495f, 0.0009147127f
        //0.0005435179, 0.0002183659, 0.0009561999
        //0.006787707f+ -0.0001619215f, -0.02603238f+ -0.0003721698f, 0.002012544f+ -0.0001412859f
        private double leftControllerEndVibrateTime;
        private double rightControllerEndVibrateTime;
        public bool mIsLefty;

        //for interaction chain
        public List<Rhino.DocObjects.RhinoObject> iRhObjList = new List<Rhino.DocObjects.RhinoObject>();
        public List<Rhino.Geometry.Curve> iCurveList = new List<Rhino.Geometry.Curve>();
        public List<OpenTK.Vector3> iPointList = new List<OpenTK.Vector3>();
        public List<Rhino.Geometry.Plane> iPlaneList = new List<Rhino.Geometry.Plane>(); //temporary solution for circle, rect
        public enum MenuLayout //make sure the order matches the selection numbers rect-curve-circle.
        {
            MainMenu, ExtrudeC1, ExtrudeD1Rect, ExtrudeD1Curve, ExtrudeD1Circle, ExtrudeC2, ExtrudeD2Rect, ExtrudeD2Curve, ExtrudeD2Circle,
            LoftC1, LoftD1Rect, LoftD1Curve, LoftD1Circle, LoftC2, LoftD2Rect, LoftD2Curve, LoftD2Circle, RevolveC1, RevolveD1Rect, RevolveD1Curve, RevolveD1Circle,
            SweepC1, SweepD1Rect, SweepD1Curve, SweepD1Circle, SweepC2, SweepD2Rect, SweepD2Curve, SweepD2Circle
        };
        public enum FunctionType { None = -1, Loft, Sweep, Revolve, Extrude, Patch };
        public enum ShapeType { None = -1, Rect, Curve, Circle };
        public enum DrawnType { None = -1, Surface, In3D, Plane, Reference };
        public enum CurveID { ProfileCurve1, ProfileCurve2, EndCapCurve }
        //public List<MenuLayout> menuList = new List<MenuLayout>();
        //public List<string> selectionList = new List<string>();
        public enum SelectionKey { Null = -1, ModelFun, Profile1Shape, Profile1On, Profile2Shape, Profile2On };
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
        //public Matrix4 vrToRobot = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
        //(-1.004264, 0.01445435, -0.07111868, -0.05827864)
        //(0.08518852, -0.02587833, -1.022716, 0.1467833)
        //(-0.01968208, -1.006881, 0.01732827, 0.9705337)
        //(0, 0, 0, 1)


        //public Matrix4 vrToRobot = OpenTK.Matrix4.Identity;

        /*
        public Matrix4 vrToRobot = new Matrix4(0.04553255f, 0.0110915f, -1.016086f, -0.7004303f,
                                               1.015694f, -0.01724754f, 0.06361949f, 0.02786883f,
                                               -0.02152945f, -1.000105f, -0.007235864f, 1.449791f,
                                               0, 0, 0, 1);*/
        /*
        public Matrix4 vrToRobot = new Matrix4(-61.6759f, 17.15416f, -996.1694f, -519.5059f,
                                               1028.861f, -29.56195f, -60.25785f, 30.58262f,
                                               -23.20242f, -994.0332f, -6.043113f, 1380.783f,
                                               0, 0, 0, 1);*/
        /*
        public Matrix4 vrToRobot = new Matrix4(-28.97375f, 8.693132f, -981.7787f, -626.1249f,
                                              990.0181f, -13.71652f, -31.3348f, 172.6357f,
                                              -6.955585f, -995.7741f, -15.0203f, 702.3792f,
                                              0, 0, 0, 1);
        */
        /*
        public Matrix4 vrToRobot = new Matrix4(-24.40972f, -15.1202f, -1006.631f, -619.447f,
                                             977.9764f, -15.0478f, -28.42042f, 173.4097f,
                                             -16.9345f, -985.1676f, 2.534816f, 709.4443f,
                                             0, 0, 0, 1);*/
        /*
public Matrix4 vrToRobot = new Matrix4(-25.23433f, -1.428557f, -986.1774f, -619.1987f,
994.2307f, -14.8897f, -28.13068f, 175.5754f,
-9.72579f, -1007.435f, -17.07237f, 707.9531f,
0, 0, 0, 1);*/

        public Matrix4 vrToRobot = new Matrix4(2.422155f, 23.61365f, -976.0397f, -582.7103f,
           1013.433f, -29.26103f, -0.3094192f, 173.9741f,
-13.29245f, -989.1036f, -30.18169f, 703.6801f,
0, 0, 0, 1);


        // Interactions
        private Stack<Interaction.Interaction> mInteractionStack = new Stack<Interaction.Interaction>();
        public Stack<Interaction.Interaction> mInteractionChain = new Stack<Interaction.Interaction>();
        public bool isOculus = true;

        //SweepCapFun Debugging
        public float angleD;
        public string c1D, c2D, c3D;
        public Point3d sStartP, eStartP;

        //visiable and hide designPlane
        public DesignPlane3 xzPlane, xyPlane, yzPlane;
        public SceneNode xAxis, yAxis, zAxis;

        public float rhinoTheta = 0;

        public void pushInteractionFromChain()
        {
            //get the interaction in the interactionChain
            if (mInteractionChain.Count != 0)
            {
                Interaction.Interaction nextI = mInteractionChain.Pop();
                pushInteraction(nextI);
            }
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
            return (leftControllerEndVibrateTime > gameTime);
        }

        public bool rightControllerShouldVibrate()
        {
            return (rightControllerEndVibrateTime > gameTime);
        }

        // Implement traceray
        public bool traceRay()
        {
            return false;
        }

        // Should really only be called by the main game on resize.
        public void setWindowSize(int width, int height)
        {
            this._windowWidth = width;
            this._windowHeight = height;
        }
    }
}
