using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using OpenTK;

namespace SparrowHawk.Material
{
    class RGBNormalMaterial
    {
        float mAlpha;

        RGBNormalMaterial(float alpha)
        {
            // Init Shader?
            mAlpha = alpha;
        }

        public void draw(ref Geometry.Geometry g, ref Matrix4 model, ref Matrix4 vp)
        {
            // bind shader
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            // upload indices
            // upload attribs
            // set uniforms
            // draw indexed
            
        }

    }
}
