using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using Valve.VR;

namespace SparrowHawk
{
    public class SceneNode
    {
        public string name;
        public Guid guid;
        public SceneNode parent = null;
        public List<SceneNode> children = new List<SceneNode>();
        public Geometry.Geometry geometry;
        public Material.Material material;
        public Matrix4 transform = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

        public SceneNode(string _name, ref Geometry.Geometry g, ref Material.Material m)
        {
            name = _name;
            geometry = g;
            material = m;
            guid = Guid.NewGuid();
        }

        public SceneNode(string _name)
        {
            name = _name;
            geometry = null;
            material = null;
            guid = Guid.NewGuid();
        }

        public void render(ref Matrix4 vp, Matrix4 model) {
            model = model * transform;
            if (geometry != null && material != null)
            {
                material.draw(ref geometry, ref model, ref vp);
            }
            foreach (SceneNode n in children)
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

        // Camera data
        public Matrix4 mHMDPose;

        // tracked devices
        public Valve.VR.CVRSystem mHMD;
        public Valve.VR.TrackedDevicePose_t[] mTrackedDevices = new Valve.VR.TrackedDevicePose_t[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];
        public Matrix4[] mDevicePose = new Matrix4[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];
        public char[] mDeviceClassChar = new char[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];
        public int leftControllerIdx = -1;
        public int rightControllerIdx = -1;
        public Matrix4 mLeftControllerOffset = Util.createTranslationMatrix(0.001457473f,-0.02422076f,-0.00521365f);
        //public Matrix4 mLeftControllerOffset = Util.createTranslationMatrix(0.001885863f, -0.02479392f,-0.0003346408f);
        //public Matrix4 mLeftControllerOffset = Util.createTranslationMatrix(0.00134759f, -0.02559454f, -0.005455005f);//Util.createTranslationMatrix(0,0,0);//Util.createTranslationMatrix(0.0006068f, -.02383642f, -0.00026948f);
        public Matrix4 mRightControllerOffset = Util.createTranslationMatrix(0, 0, 0);//Util.createTranslationMatrix(-0.0006068f, -.02383642f, -0.00026948f);

        // For rhino positioning
        public Rhino.RhinoDoc rhinoDoc;
        public Matrix4 robotToPlatform = new Matrix4(1,  0,  0, 0,
                                                     0, -1,  0, 0,
                                                     0,  0, -1, 0, 
                                                     0,  0,  0, 1);
        public Matrix4 platformRotation = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
        //public Matrix4 vrToRobot = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
        //(-1.004264, 0.01445435, -0.07111868, -0.05827864)
        //(0.08518852, -0.02587833, -1.022716, 0.1467833)
        //(-0.01968208, -1.006881, 0.01732827, 0.9705337)
        //(0, 0, 0, 1)


        //public Matrix4 vrToRobot = OpenTK.Matrix4.Identity;
        //public Matrix4 vrToRobot = new Matrix4(-1.018527f, -0.008061957f, 0.06252521f, -0.0658858f,
        //   -0.02257842f, -0.0356043f, -1.034634f, 0.1029692f,
        //   0.00329627f, -1.012641f, -0.006327522f, 0.7405275f,
        //   0,0,0,1);

        public Matrix4 vrToRobot = new Matrix4(0.04553255f, 0.0110915f, -1.016086f, -0.7004303f,
                                               1.015694f, -0.01724754f, 0.06361949f, 0.02786883f,
                                               -0.02152945f, -1.000105f, -0.007235864f, 1.449791f,
                                               0, 0, 0, 1);


        // Interactions
        private Stack<Interaction.Interaction> mInteractionStack = new Stack<Interaction.Interaction>();
        public bool isOculus = false;


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

        public Scene(ref Rhino.RhinoDoc doc, ref Valve.VR.CVRSystem hmd)
        {   
            rhinoDoc = doc;
            mHMD = hmd;
        }

        public void render(ref Matrix4 vp)
        {
            Matrix4 m = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
            tableGeometry.transform = this.vrToRobot.Inverted()*this.platformRotation.Inverted()* this.vrToRobot;
            staticGeometry.render(ref vp, m);
            tableGeometry.render(ref vp, m);
            leftControllerNode.render(ref vp, m);
            rightControllerNode.render(ref vp, m);
        }

        // Implement traceray
        public bool traceRay()
        {
            return false;
        }
    }
}
