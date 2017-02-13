using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;


namespace SparrowHawk
{
    public class SceneNode
    {
        public string name;
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
        }

        public SceneNode(string _name)
        {
            name = _name;
            geometry = null;
            material = null;
        }

        public void render(ref Matrix4 vp, Matrix4 model) {
            model *= transform;
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

        // Camera data
        public Matrix4 mHMDPose;

        // tracked devices
        public Valve.VR.TrackedDevicePose_t[] mTrackedDevices = new Valve.VR.TrackedDevicePose_t[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];
        public Matrix4[] mDevicePose = new Matrix4[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];
        public char[] mDeviceClassChar = new char[Valve.VR.OpenVR.k_unMaxTrackedDeviceCount];
        public int leftControllerIdx = -1;
        public int rightControllerIdx = -1;

        // For rhino positioning
        public Rhino.RhinoDoc rhinoDoc;
        public Matrix4 robotToPlatform = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
        public Matrix4 platformRotation = new Matrix4(1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1);
        public Matrix4 vrToRobot = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

        public Scene(ref Rhino.RhinoDoc doc)
        {   
            rhinoDoc = doc;
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
