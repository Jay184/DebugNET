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
            Process[] processes = Process.GetProcessesByName(name);
            Process process = processes.Length > 0 ? processes[0] : null;

            try {
                while (process != null && !process.HasExited) {
                    using (Debugger2 debugger = new Debugger2(process)) {
                        debugger.Attached += (sender, e) => Console.WriteLine("Attached!");
                        debugger.Detached += (sender, e) => Console.WriteLine("Detached!");
                        debugger.ProcessExited += (sender, e) => Console.WriteLine("Process exited!");

                        IntPtr opcodeAddress = debugger.GetAddress($"\"{ name }.exe\"+11D18");
                        IntPtr seeked = debugger.Seek($"{ name }.exe", 0x89, 0x45, 0xD0);

                        using (CancellationTokenSource tokenSource = new CancellationTokenSource()) {
                            // Attaching to the process
                            Task listener = debugger.AttachAsync(tokenSource.Token);

                            Breakpoint2 breakpoint = debugger.Breakpoints.Get(opcodeAddress);
                            //breakpoint.Hit += (sender, e) => { Console.WriteLine(e.Context.Eax); };
                            //breakpoint.Condition = e => e.Context.Eax < 1024;
                            //breakpoint.Enable(debugger, opcodeAddress);

                            tokenSource.CancelAfter(2000); // Will happen automatically when debugger is disposed.
                            listener.Wait();
                        }
                    }

                    Thread.Sleep(200);
                }
            } catch (ProcessNotFoundException) {
                Console.WriteLine("Cannot create debugger. Process not found");
            } catch (AggregateException ex) {
                throw ex.InnerException; // Could be anything
            } catch (OperationCanceledException) {
                Console.WriteLine("Listener was canceled");
            } catch (AttachException ex) {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Done.");
            //Console.ReadKey();
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
