namespace DebugNET.PInvoke {
    internal enum ObjectWaitEvent : uint {
        WAIT_OBJECT_0 = 0,
        WAIT_IO_COMPLETION = 0xC0,
        WAIT_ABANDONED = 0x80,
        WAIT_TIMEOUT = 0x0102,
        WAIT_FAILED = 0xFFFFFFFF
    }
}
