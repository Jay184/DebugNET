using System;
using System.Runtime.InteropServices;

namespace DebugNET.PInvoke {
    [StructLayout(LayoutKind.Sequential)]
    internal struct CreateProcessDebugInfo {
        public IntPtr hFile;
        public IntPtr hProcess;
        public IntPtr hThread;
        public IntPtr lpBaseOfImage;
        public uint dwDebugInfoFileOffset;
        public uint nDebugInfoSize;
        public IntPtr lpThreadLocalBase;
        public PThreadStartRoutine lpStartAddress;
        public IntPtr lpImageName;
        public ushort fUnicode;
    }
}
