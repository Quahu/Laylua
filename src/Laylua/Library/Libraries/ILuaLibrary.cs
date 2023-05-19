using System.Collections.Generic;

namespace Laylua;

/// <summary>
///     Represents a Lua library.
/// </summary>
/// <remarks>
///     This API is a <c>Laylua</c> concept and
///     does not directly represent a Lua feature.
///     <para/>
///     <see cref="ILuaLibrary"/> objects can represent the standard Lua libraries,
///     but also custom made ones that follow the opening and closing pattern.
/// </remarks>
public interface ILuaLibrary
{
    /// <summary>
    ///     Gets the name that can be used to identify this library.
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     Gets the names of the globals this library will define upon opening.
    /// </summary>
    IReadOnlyList<string> Globals { get; }

    /// <summary>
    ///     Opens this library, loading it into the Lua state.
    /// </summary>
    /// <param name="lua"> The Lua state. </param>
    /// <param name="leaveOnStack"> Whether to leave the module on the stack. </param>
    void Open(Lua lua, bool leaveOnStack);

    /// <summary>
    ///     Closes this library, unloading it from the Lua state.
    /// </summary>
    /// <param name="lua"> The Lua state. </param>
    void Close(Lua lua);
}
