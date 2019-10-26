using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DebugNET.PInvoke;

namespace DebugNET {
    public class Debugger : IDisposable {
        // These should be in PInvoke.ExceptionCodeFlags but I cannot find an offcial resource listing all values.
        protected const uint EXCEPTION_BREAKPOINT = 0x80000003;
        protected const uint EXCEPTION_SINGLE_STEP = 0x80000004;
        protected const string OFFSETPATTERN = @"(\+|\-)?(?:0x)?([a-fA-F0-9]{1,8})";
        private const ContinueStatus CONTINUESTATUS = ContinueStatus.DBG_CONTINUE;
        private const ProcessAccessFlags ACCESS =
                    ProcessAccessFlags.CREATE_THREAD |
                    ProcessAccessFlags.QUERY_INFORMATION |
                    ProcessAccessFlags.VM_OPERATION |
                    ProcessAccessFlags.VM_READ |
                    ProcessAccessFlags.VM_WRITE |
                    ProcessAccessFlags.SYNCHRONIZE;
        private const ThreadAccessFlags THREADACCESS =
                    ThreadAccessFlags.GET_CONTEXT |
                    ThreadAccessFlags.SET_CONTEXT;
        private const ContextFlags CONTEXTFLAGS =
                    ContextFlags.CONTEXT_CONTROL |
                    ContextFlags.CONTEXT_INTEGER;
        private const int DEBUGTIMEOUT = 1000;

        
        public event EventHandler<AttachedEventArgs> Attached;
        public event EventHandler<DetachedEventArgs> Detached;
        public event EventHandler<ProcessExitedEventArgs> ProcessExited;


        public bool IsAttached { get; private set; }
        public BreakpointCollection Breakpoints { get; private set; }
        public Process Process { get; private set; }
        protected IntPtr ProcessHandle { get; private set; }
        internal AutoResetEvent WaitHandle { get; private set; }


        private bool isDisposed;
        private bool doBreak;
        private BreakpointEventArgs lastEvent;
        protected CancellationToken CancellationToken { get; private set; }



        public Debugger(string processName) {
            WaitHandle = new AutoResetEvent(false);

            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0) {
                foreach (Process p in processes) {
                    if (!p.HasExited && p.Responding) {
                        Process = p;
                        break;
                    }
                }

                if (Process == null) throw new ProcessNotFoundException("Cannot open the process.",
                    new ArgumentNullException("Process is null."));

                IntPtr handle = Kernel32.OpenProcess(ACCESS, false, Process.Id);

                if (handle == IntPtr.Zero) throw new ProcessNotFoundException("Cannot open the process.",
                    new InvalidOperationException("Cannot get Process handle."));

                ProcessHandle = handle;
                Breakpoints = new BreakpointCollection(this);

            } else throw new ProcessNotFoundException("Cannot find the process.",
                new NullReferenceException("Process was null."));
        }
        public Debugger(int processID) : this(Process.GetProcessById(processID)) { }
        public Debugger(Process process) {
            WaitHandle = new AutoResetEvent(false);
            Process = process ?? throw new ProcessNotFoundException("Cannot find the process.",
                new NullReferenceException("Process was null."));

            IntPtr handle = Kernel32.OpenProcess(ACCESS, false, process.Id);

            if (handle == IntPtr.Zero) throw new ProcessNotFoundException("Cannot open the process.",
                new InvalidOperationException("Cannot get Process handle."));

            ProcessHandle = handle;
            Breakpoints = new BreakpointCollection(this);
        }



        /// <summary>Attaches the debugger to the process. Blocks the current thread.</summary>
        /// <exception cref="AttachException">When already attached.</exception>
        public void Attach() => AttachAsync(CancellationToken.None).Wait();
        /// <summary>Attaches the debugger to the process asynchronously.</summary>
        /// <returns>Task that represents the listener.</returns>
        /// <exception cref="AttachException">When already attached.</exception>
        public async Task AttachAsync() => await AttachAsync(CancellationToken.None);
        /// <summary>Attaches the debugger to the process asynchronously.</summary>
        /// <param name="token">Token used to cancel the debugging.</param>
        /// <returns>Task that represents the listener.</returns>
        /// <exception cref="AttachException">When already attached.</exception>
        public async Task AttachAsync(CancellationToken token) {
            if (IsAttached) throw new AttachException("Cannot attach twice.");

            bool isBeingDebugged = false;
            bool hasChecked = Kernel32.CheckRemoteDebuggerPresent(ProcessHandle, ref isBeingDebugged);

            if (hasChecked && isBeingDebugged) throw new AttachException("Process already being debugged.");
            await Task.Run(() => DebuggingLoop(token)).ConfigureAwait(true);
        }

