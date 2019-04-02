﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DebugNETExample {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            const string name = "bhd";
            DebugNET.Debugger debugger = new DebugNET.Debugger(name);
            debugger.SetBreakpoint("\"bhd.exe\"+31666");

            debugger.ListenToBreakpoints().RunSynchronously();

            //debugger.WriteInt32((IntPtr)0x010FF448, 20);

            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new MainForm());
        }
    }
}
