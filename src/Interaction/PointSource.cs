using System;
using Rhino;
using Rhino.Commands;
using OpenTK;

namespace SparrowHawk.Interaction
{
    public abstract class PointSource {
        public abstract Vector3 getPoint();
    }

    public class ControllerTipPointSource : PointSource
    {
        public override Vector3 getPoint()
        {
            return new Vector3();
        }
    }

    public class PlanePointSource : PointSource
    {
        public override Vector3 getPoint()
        {
            return new Vector3();
        }
    }

    public class PatchPointSource : PointSource
    {
        public override Vector3 getPoint()
        {
            return new Vector3();
        }
    }
}

