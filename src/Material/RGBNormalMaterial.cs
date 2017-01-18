using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using OpenTK;

namespace SparrowHawk.Material
{
    class RGBNormalMaterial : Material
    {
        float mAlpha;
        

        RGBNormalMaterial(float alpha, Rhino.RhinoDoc doc)
        {
            // Init Shader?
            mShader = new GLShader(doc);
            mShader.init("RGBNormal", ShaderSource.RGBNormalVertShader, ShaderSource.RGBNormalFragShader);
            mAlpha = alpha;
        }

        override public void draw(ref Geometry.Geometry g, ref Matrix4 model, ref Matrix4 vp)
        {
            // bind shader
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            mShader.uploadIndices(g.getIndices());
            mShader.uploadAttrib("position", g.getGeometry());
            // upload indices
            // upload attribs
            // set uniforms
            // draw indexed
            
        }

    }
}
