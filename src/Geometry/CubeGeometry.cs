using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparrowHawk.Geometry
{
    class CubeGeometry : Geometry
    {

        
        public CubeGeometry(float width, float height, float depth)
        {
            mGeometry= new float[72]{
              0.0f, 0.0f, 0.0f,  //bottom
              width, 0.0f, 0.0f,
              width, height, 0.0f,
              0.0f, height, 0.0f,
              
              0.0f, 0.0f, depth, //top
              width, 0.0f, depth,
              width, height, depth,  
              0.0f, height, depth,

              0.0f, height, depth, //left
              0.0f, height, 0.0f, 
              0.0f, 0.0f, 0.0f, 
              0.0f, 0.0f, depth,

              width, height, depth, //right
              width, height, 0.0f,
              width, 0.0f, 0.0f,
              width, 0.0f, depth,

              0.0f, 0.0f, 0.0f, // front
              width, 0.0f, 0.0f,
              width, 0.0f, depth,
              0.0f, 0.0f, depth,

              0.0f,  height, 0.0f, //basck
              width,  height, 0.0f,
              width,  height, depth, 
              0.0f,  height, depth
            };

            mUvs = new float[48]
            {
              0.0f, 0.0f,
              1.0f, 0.0f,
              1.0f, 1.0f,
              0.0f, 1.0f,
 
              0.0f, 0.0f,
              1.0f, 0.0f,
              1.0f, 1.0f,
              0.0f, 1.0f,

              1.0f, 0.0f,
              1.0f, 1.0f,
              0.0f, 1.0f,
              0.0f, 0.0f,

              1.0f, 0.0f,
              1.0f, 1.0f,
              0.0f, 1.0f,
              0.0f, 0.0f,

              0.0f, 1.0f,
              1.0f, 1.0f,
              1.0f, 0.0f,
              0.0f, 0.0f,

              0.0f, 1.0f,
              1.0f, 1.0f,
              1.0f, 0.0f,
              0.0f, 0.0f,
            };

            mGeometryIndices = new int[36]{ 0, 1, 2, 0, 2, 3, //front
                                        4, 5, 6, 4, 6, 7, //right
                                        8, 9, 10, 8, 10, 11, //back
                                        12, 13, 14, 12, 14, 15, //left
                                        16, 17, 18, 16, 18, 19, //upper
                                        20, 21, 22, 20, 22, 23}; //bottom

            mNumPrimitives = mGeometryIndices.Length / 3;
            primitiveType = OpenTK.Graphics.OpenGL4.BeginMode.Triangles;
        }
    }
}
