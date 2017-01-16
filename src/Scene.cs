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

        void render(ref Matrix4 vp, Matrix4 model) {
            if (geometry != null && material != null)
            {
                model *= transform;
                material.draw(geometry, ref model, ref vp);
            }
            foreach (SceneNode n in children)
            {
                n.render(ref vp, model);
            }
        }

        Matrix4 accumulateTransform()
        {
            if (parent == null)
                return transform;
            return parent.accumulateTransform() * transform;
        }

        // TODO: Actually implement raycasting!
        bool traceRay()
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
        public SceneNode rightControllerNode = new SceneNode("Right Controller Node");
        public SceneNode leftControllerNode = new SceneNode("Left Controller Node");
        // tracked devices

        // For rhino positioning
        public Rhino.RhinoDoc rhinoDoc;
        public Matrix4 robotToPlatform = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
        public Matrix4 platformRotation = new Matrix4(1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1);
        public Matrix4 vrToRobot = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

        public Scene(Rhino.RhinoDoc doc)
        {
            rhinoDoc = doc;
        }
    }
}
