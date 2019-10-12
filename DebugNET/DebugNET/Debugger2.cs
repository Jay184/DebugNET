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
    public class Debugger2 : IDisposable {
        // These should be in PInvoke.ExceptionCodeFlags but I cannot find an offcial resource listing all values.
        protected const uint EXCEPTION_BREAKPOINT = 0x80000003;
        protected const uint EXCEPTION_SINGLE_STEP = 0x80000004;
        protected const string OFFSETPATTERN = @"(\+|\-)?(?:0x)?([a-fA-F0-9]{1,8})";
        private const ProcessAccessFlags ACCESS =
                    ProcessAccessFlags.CREATE_THREAD |
                    ProcessAccessFlags.QUERY_INFORMATION |
                    ProcessAccessFlags.VM_OPERATION |
                    ProcessAccessFlags.VM_READ |
                    ProcessAccessFlags.VM_WRITE |
                    ProcessAccessFlags.SYNCHRONIZE;


        // TODO custom eventhandlers
        public event EventHandler Attached;
        public event EventHandler Detached;


        public bool IsAttached { get; private set; }
        public Process Process { get; private set; }
        protected IntPtr ProcessHandle { get; private set; }


        private bool isDisposed;



        public Debugger2(Process process) {
            Process = process ?? throw new ProcessNotFoundException("Cannot find the process.",
                new NullReferenceException("Process was null."));

            IntPtr handle = Kernel32.OpenProcess(ACCESS, false, process.Id);

            if (handle == IntPtr.Zero) throw new ProcessNotFoundException("Cannot open the process.",
                new InvalidOperationException("Cannot get Process handle."));

            ProcessHandle = handle;
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

            // TODO check remote debugging and throw if true

            // Start debugging
            if (Kernel32.DebugActiveProcess(Process.Id)) {
                IsAttached = true;
                OnAttached();
            }

            while (IsAttached) {
                await DebuggingLoop(token);
            }
        }
        /// <summary>Detaches the debugger from the process.</summary>
        /// <exception cref="AttachException">When not attached.</exception>
        public void Detach() {
            if (!IsAttached) throw new AttachException("Cannot detach twice.");

            if (Kernel32.DebugActiveProcessStop(Process.Id)) {
                OnDetached();
            }

            IsAttached = false;
        }

        protected virtual async Task DebuggingLoop(CancellationToken token) {
            Console.WriteLine("Debugging");

            if (token.IsCancellationRequested) {
                Console.WriteLine("Task {0} cancelled");
                Detach();
                token.ThrowIfCancellationRequested();
            }

            await Task.Delay(100);
        }



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
        public async Task<uint> CreateThreadAsync(IntPtr start, IntPtr parameter, uint timeout = Kernel32.UINFINITE) => await CreateThreadAsync(start, parameter, timeout);
        public async Task<uint> CreateThreadAsync(IntPtr start, uint parameter = 0, uint timeout = Kernel32.UINFINITE) {
            // Create thread in debuggee process
            IntPtr threadHandle = Kernel32.CreateRemoteThread(ProcessHandle, IntPtr.Zero, 0, start, parameter, ThreadCreationFlags.None, out uint threadID);

            Task<uint> thread = Task.Run(() => {
                Kernel32.WaitForSingleObject(threadHandle, timeout);
                Kernel32.GetExitCodeThread(threadHandle, out uint code);
                return code;
            });

            uint exitCode = await thread;
            return exitCode;
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

        #region Events
        private void OnAttached(/* TODO parameters for custom EventArgs */) {
            Attached?.Invoke(this, EventArgs.Empty);
            OnAttached(EventArgs.Empty);
        }
        protected virtual void OnAttached(EventArgs e) { }

        private void OnDetached(/* TODO parameters for custom EventArgs */) {
            Detached?.Invoke(this, EventArgs.Empty);
            OnDetached(EventArgs.Empty);
        }
        protected virtual void OnDetached(EventArgs e) { }
        #endregion

        #region IDisposable
        ~Debugger2() => Dispose(false);

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private void Dispose(bool disposing) {
            if (isDisposed) return;

            // TODO release resources
            if (IsAttached) Detach();

            Kernel32.CloseHandle(ProcessHandle);
            ProcessHandle = IntPtr.Zero;
            Process = null;

            isDisposed = true;
        }
        #endregion
    }
}
