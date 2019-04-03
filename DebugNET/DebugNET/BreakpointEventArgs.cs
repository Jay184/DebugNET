using System;
using DebugNET.PInvoke;

namespace DebugNET {
    public class BreakpointEventArgs : EventArgs {
        public Context Context { get; set; }
        public Debugger Debugger { get; private set; }
        public int ProcessId { get; private set; }
        public int ThreadId { get; private set; }

        internal BreakpointEventArgs(Debugger debugger, Context context, DebugEvent debugEvent) {
            Debugger = debugger;
            Context = context;
            ProcessId = debugEvent.ProcessId;
            ThreadId = debugEvent.ThreadId;
        }
    }
}
