using System.Runtime.InteropServices;

namespace DebugNET.PInvoke {
    [StructLayout(LayoutKind.Sequential)]
    public struct ImageNTHeaders32 {
        public uint Signature;
        public ImageFileHeader FileHeader;
        public ImageOptionalHeader32 OptionalHeader;
    }
}
