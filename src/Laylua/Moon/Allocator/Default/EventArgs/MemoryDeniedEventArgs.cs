namespace Laylua.Moon;

/// <summary>
///     Represents event data for <see cref="NativeMemoryLuaAllocator.MemoryDenied"/>.
/// </summary>
public readonly struct MemoryDeniedEventArgs
{
    /// <summary>
    ///     Gets the amount of bytes Lua failed to allocate.
    /// </summary>
    public nuint Bytes { get; }

    internal MemoryDeniedEventArgs(nuint bytes)
    {
        Bytes = bytes;
    }
}
