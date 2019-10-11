namespace DebugNET.PInvoke {
    internal enum DebugEventType : uint {
        EXCEPTION_DEBUG_EVENT = 0x01,
        CREATE_THREAD_DEBUG_EVENT = 0x02,
        CREATE_PROCESS_DEBUG_EVENT = 0x03,
        EXIT_THREAD_DEBUG_EVENT = 0x04,
        EXIT_PROCESS_DEBUG_EVENT = 0x05,
        LOAD_DLL_DEBUG_EVENT = 0x06,
        UNLOAD_DLL_DEBUG_EVENT = 0x07,
        OUTPUT_DEBUG_STRING_EVENT = 0x08,
        RIP_EVENT = 0x09
    }
}