        private void DebuggingLoop(CancellationToken token) {
            // Start debugging (This has to happen on the same thread.)
            if (Kernel32.DebugActiveProcess(Process.Id)) {
                Kernel32.DebugSetProcessKillOnExit(false);
                IsAttached = true;
                WaitHandle.Set();
                OnAttached(Process, ProcessHandle);
            }

            while (IsAttached) {
                DebugEvent debugEvent = new DebugEvent();

                // Stop when client stops the debugger.
                if (( token.IsCancellationRequested && lastEvent == null ) || Process.HasExited) break;

                // Break at any point if requested
                if (doBreak) Kernel32.DebugBreakProcess(ProcessHandle);

                // Wait for debug event
                if (Kernel32.WaitForDebugEvent(ref debugEvent, 0)) {
                    Process.Refresh();

                    #region Handle debug event
                    switch (debugEvent.DebugEventCode) {
                        case DebugEventType.CREATE_PROCESS_DEBUG_EVENT:
                            OnCreateProcess(ref debugEvent);
                            break;
                        case DebugEventType.LOAD_DLL_DEBUG_EVENT:
                            OnLoadDLL(ref debugEvent);
                            break;
                        case DebugEventType.CREATE_THREAD_DEBUG_EVENT:
                            OnCreateThread(ref debugEvent);
                            break;
                        case DebugEventType.EXCEPTION_DEBUG_EVENT:
                            OnException(ref debugEvent);
                            break;
                        case DebugEventType.EXIT_THREAD_DEBUG_EVENT:
                            OnExitThread(ref debugEvent);
                            break;
                        case DebugEventType.EXIT_PROCESS_DEBUG_EVENT:
                            OnExitProcess(ref debugEvent);
                            break;
                    }
                    #endregion

                    Kernel32.ContinueDebugEvent(debugEvent.ProcessId, debugEvent.ThreadId, CONTINUESTATUS);
                }
            }

            // Stop when process exited.
            if (Process.HasExited) {
                IsAttached = false;
                WaitHandle.Reset();
                OnProcessExited(Process);
                foreach (var item in Breakpoints) {
                    item.Value.Enabled = false;
                }
                OnDetached(Process);
                return;
            }

            // Disable all breakpoints
            Process.Suspend();
            foreach (var item in Breakpoints) {
                if (item.Value.Enabled) item.Value.Disable(this, item.Key);
            }
            Process.Resume();

            // Stop debugging
            if (Kernel32.DebugActiveProcessStop(Process.Id)) {
                IsAttached = false;
                WaitHandle.Reset();
                OnDetached(Process);
            }
        }

        private void OnCreateProcess(ref DebugEvent debugEvent) { }
        private void OnLoadDLL(ref DebugEvent debugEvent) { }
        private void OnCreateThread(ref DebugEvent debugEvent) { }
        private void OnException(ref DebugEvent debugEvent) {
            switch (debugEvent.Exception.ExceptionRecord.Code) {
                case EXCEPTION_BREAKPOINT:
                    if (doBreak) {
                        doBreak = false;
                        OnBreak(ref debugEvent);
                    } else /* if (breakpoint exist @ addr) */{
                        OnBreakpoint(ref debugEvent);
                    }
                    break;
                case EXCEPTION_SINGLE_STEP:
                    OnSingleStep(ref debugEvent);
                    break;
            }
        }
        private void OnExitThread(ref DebugEvent debugEvent) { }
        private void OnExitProcess(ref DebugEvent debugEvent) { }

