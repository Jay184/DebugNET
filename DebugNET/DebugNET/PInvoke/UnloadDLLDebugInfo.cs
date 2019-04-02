using System;
using System.Runtime.InteropServices;

namespace DebugNET.PInvoke {
    [StructLayout(LayoutKind.Sequential)]
    internal struct UnloadDLLDebugInfo {
        public IntPtr lpBaseOfDll;
    }
}
