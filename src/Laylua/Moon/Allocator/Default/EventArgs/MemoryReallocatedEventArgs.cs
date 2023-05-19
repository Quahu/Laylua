using System;

namespace Laylua.Moon;

/// <summary>
///     Represents event data for <see cref="NativeMemoryLuaAllocator.MemoryReallocated"/>.
/// </summary>
public readonly struct MemoryReallocatedEventArgs
{
    /// <summary>
    ///     Gets the address Lua reallocated from.
    /// </summary>
    public IntPtr OldAddress { get; }

    /// <summary>
    ///     Gets the amount of bytes Lua reallocated from.
    /// </summary>
    public nuint OldBytes { get; }

    /// <summary>
    ///     Gets the amount of bytes Lua reallocated to.
    /// </summary>
    public IntPtr NewAddress { get; }

    /// <summary>
    ///     Gets the amount of bytes Lua allocated to.
    /// </summary>
    public nuint NewBytes { get; }

    internal MemoryReallocatedEventArgs(IntPtr oldAddress, nuint oldBytes, IntPtr newAddress, nuint newBytes)
    {
        OldAddress = oldAddress;
        OldBytes = oldBytes;
        NewAddress = newAddress;
        NewBytes = newBytes;
    }
}
