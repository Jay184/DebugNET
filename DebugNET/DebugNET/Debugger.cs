using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DebugNET.PInvoke;

namespace DebugNET {
    public class Debugger : IDisposable {
        protected const string OffsetPattern = @"(\+|\-)?(?:0x)?([a-fA-F0-9]{1,8})";
        protected const uint EXCEPTION_INT_3 = 0x80000003;
        protected const uint EXCEPTION_SINGLE_STEP = 0x80000004;


        public event EventHandler DebuggeeClosing;

        public Process Process { get; private set; }
        protected IntPtr ProcessHandle { get; private set; }

        protected CancellationTokenSource TokenSource { get; private set; }
        protected Task ListenerTask { get; private set; }

        public bool Attached { get; private set; }
        public bool Listening { get; private set; }

        public Breakpoint[] Breakpoints => breakpoints.ToArray();
        private HashSet<Breakpoint> breakpoints;

        private bool isDisposed;



        public Debugger(Process process) {
            breakpoints = new HashSet<Breakpoint>();
            Process = process ?? throw new ProcessNotFoundException("Cannot find the process.",
                new NullReferenceException("Process was null."));


            ProcessAccessFlags processAccess = ProcessAccessFlags.CREATE_THREAD |
                    ProcessAccessFlags.QUERY_INFORMATION |
                    ProcessAccessFlags.VM_OPERATION |
                    ProcessAccessFlags.VM_READ |
                    ProcessAccessFlags.VM_WRITE |
                    ProcessAccessFlags.SYNCHRONIZE;

            ProcessHandle = Kernel32.OpenProcess(processAccess, false, process.Id);


            if (ProcessHandle == IntPtr.Zero) throw new InvalidOperationException("Could not open the process.");
        }
        public Debugger(int pid) : this(Process.GetProcessById(pid)) { }
        public Debugger(string name) : this(Process.GetProcessesByName(name).FirstOrDefault()) {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
        }



        //public bool Attach() {
        //    if (Attached) return false;

        //    Attached = Kernel32.DebugActiveProcess(Process.Id);
        //    return Attached;
        //}
        //public bool Detach() {
        //    if (!Attached) return false;

        //    if (ListenerTask != null) {
        //        if (ListenerTokenSource != null) ListenerTokenSource.Cancel(true);

        //        ListenerTask.Wait();
        //        Attached = Kernel32.DebugActiveProcessStop(Process.Id);
        //    }

        //    Attached = Kernel32.DebugActiveProcessStop(Process.Id);
        //    return !Attached;
        //}

        public Breakpoint SetBreakpoint(string address) {
            IntPtr addrPtr = GetAddress(address);
            return SetBreakpoint(addrPtr);
        }
        public Breakpoint SetBreakpoint(IntPtr address) {
            Breakpoint breakpoint = new Breakpoint(this, address);

            if (breakpoints.Add(breakpoint) && breakpoint.Enable()) {
                return breakpoint;
            }

            return null;
        }
        public void UnsetBreakpoint(Breakpoint breakpoint) {
            if (breakpoints.Contains(breakpoint)) {
                if (breakpoint.Enabled) breakpoint.Disable();

                breakpoints.Remove(breakpoint);
            }
        }

        public void SuspendProcess(Process process) {
            foreach (ProcessThread thread in process.Threads) {
                IntPtr handle = Kernel32.OpenThread(ThreadAccess.SUSPEND_RESUME, false, thread.Id);

                if (handle == IntPtr.Zero) continue;

                Kernel32.SuspendThread(handle);
                Kernel32.CloseHandle(handle);
            }
        }
        public void ResumeProcess(Process process) {
            foreach (ProcessThread thread in process.Threads) {
                IntPtr handle = Kernel32.OpenThread(ThreadAccess.SUSPEND_RESUME, false, thread.Id);

                if (handle == IntPtr.Zero) continue;

                Kernel32.ResumeThread(handle);
                Kernel32.CloseHandle(handle);
            }
        }

