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

        // Camera data
        public Matrix4 mHMDPose;

        // tracked devices
        public Valve.VR.CVRSystem mHMD;
        public Valve.VR.TrackedDevicePose_t[] mTrackedDevices = new Valve.VR.TrackedDevicePose_t[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];
        public Matrix4[] mDevicePose = new Matrix4[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];
        public char[] mDeviceClassChar = new char[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];
        public int leftControllerIdx = -1;
        public int rightControllerIdx = -1;
        public Matrix4 mLeftControllerOffset = Util.createTranslationMatrix(0.00134759f, -0.02559454f, -0.005455005f);//Util.createTranslationMatrix(0,0,0);//Util.createTranslationMatrix(0.0006068f, -.02383642f, -0.00026948f);
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

        public Matrix4 vrToRobot = new Matrix4(-0.9917692f, 0.01860419f, -0.0861549f, -0.06067098f,
           0.09522644f, 0.0138046f, -1.016119f, 0.09168084f,
           -0.02148988f, -1.012937f, 0.01351126f, 0.9823756f,
            0,0,0,1
            );

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
