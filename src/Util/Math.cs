using System.Collections.Generic;
using System.Linq;


namespace SparrowHawk.Util
{
    public static class Math
    {
        /// <summary>
        /// Get the translational component of the given Matrix4 as a Vector3
        /// </summary>
        /// <param name="M"></param>
        /// <returns></returns>
        public static OpenTK.Vector3 GetTranslationVector3(OpenTK.Matrix4 M)
        {
            OpenTK.Vector4 output = M * new OpenTK.Vector4(0, 0, 0, 1);
            return new OpenTK.Vector3(output.X, output.Y, output.Z);
        }

        public static void solveForAffineTransformOpenCV(List<OpenTK.Vector3> xs, List<OpenTK.Vector3> bs, ref OpenTK.Matrix4 M)
        {
            if (xs.Count < 4)
                return;

            List<Emgu.CV.Structure.MCvPoint3D32f> OpenCvXs = new List<Emgu.CV.Structure.MCvPoint3D32f>();
            List<Emgu.CV.Structure.MCvPoint3D32f> OpenCvBs = new List<Emgu.CV.Structure.MCvPoint3D32f>();

            //* STAR can replace with OpenTK to array
            foreach (OpenTK.Vector3 x in xs)
                OpenCvXs.Add(new Emgu.CV.Structure.MCvPoint3D32f(x.X, x.Y, x.Z));

            foreach (OpenTK.Vector3 b in bs)
                OpenCvBs.Add(new Emgu.CV.Structure.MCvPoint3D32f(b.X, b.Y, b.Z));

            byte[] inliers;
            Emgu.CV.Matrix<double> OpenCvM;
            Emgu.CV.CvInvoke.EstimateAffine3D(OpenCvXs.ToArray(), OpenCvBs.ToArray(), out OpenCvM, out inliers, 7, 0.95);

            M = new OpenTK.Matrix4(
                (float)OpenCvM[0, 0], (float)OpenCvM[0, 1], (float)OpenCvM[0, 2], (float)OpenCvM[0, 3],
                (float)OpenCvM[1, 0], (float)OpenCvM[1, 1], (float)OpenCvM[1, 2], (float)OpenCvM[1, 3],
                (float)OpenCvM[2, 0], (float)OpenCvM[2, 1], (float)OpenCvM[2, 2], (float)OpenCvM[2, 3],
                0, 0, 0, 1
            );
        }

        /// <summary>
        /// WARNING: Non-functional
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
                A.SetRow(3 * i + 1, new float[] { 0, 0, 0, 0, x.X, x.Y, x.Z, 1, 0, 0, 0, 0 });
                A.SetRow(3 * i + 2, new float[] { 0, 0, 0, 0, 0, 0, 0, 0, x.X, x.Y, x.Z, 1 });
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
                normalized[0, 0] = MVec[0, 0];
                normalized[0, 1] = MVec[1, 0];
                normalized[0, 2] = MVec[2, 0];
                normalized[1, 0] = MVec[4, 0];
                normalized[1, 1] = MVec[5, 0];
                normalized[1, 2] = MVec[6, 0];
                normalized[2, 0] = MVec[8, 0];
                normalized[2, 1] = MVec[9, 0];
                normalized[2, 2] = MVec[10, 0];
                // TODO: If this doesn't work out, try out the built-in Grahm-Schmidt method.
                normalized = normalized.GramSchmidt().Q;
                MVec[0, 0] = normalized[0, 0];
                MVec[1, 0] = normalized[0, 1];
                MVec[2, 0] = normalized[0, 2];
                MVec[4, 0] = normalized[1, 0];
                MVec[5, 0] = normalized[1, 1];
                MVec[6, 0] = normalized[1, 2];
                MVec[8, 0] = normalized[2, 0];
                MVec[9, 0] = normalized[2, 1];
                MVec[10, 0] = normalized[2, 2];
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
            bs.Add(new OpenTK.Vector3(0, 1, 0)); bs.Add(new OpenTK.Vector3(-1, 0, 0)); bs.Add(new OpenTK.Vector3(0, 0, 1)); bs.Add(new OpenTK.Vector3(-2, 2, 2));
            solveForAffineTransform(xs, bs, ref M, false);
            solveForAffineTransform(xs, bs, ref M, true);
        }

