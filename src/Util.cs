using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace SparrowHawk
{



    public static class Util
    {

        // Directly from OpenVr's openGL starter code.
        public static string GetTrackedDeviceString(ref Valve.VR.CVRSystem Hmd, uint unDevice, Valve.VR.ETrackedDeviceProperty prop)
        {
            Valve.VR.ETrackedPropertyError eError = Valve.VR.ETrackedPropertyError.TrackedProp_Success;
            uint unRequiredBufferLen = Hmd.GetStringTrackedDeviceProperty(unDevice, prop, null, 0, ref eError);
            if (unRequiredBufferLen == 0)
                return "";
            System.Text.StringBuilder pchBuffer = new System.Text.StringBuilder();
            unRequiredBufferLen = Hmd.GetStringTrackedDeviceProperty(unDevice, prop, pchBuffer, unRequiredBufferLen, ref eError);
            return pchBuffer.ToString();
        }

        /// <summary>
        /// Convert a steamVr native matrix to an OpenTK matrix for rendering.
        /// </summary>
        /// <param name="M"></param>
        /// <returns></returns>
        public static OpenTK.Matrix4 steamVRMatrixToMatrix4(Valve.VR.HmdMatrix44_t M)
        {
            OpenTK.Matrix4 output = new OpenTK.Matrix4(M.m0, M.m1, M.m2, M.m3,
                                      M.m4, M.m5, M.m6, M.m7,
                                      M.m8, M.m9, M.m10, M.m11,
                                      M.m12, M.m13, M.m14, M.m15);
            return output;

        }

        /// <summary>
        /// Convert a steamVr native matrix to an OpenTK matrix for rendering.
        /// </summary>
        /// <param name="M"></param>
        /// <returns></returns>
        public static OpenTK.Matrix4 steamVRMatrixToMatrix4(Valve.VR.HmdMatrix34_t M)
        {

            OpenTK.Matrix4 output = new OpenTK.Matrix4(M.m0, M.m1, M.m2, M.m3,
                                      M.m4, M.m5, M.m6, M.m7,
                                      M.m8, M.m9, M.m10, M.m11,
                                         0, 0, 0, 1);
            return output;
        }

        /// <summary>
        /// Get the translational component of the given Matrix4 as a Vector3
        /// </summary>
        /// <param name="M"></param>
        /// <returns></returns>
        public static OpenTK.Vector3 getTranslationVector3(OpenTK.Matrix4 M)
        {
            OpenTK.Vector4 output = M * new OpenTK.Vector4(0, 0, 0, 1);
            return new OpenTK.Vector3(output.X, output.Y, output.Z);
        }

        /// <summary>
        /// Find the matrix M that gives the best mapping Mx_i = b_i for all pairs
        /// of vectors (x_i, b_i).
        /// </summary>
        /// <param name="x">Untransformed vectors</param>
        /// <param name="b">Transformed vectors</param>
        /// <param name="getOrthogonal">If true, ensure that M has orthonormal columns.</param>
        /// <returns></returns>
        public static bool solveForAffineTransform(List<OpenTK.Vector3> xs, List<OpenTK.Vector3> bs, ref OpenTK.Matrix4 M, bool getOrthogonal = true)
        {
            // Guard, and then build matrices
            int n = xs.Count();
            if (n < 4)
                return false;

            MathNet.Numerics.LinearAlgebra.Matrix<float> A = MathNet.Numerics.LinearAlgebra.CreateMatrix.Dense<float>(3 * n, 12);
            MathNet.Numerics.LinearAlgebra.Matrix<float> B = MathNet.Numerics.LinearAlgebra.CreateMatrix.Dense<float>(3 * n, 1);
            MathNet.Numerics.LinearAlgebra.Matrix<float> MVec = MathNet.Numerics.LinearAlgebra.CreateMatrix.Dense<float>(12, 1);

            // Fill up A and B
            int i = 0;
            foreach (OpenTK.Vector3 x in xs)
            {
                A.SetRow(3 * i + 0, new float[] { x.X, x.Y, x.Z, 1, 0, 0, 0, 0, 0, 0, 0, 0 });
                A.SetRow(3 * i + 1, new float[] { 0, 0, 0, 0, x.X, x.Y, x.Z, 1, 0, 0, 0, 0});
                A.SetRow(3 * i + 2, new float[] { 0, 0, 0, 0, 0, 0, 0, 0, x.X, x.Y, x.Z, 1});
                i++;
            }
            i = 0;
            foreach (OpenTK.Vector3 b in bs)
            {
                B.SetRow(3 * i + 0, new float[] { b.X });
                B.SetRow(3 * i + 1, new float[] { b.Y });
                B.SetRow(3 * i + 2, new float[] { b.Z });
                i++;
            }

            // Solve for M
            var qr = A.QR();
            MVec = qr.R.Solve(qr.Q.TransposeThisAndMultiply(B));

            // Orthogonalize if desired
            if (getOrthogonal)
            {
                var normalized = MathNet.Numerics.LinearAlgebra.CreateMatrix.Dense<float>(3, 3);
                normalized[0, 0] = MVec[0,0];
                normalized[0, 1] = MVec[1,0];
                normalized[0, 2] = MVec[2,0];
                normalized[1, 0] = MVec[4,0];
                normalized[1, 1] = MVec[5,0];
                normalized[1, 2] = MVec[6,0];
                normalized[2, 0] = MVec[8,0];
                normalized[2, 1] = MVec[9,0];
                normalized[2, 2] = MVec[10,0];
                // TODO: If this doesn't work out, try out the built-in Grahm-Schmidt method.
                normalized = normalized.GramSchmidt().Q;
                MVec[0,0] = normalized[0, 0];
                MVec[1,0] = normalized[0, 1];
                MVec[2,0] = normalized[0, 2];
                MVec[4,0] = normalized[1, 0];
                MVec[5,0] = normalized[1, 1];
                MVec[6,0] = normalized[1, 2];
                MVec[8,0] = normalized[2, 0];
                MVec[9,0] = normalized[2, 1];
                MVec[10,0] = normalized[2, 2];
            }

            for (i = 0; i < 3; i++)
                for (int j = 0; j < 4; j++)
                    M[i, j] = MVec[4 * i + j, 0];
            M[3, 0] = 0; M[3, 1] = 0; M[3, 2] = 0; M[3, 3] = 1;

            return true;
        }

        public static void TestAffineSolve()
        {
            OpenTK.Matrix4 M = new OpenTK.Matrix4();
            //float norm;

            // Test the identity with no translation
            List<OpenTK.Vector3> xs = new List<OpenTK.Vector3>();
            List<OpenTK.Vector3> bs = new List<OpenTK.Vector3>();
            xs.Add(new OpenTK.Vector3(1, 0, 0)); xs.Add(new OpenTK.Vector3(0, 1, 0)); xs.Add(new OpenTK.Vector3(0, 0, 1)); xs.Add(new OpenTK.Vector3(2, 2, 2));
            bs.Add(new OpenTK.Vector3(1, 0, 0)); bs.Add(new OpenTK.Vector3(0, 1, 0)); bs.Add(new OpenTK.Vector3(0, 0, 1)); bs.Add(new OpenTK.Vector3(2, 2, 2));
            solveForAffineTransform(xs, bs, ref M, false);
            // Normalized
            solveForAffineTransform(xs, bs, ref M, true);

            // Identity with a translation
            xs.Clear(); bs.Clear();
            xs.Add(new OpenTK.Vector3(1, 0, 0)); xs.Add(new OpenTK.Vector3(0, 1, 0)); xs.Add(new OpenTK.Vector3(0, 0, 1)); xs.Add(new OpenTK.Vector3(2, 2, 2));
            bs.Add(new OpenTK.Vector3(0, -1, -1)); bs.Add(new OpenTK.Vector3(-1, 0, -1)); bs.Add(new OpenTK.Vector3(-1, -1, 0)); bs.Add(new OpenTK.Vector3(1, 1, 1));
            // Normalized
            solveForAffineTransform(xs, bs, ref M, false);
            solveForAffineTransform(xs, bs, ref M, true);

            // Rotation 
            xs.Clear(); bs.Clear();
            xs.Add(new OpenTK.Vector3(1, 0, 0)); xs.Add(new OpenTK.Vector3(0, 1, 0)); xs.Add(new OpenTK.Vector3(0, 0, 1)); xs.Add(new OpenTK.Vector3(2, 2, 2));
            bs.Add(new OpenTK.Vector3(0,1,0)); bs.Add(new OpenTK.Vector3(-1, 0, 0)); bs.Add(new OpenTK.Vector3(0, 0, 1)); bs.Add(new OpenTK.Vector3(-2, 2, 2));
            solveForAffineTransform(xs, bs, ref M, false);
            solveForAffineTransform(xs, bs, ref M, true);
        }

        public static void MarkPoint(ref SceneNode node, OpenTK.Vector3 p, OpenTK.Graphics.Color4 color)
        {
            Geometry.Geometry g = new Geometry.PointMarker(p);
            Material.Material m = new Material.SingleColorMaterial(color.R, color.G, color.B, color.A);
            SceneNode child = new SceneNode("Point", ref g, ref m);
            child.transform = new OpenTK.Matrix4(1, 0, 0, p.X, 0, 1, 0, p.Y, 0, 0, 1, p.Z, 0, 0, 0, 1);
            node.add(ref child);
        }
    }
}
