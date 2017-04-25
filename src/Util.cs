using Rhino.Geometry;
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

        //using opencv by Eric                
        // TODO remove redundant processing at *
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

        public static void MarkPoint(ref SceneNode node, OpenTK.Vector3 p, float r, float g, float b)
        {
            Geometry.Geometry geo = new Geometry.PointMarker(p);
            Material.Material m = new Material.SingleColorMaterial(r, g, b, 1);
            SceneNode child = new SceneNode("Point", ref geo, ref m);
            child.transform = new OpenTK.Matrix4(1, 0, 0, p.X, 0, 1, 0, p.Y, 0, 0, 1, p.Z, 0, 0, 0, 1);
            node.add(ref child);
        }

        public static SceneNode MarkPointSN(ref SceneNode node, OpenTK.Vector3 p, float r, float g, float b)
        {
            Geometry.Geometry geo = new Geometry.PointMarker(p);
            Material.Material m = new Material.SingleColorMaterial(r, g, b, 1);
            SceneNode child = new SceneNode("Point", ref geo, ref m);
            child.transform = new OpenTK.Matrix4(1, 0, 0, p.X, 0, 1, 0, p.Y, 0, 0, 1, p.Z, 0, 0, 0, 1);
            node.add(ref child);
            return child;
        }

        public static OpenTK.Vector3 vrToPlatformVector(ref Scene scene, OpenTK.Vector3 v) 
        {
            
            if (scene.vrToRobot.Equals(OpenTK.Matrix4.Identity))
            {
                v = Util.transformVec(Util.mGLToRhino, v);
                //v *= 1000;
            }
            else
            {
                v = Util.transformVec(scene.vrToRobot, v);
                v = Util.transformVec(scene.robotToPlatform, v);
                //platform to rhino
                //v = Util.transformVec(scene.platformRotation.Inverted(), v);
            }
            //v *= 1000;
            return v;
        }

        public static OpenTK.Vector3 vrToPlatformPoint(ref Scene scene, OpenTK.Vector3 p)
        {
            
            if (scene.vrToRobot.Equals(OpenTK.Matrix4.Identity))
            {
                p = Util.transformPoint(Util.mGLToRhino, p);
               // p *= 1000;
            }
            else
            {
                p = Util.transformPoint(scene.vrToRobot, p);
                p = Util.transformPoint(scene.robotToPlatform, p);
                //platform to rhino
                //p = Util.transformPoint(scene.platformRotation.Inverted(), p);
            }

            //p *= 1000;
            return p;
        }

        public static OpenTK.Matrix4 platformToVR(ref Scene scene)
        {

            OpenTK.Matrix4 m = OpenTK.Matrix4.Identity; //OpenTK.Matrix4.CreateScale(0.001f);
            if (scene.vrToRobot.Equals(OpenTK.Matrix4.Identity))
            {
                m = OpenTK.Matrix4.CreateScale(0.001f);
                m = Util.mRhinoToGL * m;
            }
            else
            {
                m = scene.vrToRobot.Inverted() * scene.robotToPlatform.Inverted() * m;
                //rhino to platform
                //m = scene.vrToRobot.Inverted() * scene.robotToPlatform.Inverted() * scene.platformRotation * m;
            }


            return m;
        }

        public static OpenTK.Vector3 platformToVRPoint(ref Scene scene, OpenTK.Vector3 p)
        {
            //p /= 1000;
            if (scene.vrToRobot.Equals(OpenTK.Matrix4.Identity))
            {
                p /= 1000;
                p = Util.transformPoint(Util.mRhinoToGL, p);
            }
            else
            {
                //rhino to platform
                //p = Util.transformPoint(scene.platformRotation, p);
                p = Util.transformPoint(scene.robotToPlatform.Inverted(), p);
                p = Util.transformPoint(scene.vrToRobot.Inverted(), p);
            }

            
            return p;
        }

        public static OpenTK.Vector3 platformToVRVec(ref Scene scene, OpenTK.Vector3 v)
        {
            //v /= 1000;
            if (scene.vrToRobot.Equals(OpenTK.Matrix4.Identity))
            {
                v /= 1000;
                v = Util.transformPoint(Util.mRhinoToGL, v);
            }
            else
            {
                //rhino to platform
                //v = Util.transformVec(scene.platformRotation, v);
                v = Util.transformVec(scene.robotToPlatform.Inverted(), v);
                v = Util.transformVec(scene.vrToRobot.Inverted(), v);
            }

           
            return v;
        }

        public static Rhino.Geometry.Vector3f openTkToRhinoVector(OpenTK.Vector3 v)
        {
            return new Rhino.Geometry.Vector3f(v.X, v.Y, v.Z);
        }

        public static Rhino.Geometry.Point3f openTkToRhinoPoint(OpenTK.Vector3 p)
        {
            return new Rhino.Geometry.Point3f(p.X, p.Y, p.Z);
        }

        public static OpenTK.Vector3 transformPoint(OpenTK.Matrix4 M, OpenTK.Vector3 p)
        {
            OpenTK.Vector4 homogenousPoint = new OpenTK.Vector4(p.X, p.Y, p.Z, 1);
            homogenousPoint = M * homogenousPoint;
            return new OpenTK.Vector3(homogenousPoint.X, homogenousPoint.Y, homogenousPoint.Z);
        }

        public static OpenTK.Vector3 transformVec(OpenTK.Matrix4 M, OpenTK.Vector3 p)
        {
            OpenTK.Vector4 homogenousVec = new OpenTK.Vector4(p.X, p.Y, p.Z, 0);
            homogenousVec = M * homogenousVec;
            return new OpenTK.Vector3(homogenousVec.X, homogenousVec.Y, homogenousVec.Z);
        }

        public static OpenTK.Matrix4 getTransformInVR(OpenTK.Matrix4 m)
        {
            OpenTK.Matrix4 sm = OpenTK.Matrix4.CreateScale(1000f);
            return m = sm.Inverted() * m * sm;
        }

        public static Transform OpenTKToRhinoTransform(OpenTK.Matrix4 M)
        {
            Transform t = new Transform();
            t.M00 = M.M11;
            t.M01 = M.M12;
            t.M02 = M.M13;
            t.M03 = M.M14;
            t.M10 = M.M21;
            t.M11 = M.M22;
            t.M12 = M.M23;
            t.M13 = M.M24;
            t.M20 = M.M31;
            t.M21 = M.M32;
            t.M22 = M.M33;
            t.M23 = M.M34;
            t.M30 = M.M41;
            t.M31 = M.M42;
            t.M32 = M.M43;
            t.M33 = M.M44;
            return t;
        }

        public static OpenTK.Matrix4 rhinoToOpenTKTransform(Transform t)
        {
            OpenTK.Matrix4 M = new OpenTK.Matrix4();
            M.M11 = (float) t.M00; 
            M.M12 = (float) t.M01; 
            M.M13 = (float) t.M02; 
            M.M14 = (float) t.M03; 
            M.M21 = (float) t.M10; 
            M.M22 = (float) t.M11; 
            M.M23 = (float) t.M12; 
            M.M24 = (float) t.M13; 
            M.M31 = (float) t.M20; 
            M.M32 = (float) t.M21; 
            M.M33 = (float) t.M22; 
            M.M34 = (float) t.M23; 
            M.M41 = (float) t.M30; 
            M.M42 = (float) t.M31; 
            M.M43 = (float) t.M32;
            M.M44 = (float) t.M33; 
            return M;
        }

        public enum OculusButtonId
        {
            k_EButton_Oculus_Trigger = 33, k_EButton_Oculus_Stick = 32, k_EButton_Oculus_BY = 1, k_EButton_Oculus_AX = 7,
            k_EButton_Oculus_Grip = 34
        };


        //Quick test about Douglas-Peucker for rhino points, return point3d with rhino coordinate system
        public static List<OpenTK.Vector3> DouglasPeucker(ref List<OpenTK.Vector3> points, int startIndex, int lastIndex, float epsilon)
        {
            float dmax = 0f;
            int index = startIndex;

            for (int i = index + 1; i < lastIndex; ++i)
            {
                float d = PointLineDistance(points[i], points[startIndex], points[lastIndex]);
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

        public static float PointLineDistance(OpenTK.Vector3 point, OpenTK.Vector3 start, OpenTK.Vector3 end)
        {

            if (start == end)
            {
                return (float)Math.Sqrt(Math.Pow(point.X - start.X, 2) + Math.Pow(point.Y - start.Y, 2) + Math.Pow(point.Z - start.Z, 2));
            }

            OpenTK.Vector3 u = new OpenTK.Vector3(end.X - start.X, end.Y - start.Y, end.Z - start.Z);
            OpenTK.Vector3 pq = new OpenTK.Vector3(point.X - start.X, point.Y - start.Y, point.Z - start.Z);

            return OpenTK.Vector3.Cross(pq, u).Length / u.Length;
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
       
            float [] mGeometry = new float[3 * 3 * mesh.mNumPrimitives];
            int[] mGeometryIndices = new int[3 * mesh.mNumPrimitives];
            float[] mNormals = new float[3 * 3 * mesh.mNumPrimitives];

            OpenTK.Vector3[] faceVertices = new OpenTK.Vector3[3];
            for (int f = 0; f < mesh.mNumPrimitives; f++)
            {
                for (int v = 0; v < 3; v++)
                {
                    mGeometry[9 * f + 3 * v + 0] = mesh.mGeometry[3*mesh.mGeometryIndices[3 * f + v] + 0];
                    mGeometry[9 * f + 3 * v + 1] = mesh.mGeometry[3*mesh.mGeometryIndices[3 * f + v] + 1];
                    mGeometry[9 * f + 3 * v + 2] = mesh.mGeometry[3*mesh.mGeometryIndices[3 * f + v] + 2];
                    faceVertices[v] = new OpenTK.Vector3(mGeometry[9 * f + 3 * v + 0], mGeometry[9 * f + 3 * v + 1], mGeometry[9 * f + 3 * v + 2]);
                }
                OpenTK.Vector3 n = Util.calculateFaceNormal(faceVertices[0], faceVertices[1], faceVertices[2]);
                for (int v = 0; v < 3; v++) {
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


        //Interaction Utility Functions by Eric
        public static OpenTK.Matrix4 mRhinoToGL = new OpenTK.Matrix4(1, 0, 0, 0,
                                                              0, 0, 1, 0,
                                                              0, -1, 0, 0,
                                                              0, 0, 0, 1);

        public static OpenTK.Matrix4 mGLToRhino = new OpenTK.Matrix4(1, 0, 0, 0,
                                                              0, 0, -1, 0,
                                                              0, 1, 0, 0,
                                                              0, 0, 0, 1);
        //x_robot = -z_gl, y_robot = X_gl, Z_robot = -Y_gl
        public static OpenTK.Matrix4 mRobotToGL = new OpenTK.Matrix4(0, 1, 0, 0,
                                                             0, 0, -1, 0,
                                                             -1, 0, 0, 0,
                                                             0, 0, 0, 1);

        public static Guid addSceneNode(ref Scene mScene, Brep brep, ref Material.Material mesh_m)
        {
            //TODO: detect the # of faces
            Mesh base_mesh = new Mesh();
            if (brep != null)
            {
                Mesh[] meshes = Mesh.CreateFromBrep(brep, MeshingParameters.Default);

                foreach (Mesh mesh in meshes)
                    base_mesh.Append(mesh);

                Rhino.DocObjects.ObjectAttributes attr = new Rhino.DocObjects.ObjectAttributes();
                attr.Name = "brepMesh";
                Guid guid = mScene.rhinoDoc.Objects.AddBrep(brep, attr);
                mScene.rhinoDoc.Views.Redraw();

                Geometry.Geometry meshStroke_g = new Geometry.RhinoMesh(ref mScene);
                
                ((Geometry.RhinoMesh)meshStroke_g).setMesh(ref base_mesh);
                
                SceneNode ccMeshSN = new SceneNode("brepMesh", ref meshStroke_g, ref mesh_m);            
                mScene.tableGeometry.add(ref ccMeshSN);
                
                //add reference SceneNode to brep and vice versa
                mScene.brepToSceneNodeDic.Add(guid, ccMeshSN);
                mScene.SceneNodeToBrepDic.Add(ccMeshSN.guid, mScene.rhinoDoc.Objects.Find(guid));

                return guid;
            }else
            {
                return Guid.Empty;
            }
        }

        public static Guid addSceneNode(ref Scene mScene, Brep brep, ref Material.Material mesh_m, string name)
        {
            //TODO: detect the # of faces
            Mesh base_mesh = new Mesh();
            if (brep != null)
            {
                Mesh[] meshes = Mesh.CreateFromBrep(brep, MeshingParameters.Default);

                foreach (Mesh mesh in meshes)
                    base_mesh.Append(mesh);

                long ticks = DateTime.Now.Ticks;
                byte[] bytes = BitConverter.GetBytes(ticks);
                string timeuid = Convert.ToBase64String(bytes).Replace('+', '_').Replace('/', '-').TrimEnd('=');

                Rhino.DocObjects.ObjectAttributes attr = new Rhino.DocObjects.ObjectAttributes();
                attr.Name = name + timeuid;
                Guid guid = mScene.rhinoDoc.Objects.AddBrep(brep, attr);
                mScene.rhinoDoc.Views.Redraw();

                Geometry.Geometry meshStroke_g = new Geometry.RhinoMesh(ref mScene);

                ((Geometry.RhinoMesh)meshStroke_g).setMesh(ref base_mesh);

                SceneNode ccMeshSN = new SceneNode(name, ref meshStroke_g, ref mesh_m);
                mScene.tableGeometry.add(ref ccMeshSN);

                //add reference SceneNode to brep and vice versa
                mScene.brepToSceneNodeDic.Add(guid, ccMeshSN);
                mScene.SceneNodeToBrepDic.Add(ccMeshSN.guid, mScene.rhinoDoc.Objects.Find(guid));

                return guid;

            }else
            {
                return Guid.Empty;
            }
        }

        public static Guid addSceneNode(ref Scene mScene, Brep brep, ref Material.Material mesh_m, string name, Transform t, bool renderLate = false)
        {
            //TODO: detect the # of faces
            Mesh base_mesh = new Mesh();
            if (brep != null)
            {
                Mesh[] meshes = Mesh.CreateFromBrep(brep, MeshingParameters.Default);

                foreach (Mesh mesh in meshes)
                    base_mesh.Append(mesh);
                Rhino.DocObjects.ObjectAttributes attr = new Rhino.DocObjects.ObjectAttributes();
                attr.Name = name;
                Guid guid = mScene.rhinoDoc.Objects.AddBrep(brep, attr);
                //add name attribute for printing
                //mScene.rhinoDoc.Objects.Find(guid).Attributes.Name = "a" + guid.ToString();
                //mScene.rhinoDoc.Objects.Find(guid).CommitChanges();
                mScene.rhinoDoc.Views.Redraw();

                Geometry.Geometry meshStroke_g = new Geometry.RhinoMesh2(ref mScene, t);

                ((Geometry.RhinoMesh2)meshStroke_g).setMesh(ref base_mesh);

                SceneNode ccMeshSN = new SceneNode(name, ref meshStroke_g, ref mesh_m,renderLate);
                mScene.tableGeometry.add(ref ccMeshSN);

                //add reference SceneNode to brep and vice versa
                mScene.brepToSceneNodeDic.Add(guid, ccMeshSN);
                mScene.SceneNodeToBrepDic.Add(ccMeshSN.guid, mScene.rhinoDoc.Objects.Find(guid));

                return guid;

            }
            else
            {
                return Guid.Empty;
            }
        }

        public static void removeSceneNode(ref Scene mScene, Guid guid)
        {
            SceneNode deleteSN = mScene.brepToSceneNodeDic[guid];
            mScene.brepToSceneNodeDic.Remove(guid);
            mScene.SceneNodeToBrepDic.Remove(deleteSN.guid);

            mScene.rhinoDoc.Objects.Delete(guid, true);
            foreach (SceneNode sn in mScene.tableGeometry.children)
            {
                if (sn.guid == deleteSN.guid)
                {
                    mScene.tableGeometry.children.Remove(sn);
                    break;
                }
            }

        }

        public static OpenTK.Matrix4 getCoordinateTransM(OpenTK.Vector3 startO, OpenTK.Vector3 targetO, OpenTK.Vector3 normal1, OpenTK.Vector3 normal2)
        {
            //translation
            OpenTK.Matrix4 transToOrigin = new OpenTK.Matrix4();
            OpenTK.Matrix4.CreateTranslation(-startO.X, -startO.Y, -startO.Z, out transToOrigin);
            transToOrigin.Transpose();

            OpenTK.Matrix4 transToTarget = new OpenTK.Matrix4();
            OpenTK.Matrix4.CreateTranslation(targetO.X, targetO.Y, targetO.Z, out transToTarget);
            transToTarget.Transpose();

            //rotation
            OpenTK.Vector3 rotation_axis = OpenTK.Vector3.Cross(normal1, normal2);
            rotation_axis.Normalize();
            float rotation_angles = OpenTK.Vector3.CalculateAngle(normal1, normal2);
            OpenTK.Matrix4 rotM = new OpenTK.Matrix4();
            OpenTK.Matrix4.CreateFromAxisAngle(rotation_axis, rotation_angles, out rotM);
            rotM.Transpose();

            return transToTarget * rotM * transToOrigin;
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
            var A = MathNet.Numerics.LinearAlgebra.CreateMatrix.Dense<float>(matrices.Count*(matrices.Count), 3);
            var B = MathNet.Numerics.LinearAlgebra.CreateMatrix.Dense<float>(matrices.Count * (matrices.Count), 1);
            int row = 0;
            for (int i = 0; i < matrices.Count-1; i++)
                {
                    A.SetRow(row, new float[] {matrices.ElementAt(i).M11 - matrices.ElementAt(i+1).M11,
                                               matrices.ElementAt(i).M12 - matrices.ElementAt(i+1).M12,
                                               matrices.ElementAt(i).M13 - matrices.ElementAt(i+1).M13});

                    A.SetRow(row+1, new float[] {matrices.ElementAt(i).M21 - matrices.ElementAt(i+1).M21,
                                                 matrices.ElementAt(i).M22 - matrices.ElementAt(i+1).M22,
                                                 matrices.ElementAt(i).M23 - matrices.ElementAt(i+1).M23});

                    A.SetRow(row+2, new float[] {matrices.ElementAt(i).M31 - matrices.ElementAt(i+1).M31,
                                                 matrices.ElementAt(i).M32 - matrices.ElementAt(i+1).M32, 
                                                 matrices.ElementAt(i).M33 - matrices.ElementAt(i+1).M33});

                    B.SetRow(row, new float[] { matrices.ElementAt(i+1).M14 - matrices.ElementAt(i).M14 });
                    B.SetRow(row+1, new float[] { matrices.ElementAt(i+1).M24 - matrices.ElementAt(i).M24 });
                    B.SetRow(row+2, new float[] { matrices.ElementAt(i+1).M34 - matrices.ElementAt(i).M34 });
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

        public static OpenTK.Matrix4 getControllerTipPosition(ref Scene scene, bool left)
        {
            if (left)
            {
                return scene.mDevicePose[scene.leftControllerIdx] * scene.mLeftControllerOffset;
            }
            return scene.mDevicePose[scene.rightControllerIdx] * scene.mRightControllerOffset;
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
		public static int getDominantRotationAxis(OpenTK.Matrix4 M) {
            OpenTK.Quaternion R = M.ExtractRotation();
            if (Math.Abs(R.X) > Math.Abs(R.Y) && Math.Abs(R.X) > Math.Abs(R.Z))
                return 0;
            else if (Math.Abs(R.Y) > Math.Abs(R.Z))
                return 1;
            return 2;
		}




    }

    


    //public static OpenTK.Matrix4
}
