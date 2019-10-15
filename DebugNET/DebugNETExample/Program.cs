using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DebugNET;

// Avoid conflict with .Net Debugger class in System.Diagnostics
using Debugger = DebugNET.Debugger;

namespace DebugNETExample {
    public static class Program {
        public const string name = "DebugeeProgram";

        /// <summary>
        /// <para>The main entry point for the application.</para>
        /// <para>This method demonstrates the usage of the debugger.</para>
        /// </summary>
        [STAThread]
        public static void Main() {
            // Get process instance.
            Process[] processes = Process.GetProcessesByName(name);
            Process process = processes.Length > 0 ? processes[0] : null;

            // Start the process when not running.
            if (process == null) process = Process.Start($"{ name }.exe");


            try {
                // Using statement to take care of disposing the debugger.
                using (Debugger debugger = new Debugger(process)) {

                    // Register events.
                    debugger.Attached += (sender, e) => Console.WriteLine($"Attached to { e.Process.ProcessName }!");
                    debugger.Detached += (sender, e) => Console.WriteLine($"Detached from { e.Process.ProcessName }!");
                    debugger.ProcessExited += (sender, e) => Console.WriteLine($"{ e.Process.ProcessName } exited!");


                    // Retrieve address by code.
                    IntPtr codeAddress = debugger.Seek($"{ name }.exe", 0x89, 0x45, 0xD0);


                    // Retrieve address by module-offset pair.
                    IntPtr address = debugger.GetAddress($"\"{ name }.exe\"+13648");


                    // Resolving pointers should be wrapped in a try catch block.
                    try {
                        IntPtr invalidAddress = debugger.GetAddress(IntPtr.Zero);
                    } catch (AccessViolationException) { // Cannot read pointer.
                    } catch (ArgumentException) { // Provided an invalid pointer.
                    }


                    // Allocate memory in the process with rwx access.
                    IntPtr memory = debugger.AllocateMemory();
                    debugger.WriteByte(memory, 0xFF);
                    debugger.FreeMemory(memory);


                    // Loading a library into the process.
                    IntPtr handle = debugger.InjectLibrary("inject.dll");
                    IntPtr parameter = debugger.AllocateMemory();
                    debugger.WriteUInt32(parameter, 22);
                    debugger.WriteUInt32(parameter + 4, 20);


                    // Executing remote functions. See inject project to see how the functions have to be defined for this to work.
                    Task<uint> echoTask = debugger.ExecuteRemoteFunctionAsync("inject.dll", handle, "echo", 12345678);
                    Task<uint> fibonacciTask = debugger.ExecuteRemoteFunctionAsync("inject.dll", handle, "fibonacci", 10);
                    Task<uint> addTask = debugger.ExecuteRemoteFunctionAsync("inject.dll", handle, "add", parameter);

                    // Tell the tasks what to do when they are done
                    Action<Task<uint>> printTaskResult = t => Console.WriteLine($"Function executed: { t.Result }");
                    echoTask.ContinueWith(printTaskResult);
                    fibonacciTask.ContinueWith(printTaskResult);
                    addTask.ContinueWith(printTaskResult);

                    // When all functions ran, release the library and the allocated memory
                    Task.WhenAll(echoTask, fibonacciTask, addTask).ContinueWith(t => {
                        debugger.FreeMemory(parameter);
                        debugger.FreeRemoteLibrary(handle);
                    });


                    try {
                        /*
                        Listening can be wrapped in a DebugListener class with the following members:
                            private CancellationTokenSource TokenSource;
                            private DebugListener()
                            public Task GetListener()
                            public void Cancel()
                            ~DebugListener()
                        */

                        // Using statement around a CancellationTokenSource
                        using (CancellationTokenSource tokenSource = new CancellationTokenSource()) {

                            // Attaching to the process.
                            Task listener = debugger.AttachAsync(tokenSource.Token);

                            // Preferred way to create a breakpoint.
                            debugger.Breakpoints.Add(codeAddress,
                            (sender, e) => Console.WriteLine(e.Context.Eax),
                            e => e.Context.Eax < 200);

                            // Stopping the debugger.
                            tokenSource.CancelAfter(3000);
                            listener.Wait();
                        }
                    } catch (AggregateException ex) {
                        foreach (Exception exception in ex.InnerExceptions) {

                            if (exception is AttachException) {
                                // Cannot attach to process.
                                Console.WriteLine(exception.Message);

                            } else throw exception;

                        }
                    }
                }

                Console.WriteLine("Done.");
                Console.ReadKey();
            } catch (ProcessNotFoundException ex) {
                Console.WriteLine($"Process cannot be found. { ex.InnerException.Message }");
                Console.ReadKey();
            }
        }
    }
}