        private void OnBreakpoint(ref DebugEvent debugEvent) {
            IntPtr address = debugEvent.Exception.ExceptionRecord.Address;

            if (Breakpoints.TryGetValue(address, out Breakpoint breakpoint)) {
                // Breakpoint was set at this address

                // Get Context
                Context context = new Context() { ContextFlags = CONTEXTFLAGS };
                IntPtr threadHandle = Kernel32.OpenThread(THREADACCESS, false, debugEvent.ThreadId);

                if (threadHandle == IntPtr.Zero) return;

                Kernel32.SuspendThread(threadHandle);

                if (Kernel32.GetThreadContext(threadHandle, ref context)) {
                    context.Eip = (uint)address; // Re-execute the instruction where the exception occured.
                    BreakpointEventArgs eventArgs = new BreakpointEventArgs(this, breakpoint, debugEvent, context);

                    if (breakpoint.Condition == null || breakpoint.Condition(eventArgs)) {
                        // Condition met. Trigger event.
                        breakpoint.OnHit(eventArgs);
                        context = eventArgs.Context;

                        if (breakpoint.Enabled /* && EIP unchanged */) {
                            // Breakpoint is still active, set up to re-enable on the next instruction.
                            lastEvent = eventArgs;
                            context.EFlags |= 0x100; // Trap Flag, single step instruction (Enable breakpoint on next instruction).
                            WriteByte(address, breakpoint.Instruction);
                        }

                    } else {
                        // Condition failed, set up to re-enable on the next instruction.
                        lastEvent = eventArgs;
                        context.EFlags |= 0x100; // Single step instruction (Enable breakpoint on next instruction).
                        WriteByte(address, breakpoint.Instruction);
                    }


                    // Set Context back
                    Kernel32.SetThreadContext(threadHandle, ref context);
                }

                Kernel32.ResumeThread(threadHandle);
                Kernel32.CloseHandle(threadHandle);
            }
        }
        private void OnSingleStep(ref DebugEvent debugEvent) {
            if (lastEvent != null) {
                WriteByte(lastEvent.Address, 0xCC);
                lastEvent = null;
            }
        }
        private void OnBreak(ref DebugEvent debugEvent) {
            Console.WriteLine("Break @ " + debugEvent.Exception.ExceptionRecord.Address);
        }


        public void Break() => doBreak = true;
        

        public IntPtr AllocateMemory(int size = 4096) {
            return Kernel32.VirtualAllocEx(ProcessHandle, IntPtr.Zero, (uint)size, AllocationType.MEM_COMMIT | AllocationType.MEM_RESERVE, MemoryProtection.PAGE_EXECUTE_READWRITE);
        }
        public IntPtr AllocateMemory(IntPtr address, int size) {
            return Kernel32.VirtualAllocEx(ProcessHandle, address, (uint)size, AllocationType.MEM_COMMIT | AllocationType.MEM_RESERVE, MemoryProtection.PAGE_EXECUTE_READWRITE);
        }

        public bool FreeMemory(IntPtr start) {
            return Kernel32.VirtualFreeEx(ProcessHandle, start, 0, AllocationType.MEM_RELEASE);
        }
        public bool FreeMemory(IntPtr start, int size) {
            return Kernel32.VirtualFreeEx(ProcessHandle, start, (uint)size, AllocationType.MEM_RELEASE);
        }

        public uint CreateThread(IntPtr start, IntPtr parameter, uint timeout = Kernel32.UINFINITE) => CreateThreadAsync(start, parameter, timeout).Result;
        public uint CreateThread(IntPtr start, uint parameter = 0, uint timeout = Kernel32.UINFINITE) => CreateThreadAsync(start, parameter, timeout).Result;
        public async Task<uint> CreateThreadAsync(IntPtr start, IntPtr parameter, uint timeout = Kernel32.UINFINITE) => await CreateThreadAsync(start, (uint)parameter, timeout);
        public async Task<uint> CreateThreadAsync(IntPtr start, uint parameter = 0, uint timeout = Kernel32.UINFINITE) {
            // Create thread in debuggee process
            IntPtr threadHandle = Kernel32.CreateRemoteThread(ProcessHandle, IntPtr.Zero, 0, start, parameter, ThreadCreationFlags.CREATE_SUSPENDED, out uint threadID);

            Task<uint> thread = Task.Run(() => {
                Kernel32.ResumeThread(threadHandle);
                Kernel32.WaitForSingleObject(threadHandle, timeout);
                Kernel32.GetExitCodeThread(threadHandle, out uint code);
                Kernel32.CloseHandle(threadHandle);
                return code;
            });

            uint exitCode = await thread;
            return exitCode;
        }


