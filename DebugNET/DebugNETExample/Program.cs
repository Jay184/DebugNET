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
            CancellationTokenSource tokenSource;

            try {
                using (Debugger2 debugger = new Debugger2(process)) {
                    // Reading values
                    IntPtr address = (IntPtr)( ( ( 0x008FFD70 ) ) );
                    byte value = debugger.ReadByte(address);

                    // Writing values
                    value--;
                    debugger.WriteByte(address, value);

                    tokenSource = new CancellationTokenSource();
                    var listener = debugger.AttachAsync(tokenSource.Token);


                    // Breakpoints
                    IntPtr opcodeAddress = debugger.GetAddress("\"DebugeeProgram.exe\"+13BD8");

                    tokenSource.Cancel(true);
                    listener.Wait();
                    //}

                    // Thread creation

                    debugger.Detach(); // TODO make sure only one listener can exist with if(attached) return or something
                    Console.WriteLine("Detached");
                    listener.Wait();
                    Console.WriteLine("Listener ended");
                }
            } catch (ProcessNotFoundException ex) {
                Console.WriteLine(ex.Message);
            } catch (AggregateException ex) {
                throw ex.InnerException;
            } catch (OperationCanceledException ex) {
                Console.WriteLine(ex.Message);
            }

            Console.ReadKey();
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
