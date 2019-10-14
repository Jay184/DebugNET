using System;
using System.Diagnostics;

namespace DebugNET {
    public class AttachedEventArgs : EventArgs {
        public Process Process { get; private set; }
        public IntPtr ProcessHandle { get; private set; }



        internal AttachedEventArgs(Process process, IntPtr processHandle) {
            Process = process;
            ProcessHandle = processHandle;
        }
    }
}
