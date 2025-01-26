using System;

namespace Laylua;

/// <summary>
///     Represents a type responsible for reading Lua chunks into memory.
///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#3.3.2">Lua manual</a>. </para>
/// </summary>
public abstract class LuaChunkReader
{
    /// <summary>
    ///     Reads a Lua chunk into a memory block.
    /// </summary>
    /// <param name="buffer"> The memory block to read the Lua chunk into. </param>
    /// <returns>
    ///     The amount of bytes read into <paramref name="buffer"/>.
    /// </returns>
    protected internal abstract int Read(Span<byte> buffer);
}
