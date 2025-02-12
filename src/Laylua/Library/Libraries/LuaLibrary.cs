﻿using System.Collections.Generic;

namespace Laylua;

/// <summary>
///     Represents a Lua library.
/// </summary>
/// <remarks>
///     This API is a <c>Laylua</c> concept and
///     does not directly represent a Lua feature.
///     <para/>
///     <see cref="LuaLibrary"/> objects can represent the standard Lua libraries,
///     but also custom made ones that follow the opening and closing pattern.
/// </remarks>
public abstract class LuaLibrary
{
    /// <summary>
    ///     Gets the name that can be used to identify this library.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    ///     Gets the names of the globals this library will define upon opening.
    /// </summary>
    public abstract IReadOnlyList<string> Globals { get; }

    /// <summary>
    ///     Opens this library, loading it into the Lua state.
    /// </summary>
    /// <param name="thread"> The Lua thread. </param>
    /// <param name="leaveOnStack"> Whether to leave the module on the stack. </param>
    protected internal abstract void Open(Lua lua, bool leaveOnStack);

    /// <summary>
    ///     Closes this library, unloading it from the Lua state.
    /// </summary>
    /// <param name="thread"> The Lua thread. </param>
    protected internal abstract void Close(Lua lua);
}