        public static List<OpenTK.Vector3> DouglasPeucker(ref List<OpenTK.Vector3> points, int startIndex, int lastIndex, float epsilon)
        {
            float dmax = 0f;
            int index = startIndex;

            for (int i = index + 1; i < lastIndex; ++i)
            {
                float d = UtilOld.PointLineDistance(points[i], points[startIndex], points[lastIndex]);
                if (d > dmax)
                {
                    index = i;
                    dmax = d;
                }
            }

            if (dmax > epsilon)
            {
                List<OpenTK.Vector3> res1 = DouglasPeucker(ref points, startIndex, index, epsilon);
                List<OpenTK.Vector3> res2 = DouglasPeucker(ref points, index, lastIndex, epsilon);

                //watch out the coordinate system
                List<OpenTK.Vector3> finalRes = new List<OpenTK.Vector3>();
                for (int i = 0; i < res1.Count - 1; ++i)
                {
                    finalRes.Add(res1[i]);
                }

                for (int i = 0; i < res2.Count; ++i)
                {
                    finalRes.Add(res2[i]);
                }

                return finalRes;
            }
            else
            {
                return new List<OpenTK.Vector3>(new OpenTK.Vector3[] { points[startIndex], points[lastIndex] });
            }
        }

        public static OpenTK.Vector3 calculateFaceNormal(float v0x, float v0y, float v0z, float v1x, float v1y, float v1z, float v2x, float v2y, float v2z)
        {
            OpenTK.Vector3 p = new OpenTK.Vector3(v1x - v0x, v1y - v0y, v1z - v0z);
            OpenTK.Vector3 q = new OpenTK.Vector3(v2x - v0x, v2y - v0y, v2z - v0z);
            return OpenTK.Vector3.Cross(p, q).Normalized();
        }

        public static OpenTK.Vector3 calculateFaceNormal(OpenTK.Vector3 v0, OpenTK.Vector3 v1, OpenTK.Vector3 v2)
        {
            return OpenTK.Vector3.Cross(v1 - v0, v2 - v0).Normalized();
        }

        /// <summary>
        /// Adds normals to the mesh appropriate for flat shading. 
        /// Note: depending on the mesh, this may increase the number of 
        /// vertices by a factor of three!
        /// Must be a triangulated mesh
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns></returns>
        public static void addNormalsToMesh(Geometry.Geometry mesh)
        {
            if (mesh.primitiveType != OpenTK.Graphics.OpenGL4.BeginMode.Triangles)
                return;

            float[] mGeometry = new float[3 * 3 * mesh.mNumPrimitives];
            int[] mGeometryIndices = new int[3 * mesh.mNumPrimitives];
            float[] mNormals = new float[3 * 3 * mesh.mNumPrimitives];

            OpenTK.Vector3[] faceVertices = new OpenTK.Vector3[3];
            for (int f = 0; f < mesh.mNumPrimitives; f++)
            {
                for (int v = 0; v < 3; v++)
                {
                    mGeometry[9 * f + 3 * v + 0] = mesh.mGeometry[3 * mesh.mGeometryIndices[3 * f + v] + 0];
                    mGeometry[9 * f + 3 * v + 1] = mesh.mGeometry[3 * mesh.mGeometryIndices[3 * f + v] + 1];
                    mGeometry[9 * f + 3 * v + 2] = mesh.mGeometry[3 * mesh.mGeometryIndices[3 * f + v] + 2];
                    faceVertices[v] = new OpenTK.Vector3(mGeometry[9 * f + 3 * v + 0], mGeometry[9 * f + 3 * v + 1], mGeometry[9 * f + 3 * v + 2]);
                }
                OpenTK.Vector3 n = calculateFaceNormal(faceVertices[0], faceVertices[1], faceVertices[2]);
                for (int v = 0; v < 3; v++)
                {
                    mNormals[9 * f + 3 * v + 0] = n.X;
                    mNormals[9 * f + 3 * v + 1] = n.Y;
                    mNormals[9 * f + 3 * v + 2] = n.Z;
                }
            }

            for (int i = 0; i < mGeometryIndices.Count(); i++)
                mGeometryIndices[i] = i;

            mesh.mGeometry = mGeometry;
            mesh.mGeometryIndices = mGeometryIndices;
            mesh.mNormals = mNormals;
        }

