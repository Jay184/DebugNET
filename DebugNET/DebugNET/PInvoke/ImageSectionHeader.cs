using System.Runtime.InteropServices;

namespace DebugNET.PInvoke {
    [StructLayout(LayoutKind.Sequential)]
    public struct ImageSectionHeader {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public char[] SectionName;
        public uint VirtualSize;
        public uint VirtualAddress;
        public uint SizeOfRawData;
        public uint PointerToRawData;
        public uint PointerToRelocations;
        public uint PointerToLinenumbers;
        public ushort NumberOfRelocations;
        public ushort NumberOfLinenumbers;
        public DataSectionFlags Characteristics;

        public string Name => new string(SectionName).TrimEnd('\0');
    }
}
