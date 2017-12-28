using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using System.Drawing;
using System.Drawing.Imaging;

namespace SparrowHawk.Renderer
{

    // Adapted from this post:
    // https://gamedev.stackexchange.com/questions/123978/c-opentk-text-rendering

    public class Font
    {
        public string fontBitmapFilename = "test.png";
        public int glyphsPerLine = 16;
        public int glyphLineCount = 16;
        public int glyphWidth = 11;
        public int glyphHeight = 22;

        public int charXSpacing = 11;

        public string text = "GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);";

        public Bitmap bitmap { get { if (_bitmap == null) loadBitmap(); return _bitmap; }  }
        private Bitmap _bitmap;

        // Used to offset rendering glyphs to bitmap
        public int atlasOffsetX = -3, AtlassOffsetY = -1;
        public int fontSize = 14;
        public bool bitmapFont = false;
        public string fromFile; //= "joystix monospace.ttf";
        public string FontName = "Consolas";

        private int mTextureIndex;

        private void loadBitmap()
        {

        }
    }

    public class TextRenderer
    {
        private GLShader mShader;
        
        public TextRenderer ()
        {
            mShader = new GLShader();
            mShader.init("TextureMaterial", Material.ShaderSource.TextureVertShader, Material.ShaderSource.TextureFragShader);
        }
        public void drawText(int x, int y, string text, Font font, int texWidth, int texHeight)
        {
            mShader.bind();


            
            texWidth = font.bitmap.Width;
            texHeight = font.bitmap.Height;

            //Flip the image
            //if (flip_y)
            //    bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

            GL.ActiveTexture(TextureUnit.Texture0);
            //Generate a new texture target in gl
            int textureIndex = GL.GenTexture();
            //bind the texture newly/empty created with GL.GenTexture
            GL.BindTexture(TextureTarget.Texture2D, textureIndex);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);

            Bitmap bitmap = font.bitmap;
            //Load the data from are loaded image into virtual memory so it can be read at runtime
            BitmapData bitmap_data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                    ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmap.Width, bitmap.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, bitmap_data.Scan0);
            //Release from memory

            
            float u_step = (float)font.glyphWidth / texWidth;
            float v_step = (float)font.glyphWidth / texWidth;

            //foreach (char idx in text) {
            //    float u = (float)(idx % font.glyphsPerLine) * u_step;
            //    float v = (float)(idx / font.glyphsPerLine) * v_step;
            //
            //    GL.TexSubImage2D(TextureTarget.Texture2D, 0, u, v, font.glyphWidth, font.glyphHeight, OpenTK.Graphics.OpenGL4.PixelFormat.UnsignedInt, bitmap.   )



            bitmap.UnlockBits(bitmap_data);
            //GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, bitmap.Width, bitmap.Height, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, bitmap_data.Scan0);
            //get rid of bitmap object its no longer needed in this method
            GL.BindTexture(TextureTarget.Texture2D, 0);
            bitmap.Dispose();
            }
        }
        
}
