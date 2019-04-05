using System;

namespace DebugNET {
    /// <summary>
    /// Represents a breakpoint, used by debuggers to halt at a specific point in the execution of their debuggee.
    /// </summary>
    public class Breakpoint {
        /// <summary>
        /// Executed when this breakpoint is hit by the debugee.
        /// </summary>
        public event EventHandler<BreakpointEventArgs> Hit;

        /// <summary>
        /// The location when to halt the debugee
        /// </summary>
        public readonly IntPtr Address;
        /// <summary>
        /// Determines if the breakpoint is currently enabled
        /// </summary>
        public bool Enabled { get; private set; }

        /// <summary>
        /// <para>Saved instruction at the time this breakpoint was created.</para>
        /// <para>Note that using several breakpoints at the same address should not be permitted.</para>
        /// </summary>
        private readonly byte OldInstruction;
        /// <summary>
        /// Reference to the debugger instance used to write to the memory
        /// </summary>
        protected Debugger Debugger { get; private set; }



        /// <summary>
        /// Creates a new breakpoint instance.
        /// </summary>
        /// <param name="debugger">Reference to the debugger</param>
        internal Breakpoint(Debugger debugger, IntPtr address) {
            Debugger = debugger;
            Address = address;

            OldInstruction = debugger.ReadByte(Address);
        }



        internal protected virtual void OnHit(BreakpointEventArgs e) {
            Hit?.Invoke(Debugger, e);
        }


        /// <summary>
        /// Enables the breakpoint.
        /// </summary>
        public bool Enable() {
            if (Enabled) return false;

            Debugger.WriteByte(Address, 0xCC);
            Enabled = true;
            return true;
        }
        /// <summary>
        /// Disables the breakpoint.
        /// </summary>
        public bool Disable() {
            if (!Enabled) return false;

            Debugger.WriteByte(Address, OldInstruction);
            Enabled = false;
            return false;
        }
    }
}
