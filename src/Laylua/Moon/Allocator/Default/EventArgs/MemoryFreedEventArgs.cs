using System;

namespace Laylua.Moon;

/// <summary>
///     Represents event data for <see cref="NativeMemoryLuaAllocator.MemoryFreed"/>.
/// </summary>
public readonly struct MemoryFreedEventArgs
{
    /// <summary>
    ///     Gets the address Lua freed.
    /// </summary>
    public IntPtr Address { get; }

    /// <summary>
    ///     Gets the amount of bytes Lua freed.
    /// </summary>
    public nuint Bytes { get; }

    internal MemoryFreedEventArgs(IntPtr address, nuint bytes)
    {
        Address = address;
        Bytes = bytes;
    }
}