        public IntPtr InjectLibrary(string libraryPath) {
            IntPtr memory = AllocateMemory();
            WriteText(memory, libraryPath, Encoding.ASCII);

            IntPtr kernelHandle = Kernel32.GetModuleHandle("kernel32.dll");
            IntPtr functionAddress = Kernel32.GetProcAddress(kernelHandle, "LoadLibraryA");
            IntPtr remoteHandle = (IntPtr)CreateThread(functionAddress, memory);

            FreeMemory(memory);
            return remoteHandle;
        }
        public uint ExecuteRemoteFunction(string libraryPath, IntPtr moduleHandle, string functionName, IntPtr parameter) => ExecuteRemoteFunctionAsync(libraryPath, moduleHandle, functionName, (uint)parameter).Result;
        public uint ExecuteRemoteFunction(string libraryPath, IntPtr moduleHandle, string functionName, uint parameter = 0) => ExecuteRemoteFunctionAsync(libraryPath, moduleHandle, functionName, parameter).Result;
        public async Task<uint> ExecuteRemoteFunctionAsync(string libraryPath, IntPtr moduleHandle, string functionName, IntPtr parameter) => await ExecuteRemoteFunctionAsync(libraryPath, moduleHandle, functionName, (uint)parameter);
        public async Task<uint> ExecuteRemoteFunctionAsync(string libraryPath, IntPtr moduleHandle, string functionName, uint parameter = 0) {
            IntPtr localHandle = Kernel32.LoadLibrary(libraryPath);
            IntPtr localAddress = Kernel32.GetProcAddress(localHandle, functionName);
            Kernel32.FreeLibrary(localHandle);

            int offset = (int)localAddress - (int)localHandle;
            IntPtr functionAddress = moduleHandle + offset;

            return await CreateThreadAsync(functionAddress, parameter);
        }
        public bool FreeRemoteLibrary(IntPtr moduleHandle) {
            IntPtr kernelHandle = Kernel32.GetModuleHandle("kernel32.dll");
            IntPtr functionAddress = Kernel32.GetProcAddress(kernelHandle, "FreeLibrary");
            uint exitCode = CreateThread(functionAddress, moduleHandle);

            return exitCode != 0;
        }



        /// <summary>Returns the address to a string.</summary>
        /// <param name="address">String in form "module"+offsets</param>
        /// <returns>Pointer to the address specified.</returns>
        /// <exception cref="ArgumentNullException">address</exception>
        /// <exception cref="ArgumentException">module name</exception>
        public IntPtr GetAddress(string address) {
            if (string.IsNullOrEmpty(address)) throw new ArgumentNullException("address");

            string moduleName = string.Empty;
            IntPtr baseAddress = IntPtr.Zero;
            int startIndex = address.IndexOf('"');
            int endIndex = -1;

            if (startIndex != -1) {
                endIndex = address.IndexOf('"', startIndex + 1);
            }

            if (endIndex == -1) throw new ArgumentException("Invalid module name. Could not find matching \".");


            moduleName = address.Substring(startIndex + 1, endIndex - 1);
            address = address.Substring(endIndex + 1);


            int[] offsets = GetOffsets(address, out baseAddress);


            return GetAddress(moduleName, baseAddress, offsets);

            // if (!string.IsNullOrEmpty(moduleName)) return GetAddress(moduleName, baseAddress, offsets);
            // return GetAddress(baseAddress, offsets);
        }
        /// <summary>Returns the address to a string.</summary>
        /// <param name="moduleName">Name of the module.</param>
        /// <param name="baseAddress">Address.</param>
        /// <param name="offsets">Array of offsets.</param>
        /// <returns>Pointer to the address specified.</returns>
        /// <exception cref="ArgumentNullException">module name</exception>
        public IntPtr GetAddress(string moduleName, IntPtr baseAddress, params int[] offsets) {
            if (string.IsNullOrEmpty(moduleName)) throw new ArgumentNullException("moduleName");

            ProcessModule module = FindModule(moduleName);
            if (module == null) return IntPtr.Zero;


            IntPtr address = IntPtr.Add(module.BaseAddress, baseAddress.ToInt32());
            return GetAddress(address, offsets);
        }
        /// <summary>Returns the address to a string.</summary>
        /// <param name="baseAddress"></param>
        /// <param name="offsets"></param>
        /// <returns>Pointer to the address specified.</returns>
        /// <exception cref="ArgumentException">base address</exception>
        public IntPtr GetAddress(IntPtr baseAddress, params int[] offsets) {
            if (baseAddress == IntPtr.Zero) throw new ArgumentException("Invalid base address");

            if (offsets == null || offsets.Length < 1) return baseAddress;


            IntPtr address = baseAddress;

            foreach (int offset in offsets) {
                address = IntPtr.Add(ReadPtr(address), offset);
            }

            return address;
        }

