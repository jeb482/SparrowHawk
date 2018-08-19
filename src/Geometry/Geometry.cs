﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;
using Math = SparrowHawk.Util.Math;



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

        public Geometry(string filename)
        {
            LoadObjFile(filename);
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
        void LoadObjFile(string filename)
        {
        //    ObjParser.Obj obj = new ObjParser.Obj();
        //    obj.LoadObj(filename);
        //    mGeometry = new float[3*3*obj.FaceList.Count];
        //    mGeometryIndices = new int[3 * obj.FaceList.Count];
        //    mNormals = new float[3*3*obj.FaceList.Count];
        //    mUvs = new float[2*3*obj.FaceList.Count];

        //    // This is the smart indexed way, but I need flat shading right now.
        //    //int i = 0;
        //    //foreach (var v in obj.VertexList)
        //    //{
        //    //    mGeometry[3 * i + 0] = (float)v.X;
        //    //    mGeometry[3 * i + 1] = (float)v.Y;
        //    //    mGeometry[3 * i + 2] = (float)v.Z;
        //    //    i++;
        //    //}
        //    //i = 0;
        //    //foreach (var f in obj.FaceList)
        //    //{
        //    //    mGeometryIndices[3 * i + 0] = f.VertexIndexList[0] - 1;
        //    //    mGeometryIndices[3 * i + 1] = f.VertexIndexList[1] - 1;
        //    //    mGeometryIndices[3 * i + 2] = f.VertexIndexList[2] - 1;
        //    //    i++;
        //    //}

        //    mNumPrimitives = obj.FaceList.Count;
        //    primitiveType = OpenTK.Graphics.OpenGL4.BeginMode.Triangles;
        //    int i = 0;
        //    Vector3[] normalCreators = new Vector3[3];
        //    Vector3 n;
        //    foreach (var f in obj.FaceList)
        //    {
        //        for (int v = 0; v < 3; v++)
        //        {
        //            normalCreators[v] = new Vector3((float)obj.VertexList.ElementAt(f.VertexIndexList[v] - 1).X, (float)obj.VertexList.ElementAt(f.VertexIndexList[v] - 1).Y, (float)obj.VertexList.ElementAt(f.VertexIndexList[v] - 1).Z);
        //            mGeometry[9 * i + 3 * v + 0] = (float)obj.VertexList.ElementAt(f.VertexIndexList[v] - 1).X;
        //            mGeometry[9 * i + 3 * v + 1] = (float)obj.VertexList.ElementAt(f.VertexIndexList[v] - 1).Y;
        //            mGeometry[9 * i + 3 * v + 2] = (float)obj.VertexList.ElementAt(f.VertexIndexList[v] - 1).Z;
        //        }
        //        n = Math.calculateFaceNormal(normalCreators[0], normalCreators[1], normalCreators[2]);
        //        for (int j = 0; j < 3; j++)
        //        {
        //            mNormals[9 * i + 3 * j + 0] = n.X;
        //            mNormals[9 * i + 3 * j + 1] = n.Y;
        //            mNormals[9 * i + 3 * j + 2] = n.Z;
        //        }

        //        if (obj.TextureList.Count > 0)
        //        {
        //            for (int j = 0; j < 3; j++)
        //            {
        //                mUvs[6 * i + 2 * j + 0] = (float)obj.TextureList[f.TextureVertexIndexList.ElementAt(j)-1].X;
        //                mUvs[6 * i + 2 * j + 1] = (float)obj.TextureList[f.TextureVertexIndexList.ElementAt(j)-1].Y;
        //            }
        //        }
        //        i++;
        //    }

        //    for (i = 0; i < mGeometryIndices.Length; i++)
        //    {
        //        mGeometryIndices[i] = i;
        //    }

        }

        // TODO: RayTracer

        // TODO: Build AABB
    }
}
