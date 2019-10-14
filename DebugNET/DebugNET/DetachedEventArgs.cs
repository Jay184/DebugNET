using System;
using System.Diagnostics;

namespace DebugNET {
    public class DetachedEventArgs : EventArgs {
        public Process Process { get; private set; }



        internal DetachedEventArgs(Process process) {
            Process = process;
        }
    }
}
