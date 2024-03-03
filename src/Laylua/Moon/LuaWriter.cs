using System;

namespace Laylua.Moon;

/// <summary>
///     Represents a writer responsible for writing binary Lua code into e.g. a file.
/// </summary>
public abstract unsafe class LuaWriter : IDisposable
{
    /// <summary>
    ///     Writes a memory block of Lua code.
    /// </summary>
    /// <remarks>
    ///     The code must not throw any exceptions.
    /// </remarks>
    /// <param name="L"> The Lua state. </param>
    /// <param name="data"> The memory block to write. </param>
    /// <param name="length"> The size of the memory block. </param>
    /// <returns>
    ///     <see langword="0"/> if the write operation succeeded.
    ///     Otherwise, any other value, indicating failure and preventing <see cref="Write"/> from being called again.
    /// </returns>
    protected internal abstract int Write(lua_State* L, byte* data, nuint length);

    protected virtual void Dispose(bool disposing)
    { }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
