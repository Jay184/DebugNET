using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DebugNET.PInvoke;

namespace DebugNETExample {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            const string name = "DebugeeProgram";
            DebugNET.Debugger debugger = new DebugNET.Debugger(name);
            debugger.SetBreakpoint("\"DebugeeProgram.exe\"+13BD8");

            debugger.BreakpointHit += (sender, e) => {
                Context c = e.Context;

                Console.WriteLine(c.Eax);

                c.Eax *= 2;
                e.Context = c;
            };

            debugger.ListenToBreakpoints();
            //debugger.WriteInt32((IntPtr)0x010FF448, 20);

            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new MainForm());
        }
    }
}
