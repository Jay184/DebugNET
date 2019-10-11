using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace DebugNET.PInvoke {
    /// <summary>
    /// Collection of P/Invoke methods related to threads, processes, memory and debugging
    /// </summary>
    internal static class Kernel32 {
        /// <summary>Represents INFINITE / INFINITY in most P/Invoke methods</summary>
        internal const int INFINITE = -1;

        /// <summary>Opens an existing local process object.</summary>
        /// <param name="dwDesiredAccess">The access to the process object.</param>
        /// <param name="bInheritHandle">If this value is TRUE, processes created by this process will inherit the handle. Otherwise, the processes do not inherit this handle.</param>
        /// <param name="dwProcessId">The identifier of the local process to be opened. If the specified process is NULL, the function fails and the last error code is ERROR_INVALID_PARAMETER.</param>
        /// <returns>If the function succeeds, the return value is an open handle to the specified process; otherwise, it is NULL</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        /// <summary>Opens an existing thread object.</summary>
        /// <param name="dwDesiredAccess">The access to the thread object.</param>
        /// <param name="bInheritHandle">If this value is TRUE, processes created by this process will inherit the handle. Otherwise, the processes do not inherit this handle.</param>
        /// <param name="dwThreadId">The identifier of the thread to be opened.</param>
        /// <returns>If the function succeeds, the return value is an open handle to the specified thread; otherwise, it is NULL.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, int dwThreadId);

        /// <summary>Closes an open object handle.</summary>
        /// <param name="hObject">A valid handle to an open object.</param>
        /// <returns>If the function succeeds, the return value is nonzero; othewise, it is zero.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr hObject);


        /// <summary>Enables a debugger to attach to an active process and debug it.</summary>
        /// <param name="dwProcessId">The identifier for the process to be debugged. The debugger is granted debugging access to the process as if it created the process with the DEBUG_ONLY_THIS_PROCESS flag.</param>
        /// <returns>If the function succeeds, the return value is nonzero; otherwise, it is zero.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DebugActiveProcess([In] int dwProcessId);

        /// <summary>Stops the debugger from debugging the specified process.</summary>
        /// <param name="dwProcessId">The identifier of the process to stop debugging.</param>
        /// <returns>If the function succeeds, the return value is nonzero; otherwise, it is zero.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DebugActiveProcessStop([In] int dwProcessId);


        /// <summary>Waits for a debugging event to occur in a process being debugged.</summary>
        /// <param name="lpDebugEvent">A pointer to a DEBUG_EVENT structure that receives information about the debugging event.</param>
        /// <param name="dwMilliseconds">The number of milliseconds to wait for a debugging event. If this parameter is zero, the function tests for a debugging event and returns immediately. If the parameter is INFINITE, the function does not return until a debugging event has occurred.</param>
        /// <returns>If the function succeeds, the return value is nonzero; otherwise, it is zero.</returns>
        [DllImport("kernel32.dll", EntryPoint = "WaitForDebugEvent")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WaitForDebugEvent(ref DebugEvent lpDebugEvent, int dwMilliseconds);

        /// <summary>Waits for a debugging event to occur in a process being debugged.</summary>
        /// <param name="lpDebugEvent">A pointer to a DEBUG_EVENT structure that receives information about the debugging event.</param>
        /// <param name="dwMilliseconds">The number of milliseconds to wait for a debugging event. If this parameter is zero, the function tests for a debugging event and returns immediately. If the parameter is INFINITE, the function does not return until a debugging event has occurred.</param>
        /// <returns>If the function succeeds, the return value is nonzero; otherwise, it is zero.</returns>
        [DllImport("kernel32.dll", EntryPoint = "WaitForDebugEventEx")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WaitForDebugEventEx(ref DebugEvent lpDebugEvent, int dwMilliseconds);

        /// <summary>Enables a debugger to continue a thread that previously reported a debugging event.</summary>
        /// <param name="dwProcessId">The process identifier of the process to continue.</param>
        /// <param name="dwThreadId">The thread identifier of the thread to continue. The combination of process identifier and thread identifier must identify a thread that has previously reported a debugging event.</param>
        /// <param name="dwContinueStatus">The options to continue the thread that reported the debugging event.</param>
        /// <returns>If the function succeeds, the return value is nonzero; otherwise, it is zero.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ContinueDebugEvent(int dwProcessId, int dwThreadId, ContinueStatus dwContinueStatus);


        /// <summary>Suspends the specified thread.</summary>
        /// <param name="hThread">A handle to the thread that is to be suspended. The handle must have the THREAD_SUSPEND_RESUME access right.</param>
        /// <returns>If the function succeeds, the return value is the thread's previous suspend count; otherwise, it is -1 (DWORD).</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int SuspendThread(IntPtr hThread);

        /// <summary>Decrements a thread's suspend count. When the suspend count is decremented to zero, the execution of the thread is resumed.</summary>
        /// <param name="hThread">A handle to the thread to be restarted. This handle must have the THREAD_SUSPEND_RESUME access right.</param>
        /// <returns>If the function succeeds, the return value is the thread's previous suspend count; otherwhise, it is -1 (DWORD).</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int ResumeThread(IntPtr hThread);


        /// <summary>Retrieves the context of the specified thread.</summary>
        /// <param name="hThread">A handle to the thread whose context is to be retrieved. The handle must have THREAD_GET_CONTEXT access to the thread.</param>
        /// <param name="lpContext">A pointer to a CONTEXT structure that receives the appropriate context of the specified thread. The value of the ContextFlags member of this structure specifies which portions of a thread's context are retrieved. The CONTEXT structure is highly processor specific.</param>
        /// <returns>If the function succeeds, the return value is nonzero; otherwise, it is zero.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetThreadContext(IntPtr hThread, ref Context lpContext);
        /// <summary>Retrieves the context of the specified x64 thread.</summary>
        /// <param name="hThread">A handle to the thread whose context is to be retrieved. The handle must have THREAD_GET_CONTEXT and THREAD_QUERY_INFORMATION access to the thread.</param>
        /// <param name="lpContext">A pointer to a CONTEXT structure that receives the appropriate context of the specified thread. The value of the ContextFlags member of this structure specifies which portions of a thread's context are retrieved. The CONTEXT structure is highly processor specific.</param>
        /// <returns>If the function succeeds, the return value is nonzero; otherwise, it is zero.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetThreadContext(IntPtr hThread, ref Context64 lpContext);

        /// <summary>Sets the context for the specified thread.</summary>
        /// <param name="hThread">A handle to the thread whose context is to be set. The handle must have the THREAD_SET_CONTEXT access right to the thread.</param>
        /// <param name="lpContext">A pointer to a CONTEXT structure that contains the context to be set in the specified thread. The value of the ContextFlags member of this structure specifies which portions of a thread's context to set. Some values in the CONTEXT structure that cannot be specified are silently set to the correct value. This includes bits in the CPU status register that specify the privileged processor mode, global enabling bits in the debugging register, and other states that must be controlled by the operating system.</param>
        /// <returns>If the context was set, the return value is nonzero; otherwise, it is zero.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SetThreadContext(IntPtr hThread, ref Context lpContext);
        /// <summary>Sets the context for the specified x64 thread.</summary>
        /// <param name="hThread">A handle to the thread whose context is to be set. The handle must have the THREAD_SET_CONTEXT access right to the thread.</param>
        /// <param name="lpContext">A pointer to a CONTEXT structure that contains the context to be set in the specified thread. The value of the ContextFlags member of this structure specifies which portions of a thread's context to set. Some values in the CONTEXT structure that cannot be specified are silently set to the correct value. This includes bits in the CPU status register that specify the privileged processor mode, global enabling bits in the debugging register, and other states that must be controlled by the operating system.</param>
        /// <returns>If the context was set, the return value is nonzero; otherwise, it is zero.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SetThreadContext(IntPtr hThread, ref Context64 lpContext);


        /// <summary>Reads data from an area of memory in a specified process. The entire area to be read from must be accessible or the operation fails.</summary>
        /// <param name="hProcess">A handle to the process with memory that is being read. The handle must have PROCESS_VM_READ access to the process.</param>
        /// <param name="lpBaseAddress">A pointer to the base address in the specified process from which to read. Before any data transfer occurs, the system verifies that all data in the base address and memory of the specified size is accessible for read access, and if it is not accessible the function fails.</param>
        /// <param name="lpBuffer">A pointer to a buffer that receives the contents from the address space of the specified process.</param>
        /// <param name="dwSize">The number of bytes to be read from the specified process.</param>
        /// <param name="dwNumberOfBytesRead">A pointer to a variable that receives the number of bytes transferred into the specified buffer. If dwNumberOfBytesRead is NULL, the parameter is ignored.</param>
        /// <returns>If the function succeeds, the return value is nonzero; otherwise, it is zero.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out int dwNumberOfBytesRead);

        /// <summary>Writes data to an area of memory in a specified process. The entire area to be written to must be accessible or the operation fails.</summary>
        /// <param name="hProcess">A handle to the process memory to be modified. The handle must have PROCESS_VM_WRITE and PROCESS_VM_OPERATION access to the process.</param>
        /// <param name="lpBaseAddress">A pointer to the base address in the specified process to which data is written. Before data transfer occurs, the system verifies that all data in the base address and memory of the specified size is accessible for write access, and if it is not accessible, the function fails.</param>
        /// <param name="lpBuffer">A pointer to the buffer that contains data to be written in the address space of the specified process.</param>
        /// <param name="dwSize">The number of bytes to be written to the specified process.</param>
        /// <param name="dwNumberOfBytesWritten">A pointer to a variable that receives the number of bytes transferred into the specified process. This parameter is optional. If dwNumberOfBytesWritten is NULL, the parameter is ignored.</param>
        /// <returns>If the function succeeds, the return value is nonzero; otherwise, it is zero.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int dwNumberOfBytesWritten);


        /// <summary>Reserves, commits, or changes the state of a region of memory within the virtual address space of a specified process. The function initializes the memory it allocates to zero.</summary>
        /// <param name="hProcess">The handle to a process. The function allocates memory within the virtual address space of this process. The handle must have the PROCESS_VM_OPERATION access right.</param>
        /// <param name="lpAddress">The pointer that specifies a desired starting address for the region of pages that you want to allocate. If you are reserving memory, the function rounds this address down to the nearest multiple of the allocation granularity. If you are committing memory that is already reserved, the function rounds this address down to the nearest page boundary.To determine the size of a page and the allocation granularity on the host computer, use the GetSystemInfo function. If lpAddress is NULL, the function determines where to allocate the region. If this address is within an enclave that you have not initialized by calling InitializeEnclave, VirtualAllocEx allocates a page of zeros for the enclave at that address.The page must be previously uncommitted, and will not be measured with the EEXTEND instruction of the Intel Software Guard Extensions programming model. If the address in within an enclave that you initialized, then the allocation operation fails with the ERROR_INVALID_ADDRESS error.</param>
        /// <param name="dwSize">The size of the region of memory to allocate, in bytes. If lpAddress is NULL, the function rounds dwSize up to the next page boundary. If lpAddress is not NULL, the function allocates all pages that contain one or more bytes in the range from lpAddress to lpAddress+dwSize.This means, for example, that a 2-byte range that straddles a page boundary causes the function to allocate both pages.</param>
        /// <param name="flAllocationType">The type of memory allocation. This parameter must contain one of the following values.</param>
        /// <param name="flProtect">The memory protection for the region of pages to be allocated.</param>
        /// <returns>If the function succeeds, the return value is the base address of the allocated region of pages; otherwise, it is NULL.</returns>
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        internal static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        /// <summary>Releases, decommits, or releases and decommits a region of memory within the virtual address space of a specified process.</summary>
        /// <param name="hProcess">A handle to a process. The function frees memory within the virtual address space of the process. The handle must have the PROCESS_VM_OPERATION access right.</param>
        /// <param name="lpAddress">A pointer to the starting address of the region of memory to be freed. If the dwFreeType parameter is MEM_RELEASE, lpAddress must be the base address returned by the VirtualAllocEx function when the region is reserved.</param>
        /// <param name="dwSize">The size of the region of memory to free, in bytes. If the dwFreeType parameter is MEM_RELEASE, dwSize must be zero. The function frees the entire region that is reserved in the initial allocation call to VirtualAllocEx. If dwFreeType is MEM_DECOMMIT, the function decommits all memory pages that contain one or more bytes in the range from the lpAddress parameter to (lpAddress+dwSize). This means, for example, that a 2-byte region of memory that straddles a page boundary causes both pages to be decommitted. If lpAddress is the base address returned by VirtualAllocEx and dwSize is 0 (zero), the function decommits the entire region that is allocated by VirtualAllocEx. After that, the entire region is in the reserved state.</param>
        /// <param name="dwFreeType">The type of free operation.</param>
        /// <returns>If the function succeeds, the return value is a nonzero value; otherwise, it is zero.</returns>
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        internal static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, AllocationType dwFreeType);


        /// <summary>Waits until the specified object is in the signaled state or the time-out interval elapses.</summary>
        /// <param name="hHandle">A handle to the object. If this handle is closed while the wait is still pending, the function's behavior is undefined. The handle must have the SYNCHRONIZE access right.</param>
        /// <param name="dwMilliseconds">The time-out interval, in milliseconds. If a nonzero value is specified, the function waits until the object is signaled or the interval elapses. If dwMilliseconds is zero, the function does not enter a wait state if the object is not signaled; it always returns immediately. If dwMilliseconds is INFINITE, the function will return only when the object is signaled.</param>
        /// <returns>If the function succeeds, the return value indicates the event that caused the function to return.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern ObjectWaitEvent WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        /// <summary>Waits until the specified object is in the signaled state, an I/O completion routine or asynchronous procedure call (APC) is queued to the thread, or the time-out interval elapses.</summary>
        /// <param name="hHandle">A handle to the object. If this handle is closed while the wait is still pending, the function's behavior is undefined. The handle must have the SYNCHRONIZE access right.</param>
        /// <param name="dwMilliseconds">The time-out interval, in milliseconds. If a nonzero value is specified, the function waits until the object is signaled, an I/O completion routine or APC is queued, or the interval elapses. If dwMilliseconds is zero, the function does not enter a wait state if the criteria is not met; it always returns immediately. If dwMilliseconds is INFINITE, the function will return only when the object is signaled or an I/O completion routine or APC is queued.</param>
        /// <param name="bAlertable">If this parameter is TRUE and the thread is in the waiting state, the function returns when the system queues an I/O completion routine or APC, and the thread runs the routine or function. Otherwise, the function does not return, and the completion routine or APC function is not executed.</param>
        /// <returns>If the function succeeds, the return value indicates the event that caused the function to return.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern ObjectWaitEvent WaitForSingleObjectEx(IntPtr hHandle, uint dwMilliseconds, bool bAlertable);


        /// <summary>Retrieves a module handle for the specified module. The module must have been loaded by the calling process.</summary>
        /// <param name="lpModuleName">The name of the loaded module (either a .dll or .exe file). If the file name extension is omitted, the default library extension .dll is appended. The file name string can include a trailing point character (.) to indicate that the module name has no extension. The string does not have to specify a path. When specifying a path, be sure to use backslashes (\), not forward slashes (/). The name is compared (case independently) to the names of modules currently mapped into the address space of the calling process. If this parameter is NULL, GetModuleHandle returns a handle to the file used to create the calling process (.exe file). The GetModuleHandle function does not retrieve handles for modules that were loaded using the LOAD_LIBRARY_AS_DATAFILE flag.</param>
        /// <returns>If the function succeeds, the return value is a handle to the specified module; otherwise, it is NULL.</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);

        /// <summary>Retrieves a module handle for the specified module and increments the module's reference count unless GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT is specified. The module must have been loaded by the calling process.</summary>
        /// <param name="dwFlags">This parameter can be zero or a valid value. If the module's reference count is incremented, the caller must use the FreeLibrary function to decrement the reference count when the module handle is no longer needed.</param>
        /// <param name="lpModuleName">The name of the loaded module (either a .dll or .exe file), or an address in the module (if dwFlags is GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS). For a module name, if the file name extension is omitted, the default library extension.dll is appended.The file name string can include a trailing point character (.) to indicate that the module name has no extension.The string does not have to specify a path.When specifying a path, be sure to use backslashes (\), not forward slashes (/). The name is compared(case independently) to the names of modules currently mapped into the address space of the calling process. If this parameter is NULL, the function returns a handle to the file used to create the calling process (.exe file).</param>
        /// <param name="phModule">A handle to the specified module. If the function fails, this parameter is NULL. The GetModuleHandleEx function does not retrieve handles for modules that were loaded using the LOAD_LIBRARY_AS_DATAFILE flag.</param>
        /// <returns>If the function succeeds, the return value is nonzero; otherwise, it is zero.</returns>
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool GetModuleHandleEx(ModuleHandleFlags dwFlags, string lpModuleName, out IntPtr phModule);


        /// <summary>Loads the specified module into the address space of the calling process. The specified module may cause other modules to be loaded.</summary>
        /// <param name="lpFileName">The name of the module. This can be either a library module (a .dll file) or an executable module (an .exe file). The name specified is the file name of the module and is not related to the name stored in the library module itself, as specified by the LIBRARY keyword in the module-definition (.def) file. If the string specifies a full path, the function searches only that path for the module. If the string specifies a relative path or a module name without a path, the function uses a standard search strategy to find the module; for more information, see the Remarks. If the function cannot find the module, the function fails.When specifying a path, be sure to use backslashes (\), not forward slashes (/). For more information about paths, see Naming a File or Directory. If the string specifies a module name without a path and the file name extension is omitted, the function appends the default library extension .dll to the module name.To prevent the function from appending .dll to the module name, include a trailing point character (.) in the module name string.</param>
        /// <returns>If the function succeeds, the return value is a handle to the module; otherwise, it is NULL.</returns>
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        internal static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string lpFileName);

        /// <summary>Loads the specified module into the address space of the calling process. The specified module may cause other modules to be loaded.</summary>
        /// <param name="lpFileName">A string that specifies the file name of the module to load. This name is not related to the name stored in a library module itself, as specified by the LIBRARY keyword in the module-definition (.def) file. The module can be a library module (a .dll file) or an executable module (an .exe file). If the specified module is an executable module, static imports are not loaded; instead, the module is loaded as if DONT_RESOLVE_DLL_REFERENCES was specified. See the dwFlags parameter for more information. If the string specifies a module name without a path and the file name extension is omitted, the function appends the default library extension .dll to the module name. To prevent the function from appending .dll to the module name, include a trailing point character (.) in the module name string. If the string specifies a fully qualified path, the function searches only that path for the module. When specifying a path, be sure to use backslashes (\), not forward slashes (/). If the string specifies a module name without a path and more than one loaded module has the same base name and extension, the function returns a handle to the module that was loaded first. If the string specifies a module name without a path and a module of the same name is not already loaded, or if the string specifies a module name with a relative path, the function searches for the specified module. The function also searches for modules if loading the specified module causes the system to load other associated modules (that is, if the module has dependencies). The directories that are searched and the order in which they are searched depend on the specified path and the dwFlags parameter. If the function cannot find the module or one of its dependencies, the function fails.</param>
        /// <param name="hReservedNull">This parameter is reserved for future use. It must be NULL.</param>
        /// <param name="dwFlags">The action to be taken when loading the module. If no flags are specified, the behavior of this function is identical to that of the LoadLibrary function.</param>
        /// <returns>If the function succeeds, the return value is a handle to the loaded module; otherwise, it is NULL.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);

        /// <summary>Frees the loaded dynamic-link library (DLL) module and, if necessary, decrements its reference count. When the reference count reaches zero, the module is unloaded from the address space of the calling process and the handle is no longer valid.</summary>
        /// <param name="hLibModule">A handle to the loaded library module. The LoadLibrary, LoadLibraryEx, GetModuleHandle or GetModuleHandleEx function returns this handle.</param>
        /// <returns>If the function succeeds, the return value is nonzero; otherwise, it is zero.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(IntPtr hLibModule);


        /// <summary>Creates a thread that runs in the virtual address space of another process.</summary>
        /// <param name="hProcess">A handle to the process in which the thread is to be created. The handle must have the PROCESS_CREATE_THREAD, PROCESS_QUERY_INFORMATION, PROCESS_VM_OPERATION, PROCESS_VM_WRITE, and PROCESS_VM_READ access rights.</param>
        /// <param name="lpThreadAttributes">A pointer to a SECURITY_ATTRIBUTES structure that specifies a security descriptor for the new thread and determines whether child processes can inherit the returned handle. If lpThreadAttributes is NULL, the thread gets a default security descriptor and the handle cannot be inherited. The access control lists (ACL) in the default security descriptor for a thread come from the primary token of the creator.</param>
        /// <param name="dwStackSize">The initial size of the stack, in bytes. The system rounds this value to the nearest page. If this parameter is zero, the new thread uses the default size for the executable.</param>
        /// <param name="lpStartAddress">A pointer to the application-defined function of type LPTHREAD_START_ROUTINE to be executed by the thread and represents the starting address of the thread in the remote process. The function must exist in the remote process.</param>
        /// <param name="lpParameter">A pointer to a variable to be passed to the thread function.</param>
        /// <param name="dwCreationFlags">The flags that control the creation of the thread.</param>
        /// <param name="lpThreadId">A pointer to a variable that receives the thread identifier. If this parameter is NULL, the thread identifier is not returned.</param>
        /// <returns>If the function succeeds, the return value is a handle to the new thread; otherwise, it is NULL.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, uint lpParameter, ThreadCreationFlags dwCreationFlags, out uint lpThreadId);

        /// <summary>Retrieves the termination status of the specified thread.</summary>
        /// <param name="hThread">A handle to the thread. The handle must have the THREAD_QUERY_INFORMATION or THREAD_QUERY_LIMITED_INFORMATION access right.</param>
        /// <param name="lpExitCode">A pointer to a variable to receive the thread termination status. If the thread has not terminated, the status returned is STILL_ACTIVE (259)</param>
        /// <returns>If the function succeeds, the return value is nonzero; otherwise, it is zero.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);


        /// <summary>Retrieves the address of an exported function or variable from the specified dynamic-link library (DLL).</summary>
        /// <param name="hModule">A handle to the DLL module that contains the function or variable. The LoadLibrary, LoadLibraryEx, LoadPackagedLibrary, or GetModuleHandle function returns this handle.</param>
        /// <param name="lpProcName">The function or variable name, or the function's ordinal value. If this parameter is an ordinal value, it must be in the low-order word; the high-order word must be zero.</param>
        /// <returns>If the function succeeds, the return value is the address of the exported function or variable; otherwise, it is NULL.</returns>
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        /// <summary>Retrieves the address of an exported function or variable from the specified dynamic-link library (DLL).</summary>
        /// <param name="hModule">A handle to the DLL module that contains the function or variable. The LoadLibrary, LoadLibraryEx, LoadPackagedLibrary, or GetModuleHandle function returns this handle.</param>
        /// <param name="lpProcName">The function or variable name, or the function's ordinal value. It must be in the low-order word; the high-order word must be zero.</param>
        /// <returns>If the function succeeds, the return value is the address of the exported function or variable; otherwise, it is NULL.</returns>
        [DllImport("kernel32", SetLastError = true, EntryPoint = "GetProcAddress")]
        internal static extern IntPtr GetProcAddressOrdinal(IntPtr hModule, IntPtr lpProcName);


        /// <summary>Flushes the instruction cache for the specified process.</summary>
        /// <param name="hProcess">A handle to a process whose instruction cache is to be flushed.</param>
        /// <param name="lpBaseAddress">A pointer to the base of the region to be flushed. This parameter can be NULL.</param>
        /// <param name="dwSize">The size of the region to be flushed if the lpBaseAddress parameter is not NULL, in bytes.</param>
        /// <returns>If the function succeeds, the return value is nonzero; otherwise, it is zero.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool FlushInstructionCache(IntPtr hProcess, IntPtr lpBaseAddress, uint dwSize);

        /// <summary>Determines whether the specified process is being debugged.</summary>
        /// <param name="hProcess">A handle to the process.</param>
        /// <param name="pbDebuggerPresent">A pointer to a variable that the function sets to TRUE if the specified process is being debugged, or FALSE otherwise.</param>
        /// <returns>If the function succeeds, the return value is nonzero; otherwise, it is zero.</returns>
        [DllImport("Kernel32.dll", SetLastError = true, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CheckRemoteDebuggerPresent(SafeHandle hProcess, [MarshalAs(UnmanagedType.Bool)]ref bool pbDebuggerPresent);

        /// <summary>Causes a breakpoint exception to occur in the specified process. This allows the calling thread to signal the debugger to handle the exception.</summary>
        /// <param name="dwProcessHandle">A handle to the process.</param>
        /// <returns>If the function succeeds, the return value is nonzero; otherwise, it is zero.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DebugBreakProcess(IntPtr hProcess);
    }
}
