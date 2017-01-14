using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparrowHawk
{
    static class Util
    {
        public static void WriteLine(Rhino.RhinoDoc doc, String str)
        {
            if (doc != null)
                Rhino.RhinoApp.WriteLine(str);
            else
                Console.WriteLine(str);
        }
    }
}
