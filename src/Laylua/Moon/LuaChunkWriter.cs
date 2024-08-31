namespace Laylua.Moon;

/// <summary>
///     Represents a type responsible for writing Lua chunks into an implementation-defined destination.
///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#3.3.2">Lua manual</a>. </para>
/// </summary>
public abstract unsafe class LuaChunkWriter
{
    /// <summary>
    ///     Writes the specified Lua chunk.
    /// </summary>
    /// <param name="L"> The Lua state. </param>
    /// <param name="data"> The pointer to the memory block containing the Lua chunk to write. </param>
    /// <param name="length"> The size of the memory block. </param>
    /// <returns>
    ///     <see langword="0"/> if the write operation succeeded.
    ///     Otherwise, any other value, indicating failure and preventing <see cref="Write"/> from being called again.
    /// </returns>
    protected internal abstract int Write(lua_State* L, byte* data, nuint length);
}