        /// <summary>Searches for the address of a byte pattern in a module.</summary>
        /// <param name="moduleName">Name of the module</param>
        /// <param name="data">Pattern to look for.</param>
        /// <returns>Address starting with the first entry of the pattern</returns>
        /// <exception cref="ArgumentNullException">modulename or data</exception>
        /// <exception cref="ArgumentException">empty data</exception>
        public IntPtr Seek(string moduleName, params byte[] data) {
            if (string.IsNullOrEmpty(moduleName)) throw new ArgumentNullException("moduleName");
            else if (data == null) throw new ArgumentNullException("data");
            else if (data.Length == 0) throw new ArgumentException("Empty data");


            ProcessModule module = FindModule(moduleName);
            if (module == null) return IntPtr.Zero;


            for (int i = 0; i < module.ModuleMemorySize; i++) {
                IntPtr address = module.BaseAddress + i;

                // Check if first byte is equal
                if (ReadByte(address) == data[0]) {
                    // Return if only one byte long
                    if (data.Length == 1) return address;

                    // Check if data is in the module's memory
                    if (i + data.Length >= module.ModuleMemorySize) return IntPtr.Zero;

                    // Read following N-1 bytes and compare
                    for (int j = 1; j < data.Length; j++) {
                        IntPtr address2 = address + j;

                        if (ReadByte(address2) != data[j]) break;
                        else if (j + 1 == data.Length) return address;
                    }
                }
            }

            return IntPtr.Zero;
        }

        /// <summary>Returns the first module that matches the name.</summary>
        /// <param name="name">Name of the module.</param>
        /// <returns>ProcessModule with the specified name.</returns>
        /// <exception cref="ArgumentNullException">name</exception>
        protected ProcessModule FindModule(string name) {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            foreach (ProcessModule module in Process.Modules) {
                if (module.ModuleName.Equals(name, StringComparison.CurrentCultureIgnoreCase)) return module;
            }

            return null;
        }
        /// <summary>Splits a string into an array of offsets. Additionally returns the first result seperately.</summary>
        /// <param name="address">String of offsets seperated with '+' or '-'.</param>
        /// <param name="baseAddress">First result.</param>
        /// <returns>Array of offsets.</returns>
        protected int[] GetOffsets(string address, out IntPtr baseAddress) {
            baseAddress = IntPtr.Zero;

            if (string.IsNullOrEmpty(address)) return new int[0];

            MatchCollection matches = Regex.Matches(address, OFFSETPATTERN);
            int[] offsets = new int[matches.Count - 1];

            string offsetString;
            char sign;


            // First result is base address
            if (matches.Count > 0) {
                sign = matches[0].Value[0];
                offsetString = matches[0].Groups[2].ToString();

                if (sign == '-') baseAddress = (IntPtr)( -Convert.ToInt32(offsetString, 16) );
                else baseAddress = (IntPtr)Convert.ToInt32(offsetString, 16);
            }

            // Next are offsets
            for (int i = 1; i < matches.Count; i++) {
                sign = matches[i].Value[0];
                offsetString = matches[i].Groups[2].ToString();

                // convert to int
                offsets[i - 1] = Convert.ToInt32(offsetString, 16);
                if (sign == '-') offsets[i - 1] *= -1;
            }

            return offsets;
        }


