using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using LinAlg = MathNet.Numerics.LinearAlgebra;
using SparrowHawk.Renderer;


namespace SparrowHawk.Calibration
{
    public class Spaam
    {
        public static string CalibrationDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory); //'//SpecialFolder.DesktopDirectory);
        public static string CalibrationFilename = "meta_calibration.xml";
        public static string CalibrationPath = CalibrationDir + "\\" + CalibrationFilename;

        private static Geometry.Geometry crosshairs = null;// = new Geometry.Polyline(new float[] {-1,0,1,1,0,1,0,-1,1,0,1,1});
        private static Material.SingleColorMaterial crosshairMaterial = null; // = new Material.SingleColorMaterial(1, 1, 1, 1);
     
        /// <summary>
        /// 
        /// A = GFC 
        /// i.e. P_world = P_mark * T2M * W2T (W2T should be ID)
        /// 
        /// [u,v,w] = G*p^T
        /// x,y = u/w, v/w
        /// 
        /// Based on "SPAAM for optical see-through HMD calibration for AR" by Tuceryan and Navab, 2001
        /// </summary>
        /// <param name="markPoses">The world-to-local transforms for the tracker on the optical HMD</param>
        /// <param name="screenPoints">The pixel coordinates corresponding to the known point in each pose</param>
        /// <param name="knownPoint">The known 3D calibration point in world space</param>
        /// <returns>The 3x4 projection matrix</returns>
        public static Matrix3x4 EstimateProjectionMatrix3x4(List<Matrix4> markPoses, List<Vector2> screenPoints, Vector4 knownPoint)
        {
            /// Print to get a data set
            ///
            Console.WriteLine("======= Begin Data Set =======");
            Console.WriteLine("var poses = new List<Matrix4>();");
            foreach (var p in markPoses)
                Console.WriteLine("poses.Add(new Matrix4({0:G}, {1:G}, {2:G}, {3:G},{4:G}, {5:G}, {6:G}, {7:G},{8:G}, {9:G}, {10:G}, {11:G},{12:G}, {13:G}, {14:G}, {15:G}));", p.M11, p.M12, p.M13, p.M14, p.M21, p.M22, p.M23, p.M24, p.M31, p.M32, p.M33, p.M34, p.M41, p.M42, p.M43, p.M44);
            Console.WriteLine("var screenPoints = new List<Vector2>();");
            foreach (var q in screenPoints)
                Console.WriteLine("screenPoints.Add(new Vector2({0:G}, {1:G}));", q.X, q.Y);
            Console.WriteLine("var knownPoint = new Vector4({0:G}, {1:G}, {2:G}, {3:G}));", knownPoint.X, knownPoint.Y, knownPoint.Z, knownPoint.W);
            Console.WriteLine("======= End Data Set =======");
            ///////////////


            var B = LinAlg.CreateMatrix.Dense<float>(2 * markPoses.Count, 12);
            for (int i = 0; i < markPoses.Count; i++)
            {
                Vector4 p = markPoses[i] * knownPoint;
                Vector2 s = screenPoints[i];
                p /= p.W;
                B.SetRow(2 * i, new float[] { 0,0,0,0, -p.X, -p.Y, -p.Z, -1, s.Y * p.X, s.Y*p.Y, s.Y*p.Z, s.Y});
                B.SetRow(2 * i + 1, new float[] { p.X, p.Y, p.Z, 1, 0,0,0,0, -s.X * p.X, -s.X * p.Y, -s.X * p.Z, -s.X });
            }
            var svd = B.Svd();

            var v = svd.VT.Row(11);
            Matrix3x4 P = new Matrix3x4(v.At(0), v.At(1), v.At(2), v.At(3),
                                        v.At(4), v.At(5), v.At(6), v.At(7),
                                        v.At(8), v.At(9), v.At(10), v.At(11));

            // Negate matrix if it projects point behind camera
            if (Vector4.Dot(P.Row2, markPoses[0] * knownPoint) < 0)
            {
                Console.WriteLine("Reversed");
                P *= -1;
            }
            P *= 1 / P.Row2.Length;

            // Reprojection Error
            float residual = 0;
            for (int i = 0; i < markPoses.Count; i++)
            {
                Vector4 p = markPoses[i] * knownPoint;
                p /= p.W;
                var p_screen = new Vector3(Vector4.Dot(P.Row0, p), Vector4.Dot(P.Row1, p), Vector4.Dot(P.Row2, p));
                p_screen /= p_screen.Z;
                Console.WriteLine(p_screen + " -- " + screenPoints[i]);
                residual += (screenPoints[i] - p_screen.Xy).Length; 
            }
            Console.WriteLine("Reprojection Error across " + markPoses.Count + "points: " + residual);

            return P;
        }

        
        public static Matrix4 ConstructProjectionMatrix4x4(Matrix3x4 proj, float n, float f, int r, int l, int t, int b)
        {
            // Duplicate last row.
            //r = 1; l = -1; t = 1; b = -1; n = .1f; f = 10;
            Matrix4 P = new Matrix4(proj.Row0, proj.Row1, proj.Row2, proj.Row2);
            float normZ = P.Row2.Xyz.Length;
            P = new Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0) * P;
            P.Row2 *= (-f - n);
            P.M34 += f * n * normZ;

            
            var orth = new Matrix4(2/(r-l),       0,       0, (r+l)/(l-r),
                                         0, 2/(t-b),       0, (t+b)/(b-t),
                                         0,       0, 2/(n-f), (f+n)/(n-f),
                                         0,       0,       0,           1);
            var M = orth*P;// * orth;
            //M *= 1/M.M44;
            return M; //* Matrix4.CreateOrthographic(2,2,.1f,10);//CreateOrthographicOffCenter(l, r, b, t, n, f);
        }

        public static void RenderCrosshairs(Vector2 screenPos, Color4 color, FramebufferDesc framebuffer, bool clear=true)
        {
            if (crosshairs == null)
                crosshairs = new Geometry.Polyline(new float[] { -1, 0, 1, 1, 0, 1, 0, -1, 1, 0, 1, 1 });
            if (crosshairMaterial == null)
                crosshairMaterial = new Material.SingleColorMaterial(1, 1, 1, 1);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.renderFramebufferId);
            GL.Viewport(0, 0, framebuffer.Width, framebuffer.Width);
            GL.ClearColor(0.1f,0,0.1f,1);
            if (clear)
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            crosshairs.mGeometry[1] = screenPos.Y;
            crosshairs.mGeometry[4] = screenPos.Y;
            crosshairs.mGeometry[6] = screenPos.X;
            crosshairs.mGeometry[9] = screenPos.X;
            
            var id = Matrix4.Identity;
            crosshairMaterial.draw(ref crosshairs, ref id, ref id);

        }

        public static float CalculateProjectionError(Matrix4 P, List<Matrix4> markPoses, List<Vector2> screenPoints, Vector4 knownPoint)
        {
            Console.WriteLine("P_{4x4} residual calculation");
            float residual = 0;
            for (int i = 0; i < markPoses.Count; i++)
            {
                var p = P * markPoses[i] * knownPoint;
                p /= p.W;
                Console.WriteLine(p + " -- " + screenPoints[i]);
                residual += (p.Xy - screenPoints[i]).Length;
            }

            return residual;
        }
    }

    [Serializable]
    public class MetaTwoCalibrationData
    {
        public Matrix4 leftEyeProjection = Matrix4.Identity;
        public Matrix4 rightEyeProjection = Matrix4.Identity;
    }
}
