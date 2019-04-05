using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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
                    breakpoint.Hit += (sender, e) => {
                        Context c = e.Context;

                        Console.WriteLine(c.Eax);

                        c.Eax *= 2;
                        e.Context = c;
                    };
                }

                CancellationTokenSource tokenSource;
                Task t = debugger.ListenToBreakpoints(out tokenSource);
                tokenSource.CancelAfter(5000);
                t.Wait();
                ;
            } catch (ProcessNotFoundException ex) {
                Console.WriteLine(ex.Message);
            }
            
            //debugger.WriteInt32((IntPtr)0x010FF448, 20);

            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new MainForm());
        }
    }
}
