using System;

namespace DebugNET.PInvoke {
    [Flags]
    internal enum ProcessAccessFlags : uint {
        ALL = 0x001F0FFF,
        TERMINATE = 0x01,
        CREATE_THREAD = 0x02,
        SET_SESSIONID = 0x04,
        VM_OPERATION = 0x08,
        VM_READ = 0x10,
        VM_WRITE = 0x20,
        DUPLICATE_HANDLE = 0x40,
        CREATE_PROCESS = 0x80,
        SET_QUOTA = 0x0100,
        SET_INFORMATION = 0x0200,
        QUERY_INFORMATION = 0x0400,
        QUERY_LIMITED_INFORMATION = 0x1000,
        SYNCHRONIZE = 0x00100000
    }
}