        #region Read Memory
        public void ReadMemory(IntPtr address, byte[] buffer, int size) {
            if (isDisposed) throw new ObjectDisposedException("Debugger is disposed");
            else if (buffer == null) throw new ArgumentNullException("buffer");
            else if (size <= 0) throw new ArgumentException("Size must be greater than zero");
            else if (address == IntPtr.Zero) throw new ArgumentException("Invalid address");


            if (!Kernel32.ReadProcessMemory(ProcessHandle, address, buffer, size, out int read) || size != read) {

                throw new AccessViolationException("Could not read memory");
            }
        }
        public bool ReadBoolean(IntPtr address) {
            byte[] buffer = new byte[1];
            ReadMemory(address, buffer, buffer.Length);
            return BitConverter.ToBoolean(buffer, 0);
        }
        public char ReadChar(IntPtr address) {
            byte[] buffer = new byte[1];
            ReadMemory(address, buffer, buffer.Length);
            return BitConverter.ToChar(buffer, 0);
        }
        public byte ReadByte(IntPtr address) {
            byte[] buffer = new byte[1];
            ReadMemory(address, buffer, buffer.Length);
            return buffer[0];
        }
        public short ReadInt16(IntPtr address) {
            byte[] buffer = new byte[2];
            ReadMemory(address, buffer, buffer.Length);
            return BitConverter.ToInt16(buffer, 0);
        }
        public ushort ReadUInt16(IntPtr address) {
            byte[] buffer = new byte[2];
            ReadMemory(address, buffer, buffer.Length);
            return BitConverter.ToUInt16(buffer, 0);
        }
        public int ReadInt32(IntPtr address) {
            byte[] buffer = new byte[4];
            ReadMemory(address, buffer, buffer.Length);
            return BitConverter.ToInt32(buffer, 0);
        }
        public uint ReadUInt32(IntPtr address) {
            byte[] buffer = new byte[4];
            ReadMemory(address, buffer, buffer.Length);
            return BitConverter.ToUInt32(buffer, 0);
        }
        public long ReadInt64(IntPtr address) {
            byte[] buffer = new byte[8];
            ReadMemory(address, buffer, buffer.Length);
            return BitConverter.ToInt64(buffer, 0);
        }
        public ulong ReadUInt64(IntPtr address) {
            byte[] buffer = new byte[8];
            ReadMemory(address, buffer, buffer.Length);
            return BitConverter.ToUInt64(buffer, 0);
        }
        public IntPtr ReadPtr(IntPtr address) {
            byte[] buffer = new byte[4];
            ReadMemory(address, buffer, buffer.Length);
            return (IntPtr)BitConverter.ToInt32(buffer, 0);
        }
        public UIntPtr ReadUPtr(IntPtr address) {
            byte[] buffer = new byte[4];
            ReadMemory(address, buffer, buffer.Length);
            return (UIntPtr)BitConverter.ToInt32(buffer, 0);
        }
        public float ReadFloat(IntPtr address) {
            byte[] buffer = new byte[4];
            ReadMemory(address, buffer, buffer.Length);
            return BitConverter.ToSingle(buffer, 0);
        }
        public double ReadDouble(IntPtr address) {
            byte[] buffer = new byte[8];
            ReadMemory(address, buffer, buffer.Length);
            return BitConverter.ToDouble(buffer, 0);
        }
        public string ReadText(IntPtr address, int length, Encoding encoding) {
            byte[] buffer = new byte[length];
            ReadMemory(address, buffer, buffer.Length);
            return encoding.GetString(buffer);
        }
        public string ReadText(IntPtr address, Encoding encoding) {
            int length = 0;
            do {
                length++;
            } while (ReadByte(address + length) != 0);

            byte[] buffer = new byte[length];
            ReadMemory(address, buffer, length);
            return encoding.GetString(buffer);
        }
        public T ReadStruct<T>(IntPtr address) where T : struct {
            T dummy = default(T);
            int size = Marshal.SizeOf(dummy);
            byte[] buffer = new byte[size];

            ReadMemory(address, buffer, size);

            IntPtr memory = Marshal.AllocHGlobal(size);
            Marshal.Copy(buffer, 0, memory, size);
            dummy = Marshal.PtrToStructure<T>(memory);
            Marshal.FreeHGlobal(memory);

            return dummy;
        }
        #endregion
        #region Write Memory
        public void WriteMemory(IntPtr address, byte[] buffer, int size) {
            if (isDisposed) throw new ObjectDisposedException("Debugger is disposed");
            else if (buffer == null) throw new ArgumentNullException("buffer");
            else if (size <= 0) throw new ArgumentException("Size must be greater than zero");
            else if (address == IntPtr.Zero) throw new ArgumentException("Invalid address");


            if (!Kernel32.WriteProcessMemory(ProcessHandle, address, buffer, size, out int written) || size != written) {
                throw new AccessViolationException("Could not write in memory");
            }

            Kernel32.FlushInstructionCache(ProcessHandle, address, (uint)size);
        }
        public void WriteBoolean(IntPtr address, bool value) {
            byte[] buffer = BitConverter.GetBytes(value);
            WriteMemory(address, buffer, buffer.Length);
        }
        public void WriteChar(IntPtr address, char value) {
            byte[] buffer = BitConverter.GetBytes(value);
            WriteMemory(address, buffer, buffer.Length);
        }
        public void WriteByte(IntPtr address, byte value) {
            byte[] buffer = new byte[] { value };
            WriteMemory(address, buffer, buffer.Length);
        }
        public void WriteInt16(IntPtr address, short value) {
            byte[] buffer = BitConverter.GetBytes(value);
            WriteMemory(address, buffer, buffer.Length);
        }
        public void WriteUInt16(IntPtr address, ushort value) {
            byte[] buffer = BitConverter.GetBytes(value);
            WriteMemory(address, buffer, buffer.Length);
        }
        public void WriteInt32(IntPtr address, int value) {
            byte[] buffer = BitConverter.GetBytes(value);
            WriteMemory(address, buffer, buffer.Length);
        }
        public void WriteUInt32(IntPtr address, uint value) {
            byte[] buffer = BitConverter.GetBytes(value);
            WriteMemory(address, buffer, buffer.Length);
        }
        public void WriteInt64(IntPtr address, long value) {
            byte[] buffer = BitConverter.GetBytes(value);
            WriteMemory(address, buffer, buffer.Length);
        }
        public void WriteUInt64(IntPtr address, ulong value) {
            byte[] buffer = BitConverter.GetBytes(value);
            WriteMemory(address, buffer, buffer.Length);
        }
        public void WritePtr(IntPtr address, IntPtr value) {
            byte[] buffer = BitConverter.GetBytes(value.ToInt32());
            WriteMemory(address, buffer, buffer.Length);
        }
        public void WriteUPtr(IntPtr address, UIntPtr value) {
            byte[] buffer = BitConverter.GetBytes(value.ToUInt32());
            WriteMemory(address, buffer, buffer.Length);
        }
        public void WriteFloat(IntPtr address, float value) {
            byte[] buffer = BitConverter.GetBytes(value);
            WriteMemory(address, buffer, buffer.Length);
        }
        public void WriteDouble(IntPtr address, double value) {
            byte[] buffer = BitConverter.GetBytes(value);
            WriteMemory(address, buffer, buffer.Length);
        }
        public void WriteText(IntPtr address, string value, Encoding encoding) {
            byte[] buffer = encoding.GetBytes(value);
            WriteMemory(address, buffer, buffer.Length);
        }
        public void WriteStruct<T>(IntPtr address, T value) where T : struct {
            int size = Marshal.SizeOf(value);
            byte[] buffer = new byte[size];

            IntPtr memory = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(value, memory, false);
            Marshal.Copy(memory, buffer, 0, size);
            Marshal.FreeHGlobal(memory);

            WriteMemory(address, buffer, size);
        }
        #endregion
        public void CopyMemory(IntPtr address, IntPtr destination, int size) {
            byte[] buffer = new byte[size];
            ReadMemory(address, buffer, size);
            WriteMemory(destination, buffer, size);
        }

        #region Events
        private void OnAttached(Process process, IntPtr processHandle) {
            AttachedEventArgs e = new AttachedEventArgs(process, processHandle);
            Attached?.Invoke(this, e);
            OnAttached(e);
        }
        protected virtual void OnAttached(AttachedEventArgs e) { }

        private void OnDetached(Process process) {
            DetachedEventArgs e = new DetachedEventArgs(process);
            Detached?.Invoke(this, e);
            OnDetached(e);
        }
        protected virtual void OnDetached(DetachedEventArgs e) { }

        private void OnProcessExited(Process process) {
            ProcessExitedEventArgs e = new ProcessExitedEventArgs(process);
            ProcessExited?.Invoke(this, e);
            OnProcessExited(e);
        }
        protected virtual void OnProcessExited(ProcessExitedEventArgs e) { }
        #endregion

        #region IDisposable
        ~Debugger() => Dispose(false);

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing) {
            if (isDisposed) return;

            // TODO release resources
            if (IsAttached) {
                IsAttached = false;
            }

            Kernel32.CloseHandle(ProcessHandle);
            ProcessHandle = IntPtr.Zero;

            if (!disposing) {
                // Only destructor
                Process = null;
            }

            isDisposed = true;
        }
        #endregion
    }
}
