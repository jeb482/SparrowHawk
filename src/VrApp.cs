using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparrowHawk
{
    class VrApp
    {
        static int Main(string [] args)
        {
            Rhino.RhinoDoc doc = null;
            VrGame game = new VrGame(ref doc);
            game.runMainLoop();
            return 0;
        }
    }
}