        public Task StartListen() {
            if (Listening) throw new InvalidOperationException("Debugger already listening");

            TokenSource = new CancellationTokenSource();
            CancellationToken token = TokenSource.Token;

            ListenerTask = new Task(() => Listen(token), token, TaskCreationOptions.LongRunning);
            ListenerTask.Start();
            Listening = true;

            return ListenerTask;
        }
        public Task StopListen() {
            TokenSource.Cancel();
            return ListenerTask;
        }
        private void Listen(CancellationToken token) {
            if (!Kernel32.DebugActiveProcess(Process.Id)) {
                Listening = false;
                return;
            }

            BreakpointEventArgs lastBreakpoint = null;

            while (Listening) {
                // Wait for debuggee's debug event, timeout = 0.5 seconds
                DebugEvent debugEvent = new DebugEvent();
                bool success = Kernel32.WaitForDebugEvent(ref debugEvent, 1000);

                if (lastBreakpoint == null && token.IsCancellationRequested) {
                    Kernel32.ContinueDebugEvent(debugEvent.ProcessId, debugEvent.ThreadId, ContinueStatus.DBG_CONTINUE);
                    break;
                }

                if (success && debugEvent.DebugEventCode == DebugEventType.EXIT_PROCESS_DEBUG_EVENT) {
                    Kernel32.DebugActiveProcessStop(Process.Id);
                    Listening = false;
                    DebuggeeClosing?.Invoke(this, EventArgs.Empty);
                    return;
                }

                if (success && debugEvent.DebugEventCode == DebugEventType.EXCEPTION_DEBUG_EVENT) {

                    uint errorCode = debugEvent.Exception.ExceptionRecord.Code;
                    IntPtr errorAddress = debugEvent.Exception.ExceptionRecord.Address;
                    Breakpoint breakpoint = breakpoints.SingleOrDefault(bp => bp.Address == errorAddress);

                    if (errorCode == EXCEPTION_INT_3 && breakpoint != null) {

                        // Breakpoint hit
                        breakpoint.Disable();

                        // Get Context
                        ThreadAccess access = ThreadAccess.GET_CONTEXT | ThreadAccess.SET_CONTEXT;
                        Context context = new Context() { ContextFlags = ContextFlags.CONTEXT_CONTROL | ContextFlags.CONTEXT_INTEGER };

                        IntPtr threadHandle = Kernel32.OpenThread(access, false, debugEvent.ThreadId);
                        if (threadHandle == IntPtr.Zero) throw new InvalidOperationException("Can't open thread");

                        Kernel32.GetThreadContext(threadHandle, ref context);

                        // Trigger event
                        BreakpointEventArgs eventArgs = new BreakpointEventArgs(context, debugEvent, breakpoint);
                        breakpoint.OnHit(eventArgs);

                        // Prepare for breakpoint reactivation
                        lastBreakpoint = eventArgs;
                        Context newContext = eventArgs.Context;
                        newContext.Eip = (uint)errorAddress;
                        newContext.EFlags |= 0x100; // Single step instruction

                        // Set Context back
                        Kernel32.SetThreadContext(threadHandle, ref newContext);
                        Kernel32.CloseHandle(threadHandle);

                    } else if (errorCode == EXCEPTION_SINGLE_STEP) {

                        // Instruction right after breakpoint
                        if (lastBreakpoint != null && !lastBreakpoint.Disable) {
                            lastBreakpoint.Breakpoint.Enable();
                        }
                        lastBreakpoint = null;
                    }
                }

                Kernel32.ContinueDebugEvent(debugEvent.ProcessId, debugEvent.ThreadId, ContinueStatus.DBG_CONTINUE);
            }

            SuspendProcess(Process);
            foreach (Breakpoint breakpoint in breakpoints) breakpoint.Disable();
            ResumeProcess(Process);
            Kernel32.DebugActiveProcessStop(Process.Id);
            Listening = false;
        }

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

            if (!string.IsNullOrEmpty(moduleName)) {
                return GetAddress(moduleName, baseAddress, offsets);
            }

