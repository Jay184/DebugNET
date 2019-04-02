using System.Runtime.InteropServices;

namespace DebugNET.PInvoke {
    [StructLayout(LayoutKind.Sequential)]
    internal struct OutputDebugStringInfo {
        [MarshalAs(UnmanagedType.LPStr)] public string lpDebugStringData;
        public ushort fUnicode;
        public ushort nDebugStringLength;
    }
}
