using System.Runtime.InteropServices;

namespace DebugNET.PInvoke {
    [StructLayout(LayoutKind.Sequential)]
    internal struct ExceptionDebugInfo {
        public ExceptionRecord ExceptionRecord;
        public uint dwFirstChance;
    }
}
