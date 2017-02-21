using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ObjLoader.Loader;
using OpenTK;


namespace SparrowHawk.Geometry
{
    public class Geometry
    {
        public float[] mGeometry;
        public int[] mGeometryIndices;
        public float[] mUvs;
        public float[] mNormals;
        public float[] mColors;
        public int mNumPrimitives;

        public OpenTK.Graphics.OpenGL4.BeginMode primitiveType;

        public Geometry(string filename, OpenTK.Graphics.Color4 color)
        {
            LoadObjFile(filename, color);
        }

        public Geometry(IntPtr pRenderModel, out int nTexId)
        {
            unsafe
            {
                // Unpack all the RenderModel data
                void* ptr = pRenderModel.ToPointer();
                IntPtr pVertexData = *(IntPtr*) ptr;
                ptr = (void*) ((IntPtr*)ptr + 1) ;
                uint unVertexCount = *(uint*)pRenderModel.ToPointer();
                ptr = (void*)((uint*)ptr + 1);
                IntPtr pIndexData = *(IntPtr*)pRenderModel.ToPointer();
                ptr = (void*)((IntPtr*)ptr + 1);
                uint unIndexCount = *(uint*)pRenderModel.ToPointer();
                ptr = (void*)((uint*)ptr + 1);
                nTexId = *(int*)pRenderModel.ToPointer();

                mGeometry = new float[3*unVertexCount];
                mNormals = new float[3*unVertexCount];
                mUvs = new float[2 * unVertexCount];

                Valve.VR.RenderModel_Vertex_t* vertexPtr = (Valve.VR.RenderModel_Vertex_t*) pVertexData.ToPointer();
                Valve.VR.RenderModel_Vertex_t v;
                System.UInt16* idxPtr = (System.UInt16*) pIndexData.ToPointer(); 
                
                for (uint i = 0; i < unVertexCount; i++)
                {
                    v = *(vertexPtr + i);
                    mGeometry[3 * i + 0] = v.vPosition.v0;
                    mGeometry[3 * i + 1] = v.vPosition.v1;
                    mGeometry[3 * i + 2] = v.vPosition.v2;
                    mNormals[3 * i + 0] = v.vNormal.v0;
                    mNormals[3 * i + 1] = v.vNormal.v1;
                    mNormals[3 * i + 2] = v.vNormal.v2;
                    mUvs[3 * i + 0] = v.rfTextureCoord0;
                    mUvs[3 * i + 1] = v.rfTextureCoord1;
                }

                for (uint i = 0; i < unIndexCount; i++)
                {
                    mGeometryIndices[i] = (int)*(idxPtr + i);
                }

                mNumPrimitives = (int)unIndexCount / 3;
                primitiveType = OpenTK.Graphics.OpenGL4.BeginMode.Triangles;
            }
        }

        public Geometry()
        {
        }


        // I'm doing this with too many loops but whatever.
        // Also I'm shattering the ability to put multiple meshes in one file.
        void LoadObjFile(string filename, OpenTK.Graphics.Color4 color)
        {
            var factory = new ObjLoader.Loader.Loaders.ObjLoaderFactory();
            var objLoader = factory.Create();
            var fileStream = new System.IO.FileStream(filename, System.IO.FileMode.Open);
            var data = objLoader.Load(fileStream);
            
            // THis doesnt work anyway. Thanks CJ loser.

            //foreach (var v in data.Vertices)
            //{
            //    mGeometry.Add(v.X); mGeometry.Add(v.Y); mGeometry.Add(v.Z);
            //    mColors.Add(color.R); mColors.Add(color.G); mColors.Add(color.B); mColors.Add(color.A);
            //}

            //foreach (var n in data.Normals)
            //{
            //    mNormals.Add(n.X); mNormals.Add(n.Y); mNormals.Add(n.Z);
            //}

            //foreach (var uv in data.Textures)
            //{
            //    mUvs.Add(uv.X); mUvs.Add(uv.Y);
            //}

            //for (int i = 0; i < data.Vertices.Count; i++)
            //{
            //    mGeometryIndices.Add(i);
            //}

            //// TODO: Add flat shade code;

            //mNumPrimitives = mGeometryIndices.Count / 3;
        }

        // TODO: RayTracer

        // TODO: Build AABB
    }
}
