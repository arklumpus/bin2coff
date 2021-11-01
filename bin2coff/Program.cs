/*
 * bin2coff: converts a data object into a Win32 linkable COFF binary object
 * Copyright (c) 2011 Pete Batard <pete@akeo.ie>
 * This file is part of the libwdi project: http://libwdi.sf.net
 * Modifications Copyright (c) 2018 by Artifex Software
 * Further modifications Copyright (c) 2021 by Giorgio Bianchini
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

/*
 * References:
 * http://www.vortex.masmcode.com/ (another bin2coff, without source)
 * http://msdn.microsoft.com/en-us/library/ms680198.aspx
 * http://webster.cs.ucr.edu/Page_TechDocs/pe.txt
 * http://www.delorie.com/djgpp/doc/coff/
 * http://pierrelib.pagesperso-orange.fr/exec_formats/MS_Symbol_Type_v1.0.pdf
 */

/*
  Updates from Artifex Software Inc.
    + Automatically rename '-' to '_' in generated symbols.
    + Accept 'Win32' and 'x64' as flags.
 */

/* 
   Updates by Giorgio Bianchini:
    + Translated to C#.
    + Added support for arm64 target.
    + Align data to 4-byte boundaries when generating for arm64.
 */

using SIZE_TYPE = System.UInt32;

using uint8_t = System.Byte;
using uint16_t = System.UInt16;
using uint32_t = System.UInt32;

using size_t = System.UInt64;

using int16_t = System.Int16;
using int32_t = System.Int32;


namespace bin2coff
{
    public static class Program
    {
        const string SIZE_LABEL_SUFFIX = "_size";

        const int IMAGE_SIZEOF_SHORT_NAME = 8;

        /* File header defines */
        const int IMAGE_FILE_MACHINE_ANY = 0x0000;
        const int IMAGE_FILE_MACHINE_I386 = 0x014c;
        const int IMAGE_FILE_MACHINE_IA64 = 0x0200;
        const int IMAGE_FILE_MACHINE_AMD64 = 0x8664;
        const int IMAGE_FILE_MACHINE_ARM = 0x01c0;
        const int IMAGE_FILE_MACHINE_ARM2 = 0x01c4;
        const int IMAGE_FILE_MACHINE_ARM64 = 0xaa64;

        const int IMAGE_FILE_RELOCS_STRIPPED = 0x0001;
        const int IMAGE_FILE_EXECUTABLE_IMAGE = 0x0002;
        const int IMAGE_FILE_LINE_NUMS_STRIPPED = 0x0004;
        const int IMAGE_FILE_LOCAL_SYMS_STRIPPED = 0x0008;
        const int IMAGE_FILE_AGGRESIVE_WS_TRIM = 0x0010;
        const int IMAGE_FILE_LARGE_ADDRESS_AWARE = 0x0020;
        const int IMAGE_FILE_16BIT_MACHINE = 0x0040;
        const int IMAGE_FILE_BYTES_REVERSED_LO = 0x0080;
        const int IMAGE_FILE_32BIT_MACHINE = 0x0100;
        const int IMAGE_FILE_DEBUG_STRIPPED = 0x0200;
        const int IMAGE_FILE_REM_RUN_FROM_SWAP = 0x0400;
        const int IMAGE_FILE_NET_RUN_FROM_SWAP = 0x0800;
        const int IMAGE_FILE_SYSTEM = 0x1000;
        const int IMAGE_FILE_DLL = 0x2000;
        const int IMAGE_FILE_UP_SYSTEM_ONLY = 0x4000;
        const int IMAGE_FILE_BYTES_REVERSED_HI = 0x8000;

