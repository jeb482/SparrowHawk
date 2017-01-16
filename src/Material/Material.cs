using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace SparrowHawk.Material
{
    public abstract class Material
    {
        public abstract void draw(Geometry.Geometry g, ref Matrix4 model, ref Matrix4 vp) 
    };
}
