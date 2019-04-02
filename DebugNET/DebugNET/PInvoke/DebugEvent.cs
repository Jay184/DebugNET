using System;
using System.Runtime.InteropServices;

namespace DebugNET.PInvoke {
    [StructLayout(LayoutKind.Sequential)]
    internal struct DebugEvent {
        public DebugEventType DebugEventCode;
        public int ProcessId;
        public int ThreadId;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 86, ArraySubType = UnmanagedType.U1)]
        private byte[] debugInfo;

        public ExceptionDebugInfo Exception => GetDebugInfo<ExceptionDebugInfo>();
        public CreateThreadDebugInfo CreateThread => GetDebugInfo<CreateThreadDebugInfo>();
        public CreateProcessDebugInfo CreateProcessInfo => GetDebugInfo<CreateProcessDebugInfo>();
        public ExitThreadDebugInfo ExitThread => GetDebugInfo<ExitThreadDebugInfo>();
        public ExitProcessDebugInfo ExitProcess => GetDebugInfo<ExitProcessDebugInfo>();
        public LoadDLLDebugInfo LoadDll => GetDebugInfo<LoadDLLDebugInfo>();
        public UnloadDLLDebugInfo UnloadDll => GetDebugInfo<UnloadDLLDebugInfo>();
        public OutputDebugStringInfo DebugString => GetDebugInfo<OutputDebugStringInfo>();
        public RIPInfo RipInfo => GetDebugInfo<RIPInfo>();

        private T GetDebugInfo<T>() where T : struct {
            int structSize = Marshal.SizeOf(typeof(T));
            IntPtr pointer = Marshal.AllocHGlobal(structSize);
            Marshal.Copy(debugInfo, 0, pointer, structSize);

            object result = Marshal.PtrToStructure(pointer, typeof(T));
            Marshal.FreeHGlobal(pointer);
            return (T)result;
        }
    }
}
