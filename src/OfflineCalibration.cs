using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparrowHawk
{
    class OfflineCalibration
    {
        public static List<Tuple<float, float, float, float, float, float>> getHuaishuRobotMeasurements()
        {
            var output = new List<Tuple<float, float, float, float, float, float>>();
            //
            // Original callibration with offset
            //
            //output.Add(new Tuple<float, float, float, float, float, float>(-3, -7, 1.5f, 0, 0, -90));
            //output.Add(new Tuple<float, float, float, float, float, float>(-3, -7, 1.5f, 0, 10, -90));
            //output.Add(new Tuple<float, float, float, float, float, float>(-2.5f, -7.3f, 1, 0, -40, -90));
            //output.Add(new Tuple<float, float, float, float, float, float>(-2.5f, -7.8f, 0.3f, 0, -60, -60));
            //output.Add(new Tuple<float, float, float, float, float, float>(-2.5f, -7.8f, 1, 0, -40, -60));
            //output.Add(new Tuple<float, float, float, float, float, float>(-3, -7.3f, 1.5f, 0, 0, -60));
            //output.Add(new Tuple<float, float, float, float, float, float>(-3.2f, -7.2f, 1.5f, 0, 30, -60));
            //output.Add(new Tuple<float, float, float, float, float, float>(-2.5f, -8.1f, .5f, 0, -40, -30));
            //output.Add(new Tuple<float, float, float, float, float, float>(-2.5f, -7.5f, 1.5f, 0, 0, -30));
            //output.Add(new Tuple<float, float, float, float, float, float>(-2.5f, -7.5f, 1.5f, 0, 10, -30));
            //output.Add(new Tuple<float, float, float, float, float, float>(-2.5f, -7.5f, 2, 0, 30, -30));
            //output.Add(new Tuple<float, float, float, float, float, float>(-1.5f, -8.4f, 0.5f,0, -40, 0));
            //output.Add(new Tuple<float, float, float, float, float, float>(-1.7f, -7.2f, 1.5f, 0, 0, 0));
            //output.Add(new Tuple<float, float, float, float, float, float>(-2, -6.1f, 2, 0, 40, 0));
            //output.Add(new Tuple<float, float, float, float, float, float>(-1, -6.3f, 1.5f, 0, 0, 30));
            //output.Add(new Tuple<float, float, float, float, float, float>(0, -7.3f, 1.2f, 0, -40, 30));
            //output.Add(new Tuple<float, float, float, float, float, float>(-2, -4.5f, 1.5f, 0, 40, 30));
            //output.Add(new Tuple<float, float, float, float, float, float>(0, -5.5f, 1.5f, 0, 0, 60));
            //output.Add(new Tuple<float, float, float, float, float, float>(0.6f, -6.3f, 1, 0, -40, 60));
            //output.Add(new Tuple<float, float, float, float, float, float>(-1, -4.5f, 1.5f, 0, 30, 60));
            //output.Add(new Tuple<float, float, float, float, float, float>(-2, -7.8f, 1.5f, 90, 20, -90));
            //output.Add(new Tuple<float, float, float, float, float, float>(-2, -8.3f, 1.5f, 90, 40, -90));
            //output.Add(new Tuple<float, float, float, float, float, float>(-1.5f, -6, 1.2f, 90, -20, -90));
            //output.Add(new Tuple<float, float, float, float, float, float>(-1, -5.6f, 1, 90, -40, -90));

            // 4/5/2017 Callibration from end_effector
            output.Add(new Tuple<float, float, float, float, float, float>(42.425f,     6.2f, - 92.012f,  179.235f,      90f,       0f));
            output.Add(new Tuple<float, float, float, float, float, float>(44.113f,    11.6f, - 91.044f, -132.739f,  84.021f,  17.062f));
            output.Add(new Tuple<float, float, float, float, float, float>(51.613f,  52.332f, - 77.744f, -109.583f,  99.594f,  18.995f));
            output.Add(new Tuple<float, float, float, float, float, float>(72.087f,  -7.056f, - 72.381f, -124.978f,  79.922f,  41.901f));
            output.Add(new Tuple<float, float, float, float, float, float>(   9.2f,  49.356f, - 92.382f,  135.064f,  53.031f, -21.372f));
            output.Add(new Tuple<float, float, float, float, float, float>(-9.006f, -63.819f, - 75.646f,   98.409f, 105.758f,   9.529f));
            //output.Add(new Tuple<float, float, float, float, float, float>(29.075f, -57.582f, - 75.033f,  110.383f, 107.264f,  -4.352f));
            //output.Add(new Tuple<float, float, float, float, float, float>(59.479f, -68.078f, - 45.276f,    24.45f,  15.533f, -68.243f));
            //output.Add(new Tuple<float, float, float, float, float, float>(79.013f,  -41.43f, - 49.426f,   31.972f,  15.969f, -55.967f));
            //output.Add(new Tuple<float, float, float, float, float, float>(83.077f,  -8.204f, - 61.027f,   59.855f,  31.619f, -52.868f));
            //output.Add(new Tuple<float, float, float, float, float, float>(60.976f,  -5.135f, - 80.921f,   83.162f,    63.4f, -39.106f));

            return output;
        }   



        public static OpenTK.Vector3 solveForRobotOffsetVector(List<Tuple<float, float, float, float, float, float>> measurements) {
            List<OpenTK.Matrix4> matrixMeasurements = new List<OpenTK.Matrix4>();
            foreach (var m in measurements)
            {
                OpenTK.Matrix4 M = new OpenTK.Matrix4();
                float alpha = (float)(m.Item4 * (2*Math.PI)/360) ;
                float beta =  (float)(m.Item5 * (2*Math.PI)/360) ;
                float gamma = (float)(m.Item6 * (2 * Math.PI) / 360);
                M = OpenTK.Matrix4.CreateRotationZ(-alpha) * OpenTK.Matrix4.CreateRotationY(-beta) * OpenTK.Matrix4.CreateRotationZ(-gamma);
                M.Column3 = new OpenTK.Vector4(m.Item1, m.Item2, m.Item3, 1);
                matrixMeasurements.Add(M);
            }
            return Util.solveForOffsetVector(matrixMeasurements);
        }
    }
}
