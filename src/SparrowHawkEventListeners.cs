using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;

namespace SparrowHawk
{
    /// <summary>
    /// Used to send messages from Rhino to SparrowHawk asynchronously.
    /// </summary>
    public class SparrowHawkSignal
    {
        public enum ESparrowHawkSigalType
        {
            InitType, LineType
        };

        public ESparrowHawkSigalType type;
        public float[] data; 
        public SparrowHawkSignal(ESparrowHawkSigalType _type, float[] _data)
        {
            type = _type; data = _data; 
        }
    }

    class SparrowHawkEventListeners
    {
        // TODO: This is dangerous, should implement P-C-Q myself. 
        #region Members
        private readonly EventHandler<RhinoObjectEventArgs> m_add_rhino_object_handler;
        //private Queue<SparrowHawkSignal> mSignalQueue;
        protected ConcurrentQueue<SparrowHawkSignal> mSignalQueue;
        #endregion

        SparrowHawkEventListeners()
        {
            m_add_rhino_object_handler = new EventHandler<RhinoObjectEventArgs>(OnAddRhinoObject);
            mSignalQueue = new ConcurrentQueue<SparrowHawkSignal>();
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
        /// Parses any rhino object that is added, and sends it safely to the 
        /// VR thread if necessary.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnAddRhinoObject(object sender, Rhino.DocObjects.RhinoObjectEventArgs e)
        {
            RhinoApp.WriteLine("La");
            char[] delimiters = {' ', ','};
            if (e.TheObject.Attributes.Name == "")
                return;
            string[] substrings = e.TheObject.Attributes.Name.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            if (substrings.Length == 0) return;


            switch (substrings[0])
            {
                case "init:":
                    RhinoApp.WriteLine("Initted");
                    SparrowHawkSignal s = new SparrowHawkSignal(SparrowHawkSignal.ESparrowHawkSigalType.InitType, new float[substrings.Length - 1]);
                    for (int i = 1; i < substrings.Length; i++)
                    {
                        if (!float.TryParse(substrings[i], out s.data[i - 1]))
                            return;
                    }
                    mSignalQueue.Enqueue(s);
                    break;
            }
        }
        #endregion

        /// <summary>
        /// If any Signals have been recorded, return the earliest one. Otherwise null.
        /// </summary>
        /// <returns></returns>
        public SparrowHawkSignal getOneSignal()
        {
            SparrowHawkSignal s;
            if (mSignalQueue.TryDequeue(out s))
                return s;
            return null;
        }

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


//        public impatientConsume{}

    }
}
