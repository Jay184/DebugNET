using System;
using DebugNET.PInvoke;

namespace DebugNET {
    public class BreakpointEventArgs : EventArgs {
        public Debugger2 Debugger { get; private set; }
        public Breakpoint2 Breakpoint { get; private set; }
        public IntPtr Address { get; private set; }
        public int ProcessId { get; private set; }
        public int ThreadId { get; private set; }

        public Context Context { get; set; }



        internal BreakpointEventArgs(Debugger2 debugger, Breakpoint2 breakpoint, DebugEvent debugEvent, Context context) {
            Debugger = debugger;
            Breakpoint = breakpoint;
            Address = debugEvent.Exception.ExceptionRecord.Address;
            ProcessId = debugEvent.ProcessId;
            ThreadId = debugEvent.ThreadId;

            Context = context;
        }
    }
}
