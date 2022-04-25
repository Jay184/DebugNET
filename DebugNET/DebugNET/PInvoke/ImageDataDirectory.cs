using System.Runtime.InteropServices;

namespace DebugNET.PInvoke {
    [StructLayout(LayoutKind.Sequential)]
    public struct ImageDataDirectory {
        public uint VirtualAddress;
        public uint Size;
    }
}
