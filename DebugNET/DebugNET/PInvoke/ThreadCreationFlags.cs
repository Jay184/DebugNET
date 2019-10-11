using System;

namespace DebugNET.PInvoke {
    [Flags]
    internal enum ThreadCreationFlags : uint {
        None = 0,
        CREATE_SUSPENDED = 0x04,
        STACK_SIZE_PARAM_IS_A_RESERVATION = 0x00010000
    }
}
