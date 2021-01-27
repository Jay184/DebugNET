using System;

namespace DebugNET {
    public class Breakpoint {
        public event EventHandler<BreakpointEventArgs> Hit;
        private const byte BreakPointInstruction = 0xCC;


        public Func<BreakpointEventArgs, bool> Condition { get; set; }

        public bool Enabled { get; internal set; }
        public byte Instruction { get; internal set; }
        
        

        internal Breakpoint(byte instruction) {
            Instruction = instruction;
        }



        /// <summary>
        /// Enables the breakpoint.
        /// </summary>
        public bool Enable(Debugger debugger, IntPtr address) {
            debugger.WaitHandle.WaitOne(250);

            if (Enabled || !debugger.IsAttached) return false;
            
            //if (!debugger.IsAttached) throw new AttachException("The debugger is not attached. Setting this breakpoint could crash the program.");

            debugger.WriteByte(address, BreakPointInstruction);
            Enabled = true;
            return true;
        }
        /// <summary>
        /// Disables the breakpoint.
        /// </summary>
        public bool Disable(Debugger debugger, IntPtr address) {
            if (!Enabled) return false;

            debugger.WriteByte(address, Instruction);
            Enabled = false;
            return true;
        }

        internal protected virtual void OnHit(BreakpointEventArgs e) {
            Hit?.Invoke(this, e);
        }


        public override string ToString() => Instruction.ToString("X2");
    }
}
