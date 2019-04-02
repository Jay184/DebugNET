using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace DebugNET.PInvoke {
    /// <summary>
    /// Collection of P/Invoke methods involved with threads, processes and debugging
    /// </summary>
    internal static class Kernel32 {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, int dwThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr hObject);


        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out int dwNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int dwNumberOfBytesWritten);


        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int ResumeThread(IntPtr hThread);


        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetThreadContext(IntPtr hThread, ref Context lpContext);
        // Get context of thread in x64 application
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetThreadContext(IntPtr hThread, ref Context64 lpContext);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SetThreadContext(IntPtr hThread, ref Context lpContext);
        // Set context of thread in x64 application
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SetThreadContext(IntPtr hThread, ref Context64 lpContext);


        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DebugActiveProcess([In] int dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DebugActiveProcessStop([In] int dwProcessId);


        [DllImport("kernel32.dll", EntryPoint = "WaitForDebugEvent")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WaitForDebugEvent(ref DebugEvent lpDebugEvent, int dwMilliseconds);
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ContinueDebugEvent(int dwProcessId, int dwThreadId, ContinueStatus dwContinueStatus);
    }
}
