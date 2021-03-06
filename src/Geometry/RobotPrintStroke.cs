﻿using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparrowHawk.Geometry
{
    class RobotPrintStroke : Geometry
    {
        private Scene mScene;
        public int mNumPoints;
        public List<OpenTK.Vector3> mPoints = new List<OpenTK.Vector3>();
        public List<float> vertices_array = new List<float>();
        public List<int> indices_array = new List<int>();

        public RobotPrintStroke()
        {
            mNumPoints = 0;
            primitiveType = OpenTK.Graphics.OpenGL4.BeginMode.Lines;
        }

        public RobotPrintStroke(ref Scene s)
        {
            mScene = s;
            mNumPoints = 0;
            primitiveType = OpenTK.Graphics.OpenGL4.BeginMode.Lines;
        }

        public void addPoint(OpenTK.Vector3 p)
        {

            mPoints.Add(p);

            vertices_array.Add(p.X);
            vertices_array.Add(p.Y);
            vertices_array.Add(p.Z);

            mNumPoints++;
            mGeometry = vertices_array.ToArray();
            mNumPrimitives = mNumPoints - 1;

            if (mPoints.Count >= 2)
            {
                indices_array.Add(mNumPoints - 2);
                indices_array.Add(mNumPoints - 1);
                mGeometryIndices = indices_array.ToArray();
            }

        }

        public void addEdge(OpenTK.Vector3 p1, Vector3 p2)
        {

            mPoints.Add(p1);
            mPoints.Add(p2);

            //when visualizing, stroke is in tableGeometry so we need to apply tableGeomeotry.transfrom inverted first
            p1 = UtilOld.transformPoint(mScene.tableGeometry.transform.Inverted(), p1);
            p2 = UtilOld.transformPoint(mScene.tableGeometry.transform.Inverted(), p2);

            vertices_array.Add(p1.X);
            vertices_array.Add(p1.Y);
            vertices_array.Add(p1.Z);

            mNumPoints++;

            vertices_array.Add(p2.X);
            vertices_array.Add(p2.Y);
            vertices_array.Add(p2.Z);

            mNumPoints++;

            mGeometry = vertices_array.ToArray();
            //mNumPrimitives = mNumPoints -1 ;
            mNumPrimitives = mNumPoints / 2;

            if (mPoints.Count % 2 == 0)
            {
                indices_array.Add(mNumPoints - 2);
                indices_array.Add(mNumPoints - 1);
                mGeometryIndices = indices_array.ToArray();
            }

        }


        public void removePoint()
        {
            mNumPoints = 0;
            mPoints.Clear();
            vertices_array.Clear();
            indices_array.Clear();
        }

        public void truncate(int n)
        {
            if (n > mNumPoints)
            {
                Rhino.RhinoApp.WriteLine("no enough points");
            }
            else
            {
                mNumPoints = mNumPoints - n;
                vertices_array.RemoveRange(vertices_array.Count - mNumPoints * 3, mNumPoints * 3);
                indices_array.RemoveRange(indices_array.Count - mNumPoints * 2, mNumPoints * 2);

                mGeometry = vertices_array.ToArray();
                mGeometryIndices = indices_array.ToArray();
                mNumPrimitives = mNumPoints - 1;
            }

        }




    }
}