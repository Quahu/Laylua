using System;

namespace Laylua.Moon;

/// <summary>
///     Represents a reader responsible for reading Lua code into blocks of memory.
/// </summary>
public abstract unsafe class LuaReader : IDisposable
{
    /// <summary>
    ///     Reads a memory block of Lua code.
    /// </summary>
    /// <remarks>
    ///     The code must not throw any exceptions.
    /// </remarks>
    /// <param name="L"> The Lua state. </param>
    /// <param name="bytesRead"> The size of the returned memory block. </param>
    /// <returns>
    ///     The pointer to the memory block.
    ///     This memory block must be valid until <see cref="Read"/> is called again or this reader is disposed.
    /// </returns>
    protected internal abstract byte* Read(lua_State* L, out nuint bytesRead);

    protected virtual void Dispose(bool disposing)
    { }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
