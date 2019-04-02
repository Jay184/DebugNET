using System;
using System.Runtime.InteropServices;

namespace DebugNET.PInvoke {
    [StructLayout(LayoutKind.Sequential)]
    internal struct LoadDLLDebugInfo {
        public IntPtr hFile;
        public IntPtr lpBaseOfDll;
        public uint dwDebugInfoFileOffset;
        public uint nDebugInfoSize;
        public IntPtr lpImageName;
        public ushort fUnicode;
    }
}