        /// <summary>
        /// Finds the homogenous 3d point x (with x_4 = 1) to minimize the least squares
        /// error of M_1x - M_2x = 1; Assumes that each M_i is affine4x4.
        /// </summary>
        /// <param name="matrices"></param>
        /// <returns></returns>
        public static OpenTK.Vector3 solveForOffsetVector(List<OpenTK.Matrix4> matrices)
        {
            OpenTK.Vector3 x = new OpenTK.Vector3();
            var A = MathNet.Numerics.LinearAlgebra.CreateMatrix.Dense<float>(matrices.Count * (matrices.Count), 3);
            var B = MathNet.Numerics.LinearAlgebra.CreateMatrix.Dense<float>(matrices.Count * (matrices.Count), 1);
            int row = 0;
            for (int i = 0; i < matrices.Count - 1; i++)
            {
                A.SetRow(row, new float[] {matrices.ElementAt(i).M11 - matrices.ElementAt(i+1).M11,
                        matrices.ElementAt(i).M12 - matrices.ElementAt(i+1).M12,
                        matrices.ElementAt(i).M13 - matrices.ElementAt(i+1).M13});

                A.SetRow(row + 1, new float[] {matrices.ElementAt(i).M21 - matrices.ElementAt(i+1).M21,
                        matrices.ElementAt(i).M22 - matrices.ElementAt(i+1).M22,
                        matrices.ElementAt(i).M23 - matrices.ElementAt(i+1).M23});

                A.SetRow(row + 2, new float[] {matrices.ElementAt(i).M31 - matrices.ElementAt(i+1).M31,
                        matrices.ElementAt(i).M32 - matrices.ElementAt(i+1).M32,
                        matrices.ElementAt(i).M33 - matrices.ElementAt(i+1).M33});

                B.SetRow(row, new float[] { matrices.ElementAt(i + 1).M14 - matrices.ElementAt(i).M14 });
                B.SetRow(row + 1, new float[] { matrices.ElementAt(i + 1).M24 - matrices.ElementAt(i).M24 });
                B.SetRow(row + 2, new float[] { matrices.ElementAt(i + 1).M34 - matrices.ElementAt(i).M34 });
                row += 3;
            }
            var qr = A.QR();
            var matX = qr.R.Solve(qr.Q.Transpose() * B);
            x.X = matX[0, 0];
            x.Y = matX[1, 0];
            x.Z = matX[2, 0];
            return x;
        }

        public static OpenTK.Matrix4 createTranslationMatrix(float x, float y, float z)
        {
            return new OpenTK.Matrix4(1, 0, 0, x, 0, 1, 0, y, 0, 0, 1, z, 0, 0, 0, 1);
        }

        /// <summary>
        /// Returns the integer associated with the dominant axis about which the rotation moves.
        /// Chooses Z in the two degenerate cases.
        /// x = 0
        /// y = 1
        /// z = 2
        /// Doesn't work.
        /// </summary>
        /// <param name="M">The affine matrix with some rotational componenet to analyse.</param>
        /// <returns></returns>
        public static int getDominantRotationAxis(OpenTK.Matrix4 M)
        {
            OpenTK.Quaternion R = M.ExtractRotation();
            if (System.Math.Abs(R.X) > System.Math.Abs(R.Y) && System.Math.Abs(R.X) > System.Math.Abs(R.Z))
                return 0;
            else if (System.Math.Abs(R.Y) > System.Math.Abs(R.Z))
                return 1;
            return 2;
        }
    }
}

