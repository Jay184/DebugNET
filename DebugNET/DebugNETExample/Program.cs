using System;
using System.Threading.Tasks;
using DebugNET;
using DebugNET.PInvoke;

namespace DebugNETExample {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            const string name = "DebugeeProgram";

            try {
                Debugger debugger = new Debugger(name);

                IntPtr addr = debugger.Seek("DebugeeProgram.exe", 0x89, 0x45, 0xD0); // DebugeeProgram.exe+13BD8
                Breakpoint breakpoint = debugger.SetBreakpoint(addr);


                if (breakpoint != null) {
                    breakpoint.Hit += (sender, e) => {
                        Debugger dbg = (Debugger)sender;

                        Console.Write($"> Received { e.Context.Eax } @ { e.Address.ToString("X8") }\r\n> Enter number: ");

                        string input = Console.ReadLine();

                        if (input.Equals("quit")) {
                            dbg.StopListen();
                            return;
                        }

                        if (uint.TryParse(input, out uint newValue)) {
                            IntPtr variableAddress = (IntPtr)(e.Context.Edx + 0x128);
                            debugger.WriteUInt32(variableAddress, newValue);

                            Context c = e.Context;
                            c.Eax = newValue;
                            e.Context = c;
                        }
                    };
                }

                Task t = debugger.StartListen();
                t.Wait();

            } catch (ProcessNotFoundException ex) {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
