using Laylua.Moon;

namespace Laylua.Marshaling;

/// <summary>
///     Generic variant of <see cref="ILuaUserDataHandler"/> that provides
///     typed operations to avoid boxing for value types.
/// </summary>
public unsafe interface ILuaUserDataHandler<T> : ILuaUserDataHandler
{
    /// <summary>
    ///     Pushes a typed value onto the Lua stack as userdata.
    /// </summary>
    void Push(lua_State* L, T value);

    /// <summary>
    ///     Attempts to retrieve a typed value from the Lua stack at the given index.
    /// </summary>
    bool TryGet(lua_State* L, int stackIndex, out T? value);

    /// <summary>
    ///     Attempts to materialize a <typeparamref name="T"/> from a Lua table at the given index.
    /// </summary>
    bool TryGetFromTable(lua_State* L, int stackIndex, out T? value);
}