        /* Section header defines */
        const long IMAGE_SCN_TYPE_REG = 0x00000000;
        const long IMAGE_SCN_TYPE_DSECT = 0x00000001;
        const long IMAGE_SCN_TYPE_NOLOAD = 0x00000002;
        const long IMAGE_SCN_TYPE_GROUP = 0x00000003;
        const long IMAGE_SCN_TYPE_NO_PAD = 0x00000008;
        const long IMAGE_SCN_TYPE_COPY = 0x00000010;
        const long IMAGE_SCN_CNT_CODE = 0x00000020;
        const long IMAGE_SCN_CNT_INITIALIZED_DATA = 0x00000040;
        const long IMAGE_SCN_CNT_UNINITIALIZED_DATA = 0x00000080;
        const long IMAGE_SCN_LNK_OTHER = 0x00000100;
        const long IMAGE_SCN_LNK_INFO = 0x00000200;
        const long IMAGE_SCN_TYPE_OVER = 0x00000400;
        const long IMAGE_SCN_LNK_REMOVE = 0x00000800;
        const long IMAGE_SCN_LNK_COMDAT = 0x00001000;
        const long IMAGE_SCN_MEM_FARDATA = 0x00008000;
        const long IMAGE_SCN_MEM_PURGEABLE = 0x00020000;
        const long IMAGE_SCN_MEM_16BIT = 0x00020000;
        const long IMAGE_SCN_MEM_LOCKED = 0x00040000;
        const long IMAGE_SCN_MEM_PRELOAD = 0x00080000;
        const long IMAGE_SCN_ALIGN_1BYTES = 0x00100000;
        const long IMAGE_SCN_ALIGN_2BYTES = 0x00200000;
        const long IMAGE_SCN_ALIGN_4BYTES = 0x00300000;
        const long IMAGE_SCN_ALIGN_8BYTES = 0x00400000;
        const long IMAGE_SCN_ALIGN_16BYTES = 0x00500000;
        const long IMAGE_SCN_ALIGN_32BYTES = 0x00600000;
        const long IMAGE_SCN_ALIGN_64BYTES = 0x00700000;
        const long IMAGE_SCN_ALIGN_128BYTES = 0x00800000;
        const long IMAGE_SCN_ALIGN_256BYTES = 0x00900000;
        const long IMAGE_SCN_ALIGN_512BYTES = 0x00A00000;
        const long IMAGE_SCN_ALIGN_1024BYTES = 0x00B00000;
        const long IMAGE_SCN_ALIGN_2048BYTES = 0x00C00000;
        const long IMAGE_SCN_ALIGN_4096BYTES = 0x00D00000;
        const long IMAGE_SCN_ALIGN_8192BYTES = 0x00E00000;
        const long IMAGE_SCN_ALIGN_MASK = 0x00F00000;
        const long IMAGE_SCN_LNK_NRELOC_OVFL = 0x01000000;
        const long IMAGE_SCN_MEM_DISCARDABLE = 0x02000000;
        const long IMAGE_SCN_MEM_NOT_CACHED = 0x04000000;
        const long IMAGE_SCN_MEM_NOT_PAGED = 0x08000000;
        const long IMAGE_SCN_MEM_SHARED = 0x10000000;
        const long IMAGE_SCN_MEM_EXECUTE = 0x20000000;
        const long IMAGE_SCN_MEM_READ = 0x40000000;
        const long IMAGE_SCN_MEM_WRITE = 0x80000000;

        /* Symbol entry defines */
        const short IMAGE_SYM_UNDEFINED = 0;
        const short IMAGE_SYM_ABSOLUTE = -1;
        const short IMAGE_SYM_DEBUG = -2;

        const int IMAGE_SYM_TYPE_NULL = 0x0000;
        const int IMAGE_SYM_TYPE_VOID = 0x0001;
        const int IMAGE_SYM_TYPE_CHAR = 0x0002;
        const int IMAGE_SYM_TYPE_SHORT = 0x0003;
        const int IMAGE_SYM_TYPE_INT = 0x0004;
        const int IMAGE_SYM_TYPE_LONG = 0x0005;
        const int IMAGE_SYM_TYPE_FLOAT = 0x0006;
        const int IMAGE_SYM_TYPE_DOUBLE = 0x0007;
        const int IMAGE_SYM_TYPE_STRUCT = 0x0008;
        const int IMAGE_SYM_TYPE_UNION = 0x0009;
        const int IMAGE_SYM_TYPE_ENUM = 0x000A;
        const int IMAGE_SYM_TYPE_MOE = 0x000B;
        const int IMAGE_SYM_TYPE_BYTE = 0x000C;
        const int IMAGE_SYM_TYPE_WORD = 0x000D;
        const int IMAGE_SYM_TYPE_UINT = 0x000E;
        const int IMAGE_SYM_TYPE_DWORD = 0x000F;
        const int IMAGE_SYM_TYPE_PCODE = 0x8000;

        const int IMAGE_SYM_DTYPE_NULL = 0;
        const int IMAGE_SYM_DTYPE_POINTER = 1;
        const int IMAGE_SYM_DTYPE_FUNCTION = 2;
        const int IMAGE_SYM_DTYPE_ARRAY = 3;

