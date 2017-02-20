using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;

namespace SparrowHawk
{
    class SparrowHawkEventListeners
    {
        #region Members
        private readonly EventHandler<RhinoObjectEventArgs> m_add_rhino_object_handler;
        #endregion

        SparrowHawkEventListeners()
        {
            m_add_rhino_object_handler = new EventHandler<RhinoObjectEventArgs>(OnAddRhinoObject);
        }

        public bool IsEnabled { get; private set; }

        /// <summary>
        /// The one and only EventWatcherHandlers object
        /// </summary>
        static SparrowHawkEventListeners g_instance;


        /// <summary>
        /// Returns the one and only EventWatcherHandlers object
        /// </summary>
        public static SparrowHawkEventListeners Instance
        {
            get { return g_instance ?? (g_instance = new SparrowHawkEventListeners()); }
        }

        #region Events
        /// <summary>
        /// Triggered as long as one surface contained geometry is created.
        /// in auto mode, this will trigger the timer to decide to add it to the printing queue.
        /// if the new object is a cutter, because the cutter by itself is not a printing object,
        /// won't put it into printing list. but check the cutting timer event (which is the same as printing timer)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnAddRhinoObject(object sender, Rhino.DocObjects.RhinoObjectEventArgs e)
        {
            RhinoApp.WriteLine("La");
        }
        #endregion


        public void Enable(bool enable)
        {
            if (enable != IsEnabled)
            {
                if (enable)
                {
                    RhinoDoc.AddRhinoObject += m_add_rhino_object_handler;
                }
                else
                {
                    RhinoDoc.AddRhinoObject -= m_add_rhino_object_handler;
                }
            }
            IsEnabled = enable;
        }

    }
}
