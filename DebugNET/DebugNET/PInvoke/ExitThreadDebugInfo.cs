using System.Runtime.InteropServices;

namespace DebugNET.PInvoke {
    [StructLayout(LayoutKind.Sequential)]
    internal struct ExitThreadDebugInfo {
        public uint dwExitCode;
    }

}
