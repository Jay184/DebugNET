using System.Runtime.InteropServices;

namespace DebugNET.PInvoke {
    [StructLayout(LayoutKind.Sequential)]
    public struct ImageNTHeaders64 {
        public uint Signature;
        public ImageFileHeader FileHeader;
        public ImageOptionalHeader64 OptionalHeader;
    }
}