            return GetAddress(baseAddress, offsets);
        }
        public IntPtr GetAddress(string moduleName, IntPtr baseAddress, params int[] offsets) {
            if (string.IsNullOrEmpty(moduleName)) throw new ArgumentNullException("moduleName");

            ProcessModule module = FindModule(moduleName);
            if (module == null) return IntPtr.Zero;


            IntPtr address = IntPtr.Add(module.BaseAddress, baseAddress.ToInt32());
            return GetAddress(address, offsets);
        }
        public IntPtr GetAddress(IntPtr baseAddress, params int[] offsets) {
            if (baseAddress == IntPtr.Zero) throw new ArgumentException("Invalid base address");

            if (offsets == null || offsets.Length < 1) return baseAddress;


            IntPtr address = baseAddress;

            foreach (int offset in offsets) {
                address = IntPtr.Add(ReadPtr(address), offset);
            }

            return address;
        }

        public IntPtr Seek(string moduleName, params byte[] data) {
            if (string.IsNullOrEmpty(moduleName)) throw new ArgumentNullException("moduleName");
            else if (data == null) throw new ArgumentNullException("data");
            else if (data.Length == 0) throw new ArgumentException("Empty data");


            ProcessModule module = FindModule(moduleName);
            if (module == null) return IntPtr.Zero;

            IntPtr address = module.BaseAddress;


            for (int i = 0; i < module.ModuleMemorySize; i++) {
                byte b = ReadByte(address);

                // Check if first byte is equal
                if (b == data[0]) {
                    // Return if only one byte long
                    if (data.Length == 1) return address;

                    // Check if data is in the module's memory
                    if (i + data.Length >= module.ModuleMemorySize) return IntPtr.Zero;

                    IntPtr firstAddress = address;
                    // Read following N bytes and compare
                    for (int j = 1; j < data.Length; j++) {
                        firstAddress = IntPtr.Add(firstAddress, 1);

                        if (data[j] == ReadByte(firstAddress)) {
                            if (j + 1 == data.Length) return address;

                        } else break;
                    }
                }

                address = IntPtr.Add(address, 1);
            }

            return IntPtr.Zero;
        }

        public IntPtr AllocateMemory(int size) {
            return Kernel32.VirtualAllocEx(ProcessHandle, IntPtr.Zero, (uint)size, AllocationType.MEM_COMMIT | AllocationType.MEM_RESERVE, MemoryProtection.PAGE_EXECUTE_READWRITE);
        }
        public void FreeMemory(IntPtr start) {
            Kernel32.VirtualFreeEx(ProcessHandle, start, 0, AllocationType.MEM_RELEASE);
        }

        public Task<uint> CreateThread(IntPtr start, uint parameter = 0, uint timeout = 0xFFFFFFFF) {
            // dwSize with Marshal.SizeOf(typeof(TYPE))

            // Create thread in debuggee process
            uint threadID;
            IntPtr threadHandle = Kernel32.CreateRemoteThread(ProcessHandle, IntPtr.Zero, 0, start, parameter, 0, out threadID);


            // Wait for thread to finish and release memory pages
            return Task.Run(() => {
                uint exitCode = 0;
                Kernel32.WaitForSingleObject(threadHandle, timeout);
                Kernel32.GetExitCodeThread(threadHandle, out exitCode);
                return exitCode;
            });
        }



        protected ProcessModule FindModule(string name) {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            foreach (ProcessModule module in Process.Modules) {
                if (module.ModuleName.Equals(name, StringComparison.CurrentCultureIgnoreCase)) return module;
            }

            return null;
        }
        protected int[] GetOffsets(string address, out IntPtr baseAddress) {
            baseAddress = IntPtr.Zero;

            if (string.IsNullOrEmpty(address)) return new int[0];

            MatchCollection matches = Regex.Matches(address, OffsetPattern);
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


        #region IDisposable
        ~Debugger() => Dispose(false);

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing) {
            if (isDisposed) return;

            foreach (Breakpoint breakpoint in breakpoints) {
                breakpoint.Disable();
            }
            Kernel32.CloseHandle(ProcessHandle);
            Process?.Dispose();
            ProcessHandle = IntPtr.Zero;
            isDisposed = true;
        }
        #endregion
    }
}
