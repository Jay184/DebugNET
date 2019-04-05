using System;
using DebugNET.PInvoke;

namespace DebugNET {
    public class BreakpointEventArgs : EventArgs {
        public Context Context { get; set; }
        public IntPtr Address { get; set; }
        public bool Disable { get; set; }
        //public Debugger Debugger { get; private set; }
        public Breakpoint Breakpoint { get; private set; }
        public int ProcessId { get; private set; }
        public int ThreadId { get; private set; }

        internal BreakpointEventArgs(Context context, DebugEvent debugEvent, Breakpoint breakpoint) {
            Context = context;
            Breakpoint = breakpoint;
            Address = breakpoint.Address;
            ProcessId = debugEvent.ProcessId;
            ThreadId = debugEvent.ThreadId;
        }
    }
}
