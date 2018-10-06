using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using OpenTK;
using OpenTK.Graphics;
using SparrowHawk.Renderer;

namespace SparrowHawk.Calibration
{
    public class StereoSpaam
    {
        private static Geometry.Geometry cursor = null;
        private static Material.SingleColorMaterial cursorMaterial = null;
        private static float CURSOR_WIDTH = 0.05f;
        private static float CURSOR_HEIGHT = 0.05f;

        public static void RenderCrosshairs(Vector2 screenPos, Color4 color, FramebufferDesc framebuffer, bool clear = true)
        {
            if (cursor == null)
                instantiateCursor();
            if (cursorMaterial == null)
                cursorMaterial = new Material.SingleColorMaterial(1, 1, 1, 1);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.renderFramebufferId);
            GL.Viewport(0, 0, framebuffer.Width, framebuffer.Width);
            GL.ClearColor(0.1f, 0, 0.1f, 1);
            if (clear)
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            var view = Matrix4.CreateTranslation(-screenPos.X, -screenPos.Y,0);
            view.Transpose();
            var id = Matrix4.Identity;
            cursorMaterial.draw(ref cursor, ref view, ref id);
            view[0, 0] = view[0, 0];
        }

        protected static void instantiateCursor()
        {
            float w = CURSOR_WIDTH / 2;
            float h = CURSOR_HEIGHT / 2;
            cursor = new Geometry.Polyline(new float[] {-w,0,0, w,0,0,  0,-h,0, 0,h,0,  -w,0,0,0,h,0,  0,h,0, w,0,0,  w,0,0,0,-h,0,  0,-h,0, -w,0,0 });
        }

    }
}
