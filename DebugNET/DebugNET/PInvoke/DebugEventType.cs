namespace DebugNET.PInvoke {
    internal enum DebugEventType : uint {
        RIP_EVENT = 9,
        OUTPUT_DEBUG_STRING_EVENT = 8,
        UNLOAD_DLL_DEBUG_EVENT = 7,
        LOAD_DLL_DEBUG_EVENT = 6,
        EXIT_PROCESS_DEBUG_EVENT = 5,
        EXIT_THREAD_DEBUG_EVENT = 4,
        CREATE_PROCESS_DEBUG_EVENT = 3,
        CREATE_THREAD_DEBUG_EVENT = 2,
        EXCEPTION_DEBUG_EVENT = 1,
    }
}
