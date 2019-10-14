using System;
using System.Diagnostics;

namespace DebugNET {
    public class ProcessExitedEventArgs : EventArgs {
        public Process Process { get; private set; }



        internal ProcessExitedEventArgs(Process process) {
            Process = process;
        }
    }
}