        const byte IMAGE_SYM_CLASS_END_OF_FUNCTION = 0xFF;
        const byte IMAGE_SYM_CLASS_NULL = 0x00;
        const byte IMAGE_SYM_CLASS_AUTOMATIC = 0x01;
        const byte IMAGE_SYM_CLASS_EXTERNAL = 0x02;
        const byte IMAGE_SYM_CLASS_STATIC = 0x03;
        const byte IMAGE_SYM_CLASS_REGISTER = 0x04;
        const byte IMAGE_SYM_CLASS_EXTERNAL_DEF = 0x05;
        const byte IMAGE_SYM_CLASS_LABEL = 0x06;
        const byte IMAGE_SYM_CLASS_UNDEFINED_LABEL = 0x07;
        const byte IMAGE_SYM_CLASS_MEMBER_OF_STRUCT = 0x08;
        const byte IMAGE_SYM_CLASS_ARGUMENT = 0x09;
        const byte IMAGE_SYM_CLASS_STRUCT_TAG = 0x0A;
        const byte IMAGE_SYM_CLASS_MEMBER_OF_UNION = 0x0B;
        const byte IMAGE_SYM_CLASS_UNION_TAG = 0x0C;
        const byte IMAGE_SYM_CLASS_TYPE_DEFINITION = 0x0D;
        const byte IMAGE_SYM_CLASS_UNDEFINED_STATIC = 0x0E;
        const byte IMAGE_SYM_CLASS_ENUM_TAG = 0x0F;
        const byte IMAGE_SYM_CLASS_MEMBER_OF_ENUM = 0x10;
        const byte IMAGE_SYM_CLASS_REGISTER_PARAM = 0x11;
        const byte IMAGE_SYM_CLASS_BIT_FIELD = 0x12;
        const byte IMAGE_SYM_CLASS_FAR_EXTERNAL = 0x44;
        const byte IMAGE_SYM_CLASS_BLOCK = 0x64;
        const byte IMAGE_SYM_CLASS_FUNCTION = 0x65;
        const byte IMAGE_SYM_CLASS_END_OF_STRUCT = 0x66;
        const byte IMAGE_SYM_CLASS_FILE = 0x67;
        const byte IMAGE_SYM_CLASS_SECTION = 0x68;
        const byte IMAGE_SYM_CLASS_WEAK_EXTERNAL = 0x69;
        const byte IMAGE_SYM_CLASS_CLR_TOKEN = 0x6B;

        /* Microsoft COFF File Header */
        class IMAGE_FILE_HEADER
        {
            public static size_t TypeSize = 20;

            public uint16_t Machine;
            public uint16_t NumberOfSections;
            public uint32_t TimeDateStamp;
            public uint32_t PointerToSymbolTable;
            public uint32_t NumberOfSymbols;
            public uint16_t SizeOfOptionalHeader;
            public uint16_t Characteristics;
        }

        /* Microsoft COFF Section Header */
        class IMAGE_SECTION_HEADER
        {
            public static size_t TypeSize = 40;

            public byte[] Name; //[IMAGE_SIZEOF_SHORT_NAME]
            public uint32_t VirtualSize;
            public uint32_t VirtualAddress;
            public uint32_t SizeOfRawData;
            public uint32_t PointerToRawData;
            public uint32_t PointerToRelocations;
            public uint32_t PointerToLinenumbers;
            public uint16_t NumberOfRelocations;
            public uint16_t NumberOfLinenumbers;
            public uint32_t Characteristics;
        }

        class Name
        {
            public byte[] ShortName;

            public LongName LongName
            {
                get
                {
                    return new LongName()
                    {
                        Zeroes = BitConverter.ToUInt32(this.ShortName, 0),
                        Offset = BitConverter.ToUInt32(this.ShortName, 4)
                    };
                }

                set
                {
                    byte[] bytes = new byte[8];

                    byte[] zeroes = BitConverter.GetBytes(value.Zeroes);
                    byte[] offset = BitConverter.GetBytes(value.Offset);

                    for (int i = 0; i < 4; i++)
                    {
                        bytes[i] = zeroes[i];
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        bytes[i + 4] = offset[i];
                    }

                    this.ShortName = bytes;
                }
            }
        }

        class LongName
        {
            public uint32_t Zeroes;
            public uint32_t Offset;
        }

