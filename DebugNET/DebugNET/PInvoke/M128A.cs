using System.Runtime.InteropServices;

namespace DebugNET.PInvoke {
    [StructLayout(LayoutKind.Sequential)]
    public struct M128A {
        public ulong High;
        public long Low;

        public override string ToString() {
            return string.Format("High:{0}, Low:{1}", High, Low);
        }
    }
}
