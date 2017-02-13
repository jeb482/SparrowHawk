using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Threading;

namespace SparrowHawk
{
    [System.Runtime.InteropServices.Guid("ac51bc97-6f3a-4564-8ad9-a9d62c06bb06")]
    public class SparrowHawkCommand : Command
    {
        public SparrowHawkCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static SparrowHawkCommand Instance
        {
            get; private set;
        }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName
        {
            get { return "SparrowHawk"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Launch the "Game"
            // TODO: Launch only if not running.
            RhinoApp.WriteLine("The {0} command will initialize VR.", EnglishName);
            VrGame SparrowHawkGame = new VrGame(ref doc);
            Thread windowThread = new Thread(() => SparrowHawkGame.Run());
            windowThread.Start();
            return Result.Success;
        }
    }
}
