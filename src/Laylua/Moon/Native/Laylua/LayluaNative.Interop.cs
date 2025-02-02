using System;
using System.Runtime.InteropServices;

namespace Laylua.Moon;

internal static partial class LayluaNative
{
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool VirtualProtect(IntPtr lpAddress, nuint dwSize, MemoryProtection flNewProtect, out MemoryProtection lpflOldProtect);

    [Flags]
    public enum MemoryProtection
    {
        ExecuteReadWrite = 0x40
    }

    [LibraryImport("libc", SetLastError = true)]
    private static partial int getpagesize();

    [LibraryImport("libc", SetLastError = true)]
    public static partial int mprotect(IntPtr start, nuint len, MmapProts prot);

    [Flags]
    public enum MmapProts
    {
        PROT_READ = 0x1,
        PROT_WRITE = 0x2,
        PROT_EXEC = 0x4
    }
}
