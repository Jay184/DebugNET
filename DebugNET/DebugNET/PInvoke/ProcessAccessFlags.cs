using System;

namespace DebugNET.PInvoke {
    [Flags]
    internal enum ProcessAccessFlags : uint {
        ALL = 0x1F0FFF,
        TERMINATE = 0x000001,
        CREATE_THREAD = 0x000002,
        SET_SESSIONID = 0x000004,
        VM_OPERATION = 0x000008,
        VM_READ = 0x000010,
        VM_WRITE = 0x000020,
        DUPLICATE_HANDLE = 0x000040,
        CREATE_PROCESS = 0x0000080,
        SET_QUOTA = 0x000100,
        SET_INFORMATION = 0x000200,
        QUERY_INFORMATION = 0x000400,
        QUERY_LIMITED_INFORMATION = 0x001000,
        SYNCHRONIZE = 0x100000
    }
}
