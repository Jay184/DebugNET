using System.Runtime.InteropServices;

namespace DebugNET.PInvoke {
    [StructLayout(LayoutKind.Sequential)]
    internal struct RIPInfo {
        public uint dwError;
        public uint dwType;
    }
}
