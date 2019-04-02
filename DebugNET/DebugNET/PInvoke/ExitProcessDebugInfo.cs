using System.Runtime.InteropServices;

namespace DebugNET.PInvoke {
    [StructLayout(LayoutKind.Sequential)]
    internal struct ExitProcessDebugInfo {
        public uint dwExitCode;
    }
}
