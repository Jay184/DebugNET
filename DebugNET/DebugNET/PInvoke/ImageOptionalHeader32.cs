using System.Runtime.InteropServices;

namespace DebugNET.PInvoke {
    [StructLayout(LayoutKind.Sequential)]
    public struct ImageOptionalHeader32 {
        // Standard COFF Fields
        public MagicType Magic;
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public uint SizeOfCode;
        public uint SizeOfInitializedData;
        public uint SizeOfUninitializedData;
        public uint AddressOfEntryPoint;
        public uint BaseOfCode;
        public uint BaseOfData;
        // Windows specific fields
        public uint ImageBase;
        public uint SectionAlignment;
        public uint FileAlignment;
        public ushort MajorOperatingSystemVersion;
        public ushort MinorOperatingSystemVersion;
        public ushort MajorImageVersion;
        public ushort MinorImageVersion;
        public ushort MajorSubsystemVersion;
        public ushort MinorSubsystemVersion;
        public uint Win32VersionValue;
        public uint SizeOfImage;
        public uint SizeOfHeaders;
        public uint CheckSum;
        public SubSystemType Subsystem;
        public DLLCharacteristicsType DllCharacteristics;
        public uint SizeOfStackReserve;
        public uint SizeOfStackCommit;
        public uint SizeOfHeapReserve;
        public uint SizeOfHeapCommit;
        public uint LoaderFlags;
        public uint NumberOfRvaAndSizes;
        // Data directories
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=16)]
        public ImageDataDirectory[] DataDirectory;

        public ImageDataDirectory IMAGE_DIRECTORY_ENTRY_EXPORT => DataDirectory[0];
        public ImageDataDirectory IMAGE_DIRECTORY_ENTRY_IMPORT => DataDirectory[1];
        public ImageDataDirectory IMAGE_DIRECTORY_ENTRY_RESOURCE => DataDirectory[2];
        public ImageDataDirectory IMAGE_DIRECTORY_ENTRY_EXCEPTION => DataDirectory[3];
        public ImageDataDirectory IMAGE_DIRECTORY_ENTRY_SECURITY => DataDirectory[4];
        public ImageDataDirectory IMAGE_DIRECTORY_ENTRY_BASERELOC => DataDirectory[5];
        public ImageDataDirectory IMAGE_DIRECTORY_ENTRY_DEBUG => DataDirectory[6];
        public ImageDataDirectory IMAGE_DIRECTORY_ENTRY_ARCHITECTURE => DataDirectory[7];
        public ImageDataDirectory IMAGE_DIRECTORY_ENTRY_GLOBALPTR => DataDirectory[8];
        public ImageDataDirectory IMAGE_DIRECTORY_ENTRY_TLS => DataDirectory[9];
        public ImageDataDirectory IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG => DataDirectory[10];
        public ImageDataDirectory IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT => DataDirectory[11];
        public ImageDataDirectory IMAGE_DIRECTORY_ENTRY_IAT => DataDirectory[12];
        public ImageDataDirectory IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT => DataDirectory[13];
        public ImageDataDirectory IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR => DataDirectory[14];
    }
}
