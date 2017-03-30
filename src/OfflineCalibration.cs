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
            output.Add(new Tuple<float, float, float, float, float, float>(-3, -7, 1.5f, 0, 0, -90));
            output.Add(new Tuple<float, float, float, float, float, float>(-3, -7, 1.5f, 0, 10, -90));
            output.Add(new Tuple<float, float, float, float, float, float>(-2.5f, -7.3f, 1, 0, -40, -90));
            output.Add(new Tuple<float, float, float, float, float, float>(-2.5f, -7.8f, 0.3f, 0, -60, -60));
            output.Add(new Tuple<float, float, float, float, float, float>(-2.5f, -7.8f, 1, 0, -40, -60));
            output.Add(new Tuple<float, float, float, float, float, float>(-3, -7.3f, 1.5f, 0, 0, -60));
            output.Add(new Tuple<float, float, float, float, float, float>(-3.2f, -7.2f, 1.5f, 0, 30, -60));
            output.Add(new Tuple<float, float, float, float, float, float>(-2.5f, -8.1f, .5f, 0, -40, -30));
            output.Add(new Tuple<float, float, float, float, float, float>(-2.5f, -7.5f, 1.5f, 0, 0, -30));
            output.Add(new Tuple<float, float, float, float, float, float>(-2.5f, -7.5f, 1.5f, 0, 10, -30));
            output.Add(new Tuple<float, float, float, float, float, float>(-2.5f, -7.5f, 2, 0, 30, -30));
            output.Add(new Tuple<float, float, float, float, float, float>(-1.5f, -8.4f, 0.5f,0, -40, 0));
            output.Add(new Tuple<float, float, float, float, float, float>(-1.7f, -7.2f, 1.5f, 0, 0, 0));
            output.Add(new Tuple<float, float, float, float, float, float>(-2, -6.1f, 2, 0, 40, 0));
            output.Add(new Tuple<float, float, float, float, float, float>(-1, -6.3f, 1.5f, 0, 0, 30));
            output.Add(new Tuple<float, float, float, float, float, float>(0, -7.3f, 1.2f, 0, -40, 30));
            output.Add(new Tuple<float, float, float, float, float, float>(-2, -4.5f, 1.5f, 0, 40, 30));
            output.Add(new Tuple<float, float, float, float, float, float>(0, -5.5f, 1.5f, 0, 0, 60));
            output.Add(new Tuple<float, float, float, float, float, float>(0.6f, -6.3f, 1, 0, -40, 60));
            output.Add(new Tuple<float, float, float, float, float, float>(-1, -4.5f, 1.5f, 0, 30, 60));
            output.Add(new Tuple<float, float, float, float, float, float>(-2, -7.8f, 1.5f, 90, 20, -90));
            output.Add(new Tuple<float, float, float, float, float, float>(-2, -8.3f, 1.5f, 90, 40, -90));
            output.Add(new Tuple<float, float, float, float, float, float>(-1.5f, -6, 1.2f, 90, -20, -90));
            output.Add(new Tuple<float, float, float, float, float, float>(-1, -5.6f, 1, 90, -40, -90));

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
