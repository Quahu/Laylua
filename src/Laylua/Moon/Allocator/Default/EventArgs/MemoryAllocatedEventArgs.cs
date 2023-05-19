using System;

namespace Laylua.Moon;

/// <summary>
///     Represents event data for <see cref="NativeMemoryLuaAllocator.MemoryAllocated"/>.
/// </summary>
public readonly struct MemoryAllocatedEventArgs
{
    /// <summary>
    ///     Gets the address Lua allocated.
    /// </summary>
    public IntPtr Address { get; }

    /// <summary>
    ///     Gets the amount of bytes Lua allocated.
    /// </summary>
    public nuint Bytes { get; }

    internal MemoryAllocatedEventArgs(IntPtr address, nuint bytes)
    {
        Address = address;
        Bytes = bytes;
    }
}
