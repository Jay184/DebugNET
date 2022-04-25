using System;
using System.Runtime.InteropServices;

namespace DebugNET.PInvoke {
    [StructLayout(LayoutKind.Sequential)]
    internal struct ExceptionRecord {
        public uint Code;
        public uint Flags;
        public IntPtr Record;
        public IntPtr Address;
        public uint NumberParameters;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15, ArraySubType = UnmanagedType.U4)]
        public uint[] Information;
    }
}
