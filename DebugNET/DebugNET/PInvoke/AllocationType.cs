using System;

namespace DebugNET.PInvoke {
    [Flags]
    public enum AllocationType : uint {
        // Alloc Types
        MEM_COMMIT = 0x1000,
        MEM_RESERVE = 0x2000,
        MEM_RESET = 0x80000,
        MEM_PHYSICAL = 0x400000,
        MEM_TOP_DOWN = 0x100000,
        MEM_RESET_UNDO = 0x1000000,
        MEM_LARGE_PAGES = 0x20000000,

        // Free Types
        MEM_COALESCE_PLACEHOLDERS = 0x01,
        MEM_PRESERVE_PLACEHOLDER = 0x02,
        MEM_DECOMMIT = 0x4000,
        MEM_RELEASE = 0x8000
    }
}
