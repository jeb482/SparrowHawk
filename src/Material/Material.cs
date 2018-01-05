using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace SparrowHawk.Material
{
    public abstract class Material
    {
        protected GLShader mShader;
        protected Rhino.RhinoDoc mDoc;

        public abstract void draw(ref Geometry.Geometry g, ref Matrix4 model, ref Matrix4 vp);
        protected virtual void finalize()
        {
            if (mShader != null && mShader.isInitialized)
            {
                mShader.free();
            }
        }
        
        public virtual void setAlpha(float alpha){}
    }
}
