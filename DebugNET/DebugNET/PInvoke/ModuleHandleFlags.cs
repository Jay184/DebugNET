using System;

namespace DebugNET.PInvoke {
    [Flags]
    internal enum ModuleHandleFlags : uint {
        GET_MODULE_HANDLE_EX_FLAG_PIN = 0x01,
        GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT = 0x02,
        GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS = 0x04
    }
}
