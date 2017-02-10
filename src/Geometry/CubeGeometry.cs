using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparrowHawk.Geometry
{
    class CubeGeometry : Geometry
    {
        public CubeGeometry()
        {

        }

        public CubeGeometry(float width, float depth, float height)
        {
            mGeometry= new float[72]{
              0.0f, 0.0f, 0.0f,  //bottom
              3.0f, 0.0f, 0.0f,
              3.0f, 3.0f, 0.0f,
              0.0f, 3.0f, 0.0f,
              
              0.0f, 0.0f, -3.0f, //top
              3.0f, 0.0f, -3.0f, 
              3.0f, 3.0f, -3.0f,  
              0.0f, 3.0f, -3.0f,

              0.0f, 3.0f, -3.0f, //left
              0.0f, 3.0f, 0.0f, 
              0.0f, 0.0f, 0.0f, 
              0.0f, 0.0f, -3.0f,

              3.0f, 3.0f, -3.0f, //right
              3.0f, 3.0f, 0.0f, 
              3.0f, 0.0f, 0.0f,  
              3.0f, 0.0f, -3.0f,

              0.0f, 0.0f, 0.0f, // front
              3.0f, 0.0f, 0.0f,
              3.0f, 0.0f, -3.0f,
              0.0f, 0.0f, -3.0f,

              0.0f,  3.0f, 0.0f, //basck
              3.0f,  3.0f, 0.0f, 
              3.0f,  3.0f, -3.0f, 
              0.0f,  3.0f, -3.0f
            };

            mGeometryIndices = new int[]{ 0, 1, 2, 0, 2, 3, //front
                                        4, 5, 6, 4, 6, 7, //right
                                        8, 9, 10, 8, 10, 11, //back
                                        12, 13, 14, 12, 14, 15, //left
                                        16, 17, 18, 16, 18, 19, //upper
                                        20, 21, 22, 20, 22, 23}; //bottom


        }
    }
}
