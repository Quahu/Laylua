namespace Laylua.Moon;

/// <summary>
///     Represents event data for <see cref="NativeMemoryLuaAllocator.MemoryOutOfMemory"/>.
/// </summary>
public readonly struct MemoryOutOfMemoryEventArgs
{
    /// <summary>
    ///     Gets the amount of bytes Lua failed to allocate.
    /// </summary>
    public nuint Bytes { get; }

    internal MemoryOutOfMemoryEventArgs(nuint bytes)
    {
        Bytes = bytes;
    }
}
