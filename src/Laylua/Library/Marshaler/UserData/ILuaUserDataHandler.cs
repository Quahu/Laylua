using System;
using System.Diagnostics.CodeAnalysis;
using Laylua.Moon;

namespace Laylua.Marshaling;

/// <summary>
///     Defines the contract for a userdata handler that manages
///     pushing, retrieving, and defining CLR types as Lua userdata.
/// </summary>
/// <remarks>
///     Source-generated handlers implement this interface on a nested
///     <c>LuaUserDataHandler</c> class. A reflection-based fallback
///     also implements this interface for unannotated types.
/// </remarks>
public unsafe interface ILuaUserDataHandler
{
    /// <summary>
    ///     Gets the CLR type this handler manages.
    /// </summary>
    Type Type { get; }

    /// <summary>
    ///     Pushes a CLR value onto the Lua stack as userdata.
    /// </summary>
    /// <param name="L"> The Lua state. </param>
    /// <param name="value"> The CLR object to push. </param>
    void Push(lua_State* L, object value);

    /// <summary>
    ///     Attempts to retrieve a CLR value from the Lua stack at the given index.
    /// </summary>
    /// <param name="L"> The Lua state. </param>
    /// <param name="stackIndex"> The stack index. </param>
    /// <param name="value"> The retrieved CLR object, or <c>null</c> if retrieval failed. </param>
    /// <returns> <c>true</c> if a value was successfully retrieved; otherwise <c>false</c>. </returns>
    bool TryGet(lua_State* L, int stackIndex, out object? value);

    /// <summary>
    ///     Attempts to materialize a CLR object from a Lua table at the given index.
    /// </summary>
    /// <param name="L"> The Lua state. </param>
    /// <param name="stackIndex"> The stack index of the table. </param>
    /// <param name="value"> The materialized CLR object, or <c>null</c> if conversion failed. </param>
    /// <param name="error"> The conversion error, if conversion failed. </param>
    /// <returns> <c>true</c> if a value was successfully materialized; otherwise <c>false</c>. </returns>
    bool TryGetFromTable(
        lua_State* L,
        int stackIndex,
        out object? value,
        [NotNullWhen(false)] out LuaConversionError? error);

    /// <summary>
    ///     Pushes the type definition (static members) onto the Lua stack.
    /// </summary>
    /// <param name="L"> The Lua state. </param>
    void PushDefinition(lua_State* L);

    /// <summary>
    ///     Removes any metatables or registry entries this handler created in the given Lua state.
    ///     Called when the handler is unregistered from <see cref="LuaTypeRegistry"/>.
    /// </summary>
    /// <param name="L"> The Lua state. </param>
    void CleanUpState(lua_State* L);
}
