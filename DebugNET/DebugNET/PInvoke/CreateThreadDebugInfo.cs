using System;
using System.Runtime.InteropServices;

namespace DebugNET.PInvoke {
    internal delegate uint PThreadStartRoutine(IntPtr lpThreadParameter);

    [StructLayout(LayoutKind.Sequential)]
    internal struct CreateThreadDebugInfo {
        public IntPtr hThread;
        public IntPtr lpThreadLocalBase;
        public PThreadStartRoutine lpStartAddress;
    }
}
