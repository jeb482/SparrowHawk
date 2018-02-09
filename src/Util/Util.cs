using OpenTK;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static SparrowHawk.Scene;


namespace SparrowHawk
{
    namespace Util
    {

        public static class Rhino
        {
        }
        public static class Scene
        {
        }
        public static class OpenVr
        {

        }
    }

    public static class UtilOld
    {

        // Directly from OpenVr's openGL starter code.
        public static string GetTrackedDeviceString(ref Valve.VR.CVRSystem Hmd, uint unDevice, Valve.VR.ETrackedDeviceProperty prop)
        {
            if (Hmd == null)
                return "?";
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

        //using opencv by Eric                
        // TODO remove redundant processing at *


        public static void MarkPointVR(ref Scene mScene, OpenTK.Vector3 p, ref Geometry.Geometry geo, ref Material.Material m, out SceneNode pointSN)
        {
            pointSN = new SceneNode("EditPoint", ref geo, ref m);
            pointSN.transform = new OpenTK.Matrix4(1, 0, 0, p.X, 0, 1, 0, p.Y, 0, 0, 1, p.Z, 0, 0, 0, 1);
            UtilOld.addSceneNode(ref mScene, ref pointSN);
        }

        public static SceneNode MarkProjectionPoint(ref Scene mScene, OpenTK.Vector3 p, float r, float g, float b)
        {
            Geometry.Geometry geo = new Geometry.DrawPointMarker(new OpenTK.Vector3(0, 0, 0));
            Material.Material m = new Material.SingleColorMaterial(1, 1, 1, 1);//prject point color
            SceneNode drawPoint = new SceneNode("drawPoint", ref geo, ref m);
            drawPoint.transform = new OpenTK.Matrix4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
            mScene.tableGeometry.add(ref drawPoint);
            return drawPoint;
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
            // p is already rotation inverted.
            Geometry.Geometry geo = new Geometry.DrawPointMarker(p);
            Material.Material m = new Material.SingleColorMaterial(r, g, b, 1);
            SceneNode child = new SceneNode("EditPoint", ref geo, ref m);
            child.transform = new OpenTK.Matrix4(1, 0, 0, p.X, 0, 1, 0, p.Y, 0, 0, 1, p.Z, 0, 0, 0, 1);
            node.add(ref child);
            return child;
        }

        public static void MarkDebugPoint(ref Scene mScene, OpenTK.Vector3 p, float r, float g, float b)
        {
            // p is already rotation inverted.
            Geometry.Geometry geo = new Geometry.DrawPointMarker(p);
            Material.Material m = new Material.SingleColorMaterial(r, g, b, 1);
            SceneNode child = new SceneNode("DebugPoint", ref geo, ref m);
            child.transform = new OpenTK.Matrix4(1, 0, 0, p.X, 0, 1, 0, p.Y, 0, 0, 1, p.Z, 0, 0, 0, 1);
            UtilOld.addSceneNode(ref mScene, ref child);
        }

        public static OpenTK.Vector3 vrToPlatformVector(ref Scene scene, OpenTK.Vector3 v)
        {

            if (scene.vrToRobot.Equals(OpenTK.Matrix4.Identity))
            {
                v = UtilOld.transformVec(UtilOld.mGLToRhino, v);
                //v *= 1000;
            }
            else
            {
                v = UtilOld.transformVec(scene.vrToRobot, v);
                v = UtilOld.transformVec(scene.robotToPlatform, v);
                //platform to rhino
                //v = Util.transformVec(scene.platformRotation, v);
            }
            //v *= 1000;
            return v;
        }

        public static OpenTK.Vector3 vrToPlatformPoint(ref Scene scene, OpenTK.Vector3 p)
        {

            if (scene.vrToRobot.Equals(OpenTK.Matrix4.Identity))
            {
                p = UtilOld.transformPoint(UtilOld.mGLToRhino, p);
                // p *= 1000;
            }
            else
            {
                p = UtilOld.transformPoint(scene.vrToRobot, p);
                p = UtilOld.transformPoint(scene.robotToPlatform, p);
                //platform to rhino
                //p = Util.transformPoint(scene.platformRotation, p);
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
                m = UtilOld.mRhinoToGL * m;
            }
            else
            {

                //rhino to platform
                m = scene.vrToRobot.Inverted() * scene.robotToPlatform.Inverted() * scene.platformRotation.Inverted() * m;
                //m = scene.tableGeometry.transform.Inverted() * scene.vrToRobot.Inverted() * scene.robotToPlatform.Inverted() * m;
            }


            return m;
        }

        public static OpenTK.Vector3 platformToVRPoint(ref Scene scene, OpenTK.Vector3 p)
        {
            //p /= 1000;
            if (scene.vrToRobot.Equals(OpenTK.Matrix4.Identity))
            {
                p /= 1000;
                p = UtilOld.transformPoint(UtilOld.mRhinoToGL, p);
            }
            else
            {
                //rhino to platform
                //p = Util.transformPoint(scene.platformRotation.Inverted(), p);
                p = UtilOld.transformPoint(scene.robotToPlatform.Inverted(), p);
                p = UtilOld.transformPoint(scene.vrToRobot.Inverted(), p);
                //p = Util.transformPoint(scene.tableGeometry.transform.Inverted(), p);
            }


            return p;
        }

        public static OpenTK.Vector3 platformToVRVec(ref Scene scene, OpenTK.Vector3 v)
        {
            //v /= 1000;
            if (scene.vrToRobot.Equals(OpenTK.Matrix4.Identity))
            {
                v /= 1000;
                v = UtilOld.transformPoint(UtilOld.mRhinoToGL, v);
            }
            else
            {
                //rhino to platform
                //v = Util.transformVec(scene.platformRotation.Inverted(), v);
                v = UtilOld.transformVec(scene.robotToPlatform.Inverted(), v);
                v = UtilOld.transformVec(scene.vrToRobot.Inverted(), v);
                //v = Util.transformVec(scene.tableGeometry.transform.Inverted(), v);
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

        public static Vector3 RhinoToOpenTKPoint(Point3d p)
        {
            return new Vector3((float)p.X, (float)p.Y, (float)p.Z);
        }

        public static Vector3 RhinoToOpenTKVector(Rhino.Geometry.Vector3d p)
        {
            return new Vector3((float)p.X, (float)p.Y, (float)p.Z);
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
            M.M11 = (float)t.M00;
            M.M12 = (float)t.M01;
            M.M13 = (float)t.M02;
            M.M14 = (float)t.M03;
            M.M21 = (float)t.M10;
            M.M22 = (float)t.M11;
            M.M23 = (float)t.M12;
            M.M24 = (float)t.M13;
            M.M31 = (float)t.M20;
            M.M32 = (float)t.M21;
            M.M33 = (float)t.M22;
            M.M34 = (float)t.M23;
            M.M41 = (float)t.M30;
            M.M42 = (float)t.M31;
            M.M43 = (float)t.M32;
            M.M44 = (float)t.M33;
            return M;
        }

        public enum OculusButtonId
        {
            k_EButton_Oculus_Trigger = 33, k_EButton_Oculus_Stick = 32, k_EButton_Oculus_BY = 1, k_EButton_Oculus_AX = 7,
            k_EButton_Oculus_Grip = 34
        };


        //Quick test about Douglas-Peucker for rhino points, return point3d with rhino coordinate system

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


        public static void setPlaneAlpha(ref Scene mScene, float alpha)
        {
            mScene.xyPlane.setAlpha(alpha);
            mScene.yzPlane.setAlpha(alpha);
            mScene.xzPlane.setAlpha(alpha);

            if (alpha > 0)
            {
                ((Material.SingleColorMaterial)mScene.xAxis.material).setAlpha(1.0f);
                ((Material.SingleColorMaterial)mScene.yAxis.material).setAlpha(1.0f);
                ((Material.SingleColorMaterial)mScene.zAxis.material).setAlpha(1.0f);
            }
            else
            {
                ((Material.SingleColorMaterial)mScene.xAxis.material).setAlpha(0.0f);
                ((Material.SingleColorMaterial)mScene.yAxis.material).setAlpha(0.0f);
                ((Material.SingleColorMaterial)mScene.zAxis.material).setAlpha(0.0f);
            }
        }

        public static void hideOtherPlanes(ref Scene mScene, string name)
        {
            if (name.Contains("planeXY"))
            {
                mScene.xyPlane.setAlpha(0.4f);
                mScene.yzPlane.setAlpha(0f);
                mScene.xzPlane.setAlpha(0f);
                ((Material.SingleColorMaterial)mScene.xAxis.material).setAlpha(1.0f);
                ((Material.SingleColorMaterial)mScene.yAxis.material).setAlpha(1.0f);
                ((Material.SingleColorMaterial)mScene.zAxis.material).setAlpha(0.0f);

            }
            else if (name.Contains("planeYZ"))
            {
                mScene.xyPlane.setAlpha(0f);
                mScene.yzPlane.setAlpha(0.4f);
                mScene.xzPlane.setAlpha(0f);
                ((Material.SingleColorMaterial)mScene.xAxis.material).setAlpha(0.0f);
                ((Material.SingleColorMaterial)mScene.yAxis.material).setAlpha(1.0f);
                ((Material.SingleColorMaterial)mScene.zAxis.material).setAlpha(1.0f);
            }
            else if (name.Contains("planeXZ"))
            {
                mScene.xyPlane.setAlpha(0f);
                mScene.yzPlane.setAlpha(0f);
                mScene.xzPlane.setAlpha(0.4f);
                ((Material.SingleColorMaterial)mScene.xAxis.material).setAlpha(1.0f);
                ((Material.SingleColorMaterial)mScene.yAxis.material).setAlpha(0.0f);
                ((Material.SingleColorMaterial)mScene.zAxis.material).setAlpha(1.0f);
            }
            else if (name.Contains("all"))
            {
                mScene.xyPlane.setAlpha(0f);
                mScene.yzPlane.setAlpha(0f);
                mScene.xzPlane.setAlpha(0f);
                ((Material.SingleColorMaterial)mScene.xAxis.material).setAlpha(0.0f);
                ((Material.SingleColorMaterial)mScene.yAxis.material).setAlpha(0.0f);
                ((Material.SingleColorMaterial)mScene.zAxis.material).setAlpha(0.0f);
            }
        }

        public static int snapToPoints(ref Point3d projectP, ref List<Point3d> pointsList, List<int> ignoreIndexes = null)
        {
            if (pointsList.Count == 0)
            {
                return -1;
            }

            bool snap = false;
            float minD = 1000000f;
            int minIndex = -1;

            for (int i = 0; i < pointsList.Count; i++)
            {
                if (ignoreIndexes != null && ignoreIndexes.Contains(i))
                {
                    continue;
                }

                float distance = (float)Math.Sqrt(Math.Pow(projectP.X - pointsList[i].X, 2) + Math.Pow(projectP.Y - pointsList[i].Y, 2) + Math.Pow(projectP.Z - pointsList[i].Z, 2));
                if (distance < minD && distance < 20) //mm unit
                {
                    snap = true;
                    minIndex = i;
                    minD = distance;
                }
            }

            if (snap)
            {
                projectP = pointsList[minIndex];
                return minIndex;
            }
            else
            {
                return -1;
            }
        }


        public static void rayCasting(Point3d pos, Rhino.Geometry.Vector3d dir, ref List<Rhino.DocObjects.ObjRef> rhinoObjs, ref Point3d projectP, out Guid projectObjID)
        {
            //deal with empty scene or drawing in 3D case         
            if (rhinoObjs.Count == 0)
            {
                projectP = pos;
                projectObjID = Guid.Empty;
                return;
            }

            float minD = 1000000f;
            int minIndex = -1;
            Point3d tmpProjectP = new Point3d();
            Rhino.DocObjects.RhinoObject tmpprojectObj = null;

            Ray3d ray = new Ray3d(pos, dir);
            //project to the closest one
            for (int i = 0; i < rhinoObjs.Count; i++)
            {
                List<GeometryBase> geometries = new List<GeometryBase>();
                //Rhino.DocObjects.ObjRef obj = new Rhino.DocObjects.ObjRef(rhinoObjs[i]);
                geometries.Add(rhinoObjs[i].Object().Geometry);
                Point3d[] rayIntersections = Rhino.Geometry.Intersect.Intersection.RayShoot(ray, geometries, 1);
                if (rayIntersections != null)
                {
                    float distance = (float)Math.Sqrt(Math.Pow(rayIntersections[0].X - pos.X, 2) + Math.Pow(rayIntersections[0].Y - pos.Y, 2) + Math.Pow(rayIntersections[0].Z - pos.Z, 2));
                    if (distance < minD)
                    {
                        minIndex = i;
                        minD = distance;
                        tmpProjectP = rayIntersections[0];
                        tmpprojectObj = rhinoObjs[i].Object();
                    }
                }
            }

            //hit object
            if (tmpprojectObj != null)
            {
                projectP = tmpProjectP;
                projectObjID = tmpprojectObj.Id;
            }
            else
            {
                projectP = projectP; //last projection point position, deal with the case when users draw out  of the surface
                projectObjID = Guid.Empty;
            }

        }

        //add sceneNode with SceneNode data
        public static void addSceneNode(ref Scene mScene, ref SceneNode SN, bool isStatic = false)
        {
            if (!isStatic)
            {
                SN.transform = mScene.tableGeometry.transform.Inverted() * SN.transform;
                mScene.tableGeometry.add(ref SN);
            }
            else
            {
                mScene.staticGeometry.add(ref SN);
            }
        }

        //add sceneNode with Brep data
        public static bool addSceneNode(ref Scene mScene, ref Brep brep, ref Material.Material mesh_m, string name, out SceneNode SN, bool isStatic = false)
        {
            Mesh base_mesh = new Mesh();
            if (brep != null)
            {
                Mesh[] meshes = Mesh.CreateFromBrep(brep, MeshingParameters.Default);

                foreach (Mesh mesh in meshes)
                {
                    base_mesh.Append(mesh);
                }

                Geometry.Geometry meshStroke_g = new Geometry.RhinoMesh(ref mScene);
                ((Geometry.RhinoMesh)meshStroke_g).setMesh(ref base_mesh);

                SceneNode ccMeshSN = new SceneNode(name, ref meshStroke_g, ref mesh_m);
                if (!isStatic)
                {
                    ccMeshSN.transform = mScene.tableGeometry.transform.Inverted() * ccMeshSN.transform;
                    mScene.tableGeometry.add(ref ccMeshSN);
                }
                else
                {
                    mScene.staticGeometry.add(ref ccMeshSN);
                }

                SN = ccMeshSN;

                return true;
            }else
            {
                SN = null;

                return false;
            }
        }

        //update sceneNode with Brep data, create one if necessary, try not use ref for brep, there are some werid bugs
        public static bool updateSceneNode(ref Scene mScene, Brep brep, ref Material.Material mesh_m, string name, ref SceneNode SN, bool isStatic = false)
        {      
            if (brep != null)
            {
                /*
                SceneNode modelSN = null;
                foreach (SceneNode sn in mScene.tableGeometry.children)
                {
                    if (sn.name == name)
                    {
                        modelSN = sn;
                        break;
                    }
                }
                if (modelSN == null)
                {
                    addSceneNode(ref mScene, ref brep, ref mesh_m, name, out modelSN);
                }
                else
                {
                    Mesh base_mesh = new Mesh();
                    Mesh[] meshes = Mesh.CreateFromBrep(brep, MeshingParameters.Default);

                    foreach (Mesh mesh in meshes)
                    {
                        base_mesh.Append(mesh);
                    }

                    Geometry.Geometry meshStroke_g = new Geometry.RhinoMesh(ref mScene);
                    ((Geometry.RhinoMesh)meshStroke_g).setMesh(ref base_mesh);
                    modelSN.geometry = meshStroke_g;
                }
                SN = modelSN;
                return true;
                */
                
                if(SN == null)
                {
                    addSceneNode(ref mScene, ref brep, ref mesh_m, name, out SN);
                }
                else
                {
                    //check if there is one in tableGeometry
                    SceneNode modelSN = null;
                    for (int i = 0; i < mScene.tableGeometry.children.Count; i++)
                    {
                        SceneNode sn = mScene.tableGeometry.children[i];
                        if (sn.guid == SN.guid)
                        {
                            modelSN = sn;
                            Mesh base_mesh = new Mesh();
                            Mesh[] meshes = Mesh.CreateFromBrep(brep, MeshingParameters.Default);

                            foreach (Mesh mesh in meshes)
                            {
                                base_mesh.Append(mesh);
                            }

                            Geometry.Geometry meshStroke_g = new Geometry.RhinoMesh(ref mScene);
                            ((Geometry.RhinoMesh)meshStroke_g).setMesh(ref base_mesh);
                            modelSN.geometry = meshStroke_g;
                            break;
                        }
                    }
                    SN = modelSN;

                }
                return true;
                
            }
            else
            {
                SN = null;
                return false;
            }
            
        }

        //rendering a brep ONLY in Rhino, e.g. create an invisable edit plane
        public static Guid addRhinoObject(ref Scene mScene, ref Brep brep, string name)
        {
            if (brep != null)
            {
                string timeuid = generateUniqueTimeID();

                Rhino.DocObjects.ObjectAttributes attr = new Rhino.DocObjects.ObjectAttributes();
                attr.Name = name + timeuid;
                Guid guid = mScene.rhinoDoc.Objects.AddBrep(brep, attr);
                mScene.rhinoDoc.Views.Redraw();

                return guid;
            }
            else
            {
                return Guid.Empty;
            }
        }

        //render a brep in both Rhino and VR, boolean OP has a strong assumption
        public static Guid addRhinoObjectSceneNode(ref Scene mScene, ref Brep brep, ref Material.Material mesh_m, string name, out SceneNode renderObjSN, bool isStatic = false, bool booleanOP = false)
        {
            //TODO: detect the # of faces
            Mesh base_mesh = new Mesh();
            renderObjSN = null;
            if (brep != null)
            {

                Guid guid = addRhinoObject(ref mScene, ref brep, name);
                Rhino.DocObjects.RhinoObject rhinoObj = mScene.rhinoDoc.Objects.Find(guid);

                if (booleanOP)
                {
                    boolOPwithModel(ref mScene, ref rhinoObj, out brep);
                }

                addSceneNode(ref mScene, ref brep, ref mesh_m, name, out renderObjSN);

                //map reference brep to SceneNode and vice versa 
                if (renderObjSN != null)
                {
                    mScene.BiDictionaryRhinoVR.Add(guid, renderObjSN);
                    return guid;
                }
                else
                {
                    return Guid.Empty;
                }

            }
            else
            {
                return Guid.Empty;
            }
        }

        
        public static void updateRhinoObjectSceneNode(ref Scene mScene, ref Rhino.DocObjects.ObjRef rhinoObjRef, Rhino.Geometry.Transform transM)
        {
            //TODO-WTF https://discourse.mcneel.com/t/continuous-transformation-in-a-loop/37093/6
            Rhino.DocObjects.RhinoObject rhinoObj = rhinoObjRef.Object();
            Point3d c1 = rhinoObjRef.Object().Geometry.GetBoundingBox(true).Center;
            SceneNode updateSN = mScene.BiDictionaryRhinoVR.GetByFirst(rhinoObjRef.ObjectId);           
            mScene.rhinoDoc.Objects.Transform(rhinoObjRef.ObjectId, transM, true);          
            mScene.rhinoDoc.Views.Redraw();

            //debug
            Point3d c2 = rhinoObjRef.Brep().GetBoundingBox(true).Center;
            Matrix4 transMVR =  mScene.vrToRobot.Inverted() * mScene.robotToPlatform.Inverted() * UtilOld.rhinoToOpenTKTransform(transM) * mScene.robotToPlatform * mScene.vrToRobot;
            updateSN.transform = mScene.tableGeometry.transform.Inverted() * transMVR * updateSN.transform;
        }

        
        public static void setSceneNodeTrasnform (ref Scene mScene, ref SceneNode updateSN, Matrix4 transMVR)
        {
            updateSN.transform = mScene.tableGeometry.transform.Inverted() * transMVR * updateSN.transform;
        }

        public static string generateUniqueTimeID()
        {
            long ticks = DateTime.Now.Ticks;
            byte[] bytes = BitConverter.GetBytes(ticks);
            return Convert.ToBase64String(bytes).Replace('+', '_').Replace('/', '-').TrimEnd('=');
        }

        //TODO- for extrude and sweeep
        public static void boolOPwithSurface()
        {

        }

        public static void boolOPwithModel(ref Scene mScene, ref Rhino.DocObjects.RhinoObject rhinoObj, out Brep newBrep)
        {
            Rhino.DocObjects.ObjectEnumeratorSettings settings = new Rhino.DocObjects.ObjectEnumeratorSettings();
            settings.ObjectTypeFilter = Rhino.DocObjects.ObjectType.Brep;

            List<Brep> boolResultBrep = new List<Brep>();
            Brep brep = (Brep)rhinoObj.Geometry;
            newBrep = brep;

            if (mScene.rhinoDoc.Objects.Count() > 1)
            {
                
                foreach (Rhino.DocObjects.RhinoObject rhObj in mScene.rhinoDoc.Objects.GetObjectList(settings))
                {
                    //assume only intersect with one object at most
                    if (rhObj.Attributes.Name.Contains("aprint") && rhObj.Attributes.Name != rhinoObj.Name)
                    {

                        Brep[] boolBrep = brep.Split((Brep)rhObj.Geometry, mScene.rhinoDoc.ModelAbsoluteTolerance);
                        if (boolBrep != null && boolBrep.Length > 0)
                        {
                            float maxD = 0f;
                            int maxIndex = -1;
                            for (int i = 0; i < boolBrep.Length; i++)
                            {

                                //get the correct split brep by comparing their center
                                float distance = computePointDistance(RhinoToOpenTKPoint(boolBrep[i].GetBoundingBox(true).Center), RhinoToOpenTKPoint(rhObj.Geometry.GetBoundingBox(true).Center));

                                if (distance > maxD)
                                {
                                    maxD = distance;
                                    maxIndex = i;
                                }

                            }
                            if (maxIndex != -1)
                            {
                                //get the correct split brep by comparing their center
                                newBrep = boolBrep[maxIndex];
                            }
                            else
                            {
                                newBrep = brep;
                            }
                            break;
                        }
                    }
                }
            }else
            {
                newBrep = brep;
            }
        }


        public static bool removeRhinoObject(ref Scene mScene, Guid guid)
        {
            bool isSucceed = mScene.rhinoDoc.Objects.Delete(guid, true);
            mScene.rhinoDoc.Views.Redraw();
            return isSucceed;
        }

        public static void removeSceneNode(ref Scene mScene, ref SceneNode SN, bool isStatic = false)
        {
            if (!isStatic)
            {
                mScene.tableGeometry.children.Remove(SN);
            }
            else
            {
                mScene.staticGeometry.children.Remove(SN);
            }
            //mScene.rhinoDoc.Views.Redraw();
        }

        public static void removeRhinoObjectSceneNode(ref Scene mScene, Guid guid, bool isStatic = false)
        {
            SceneNode deleteSN = mScene.BiDictionaryRhinoVR.GetByFirst(guid);

            mScene.rhinoDoc.Objects.Delete(guid, true);

            removeSceneNode(ref mScene, ref deleteSN, isStatic);

            mScene.BiDictionaryRhinoVR.RemoveByFirst(guid);

            mScene.rhinoDoc.Views.Redraw();

        }

        public static void removeRhinoObjectSceneNode(ref Scene mScene, ref SceneNode deleteSN, bool isStatic = false)
        {
            Guid delGuid = mScene.BiDictionaryRhinoVR.GetBySecond(deleteSN);
            mScene.rhinoDoc.Objects.Delete(delGuid, true);

            removeSceneNode(ref mScene, ref deleteSN, isStatic);

            mScene.BiDictionaryRhinoVR.RemoveByFirst(delGuid);

            mScene.rhinoDoc.Views.Redraw();

        }


        public static void clearAllModel(ref Scene mScene)
        {
            Rhino.DocObjects.ObjectEnumeratorSettings settings = new Rhino.DocObjects.ObjectEnumeratorSettings();
            settings.ObjectTypeFilter = Rhino.DocObjects.ObjectType.Brep;

            foreach (Rhino.DocObjects.RhinoObject rhObj in mScene.rhinoDoc.Objects.GetObjectList(settings))
            {
                if (rhObj.Attributes.Name.Contains("aprint") || rhObj.Attributes.Name.Contains("patchSurface"))
                {
                    removeRhinoObjectSceneNode(ref mScene, rhObj.Id);
                }
            }

            mScene.rhinoDoc.Views.Redraw();
        }

        public static void clearPlanePoints(ref Scene mScene)
        {
            mScene.iPointList.Clear();
        }

        public static void clearCurveTargetRhObj(ref Scene mScene)
        {
            //find panel and delete it

            Rhino.DocObjects.ObjectEnumeratorSettings settings = new Rhino.DocObjects.ObjectEnumeratorSettings();
            settings.ObjectTypeFilter = Rhino.DocObjects.ObjectType.Brep;
            foreach (Rhino.DocObjects.RhinoObject rhObj in mScene.rhinoDoc.Objects.GetObjectList(settings))
            {
                //check for different drawing curve types
                if (rhObj.Attributes.Name.Contains("panel"))
                {
                    mScene.rhinoDoc.Objects.Delete(rhObj.Id, true);
                }

            }

            mScene.iCurveList.Clear();

            mScene.rhinoDoc.Views.Redraw();
        }

        public static Point3d getPointfromString(string str)
        {
            char[] delimiters = { ' ', ',' };
            string[] substrings = str.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

            return new Point3d(Double.Parse(substrings[0]), Double.Parse(substrings[1]), Double.Parse(substrings[2]));
        }

        public static Rhino.Geometry.Vector3d getVectorfromString(string str)
        {
            char[] delimiters = { ' ', ',' };
            string[] substrings = str.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

            return new Rhino.Geometry.Vector3d(Double.Parse(substrings[0]), Double.Parse(substrings[1]), Double.Parse(substrings[2]));
        }

        public static void showLaser(ref Scene mScene, bool isShowing)
        {
           foreach(SceneNode sn in mScene.rightControllerNode.children)
            {
                if(sn.name == "ControllerRay")
                {
                    if (isShowing)
                    {
                        sn.material.setAlpha(1.0f);
                    }else
                    {
                        sn.material.setAlpha(0.0f);
                    }
                }
            }
        }

        public static Brep RevolveFunc(ref Scene mScene, ref List<Curve> curveList)
        {
            Line axis = new Line();
            if ((AxisType)mScene.selectionDic[SelectionKey.RevolveAxis] == AxisType.worldX)
            {
                axis = new Line(new Point3d(0, 0, 0), new Point3d(1, 0, 0));
            }
            else if ((AxisType)mScene.selectionDic[SelectionKey.RevolveAxis] == AxisType.worldY)
            {
                axis = new Line(new Point3d(0, 0, 0), new Point3d(0, 1, 0));
            }
            else
            {
                axis = new Line(new Point3d(0, 0, 0), new Point3d(0, 0, 1));
            }
            RevSurface revsrf = RevSurface.Create(curveList[curveList.Count - 1], axis);
            return Brep.CreateFromRevSurface(revsrf, false, false);
        }

        public static Brep ExtrudeFunc(ref Scene mScene, ref List<Curve> curveList)
        {
            //TODO-using Sweep fnction to do and find the intersect point
            Curve railCurve = curveList[curveList.Count - 1];
            Plane curvePlane;
            double height = 0;
            if (curveList[curveList.Count - 2].TryGetPlane(out curvePlane))
            {
                OpenTK.Vector3 heightVector = new OpenTK.Vector3((float)(railCurve.PointAtEnd.X - railCurve.PointAtStart.X), (float)(railCurve.PointAtEnd.Y - railCurve.PointAtStart.Y), (float)(railCurve.PointAtEnd.Z - railCurve.PointAtStart.Z));
                OpenTK.Vector3 planeNormal = new OpenTK.Vector3((float)curvePlane.Normal.X, (float)curvePlane.Normal.Y, (float)curvePlane.Normal.Z);
                planeNormal.Normalize();
                height = OpenTK.Vector3.Dot(heightVector, planeNormal) / planeNormal.Length;
            }

            Rhino.Geometry.Extrusion extrusion = Rhino.Geometry.Extrusion.Create(curveList[curveList.Count - 2], height, true);
            return extrusion.ToBrep();
        }

        /*
        public static Brep LoftFunc(ref Scene mScene, ref List<Curve> curveList)
        {
            List<Curve> profileCurves = new List<Curve>();

            //in our scenario only 2 curves
            foreach (Curve curve in curveList)
            {
                profileCurves.Add(curve);
            }
            //pass the user data to new curve
            string oldCurveOnObjID0 = profileCurves[0].GetUserString(CurveData.CurveOnObj.ToString());
            string oldPlaneOrigin0 = profileCurves[0].GetUserString(CurveData.PlaneOrigin.ToString());
            string oldPlaneNormal0 = profileCurves[0].GetUserString(CurveData.PlaneNormal.ToString());

            string oldCurveOnObjID1 = profileCurves[1].GetUserString(CurveData.CurveOnObj.ToString());
            string oldPlaneOrigin1 = profileCurves[1].GetUserString(CurveData.PlaneOrigin.ToString());
            string oldPlaneNormal1 = profileCurves[1].GetUserString(CurveData.PlaneNormal.ToString());

            //need to project to the first RhinoObj, but need to translate profile curve along it's normal first and project -normal
            if ((Scene.ShapeType)mScene.selectionDic[Scene.SelectionKey.Profile1Shape] == Scene.ShapeType.Circle || (Scene.ShapeType)mScene.selectionDic[Scene.SelectionKey.Profile1Shape] == Scene.ShapeType.Rect)
            {
                Rhino.DocObjects.ObjRef curveOnObj1Ref = new Rhino.DocObjects.ObjRef(new Guid(profileCurves[0].GetUserString(CurveData.CurveOnObj.ToString())));

                Plane plane1 = new Plane(UtilOld.getPointfromString(profileCurves[0].GetUserString(CurveData.PlaneOrigin.ToString())),
                    UtilOld.getVectorfromString(profileCurves[0].GetUserString(CurveData.PlaneNormal.ToString())));

                Plane plane2 = new Plane(UtilOld.getPointfromString(profileCurves[1].GetUserString(CurveData.PlaneOrigin.ToString())),
                   UtilOld.getVectorfromString(profileCurves[1].GetUserString(CurveData.PlaneNormal.ToString())));

                Transform projectTranslate = Transform.Translation(50 * plane1.Normal);
                profileCurves[0].Transform(projectTranslate);

                Curve projectCurve = Curve.ProjectToBrep(profileCurves[0], (Brep)curveOnObj1Ref.Object().Geometry, -plane1.Normal, mScene.rhinoDoc.ModelAbsoluteTolerance)[0].ToNurbsCurve();
                profileCurves[0] = projectCurve;

                Point3d sPlaneCenter = profileCurves[0].GetBoundingBox(true).Center;
                Point3d ePlaneCenter = profileCurves[1].GetBoundingBox(true).Center;
                Rhino.Geometry.Vector3d direction = new Rhino.Geometry.Vector3d(ePlaneCenter.X - sPlaneCenter.X, ePlaneCenter.Y - sPlaneCenter.Y, ePlaneCenter.Z - sPlaneCenter.Z);
                Vector3 loftDirection = UtilOld.RhinoToOpenTKVector(direction);

                OpenTK.Vector3 n1 = UtilOld.RhinoToOpenTKVector(plane1.Normal);
                OpenTK.Vector3 n2 = UtilOld.RhinoToOpenTKVector(plane2.Normal);

                //n1,n2 should be the same with railNormal and railEndNormal
                n1.Normalize();
                n2.Normalize();
                loftDirection.Normalize();

                //check the plane normal
                if (Vector3.Dot(n1, loftDirection) < 0)
                    n1 = -n1;
                if (Vector3.Dot(n2, loftDirection) < 0)
                    n2 = -n2;

                //angle = atan2(norm(cross(a,b)), dot(a,b))
                float angle = Vector3.Dot(n1, n2);
                CurveOrientation dir = profileCurves[0].ClosedCurveOrientation(new Rhino.Geometry.Vector3d(n1.X, n1.Y, n1.Z)); //new Vector3(0,0,1)
                CurveOrientation dir2 = profileCurves[1].ClosedCurveOrientation(new Rhino.Geometry.Vector3d(n2.X, n2.Y, n2.Z)); //new Vector3(0,0,1)

                //debugging
                mScene.angleD = angle;
                mScene.c1D = dir.ToString();
                mScene.c2D = dir2.ToString();

                //testing seems bug, try compare by ourselves
                //if(!Curve.DoDirectionsMatch(profileCurves[0], profileCurves[1]))
                //If the dot product is greater than 0 both vectors are pointing in the same direction
                if (angle >= 0)
                {
                    if (dir != dir2)
                    {
                        profileCurves[1].Reverse();
                    }
                }
                else
                {
                    if (dir == dir2)
                    {
                        profileCurves[1].Reverse();
                    }
                }

                //calculate the seam point by raycvasting
                Point3d startP = profileCurves[0].PointAtStart;
                Ray3d ray1 = new Ray3d(startP, direction);
                double curveT0 = 0;
                profileCurves[0].ClosestPoint(startP, out curveT0);
                profileCurves[0].ChangeClosedCurveSeam(curveT0);
                mScene.sStartP = new Point3d(profileCurves[0].PointAt(curveT0));
                //mScene.sStartP = profileCurves[0].GetBoundingBox(true).Center;

                Rhino.DocObjects.ObjRef curveOnObj2Ref = new Rhino.DocObjects.ObjRef(new Guid(mScene.iCurveList[mScene.iCurveList.Count - 1].GetUserString(CurveData.CurveOnObj.ToString())));
                List<GeometryBase> geometries = new List<GeometryBase>();
                geometries.Add(curveOnObj2Ref.Object().Geometry);
                //must be a brep or surface, not mesh
                Point3d[] rayIntersections = Rhino.Geometry.Intersect.Intersection.RayShoot(ray1, geometries, 1);
                if (rayIntersections != null)
                {
                    //find the closest point on profileCurve2
                    double curveT1 = 0;
                    profileCurves[1].ClosestPoint(rayIntersections[0], out curveT1);
                    profileCurves[1].ChangeClosedCurveSeam(curveT1);
                    mScene.eStartP = new Point3d(profileCurves[1].PointAt(curveT1));
                    //mScene.eStartP = new Point3d(ePlaneCenter);
                }

            }

            if ((Scene.ShapeType)mScene.selectionDic[Scene.SelectionKey.Profile1Shape] == Scene.ShapeType.Circle)
            {
                profileCurves[0] = profileCurves[0].Rebuild(((NurbsCurve)profileCurves[0]).Points.Count, profileCurves[0].Degree, true);
                profileCurves[1] = profileCurves[1].Rebuild(((NurbsCurve)profileCurves[1]).Points.Count, profileCurves[1].Degree, true);

                curveList[0].SetUserString(CurveData.CurveOnObj.ToString(), oldCurveOnObjID0);
                curveList[0].SetUserString(CurveData.PlaneOrigin.ToString(), oldPlaneOrigin0);
                curveList[0].SetUserString(CurveData.PlaneNormal.ToString(), oldPlaneNormal0);

                curveList[1].SetUserString(CurveData.CurveOnObj.ToString(), oldCurveOnObjID1);
                curveList[1].SetUserString(CurveData.PlaneOrigin.ToString(), oldPlaneOrigin1);
                curveList[1].SetUserString(CurveData.PlaneNormal.ToString(), oldPlaneNormal1);
            }

            //debug
            //mScene.rhinoDoc.Objects.AddCurve(profileCurves[0]);
            //mScene.rhinoDoc.Objects.AddCurve(profileCurves[1]);
            Rhino.DocObjects.ObjectEnumeratorSettings settings = new Rhino.DocObjects.ObjectEnumeratorSettings();
            settings.ObjectTypeFilter = Rhino.DocObjects.ObjectType.Curve;
            settings.NameFilter = "patchCurve";
            bool isExist = false;
            foreach (Rhino.DocObjects.RhinoObject rhObj in mScene.rhinoDoc.Objects.GetObjectList(settings))
            {
                isExist = true;
                break;
            }
            if (!isExist)
            {
                Rhino.DocObjects.ObjectAttributes attr = new Rhino.DocObjects.ObjectAttributes();
                attr.Name = "patchCurve";
                mScene.rhinoDoc.Objects.AddCurve(profileCurves[0], attr);
            }

            Brep[] loftBreps = Brep.CreateFromLoft(profileCurves, Point3d.Unset, Point3d.Unset, LoftType.Tight, false);
            Brep brep = new Brep();
            foreach (Brep bp in loftBreps)
            {
                brep.Append(bp);
            }

            return brep;
        }
        */
        public static Brep LoftFunc(ref Scene mScene, ref List<Curve> curveList)
        {
            Brep[] loftBreps = Brep.CreateFromLoft(curveList, Point3d.Unset, Point3d.Unset, LoftType.Tight, false);
            Brep brep = new Brep();
            foreach (Brep bp in loftBreps)
            {
                brep.Append(bp);
            }

            return brep;
        }

        //using loft function to implement
        public static Brep ExtrudeCapFunc(ref Scene mScene, ref List<Curve> curveList)
        {
            List<Curve> profileCurves = new List<Curve>();
            profileCurves.Add(curveList[0]);
            profileCurves.Add(curveList[2]);

            NurbsCurve rail = (NurbsCurve)curveList[1];
            PolylineCurve railPL = rail.ToPolyline(0, 0, 0, 0, 0, 1, 1, 0, true);
            OpenTK.Vector3 railStartPoint = UtilOld.RhinoToOpenTKPoint(railPL.PointAtStart);
            OpenTK.Vector3 railstartNormal = UtilOld.RhinoToOpenTKVector(railPL.TangentAtStart);
            OpenTK.Vector3 railEndPoint = UtilOld.RhinoToOpenTKPoint(railPL.PointAtEnd);
            OpenTK.Vector3 railEndNormal = UtilOld.RhinoToOpenTKVector(railPL.TangentAtEnd);

            //changing seam
            OpenTK.Matrix4 transMEnd = UtilOld.getCoordinateTransM(railStartPoint, railEndPoint, railstartNormal, railEndNormal);
            Transform t = UtilOld.OpenTKToRhinoTransform(transMEnd);

            if ((Scene.ShapeType)mScene.selectionDic[Scene.SelectionKey.Profile1Shape] == Scene.ShapeType.Circle)
            {
                Point3d startP = profileCurves[0].PointAtStart;
                double curveT0 = 0;
                profileCurves[0].ClosestPoint(startP, out curveT0);
                profileCurves[0].ChangeClosedCurveSeam(curveT0);
                mScene.sStartP = new Point3d(profileCurves[0].PointAtStart);
                startP.Transform(t);
                mScene.eStartP = new Point3d(startP);



            }

            Plane curvePlane1;
            Plane curvePlane2;
            if (profileCurves[0].TryGetPlane(out curvePlane1) && profileCurves[1].TryGetPlane(out curvePlane2))
            {
                if ((Scene.ShapeType)mScene.selectionDic[Scene.SelectionKey.Profile1Shape] == Scene.ShapeType.Rect)
                {
                    //testing create new rect
                    //TODO- don't need to move to the railcurve start unlike sweep
                    Vector3 rectCenterRhino1 = UtilOld.vrToPlatformPoint(ref mScene, mScene.iPointList[0]);
                    Vector3 rectBottomRightRhino1 = UtilOld.vrToPlatformPoint(ref mScene, mScene.iPointList[1]);

                    Vector3 rectDiagonal1 = new Vector3((float)(rectCenterRhino1.X - rectBottomRightRhino1.X), (float)(rectCenterRhino1.Y - rectBottomRightRhino1.Y), (float)(rectCenterRhino1.Z - rectBottomRightRhino1.Z));
                    float lenDiagonal1 = rectDiagonal1.Length;
                    Vector3 rectLeftTop = new Vector3((float)rectCenterRhino1.X, (float)rectCenterRhino1.Y, (float)rectCenterRhino1.Z) + lenDiagonal1 * rectDiagonal1.Normalized();
                    Point3d topLeftP = new Point3d(rectLeftTop.X, rectLeftTop.Y, rectLeftTop.Z);

                    Vector3 rectCenterRhino2 = UtilOld.vrToPlatformPoint(ref mScene, mScene.iPointList[2]);
                    Vector3 rectBottomRightRhino2 = UtilOld.vrToPlatformPoint(ref mScene, mScene.iPointList[3]);

                    Vector3 rectDiagonal2 = new Vector3((float)(rectCenterRhino2.X - rectBottomRightRhino2.X), (float)(rectCenterRhino2.Y - rectBottomRightRhino2.Y), (float)(rectCenterRhino2.Z - rectBottomRightRhino2.Z));
                    float lenDiagonal2 = rectDiagonal2.Length;
                    Vector3 rectLeftTop2 = new Vector3((float)rectCenterRhino2.X, (float)rectCenterRhino2.Y, (float)rectCenterRhino2.Z) + lenDiagonal2 * rectDiagonal2.Normalized();
                    Point3d topLeftP2 = new Point3d(rectLeftTop2.X, rectLeftTop2.Y, rectLeftTop2.Z);

                    Plane testPlane1 = new Plane(UtilOld.openTkToRhinoPoint(rectCenterRhino1), railPL.TangentAtStart);
                    Plane testPlane2 = new Plane(UtilOld.openTkToRhinoPoint(rectCenterRhino2), railPL.TangentAtEnd);

                    Rectangle3d tmpRect1 = new Rectangle3d(testPlane1, topLeftP, UtilOld.openTkToRhinoPoint(rectBottomRightRhino1));
                    //Rectangle3d rect1 = new Rectangle3d(testPlane1, tmpRect1.Width, tmpRect1.Height);
                    Rectangle3d rect1 = new Rectangle3d(testPlane1, new Interval(-tmpRect1.Width / 2, tmpRect1.Width / 2), new Interval(-tmpRect1.Height / 2, tmpRect1.Height / 2));

                    //Rectangle3d tmpRect2 = new Rectangle3d(testPlane2, topLeftP2, Util.openTkToRhinoPoint(rectBottomRightRhino2));
                    //Rectangle3d rect2 = new Rectangle3d(testPlane2, tmpRect2.Width, tmpRect2.Height);

                    //TODO- after chaning the seam, the profile curve will moves
                    //how we pre-move the profile curves to make it's seam to the orginal center position. only tranlation?
                    //try create the profile curve on the same plane first to match the cornel points then transform to the end
                    Rectangle3d tmpRect2 = new Rectangle3d(testPlane2, topLeftP2, UtilOld.openTkToRhinoPoint(rectBottomRightRhino2));
                    Rectangle3d rect2 = new Rectangle3d(testPlane2, new Interval(-tmpRect2.Width / 2, tmpRect2.Width / 2), new Interval(-tmpRect2.Height / 2, tmpRect2.Height / 2));
                    /*
                    Rectangle3d rect2 = new Rectangle3d(testPlane1, new Interval(-tmpRect2.Width / 2, tmpRect2.Width / 2), new Interval(-tmpRect2.Height / 2, tmpRect2.Height / 2));
                    Transform tEnd = Util.OpenTKToRhinoTransform(transMEnd);
                    rect2.Transform(tEnd);*/
                    profileCurves[0] = rect1.ToNurbsCurve();
                    profileCurves[1] = rect2.ToNurbsCurve();

                    //testing changing seam
                    /*
                    double curveT0 = 0;
                    //profileCurves[0].ClosestPoint(rect1.Corner(3), out curveT0);
                    profileCurves[0].ClosestPoint(profileCurves[0].PointAtStart, out curveT0);
                    profileCurves[0].ChangeClosedCurveSeam(curveT0);

                    double curveT = 0;
                    //profileCurves[1].ClosestPoint(rect2.Corner(3), out curveT);
                    profileCurves[1].ClosestPoint(profileCurves[1].PointAtStart, out curveT);
                    profileCurves[1].ChangeClosedCurveSeam(curveT);
                    */
                }

                OpenTK.Vector3 n1 = new Vector3((float)curvePlane1.Normal.X, (float)curvePlane1.Normal.Y, (float)curvePlane1.Normal.Z);
                OpenTK.Vector3 n2 = new Vector3((float)curvePlane2.Normal.X, (float)curvePlane2.Normal.Y, (float)curvePlane2.Normal.Z);

                //n1,n2 should be the same with railNormal and railEndNormal
                n1.Normalize();
                n2.Normalize();

                //angle = atan2(norm(cross(a,b)), dot(a,b))
                float angle = Vector3.Dot(n1, n2);
                CurveOrientation dir = profileCurves[0].ClosedCurveOrientation(railPL.TangentAtStart); //new Vector3(0,0,1)
                CurveOrientation dir2 = profileCurves[1].ClosedCurveOrientation(railPL.TangentAtStart); //new Vector3(0,0,1)

                //debugging
                mScene.angleD = angle;
                mScene.c1D = dir.ToString();
                mScene.c2D = dir2.ToString();

                //testing seems bug, try compare by ourselves
                //if(!Curve.DoDirectionsMatch(profileCurves[0], profileCurves[1]))
                if (dir != dir2)
                {
                    profileCurves[1].Reverse();
                }
                //debug rect
                if ((Scene.ShapeType)mScene.selectionDic[Scene.SelectionKey.Profile1Shape] == Scene.ShapeType.Circle)
                {
                    double curveT = 0;
                    profileCurves[1].ClosestPoint(mScene.eStartP, out curveT);
                    profileCurves[1].ChangeClosedCurveSeam(curveT);
                }
                CurveOrientation dir3 = profileCurves[1].ClosedCurveOrientation(UtilOld.openTkToRhinoVector(railEndNormal)); //new Vector3(0,0,1)
                mScene.c3D = dir3.ToString();
            }


            Brep[] loftBreps = Brep.CreateFromLoft(profileCurves, Point3d.Unset, Point3d.Unset, LoftType.Tight, false);
            Brep brep = new Brep();
            foreach (Brep bp in loftBreps)
            {
                brep.Append(bp);
            }

            return brep;

        }

        public static Brep SweepFun(ref Scene mScene, ref List<Curve> curveList)
        {
            /*
            //compute the normal of the first point of the rail curve
            NurbsCurve rail = (NurbsCurve)curveList[curveList.Count - 1];
            PolylineCurve railPL = rail.ToPolyline(0, 0, 0, 0, 0, 1, 1, 0, true);
            OpenTK.Vector3 railStartPoint = new OpenTK.Vector3((float)railPL.PointAtStart.X, (float)railPL.PointAtStart.Y, (float)railPL.PointAtStart.Z);
            OpenTK.Vector3 railNormal = new OpenTK.Vector3((float)railPL.TangentAtStart.X, (float)railPL.TangentAtStart.Y, (float)railPL.TangentAtStart.Z);

            //need to calculate the center and normal from curve
            //OpenTK.Vector3 shapeCenter = Util.vrToPlatformPoint(ref mScene, mScene.iPointList[0]);
            //OpenTK.Vector3 shapeNormal = new OpenTK.Vector3((float)mScene.iPlaneList[0].Normal.X, (float)mScene.iPlaneList[0].Normal.Y, (float)mScene.iPlaneList[0].Normal.Z);
            OpenTK.Vector3 shapeCenter = new Vector3((float)curveList[curveList.Count - 2].GetBoundingBox(true).Center.X, (float)curveList[curveList.Count - 2].GetBoundingBox(true).Center.Y, (float)curveList[curveList.Count - 2].GetBoundingBox(true).Center.Z);
            Plane curvePlane = new Plane();
            OpenTK.Vector3 shapeNormal = new Vector3(0, 0, 0);
            Double tolerance = 0;
            while (tolerance < 100)
            {
                if (curveList[curveList.Count - 2].TryGetPlane(out curvePlane, tolerance))
                {
                    shapeNormal = new OpenTK.Vector3((float)curvePlane.Normal.X, (float)curvePlane.Normal.Y, (float)curvePlane.Normal.Z);
                    break;
                }
                tolerance++;
            }

            //TODO- transM everytime is different in rotation. However, circle is fine since we won't see the roation.
            OpenTK.Matrix4 transM = Util.getCoordinateTransM(shapeCenter, railStartPoint, shapeNormal, railNormal);
            //Rhino.RhinoApp.WriteLine("railNormal: " + railNormal.ToString());
            //Rhino.RhinoApp.WriteLine("transM: " + transM.ToString());

            Transform t = new Transform();
            if ((Scene.ShapeType)mScene.selectionDic[Scene.SelectionKey.Profile1Shape] == Scene.ShapeType.Circle)
            {
                t = Util.OpenTKToRhinoTransform(transM);
                ((NurbsCurve)curveList[curveList.Count - 2]).Transform(t);
            }
            else if ((Scene.ShapeType)mScene.selectionDic[Scene.SelectionKey.Profile1Shape] == Scene.ShapeType.Rect)
            {
                //TODO-rotating to align rect. but need to know the drawplane noraml to allign with
                curvePlane.Transform(Util.OpenTKToRhinoTransform(transM));
                OpenTK.Vector3 testAxis = Util.RhinoToOpenTKVector(curvePlane.XAxis).Normalized();
                //OpenTK.Matrix4 transM2 = Util.getTransMAroundAxis(railStartPoint, testAxis, new Vector3(1, 0, 0), Util.RhinoToOpenTKPoint(curvePlane.Normal)); //still affect by the different normal
                Plane plane1 = new Plane(Util.getPointfromString(mScene.iCurveList[mScene.iCurveList.Count - 1].GetUserString(CurveData.PlaneNormal.ToString())),
                    Util.getVectorfromString(mScene.iCurveList[mScene.iCurveList.Count - 1].GetUserString(CurveData.PlaneOrigin.ToString())));

                OpenTK.Matrix4 transM2 = Util.getCoordinateTransM(railStartPoint, railStartPoint, testAxis, Util.RhinoToOpenTKVector(plane1.Normal));
                t = Util.OpenTKToRhinoTransform(transM2 * transM);
                Rhino.RhinoApp.WriteLine("angle: " + OpenTK.Vector3.CalculateAngle(testAxis, new Vector3(1, 0, 0)));

                ////Transform t = Util.OpenTKToRhinoTransform(transM);
                ((NurbsCurve)curveList[curveList.Count - 2]).Transform(t);
            }


            NurbsCurve circleCurve = (NurbsCurve)curveList[curveList.Count - 2];
            

            //cruves coordinate are in rhino, somehow cap didn't work and need to call CapPlanarHoles
            Brep[] breps = Brep.CreateFromSweep(curveList[curveList.Count - 1], circleCurve, false, mScene.rhinoDoc.ModelAbsoluteTolerance);
            Brep brep = breps[0];

            
            Transform invT;
            if (t.TryGetInverse(out invT))
                ((NurbsCurve)curveList[curveList.Count - 2]).Transform(invT);
            */

            //we assume that the rail curve always start from the center of the profile curve
            Brep[] breps = Brep.CreateFromSweep(curveList[curveList.Count - 1], curveList[curveList.Count - 2], false, mScene.rhinoDoc.ModelAbsoluteTolerance);
            Brep brep = breps[0];

            return brep;
        }

        //TODO- Profile Curve 1 has been changed before we call SweepCapFun
        public static Brep SweepCapFun(ref Scene mScene, ref List<Curve> curveList)
        {

           
            //compute the transfrom from railStart to railEnd
            NurbsCurve rail = curveList[curveList.Count - 2].ToNurbsCurve();
            OpenTK.Vector3 railStartPoint = UtilOld.RhinoToOpenTKPoint(rail.PointAtStart);
            OpenTK.Vector3 railStartNormal = UtilOld.RhinoToOpenTKVector(rail.TangentAtStart);
            OpenTK.Vector3 railEndPoint = UtilOld.RhinoToOpenTKPoint(rail.PointAtEnd);
            OpenTK.Vector3 railEndNormal = UtilOld.RhinoToOpenTKVector(rail.TangentAtEnd);

            OpenTK.Matrix4 transMStartEnd = new Matrix4();
            transMStartEnd = UtilOld.getCoordinateTransM(railStartPoint, railEndPoint, railStartNormal, railEndNormal);
            Transform tStartEnd = UtilOld.OpenTKToRhinoTransform(transMStartEnd);

            List<Curve> profileCurves = new List<Curve>();
            profileCurves.Add(curveList[curveList.Count - 3]);
            profileCurves.Add(curveList[curveList.Count - 1]);

            //pass the user data to new curve
            string oldCurveOnObjID0 = profileCurves[0].GetUserString(CurveData.CurveOnObj.ToString());
            string oldPlaneOrigin0 = profileCurves[0].GetUserString(CurveData.PlaneOrigin.ToString());
            string oldPlaneNormal0 = profileCurves[0].GetUserString(CurveData.PlaneNormal.ToString());

            string oldCurveOnObjID1 = profileCurves[1].GetUserString(CurveData.CurveOnObj.ToString());
            string oldPlaneOrigin1 = profileCurves[1].GetUserString(CurveData.PlaneOrigin.ToString());
            string oldPlaneNormal1 = profileCurves[1].GetUserString(CurveData.PlaneNormal.ToString());

            //debugging
            //mScene.rhinoDoc.Objects.AddCurve(profileCurves[0]);
            //mScene.rhinoDoc.Objects.AddCurve(profileCurves[1]);

            Plane curvePlane1;
            Plane curvePlane2;
            profileCurves[0].TryGetPlane(out curvePlane1);
            profileCurves[1].TryGetPlane(out curvePlane2);

            //store the starting point for changing seam laster
            if ((Scene.ShapeType)mScene.selectionDic[Scene.SelectionKey.Profile1Shape] == Scene.ShapeType.Circle)
            {
                Point3d startP = profileCurves[0].PointAtStart;
                double curveT0 = 0;
                profileCurves[0].ClosestPoint(startP, out curveT0);
                profileCurves[0].ChangeClosedCurveSeam(curveT0);
                mScene.sStartP = new Point3d(profileCurves[0].PointAtStart);
                startP.Transform(tStartEnd);
                mScene.eStartP = new Point3d(startP);

                //OpenTK.Vector3 n1 = new Vector3((float)curvePlane1.Normal.X, (float)curvePlane1.Normal.Y, (float)curvePlane1.Normal.Z);
                //OpenTK.Vector3 n2 = new Vector3((float)curvePlane2.Normal.X, (float)curvePlane2.Normal.Y, (float)curvePlane2.Normal.Z);

                OpenTK.Vector3 n1 = railStartNormal;
                OpenTK.Vector3 n2 = railEndNormal;

                //n1,n2 should be the same with railNormal and railEndNormal
                n1.Normalize();
                n2.Normalize();

                //angle = atan2(norm(cross(a,b)), dot(a,b))
                float angle = Vector3.Dot(n1, n2);
                CurveOrientation dir = profileCurves[0].ClosedCurveOrientation(new Rhino.Geometry.Vector3d(n1.X, n1.Y, n1.Z)); //new Vector3(0,0,1)
                CurveOrientation dir2 = profileCurves[1].ClosedCurveOrientation(new Rhino.Geometry.Vector3d(n2.X, n2.Y, n2.Z)); //new Vector3(0,0,1)

                //debugging
                mScene.angleD = angle;
                mScene.c1D = dir.ToString();
                mScene.c2D = dir2.ToString();
                Rhino.RhinoApp.WriteLine("angle: " + mScene.angleD + ", c1D: " + mScene.c1D + ", c2D:" + mScene.c2D);
                //testing seems bug, try compare by ourselves
                //if(!Curve.DoDirectionsMatch(profileCurves[0], profileCurves[1]))
                //If the dot product is greater than 0 both vectors are pointing in the same direction
                if (angle >= 0)
                {
                    if (dir != dir2)
                    {
                        profileCurves[1].Reverse();
                    }
                }
                else
                {
                    //TODO- somehow it didn't work
                    /*
                    if (dir == dir2)
                    {
                        profileCurves[1].Reverse();
                    }*/

                    if (dir != dir2)
                    {
                        profileCurves[1].Reverse();
                    }

                }

                double curveT = 0;
                profileCurves[1].ClosestPoint(mScene.eStartP, out curveT);
                profileCurves[1].ChangeClosedCurveSeam(curveT);

                CurveOrientation dir3 = profileCurves[1].ClosedCurveOrientation(UtilOld.openTkToRhinoVector(railEndNormal)); //new Vector3(0,0,1)
                mScene.c3D = dir3.ToString();

            }
            else if ((Scene.ShapeType)mScene.selectionDic[Scene.SelectionKey.Profile1Shape] == Scene.ShapeType.Rect)
            {

                //changing the curve direction
                //OpenTK.Vector3 n1 = new Vector3((float)curvePlane1.Normal.X, (float)curvePlane1.Normal.Y, (float)curvePlane1.Normal.Z);
                //OpenTK.Vector3 n2 = new Vector3((float)curvePlane2.Normal.X, (float)curvePlane2.Normal.Y, (float)curvePlane2.Normal.Z);
                OpenTK.Vector3 n1 = railStartNormal;
                OpenTK.Vector3 n2 = railEndNormal;

                //n1,n2 should be the same with railNormal and railEndNormal
                n1.Normalize();
                n2.Normalize();

                //angle = atan2(norm(cross(a,b)), dot(a,b))
                float angle = Vector3.Dot(n1, n2);
                CurveOrientation dir = profileCurves[0].ClosedCurveOrientation(UtilOld.openTkToRhinoVector(n1)); //new Vector3(0,0,1)
                CurveOrientation dir2 = profileCurves[1].ClosedCurveOrientation(UtilOld.openTkToRhinoVector(n2)); //new Vector3(0,0,1)

                //debugging
                mScene.angleD = angle;
                mScene.c1D = dir.ToString();
                mScene.c2D = dir2.ToString();
                Rhino.RhinoApp.WriteLine("angle: " + mScene.angleD + ", c1D: " + mScene.c1D + ", c2D:" + mScene.c2D);
                //testing seems bug, try compare by ourselves
                //if(!Curve.DoDirectionsMatch(profileCurves[0], profileCurves[1]))
                if (dir != dir2)
                {
                    profileCurves[1].Reverse();
                }

                //TODO- create the endRect on startPlane to locate the reference corner
                Rhino.Geometry.Polyline polylineStart;
                Rhino.Geometry.Polyline polylineEnd;
                if (profileCurves[0].TryGetPolyline(out polylineStart) && profileCurves[1].TryGetPolyline(out polylineEnd))
                {
                    //testing changing seam
                    
                    Rectangle3d startRect = Rectangle3d.CreateFromPolyline(polylineStart);
                    Rectangle3d endRect = Rectangle3d.CreateFromPolyline(polylineEnd);

                    //finding the corner seam
                    float d0 = (float)Math.Sqrt(Math.Pow(startRect.Corner(0).X - endRect.Corner(0).X, 2) + Math.Pow(startRect.Corner(0).Y - endRect.Corner(0).Y, 2) + Math.Pow(startRect.Corner(0).Z - endRect.Corner(0).Z, 2));
                    float d1 = (float)Math.Sqrt(Math.Pow(startRect.Corner(0).X - endRect.Corner(1).X, 2) + Math.Pow(startRect.Corner(0).Y - endRect.Corner(1).Y, 2) + Math.Pow(startRect.Corner(0).Z - endRect.Corner(1).Z, 2));
                    float d2 = (float)Math.Sqrt(Math.Pow(startRect.Corner(0).X - endRect.Corner(2).X, 2) + Math.Pow(startRect.Corner(0).Y - endRect.Corner(2).Y, 2) + Math.Pow(startRect.Corner(0).Z - endRect.Corner(2).Z, 2));
                    float d3 = (float)Math.Sqrt(Math.Pow(startRect.Corner(0).X - endRect.Corner(3).X, 2) + Math.Pow(startRect.Corner(0).Y - endRect.Corner(3).Y, 2) + Math.Pow(startRect.Corner(0).Z - endRect.Corner(3).Z, 2));

                    float[] distaneArr = { d0, d1, d2, d3 };
                    int minIndex = Array.IndexOf(distaneArr, distaneArr.Min());

                    //testing manually align
                    /*
                    double curveT0 = 0;
                    profileCurves[0].ClosestPoint(startRect.Corner(0), out curveT0);
                    profileCurves[0].ChangeClosedCurveSeam(curveT0);

                    double curveT = 0;
                    profileCurves[1].ClosestPoint(endRect.Corner(minIndex), out curveT);
                    profileCurves[1].ChangeClosedCurveSeam(curveT);
                    */
                    mScene.sStartP = startRect.Corner(0);
                    mScene.eStartP = endRect.Corner(minIndex);
                    

                    //method 1- rebuild rect but sometimes it rotate 90 degrees so width become height
                    /*
                    Rectangle3d startRect = Rectangle3d.CreateFromPolyline(polylineStart);
                    Rectangle3d endRect = Rectangle3d.CreateFromPolyline(polylineEnd);                   
                    Rectangle3d startRect2 = new Rectangle3d(curvePlane1, new Interval(-startRect.Width / 2, startRect.Width / 2), new Interval(-startRect.Height / 2, startRect.Height / 2));
                    Rectangle3d endRect2 = new Rectangle3d(curvePlane2, new Interval(-endRect.Width / 2, endRect.Width / 2), new Interval(-endRect.Height / 2, endRect.Height / 2));
                    */

                    //Method 2 - using iPointList
                    /*
                    Vector3 rectCenterRhino1 = Util.vrToPlatformPoint(ref mScene, mScene.iPointList[0]);
                    Vector3 rectCenterRhino2 = Util.vrToPlatformPoint(ref mScene, mScene.iPointList[2]);
                    Vector3 rectBottomRightRhino2 = Util.vrToPlatformPoint(ref mScene, mScene.iPointList[3]);

                    Vector3 rectDiagonal2 = new Vector3((float)(rectCenterRhino2.X - rectBottomRightRhino2.X), (float)(rectCenterRhino2.Y - rectBottomRightRhino2.Y), (float)(rectCenterRhino2.Z - rectBottomRightRhino2.Z));
                    float lenDiagonal2 = rectDiagonal2.Length;
                    Vector3 rectLeftTop2 = new Vector3((float)rectCenterRhino2.X, (float)rectCenterRhino2.Y, (float)rectCenterRhino2.Z) + lenDiagonal2 * rectDiagonal2.Normalized();
                    Point3d topLeftP2 = new Point3d(rectLeftTop2.X, rectLeftTop2.Y, rectLeftTop2.Z);

                    Rectangle3d tmpRect2 = new Rectangle3d(curvePlane1, new Interval(-endRect.Width / 2, endRect.Width / 2), new Interval(-endRect.Height / 2, endRect.Height / 2));
                    Point3d referencePoint = new Point3d(tmpRect2.Corner(3));
                    OpenTK.Matrix4 transMTest = Util.getCoordinateTransM(Util.RhinoToOpenTKPoint(curvePlane1.Origin), Util.RhinoToOpenTKPoint(curvePlane2.Origin), Util.RhinoToOpenTKPoint(curvePlane1.Normal), Util.RhinoToOpenTKPoint(curvePlane2.Normal));
                    Transform tEndTest = Util.OpenTKToRhinoTransform(transMTest);
                    referencePoint.Transform(tEndTest);

                    //Rectangle3d endRect2 = new Rectangle3d(curvePlane2, topLeftP2, Util.openTkToRhinoPoint(rectBottomRightRhino2));
                    */

                    /*
                    profileCurves[0] = startRect.ToNurbsCurve();
                    profileCurves[1] = endRect.ToNurbsCurve();

                    //changing seam
                    double curveT0 = 0;
                    profileCurves[0].ClosestPoint(startRect.Corner(3), out curveT0);
                    profileCurves[0].ChangeClosedCurveSeam(curveT0);

                    double curveT = 0;
                    profileCurves[1].ClosestPoint(referencePoint, out curveT);
                    profileCurves[1].ChangeClosedCurveSeam(curveT);


                    //debugging
                    mScene.sStartP = new Point3d(startRect.Corner(3));
                    mScene.eStartP = new Point3d(referencePoint);
                    */
                }

            }

            //solving the issue of mutilple faces in Brep by rebuilding curve
            
            if ((Scene.ShapeType)mScene.selectionDic[Scene.SelectionKey.Profile1Shape] == Scene.ShapeType.Circle)
            {
                //we assign a new reference here so profileCurves[0] isn't reference to  curveList anymore
                //the above process somehow replace profileCurves/curveList a new curve, so we need to pass the user data to curvelist again

                profileCurves[0] = profileCurves[0].Rebuild(((NurbsCurve)profileCurves[0]).Points.Count, profileCurves[0].Degree, true);
                profileCurves[1] = profileCurves[1].Rebuild(((NurbsCurve)profileCurves[1]).Points.Count, profileCurves[1].Degree, true);

                curveList[curveList.Count - 3].SetUserString(CurveData.CurveOnObj.ToString(), oldCurveOnObjID0);
                curveList[curveList.Count - 3].SetUserString(CurveData.PlaneOrigin.ToString(), oldPlaneOrigin0);
                curveList[curveList.Count - 3].SetUserString(CurveData.PlaneNormal.ToString(), oldPlaneNormal0);

                curveList[curveList.Count - 1].SetUserString(CurveData.CurveOnObj.ToString(), oldCurveOnObjID1);
                curveList[curveList.Count - 1].SetUserString(CurveData.PlaneOrigin.ToString(), oldPlaneOrigin1);
                curveList[curveList.Count - 1].SetUserString(CurveData.PlaneNormal.ToString(), oldPlaneNormal1);


            }
            else if ((Scene.ShapeType)mScene.selectionDic[Scene.SelectionKey.Profile1Shape] == Scene.ShapeType.Rect)
            {
                //TODO- rebuild will generate non-rect
                //profileCurves[0] = new NurbsCurve((NurbsCurve)profileCurves[0]);
                //profileCurves[1] = new NurbsCurve((NurbsCurve)profileCurves[1]);
            }

            //mScene.rhinoDoc.ModelAbsoluteTolerance
            //Brep[] breps = Brep.CreateFromSweep(curveList[curveList.Count - 2], profileCurves, false, mScene.rhinoDoc.ModelAbsoluteTolerance);

            //testing SweepOneRail function
            //https://github.com/mcneel/rhinocommon/blob/master/dotnet/rhino/rhinosdksweep.cs
            
            Rhino.Geometry.SweepOneRail sweep = new Rhino.Geometry.SweepOneRail();
            sweep.AngleToleranceRadians = mScene.rhinoDoc.ModelAngleToleranceRadians;
            //sweep.AngleToleranceRadians = 2*Math.PI;
            sweep.ClosedSweep = false;
            sweep.SweepTolerance = mScene.rhinoDoc.ModelAbsoluteTolerance;
            //sweep.SweepTolerance = 1000000000;
            //sweep.SetToRoadlikeTop();
            Brep[] breps = sweep.PerformSweep(curveList[curveList.Count - 2], profileCurves);
            

            if ((Scene.FunctionType)mScene.selectionDic[Scene.SelectionKey.ModelFun] == Scene.FunctionType.Sweep)
            {
                //testing boolean
                /*
                Rhino.DocObjects.ObjectEnumeratorSettings settings = new Rhino.DocObjects.ObjectEnumeratorSettings();
                settings.ObjectTypeFilter = Rhino.DocObjects.ObjectType.Brep;

                Brep[] boolBrep = null;
                foreach (Rhino.DocObjects.RhinoObject rhObj in mScene.rhinoDoc.Objects.GetObjectList(settings))
                {
                    if (rhObj.Attributes.Name.Contains("aprint"))
                    {
                        boolBrep = Rhino.Geometry.Brep.CreateBooleanDifference(rhObj.Geometry as Brep, breps[0], mScene.rhinoDoc.ModelAbsoluteTolerance);
                        break;
                    }
                }
                return boolBrep[0];
                */

                return breps[0];

            }
            else if ((Scene.FunctionType)mScene.selectionDic[Scene.SelectionKey.ModelFun] == Scene.FunctionType.Extrude)
            {
                if ((Scene.ShapeType)mScene.selectionDic[Scene.SelectionKey.Profile1Shape] == Scene.ShapeType.Rect)
                {
                    return breps[0].CapPlanarHoles(mScene.rhinoDoc.ModelAbsoluteTolerance);
                }
                else
                {
                    return breps[0];
                }
            }
            else
            {
                return breps[0];
            }
        }


        /*
        public static Brep SweepCapFun2(ref Scene mScene, ref List<Curve> curveList, string type)
        {

            //Count-1: endCurve, Count - 2: rail, Count-3: startCurve
            NurbsCurve rail = (NurbsCurve)curveList[curveList.Count - 2];
            PolylineCurve railPL = rail.ToPolyline(0, 0, 0, 0, 0, 1, 1, 0, true);
            OpenTK.Vector3 railStartPoint = new OpenTK.Vector3((float)railPL.PointAtStart.X, (float)railPL.PointAtStart.Y, (float)railPL.PointAtStart.Z);
            OpenTK.Vector3 railNormal = new OpenTK.Vector3((float)railPL.TangentAtStart.X, (float)railPL.TangentAtStart.Y, (float)railPL.TangentAtStart.Z);

            OpenTK.Vector3 shapeCenter = new Vector3((float)curveList[curveList.Count - 3].GetBoundingBox(true).Center.X, (float)curveList[curveList.Count - 3].GetBoundingBox(true).Center.Y, (float)curveList[curveList.Count - 3].GetBoundingBox(true).Center.Z);
            Plane curvePlane;
            OpenTK.Vector3 shapeNormal = new Vector3(0, 0, 0);
            Double tolerance = 0;
            while (tolerance < 100)
            {
                if (curveList[curveList.Count - 3].TryGetPlane(out curvePlane, tolerance))
                {
                    shapeNormal = new OpenTK.Vector3((float)curvePlane.Normal.X, (float)curvePlane.Normal.Y, (float)curvePlane.Normal.Z);
                    break;
                }
                tolerance++;
            }

            OpenTK.Matrix4 transM = Util.getCoordinateTransM(shapeCenter, railStartPoint, shapeNormal, railNormal);
            Transform t = Util.OpenTKToRhinoTransform(transM);

            if (type == "Circle")
            {
                Point3d sOrigin =  Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, mScene.iPointList[0]));
                sOrigin.Transform(t);
                Point3d sCircleP = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, mScene.iPointList[1]));
                sCircleP.Transform(t);

                Point3d eOrigin = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, mScene.iPointList[2]));
                Point3d eCircleP = Util.openTkToRhinoPoint(Util.vrToPlatformPoint(ref mScene, mScene.iPointList[3]));

                float sRadius = (float)Math.Sqrt(Math.Pow(sOrigin.X - sCircleP.X, 2) + Math.Pow(sOrigin.Y - sCircleP.Y, 2) + Math.Pow(sOrigin.Z - sCircleP.Z, 2));
                float eRadius = (float)Math.Sqrt(Math.Pow(eOrigin.X - eCircleP.X, 2) + Math.Pow(eOrigin.Y - eCircleP.Y, 2) + Math.Pow(eOrigin.Z - eCircleP.Z, 2));

                

            }
            else if (type == "Rect")
            {

            }
            else
            {
                return null;
            }


        }*/

        public static float computePointDistance(Vector3 p1, Vector3 p2)
        {
            return (float)Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2) + Math.Pow(p1.Z - p2.Z, 2));
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
            //parallel
            float dotV = Math.Abs(Vector3.Dot(normal1, normal2));
            if (Math.Abs(Vector3.Dot(normal1, normal2)) >= 0.95)
            {
                return transToTarget * transToOrigin;
            }
            else
            {
                OpenTK.Matrix4 rotM = new OpenTK.Matrix4();
                OpenTK.Vector3 rotation_axis = OpenTK.Vector3.Cross(normal1, normal2);
                rotation_axis.Normalize();
                float rotation_angles = OpenTK.Vector3.CalculateAngle(normal1, normal2);

                OpenTK.Matrix4.CreateFromAxisAngle(rotation_axis, rotation_angles, out rotM);
                rotM.Transpose();

                return transToTarget * rotM * transToOrigin;
            }
        }

        public static OpenTK.Matrix4 getControllerTipPosition(ref Scene scene, bool left)
        {
            //mScene.mIsLefty decide whether it's right hand or left hand
            if (scene.mIsLefty)
            {
                return scene.mDevicePose[scene.leftControllerIdx] * scene.mLeftControllerOffset;
            }
            return scene.mDevicePose[scene.rightControllerIdx] * scene.mRightControllerOffset;
        }

        public static OpenTK.Matrix4 getLeftControllerTipPosition(ref Scene scene, bool isLeft)
        {
            if (isLeft)
            {
                return scene.mDevicePose[scene.leftControllerIdx] * scene.mLeftControllerOffset;
            }
            else
            {
                return scene.mDevicePose[scene.rightControllerIdx] * scene.mRightControllerOffset;
            }
        }

        /// <summary>
        /// Naive depth sorting algorithm for a single unit of geometry.
        /// </summary>
        /// <param name="g"></param>
        public static void depthSort(Matrix4 modelView, Geometry.Geometry g)
        {
            // Only works for tris
            if (g.primitiveType != OpenTK.Graphics.OpenGL4.BeginMode.Triangles)
                return;

            // Find the midpoint of each shape
            modelView = modelView * (1 / modelView.M44);
            System.Tuple<int, float>[] midpointZ = new Tuple<int, float>[g.mNumPrimitives];
            for (int i = 0; i < g.mNumPrimitives; i++)
            {
                Vector4 a = new Vector4(g.mGeometry[3 * g.mGeometryIndices[3 * i + 0]] + 0, g.mGeometry[3 * g.mGeometryIndices[3 * i + 0]] + 1, g.mGeometry[3 * g.mGeometryIndices[3 * i + 0]] + 2, 1);
                Vector4 b = new Vector4(g.mGeometry[3 * g.mGeometryIndices[3 * i + 1]] + 0, g.mGeometry[3 * g.mGeometryIndices[3 * i + 1]] + 1, g.mGeometry[3 * g.mGeometryIndices[3 * i + 1]] + 2, 1);
                Vector4 c = new Vector4(g.mGeometry[3 * g.mGeometryIndices[3 * i + 2]] + 0, g.mGeometry[3 * g.mGeometryIndices[3 * i + 2]] + 1, g.mGeometry[3 * g.mGeometryIndices[3 * i + 2]] + 2, 1);
                midpointZ[i] = new Tuple<int, float>(i, (modelView * (a + b + c) / 3).Z);
                if (i == 0)
                {
                    //Rhino.RhinoApp.WriteLine("Z " + ((modelView * (a + b + c) / 3).Z));
                }
            }

            // Sort indices according to midpoint (a bit sloppy, but hey. 1 week until Chi.)
            int j = 0;
            int[] newIndices = new int[g.mGeometryIndices.Length];
            foreach (var p in midpointZ.OrderBy(pair => pair.Item2))
            {
                newIndices[3 * j + 0] = g.mGeometryIndices[3 * p.Item1 + 0];
                newIndices[3 * j + 1] = g.mGeometryIndices[3 * p.Item1 + 1];
                newIndices[3 * j + 2] = g.mGeometryIndices[3 * p.Item1 + 2];
                j++;
            }
            g.mGeometryIndices = newIndices;

        }


    }




    //public static OpenTK.Matrix4
}