        /* Microsoft COFF Symbol Entry */
        class IMAGE_SYMBOL
        {
            public static size_t TypeSize = 18;
            public Name N = new Name();
            public int32_t Value;
            public int16_t SectionNumber;
            public uint16_t Type;
            public uint8_t StorageClass;
            public uint8_t NumberOfAuxSymbols;
        }

        /* COFF String Table */
        class IMAGE_STRINGS
        {
            public static size_t TypeSize = 4;
            public uint32_t TotalSize;
            public byte[] Strings;
        }

        enum Targets
        {
            x86, x64, arm64, Unknown
        }

        static bool check_64bit(string arg, ref uint x86_32, out Targets target)
        {
            if (arg == "64bit" || arg == "x64")
            {
                target = Targets.x64;
            }
            else if (arg == "arm64" || arg == "ARM64")
            {
                target = Targets.arm64;
            }
            else if (arg == "32bit" || arg == "Win32")
            {
                target = Targets.x86;
            }
            else
            {
                target = Targets.Unknown;
            }

            if (arg == "64bit" || arg == "x64" || arg == "arm64" || arg == "ARM64")
            {
                x86_32 = 0; /* 0 = 64bit */
            }
            else if (arg == "32bit" || arg == "Win32")
            {
                x86_32 = 1; /* 1 = 32bit */
            }
            else
            {
                return false;
            }

            return true;
        }

