using System.Runtime.InteropServices;

namespace DebugNET.PInvoke {
    [StructLayout(LayoutKind.Sequential)]
    public struct ImageFileHeader {
        public MachineType Machine;
        public ushort NumberOfSections;
        public uint TimeDateStamp;
        public uint PointerToSymbolTable;
        public uint NumberOfSymbols;
        public ushort SizeOfOptionalHeader;
        public ImageFileCharacteristics Characteristics;


        public bool Is32Bit => Characteristics.HasFlag(ImageFileCharacteristics.IMAGE_FILE_32BIT_MACHINE);
    }
}
