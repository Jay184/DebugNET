using System;
using System.Diagnostics;
using System.Threading;
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
            Process process = Process.GetProcessesByName(name)[0];

            try {
                using (Debugger2 debugger = new Debugger2(process)) {
                    debugger.Attached += (sender, e) => Console.WriteLine("Attached!");
                    debugger.Detached += (sender, e) => Console.WriteLine("Detached!");

                    IntPtr opcodeAddress = debugger.GetAddress("\"DebugeeProgram.exe\"+13BD8");

                    
                    using (CancellationTokenSource tokenSource = new CancellationTokenSource()) {
                        // Attaching to the process
                        Task listener = debugger.AttachAsync(tokenSource.Token);

                        Thread.Sleep(1000);

                        debugger.Detach(); // Will happen automatically when disposed.
                        listener.Wait();
                        Console.WriteLine("Listener ended");
                    }

                    debugger.Process.WaitForExit();
                }
            } catch (ProcessNotFoundException) {
                Console.WriteLine("Cannot create debugger. Process not found");
            } catch (AggregateException ex) {
                throw ex.InnerException; // Could be anything
            } catch (OperationCanceledException) {
                Console.WriteLine("Listener was canceled");
            }

            return;

            /*
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
            */
        }
    }
}
