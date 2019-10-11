using System;

namespace DebugNET.PInvoke {
    [Flags]
    internal enum ThreadAccess {
        TERMINATE = 0x01,
        SUSPEND_RESUME = 0x02,
        GET_CONTEXT = 0x08,
        SET_CONTEXT = 0x10,
        SET_INFORMATION = 0x20,
        QUERY_INFORMATION = 0x40,
        SET_THREAD_TOKEN = 0x80,
        IMPERSONATE = 0x0100,
        DIRECT_IMPERSONATION = 0x0200 
    }
}
