namespace Laylua.Moon;

/// <summary>
///     Represents a type responsible for reading Lua chunks into memory.
///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#3.3.2">Lua manual</a>. </para>
/// </summary>
public abstract unsafe class LuaChunkReader
{
    /// <summary>
    ///     Reads a Lua chunk and returns it as a memory block.
    /// </summary>
    /// <remarks>
    ///     The code must not throw any exceptions.
    /// </remarks>
    /// <param name="L"> The Lua state. </param>
    /// <param name="bytesRead"> The size of the returned memory block. </param>
    /// <returns>
    ///     The pointer to the memory block.
    ///     The memory block must be valid until <see cref="Read"/> is called again.
    /// </returns>
    protected internal abstract byte* Read(lua_State* L, out nuint bytesRead);
}
