using DebugNET.PInvoke;

namespace System.Diagnostics {
    public static class ProcessExtensions {
        public static void Suspend(this Process process) {
            foreach (ProcessThread thread in process.Threads) {
                IntPtr handle = Kernel32.OpenThread(ThreadAccess.SUSPEND_RESUME, false, thread.Id);

                if (handle == IntPtr.Zero) continue;

                Kernel32.SuspendThread(handle);
                Kernel32.CloseHandle(handle);
            }
        }
        public static void Resume(this Process process) {
            foreach (ProcessThread thread in process.Threads) {
                IntPtr handle = Kernel32.OpenThread(ThreadAccess.SUSPEND_RESUME, false, thread.Id);

                if (handle == IntPtr.Zero) continue;

                Kernel32.ResumeThread(handle);
                Kernel32.CloseHandle(handle);
            }
        }
    }
}
