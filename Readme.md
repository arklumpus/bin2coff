# bin2coff

**bin2coff** is a tool to create COFF object files containing binary data, which is useful e.g. to embed a binary file in a C/C++ project.

The code on which this project is based was originally created by [Pete Batard](https://pete.akeo.ie/2011/11/bin2coff.html) as part of the _libwdi_ project and modified by Artifex Software Inc. and included in the MuPDF source code.

I translated the original code to C# and added support for producing ARM64 compatible object files (which, in addition to a different flag in the file header, also require the data to be aligned to 4-byte boundaries).

## Usage

```
bin2coff bin obj [label] [64bit|Win32|x64|arm64]
```

* `bin`: source binary data.
* `obj`: target object file, in MS COFF format.
* `label`: identifier for the extern data. If not provided, the name of the binary file without extension is used.
* `64bit` / `Win32` / `x64` / `arm64` / `ARM64`: produce an object that is compatible with the specified architecture.For 64bit/x64 and arm64, symbols are generated without leading underscores, and for arm64 the data is aligned to 4-byte boundaries; machine type is set appropriately.

With your linker set properly, typical access from a C source is:

```C
extern uint8_t  label[]     /* binary data         */
extern uint32_t label_size  /* size of binary data */
```

This program aims to be a drop-in replacement for other `bin2coff` versions (e.g. the one included with MuPDF): simply place the executable in the place where the toolchain expects it to be to make it possible to generate arm64-compatible `obj` files. For x86 and x64 targets, the binaries produced by this program should be identical to the original `bin2coff`.

## Building from source

To build the program from the source code, you will need the [.NET 6 SDK or higher](https://dotnet.microsoft.com/download/dotnet/current). You should use a Windows machine.

1. Download the source code: [bin2coff-1.0.0.tar.gz](https://github.com/arklumpus/bin2coff/archive/v1.0.0.tar.gz) (or clone the repository).
2. Open a command prompt in the folder where you have downloaded the source code, type `BuildBinaries` and press enter. This will build the binaries for the `x64`, `x86` and `arm64` versions of the program (each version can produce object files for all platforms - use the one that corresponds to the host architecture). You can find them in the `Release` folder.
3. _(Optional)_ To sign the binaries, you will need to have the `signtool` utility installed and in your `PATH`. You can get this e.g. by opening a Visual Studio Developer command prompt in the folder with the source code. Use the command `SignBinaries <path_to_certificate.p12> <certificate_password>` to sign the binaries.
