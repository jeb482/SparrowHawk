using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL4;

namespace SparrowHawk.Renderer
{

    public class DepthPeelingRenderer : VrRenderer
    {
        const uint NUM_DEPTH_PEEL_LAYERS = 4;
        FramebufferDesc[] depthPeelBuffers;
        public DepthPeelingRenderer(ref Valve.VR.CVRSystem HMD, ref Scene scene, uint mRenderWidth, uint mRenderHeight) : base(ref HMD, ref scene, mRenderWidth, mRenderHeight)
        {
            GeneratePeelingBuffers();
        }


        bool GeneratePeelingBuffers()
        {
            depthPeelBuffers = new FramebufferDesc[NUM_DEPTH_PEEL_LAYERS];
            for (int i = 0; i < NUM_DEPTH_PEEL_LAYERS; i++)
            {
                if (!CreateFrameBuffer((int)vrRenderWidth, (int)vrRenderHeight, out depthPeelBuffers[i]));
                   return false;
            }
            return true;
        }
    }
}
