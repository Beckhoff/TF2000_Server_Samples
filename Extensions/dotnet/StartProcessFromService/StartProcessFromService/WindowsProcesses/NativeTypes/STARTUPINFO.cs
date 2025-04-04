// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global

using System;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace
namespace WindowsProcesses.NativeTypes
{
    /// <summary>
    ///     https://docs.microsoft.com/en-us/windows/win32/api/processthreadsapi/ns-processthreadsapi-startupinfoa
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    // ReSharper disable once InconsistentNaming
    internal struct STARTUPINFO
    {
#pragma warning disable IDE1006 // Naming Styles
        public int cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public uint dwX;
        public uint dwY;
        public uint dwXSize;
        public uint dwYSize;
        public uint dwXCountChars;
        public uint dwYCountChars;
        public uint dwFillAttribute;
        public uint dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
#pragma warning restore IDE1006 // Naming Styles
    }
}
