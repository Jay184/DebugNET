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
                Breakpoint breakpoint = debugger.SetBreakpoint("\"DebugeeProgram.exe\"+13BD8");

                if (breakpoint != null) {
                    int i = 0;

                    breakpoint.Hit += (sender, e) => {
                        Context c = e.Context;
                        Debugger dbg = (Debugger)sender;

                        Console.Write($"> Received { c.Eax } @ { e.Address }\r\n> Enter number: ");

                        string input = Console.ReadLine();

                        if (input.Equals("quit")) {
                            dbg.StopListen();
                            return;
                        }

                        if (uint.TryParse(input, out uint newValue)) {
                            //debugger.WriteUInt32((IntPtr)xxx, newValue);
                            c.Eax = newValue;
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
