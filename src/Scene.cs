using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;


namespace SparrowHawk
{
    class SceneNode
    {
        public SceneNode parent = null;
        public List<SceneNode> children = new List<SceneNode>();
        public Geometry.Geometry geometry = null;
        public Material material = null;
        public Matrix4 transform = new Matrix4(1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1);
    }

    class Scene
    {
        public bool useOvrVision;

    }
}
