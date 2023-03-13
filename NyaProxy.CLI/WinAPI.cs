using NyaProxy.API;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace NyaProxy.CLI
{
    //Source:http://pinvoke.net
    internal static class WinAPI
    {

        internal const int STD_OUTPUT_HANDLE = -11; // per WinBase.h
        internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);    // per WinBase.h

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint mode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetConsoleMode(IntPtr handle, out uint mode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleTextAttribute(IntPtr hConsoleOutput, ushort wAttributes);

        static WinAPI()
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Windows Only.");
        }

        [Flags]
        public enum ConsoleModes : uint
        {
            ENABLE_PROCESSED_INPUT = 0x0001,
            ENABLE_LINE_INPUT = 0x0002,
            ENABLE_ECHO_INPUT = 0x0004,
            ENABLE_WINDOW_INPUT = 0x0008,
            ENABLE_MOUSE_INPUT = 0x0010,
            ENABLE_INSERT_MODE = 0x0020,
            ENABLE_QUICK_EDIT_MODE = 0x0040,
            ENABLE_EXTENDED_FLAGS = 0x0080,
            ENABLE_AUTO_POSITION = 0x0100,

            ENABLE_PROCESSED_OUTPUT = 0x0001,
            ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002,
            ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004,
            DISABLE_NEWLINE_AUTO_RETURN = 0x0008,
            ENABLE_LVB_GRID_WORLDWIDE = 0x0010
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/console/char-info-str
        /// </summary>
        public static class ConsoleColorAttributes
        {
            /// <summary> Text color contains blue.</summary>
            public const ushort FOREGROUND_BLUE = 0x0001;
            /// <summary> Text color contains green.</summary>
            public const ushort FOREGROUND_GREEN = 0x0002;
            /// <summary> Text color contains red.</summary>
            public const ushort FOREGROUND_RED = 0x0004;
            /// <summary> Text color is intensified.</summary>
            public const ushort FOREGROUND_INTENSITY = 0x0008;
            /// <summary> Background color contains blue.</summary>
            public const ushort BACKGROUND_BLUE = 0x0010;
            /// <summary> Background color contains green.</summary>
            public const ushort BACKGROUND_GREEN = 0x0020;
            /// <summary> Background color contains red.</summary>
            public const ushort BACKGROUND_RED = 0x0040;
            /// <summary> Background color is intensified.</summary>
            public const ushort BACKGROUND_INTENSITY = 0x0080;
            /// <summary> Leading byte.</summary>
            public const ushort COMMON_LVB_LEADING_BYTE = 0x0100;
            /// <summary> Trailing byte.</summary>
            public const ushort COMMON_LVB_TRAILING_BYTE = 0x0200;
            /// <summary> Top horizontal</summary>
            public const ushort COMMON_LVB_GRID_HORIZONTAL = 0x0400;
            /// <summary> Left vertical.</summary>
            public const ushort COMMON_LVB_GRID_LVERTICAL = 0x0800;
            /// <summary> Right vertical.</summary>
            public const ushort COMMON_LVB_GRID_RVERTICAL = 0x1000;
            /// <summary> Reverse foreground and background attribute.</summary>
            public const ushort COMMON_LVB_REVERSE_VIDEO = 0x4000;
            /// <summary> Underscore.</summary>
            public const ushort COMMON_LVB_UNDERSCORE = 0x8000;
        }

    }
}