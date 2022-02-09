<div id="top"></div>

# DebugNET

## About The Project

This project was implemented to lessen the dependency on programs like Cheat Engine and such by integrating their core functionalities into a .NET library.<br />
The library includes many features, like:
- Kernel32 Decorator
- Events (_Attached_, _Detached_, _Breakpoint.OnHit_, ...)
- Breakpoints (With optional conditions)
- Allocating and freeing Memory
- Resolving address strings (e.g. _"Prog.exe+0x14-C"_)
- Writing and reading to addresses (reading/writing of structs are supported!)
- Creating and running threads
- Injecting .DLL files into a remote thread
- Non-blocking I/O thanks to C#'s async/await-Pattern

You may even use it as an interface for the kernel32.dll!<br />

Check out the examples [here](DebugNET/DebugNETExample).

<div align="right">(<a href="#top">back to top</a>)</div>


### Built with

DebugNET relies heavily on [P\/Invokes](https://docs.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke) to the **kernel32.dll** of Windows.<br />
There are no further dependencies.

<div align="right">(<a href="#top">back to top</a>)</div>



## Getting Started (**Windows**)

Follow these simple steps to get a local copy running.


### Prerequisites

* [.NET **Framework**](https://dotnet.microsoft.com/en-us/download/dotnet-framework) 4.6.1 or higher (not .NET Core!)
* Development environment of your choice (Visual Studio, Sublime-Text, etc.)

<div align="right">(<a href="#top">back to top</a>)</div>


### Installation

To use this repository in your own projects download the compiled .dll file and add to your project it as a library.<br />
Check the [releases page](https://github.com/Jay184/DebugNET/releases) for a download.

<div align="right">(<a href="#top">back to top</a>)</div>


### Building

Alternatively, you can build the project yourself 

1. Clone the repository
   ```
   git clone https://github.com/Jay184/DebugNET.git
   # Or using SSH (depending on your setup)
   git clone git@github.com:Jay184/DebugNET.git
   ```

2. Add as an _existing Project_ to your own project to use its source or build it using your development environment (Visual Studio for example)

<div align="right">(<a href="#top">back to top</a>)</div>


## Usage

_For more examples, please refer to the [Example project](DebugNET/DebugNETExample)_

### Basics

Import the namespace
```csharp
using DebugNET;
using Debugger = DebugNET.Debugger; // (optional, avoids conflict with .NET's Debugger class in System.Diagnostics)
```

Grab a desired instance of the `System.Diagnostics.Process` class
```csharp
Process[] processes = Process.GetProcessesByName(processName);
Process process = processes.Length > 0 ? processes[0] : null;
```

Instantiate a `Debugger` object and read some memory addresses<br />
_Note: It's generally best-practice to use the using-Statement to take care of the dispoing process!_
```csharp
using (Debugger debugger = new Debugger(process)) {
   // Use process.Modules to find your desired module in your process!
   
   // Retrieve address by code.
   IntPtr codeAddress = debugger.Seek("process.exe", 0x89, 0x45, 0xD0);
   
   // Retrieve address by module-offset pair. (Note the escaped quotation marks!)
   IntPtr address = debugger.GetAddress("\"process.exe\"+13648");
   
   // Read a single byte at a specific address
   byte read = debugger.ReadByte(address);
   
   // Let's write back what we wrote but add one to it
   debugger.WriteByte(address, (byte)(read + 1));
}
```

<div align="right">(<a href="#top">back to top</a>)</div>


## Roadmap
 
 - [x] kernel32.dll P/Invoke decorator
 - [x] Read/Write memory of remote processes
 - [x] Breakpoints, reading CPU registers
 - [x] Async using async/await
 - [x] Seeking a byte-sequence
 - [x] Allocating memory and injecting code
 - [x] Injecting .DLL files to remote processes
 - [x] Document using DocFX
 - [ ] Exception handling
 - [ ] More examples


See the [open issues](https://github.com/Jay184/DebugNET/issues) for a full list of proposed features (and known issues).

<div align="right">(<a href="#top">back to top</a>)</div>



## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a merge request. You can also simply open an issue with the label "enhancement".
Don't forget to give the project a star! Thanks again!

1. Fork the project
1. Create your feature branch (`git checkout -b feature/AmazingFeature`)
1. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
1. Run unit tests if possible
1. Push to the branch (`git push origin feature/AmazingFeature`)
1. Open a pull request

### Codestyle

* Four space indentation
* One class per file
* Class names are written in **PascalCase**
* Function names are written in **PascalCase**
* Class variables (Properties) are written in **PascalCase** regardless of visibility
* Local variable names are writtein in **camelCase** (including function parameter names)
* Use XML to document your functions and classes before you start coding them!
* Do not include more namespaces than necessary
* Design your functions to be functional (lessen class coupling and prefer parameters over global properties)

<div align="right">(<a href="#top">back to top</a>)</div>



<!-- LICENSE -->
## License

Distributed under the Unlicense license. See [LICENSE][license-url] for more information.

<div align="right">(<a href="#top">back to top</a>)</div>



<!-- CONTACT -->
## Contact

Jay - Jay#4711

Project Link: [https://github.com/Jay184/DebugNET](https://github.com/Jay184/DebugNET)

<div align="right">(<a href="#top">back to top</a>)</div>



<!-- ACKNOWLEDGMENTS -->
## Acknowledgments

* [PInvoke.net](https://www.pinvoke.net/)
* [Choose an Open Source License](https://choosealicense.com)
* [GitHub Emoji Cheat Sheet](https://www.webpagefx.com/tools/emoji-cheat-sheet)
* [Img Shields](https://shields.io)

<div align="right">(<a href="#top">back to top</a>)</div>



<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[license-url]: https://github.com/Jay184/DebugNET/blob/master/LICENSE