        static int Main(string[] args)
        {
            //args = new string[] { @"G:\OneDrive - University of Bristol\bin2coff\Data\test.bin", @"G:\OneDrive - University of Bristol\bin2coff\Data\test_arm64_GB.obj", "testlonglonglonglong", "arm64" };

            uint argc = (uint)args.Length + 1;
            const uint16_t endian_test = 0xBE00;
            uint x86_32, last_arg;
            bool short_label, short_size;
            string label;

            //FILE* fd = NULL;

            size_t size, alloc_size;
            byte[] data, padding;
            FileStream fd;
            IMAGE_FILE_HEADER file_header;
            IMAGE_SECTION_HEADER section_header;
            IMAGE_SYMBOL[] symbol_table;
            IMAGE_STRINGS string_table;
            SIZE_TYPE data_size;
            Targets target;

            if ((args.Length < 3) || (args.Length > 5))
            {
                Console.Error.Write("\nUsage: bin2coff bin obj [label] [64bit|Win32|x64|arm64|ARM64]\n\n");
                Console.Error.Write("  bin  : source binary data.\n");
                Console.Error.Write("  obj  : target object file, in MS COFF format.\n");
                Console.Error.Write("  label: identifier for the extern data. If not provided, the name of the\n");
                Console.Error.Write("         binary file without extension is used.\n");
                Console.Error.Write("  64bit:\n  Win32:\n  x64  :\n  arm64:\n  ARM64: produce an object that is compatible with\n");
                Console.Error.Write("         the specified architecture. For 64bit/x64 and arm64, symbols are\n");
                Console.Error.Write("         generated without leading underscores, and for arm64 the data is\n");
                Console.Error.Write("         aligned to 4-byte boundaries; machine type is set appropriately.\n\n");
                Console.Error.Write("With your linker set properly, typical access from a C source is:\n\n");
                Console.Error.Write("    extern uint8_t  label[]     /* binary data         */\n");
                Console.Error.Write("    extern uint32_t label_size  /* size of binary data */\n\n");
                return 1;
            }

            if (BitConverter.GetBytes(endian_test)[0] == 0xBE)
            {
                Console.Error.Write("\nThis program is not compatible with Big Endian architectures.\n");
                Console.Error.Write("You are welcome to modify the sourcecode (GPLv3+) to make it so.\n");
                return 1;
            }

            try
            {
                data = File.ReadAllBytes(args[0]);
            }
            catch (Exception ex)
            {
                Console.Error.Write("Couldn't open file '{0}'.\n{1}\n", args[1], ex.Message);
                return 1;
            }

            size = (size_t)data.Length;

            x86_32 = 0;
            last_arg = argc;
            if (argc >= 4 && check_64bit(args[2], ref x86_32, out target))
            {
                last_arg = 4;
            }
            else if (argc >= 5 && check_64bit(args[3], ref x86_32, out target))
            {
                last_arg = 5;
            }
            else
            {
                target = Targets.x64;
            }

            if (target == Targets.arm64)
            {
                padding = new byte[(4 - data.Length % 4) % 4];
            }
            else
            {
                padding = new byte[0];
            }

            /* Label setup */
            if (argc < last_arg)
            {
                args[0] = args[0].Substring(0, args[0].LastIndexOf('.'));
                label = args[0];
            }
            else
            {
                label = args[2];
            }

            label = label.Replace("-", "_");

            short_label = (label.Length + x86_32) <= IMAGE_SIZEOF_SHORT_NAME;
            short_size = (label.Length + x86_32 + SIZE_LABEL_SUFFIX.Length) <= IMAGE_SIZEOF_SHORT_NAME;
            alloc_size = IMAGE_FILE_HEADER.TypeSize + IMAGE_SECTION_HEADER.TypeSize + size + (uint)padding.Length + sizeof(SIZE_TYPE) + 2 * IMAGE_SYMBOL.TypeSize + IMAGE_STRINGS.TypeSize;
            if (!short_label)
            {
                alloc_size += (ulong)(x86_32 + label.Length + 1);
            }
            if (!short_size)
            {
                alloc_size += (ulong)(x86_32 + label.Length + SIZE_LABEL_SUFFIX.Length + 1);
            }


            file_header = new IMAGE_FILE_HEADER();
            section_header = new IMAGE_SECTION_HEADER();
            symbol_table = new IMAGE_SYMBOL[2] { new IMAGE_SYMBOL(), new IMAGE_SYMBOL() };
            string_table = new IMAGE_STRINGS();

            /* Populate file header */
            switch (target)
            {
                case Targets.x86:
                    file_header.Machine = (ushort)IMAGE_FILE_MACHINE_I386;
                    break;
                case Targets.x64:
                    file_header.Machine = (ushort)IMAGE_FILE_MACHINE_AMD64;
                    break;
                case Targets.arm64:
                    file_header.Machine = (ushort)IMAGE_FILE_MACHINE_ARM64;
                    break;
            }

            file_header.NumberOfSections = 1;
            file_header.PointerToSymbolTable = (uint)(IMAGE_FILE_HEADER.TypeSize + IMAGE_SECTION_HEADER.TypeSize + (uint32_t)size + 4 + (uint)padding.Length);
            file_header.NumberOfSymbols = 2;
            file_header.Characteristics = IMAGE_FILE_LINE_NUMS_STRIPPED;

            /* Populate data section header */
            section_header.Name = System.Text.Encoding.ASCII.GetBytes(".data\0\0\0");
            section_header.SizeOfRawData = (uint32_t)size + 4 + (uint)padding.Length;
            section_header.PointerToRawData = (uint)(IMAGE_FILE_HEADER.TypeSize + IMAGE_SECTION_HEADER.TypeSize);
            section_header.Characteristics = (uint)(IMAGE_SCN_CNT_INITIALIZED_DATA | IMAGE_SCN_ALIGN_16BYTES | IMAGE_SCN_MEM_READ | IMAGE_SCN_MEM_WRITE);

            data_size = (SIZE_TYPE)size;

            /* Populate symbol table */
            if (short_label)
            {
                if (x86_32 == 1)
                {
                    symbol_table[0].N.ShortName = System.Text.Encoding.ASCII.GetBytes("_" + label + new string('\0', 8 - label.Length - 1));
                }
                else if (x86_32 == 0)
                {
                    symbol_table[0].N.ShortName = System.Text.Encoding.ASCII.GetBytes(label + new string('\0', 8 - label.Length));
                }
            }
            else
            {
                symbol_table[0].N.LongName = new LongName()
                {
                    Zeroes = 0,
                    Offset = (uint)IMAGE_STRINGS.TypeSize
                };
            }

            /* Ideally, we would use (IMAGE_SYM_DTYPE_ARRAY << 8) | IMAGE_SYM_TYPE_BYTE
             * to indicate an array of bytes, but the type is ignored in MS objects. */
            symbol_table[0].Type = IMAGE_SYM_TYPE_NULL;
            symbol_table[0].StorageClass = IMAGE_SYM_CLASS_EXTERNAL;
            symbol_table[0].SectionNumber = 1;
            symbol_table[0].Value = 0;              /* Offset within the section */

            if (short_size)
            {
                if (x86_32 == 1)
                {
                    //Note: the original C code appears to set the value to "\0" + label + SIZE_LABEL_SUFFIX instead - is this intended? [GB]
                    symbol_table[1].N.ShortName = System.Text.Encoding.ASCII.GetBytes("_" + label + SIZE_LABEL_SUFFIX + new string('\0', 8 - label.Length - 1));
                }
                else if (x86_32 == 0)
                {
                    symbol_table[1].N.ShortName = System.Text.Encoding.ASCII.GetBytes(label + SIZE_LABEL_SUFFIX + new string('\0', 8 - label.Length));
                }
            }
            else
            {
                symbol_table[1].N.LongName = new LongName()
                {
                    Zeroes = 0,
                    Offset = (uint)IMAGE_STRINGS.TypeSize + (uint)((short_label) ? 0 : (x86_32 + (uint32_t)label.Length + 1))
                };
            }
            symbol_table[1].Type = IMAGE_SYM_TYPE_NULL;
            symbol_table[1].StorageClass = IMAGE_SYM_CLASS_EXTERNAL;
            symbol_table[1].SectionNumber = 1;
            symbol_table[1].Value = (int32_t)size + padding.Length;  /* Offset within the section */

            /* Populate string table */
            string_table.TotalSize = (uint)IMAGE_STRINGS.TypeSize;

            string string_tableStrings = "";

            if (!short_label)
            {
                if (x86_32 == 1)
                {
                    string_tableStrings = "_" + label + "\0";
                }
                else if (x86_32 == 0)
                {
                    string_tableStrings = label + "\0";
                }

                string_table.TotalSize += x86_32 + (uint32_t)label.Length + 1;
            }

            if (!short_size)
            {
                if (x86_32 == 1)
                {
                    string_tableStrings += "_" + label + SIZE_LABEL_SUFFIX + "\0";
                }
                else if (x86_32 == 0)
                {
                    string_tableStrings += label + SIZE_LABEL_SUFFIX + "\0";
                }

                string_table.TotalSize += x86_32 + (uint32_t)label.Length;

                string_table.TotalSize += (uint32_t)SIZE_LABEL_SUFFIX.Length + 1;
            }

            string_table.Strings = System.Text.Encoding.ASCII.GetBytes(string_tableStrings);

            try
            {
                fd = File.Create(args[1]);
            }
            catch (Exception ex)
            {
                Console.Error.Write("Couldn't create file '{0}'.\n{1}\n", args[1], ex.Message);
                return 1;
            }

            // Write object to file [GB]
            // file_header
            // section_header
            // data
            // padding
            // data_size
            // symbol_table[0]
            // symbol_table[1]
            // string_table

            using BinaryWriter writer = new BinaryWriter(fd);

            // Write file_header [GB]
            writer.Write(file_header.Machine);
            writer.Write(file_header.NumberOfSections);
            writer.Write(file_header.TimeDateStamp);
            writer.Write(file_header.PointerToSymbolTable);
            writer.Write(file_header.NumberOfSymbols);
            writer.Write(file_header.SizeOfOptionalHeader);
            writer.Write(file_header.Characteristics);

            // Write section_header [GB]
            writer.Write(section_header.Name, 0, section_header.Name.Length);
            writer.Write(section_header.VirtualSize);
            writer.Write(section_header.VirtualAddress);
            writer.Write(section_header.SizeOfRawData);
            writer.Write(section_header.PointerToRawData);
            writer.Write(section_header.PointerToRelocations);
            writer.Write(section_header.PointerToLinenumbers);
            writer.Write(section_header.NumberOfRelocations);
            writer.Write(section_header.NumberOfLinenumbers);
            writer.Write(section_header.Characteristics);

            // Write data [GB]
            writer.Write(data, 0, data.Length);

            // Write padding [GB]
            writer.Write(padding, 0, padding.Length);

            // Write data_size [GB]
            writer.Write(data_size);

            // Write symbol_table [GB]
            for (int i = 0; i < symbol_table.Length; i++)
            {
                writer.Write(symbol_table[i].N.ShortName);
                writer.Write(symbol_table[i].Value);
                writer.Write(symbol_table[i].SectionNumber);
                writer.Write(symbol_table[i].Type);
                writer.Write(symbol_table[i].StorageClass);
                writer.Write(symbol_table[i].NumberOfAuxSymbols);
            }

            // Write string_table [GB]
            writer.Write(string_table.TotalSize);
            writer.Write(string_table.Strings, 0, string_table.Strings.Length);
            
            Console.Write("Successfully created COFF object file '{0}'\n", args[1]);

            return 0;
        }

    }
}