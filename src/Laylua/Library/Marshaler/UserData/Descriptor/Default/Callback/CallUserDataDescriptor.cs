using Laylua.Moon;

namespace Laylua.Marshaling;

public abstract unsafe class CallUserDataDescriptor : UserDataDescriptor
{
    private readonly LuaCFunction _call;

    protected CallUserDataDescriptor()
    {
        _call = L =>
        {
            var lua = LuaThread.FromExtraSpace(L);
            var top = lua_gettop(L);
            var arguments = top == 1
                ? LuaStackValueRange.Empty
                : lua.Stack.GetRange(2);

            return Call(lua, lua.Stack[1], arguments);
        };
    }

    /// <summary>
    ///     Invoked through the <c>__call</c> metamethod.
    /// </summary>
    /// <param name="lua"> The Lua state. </param>
    /// <param name="userData"> The user data. </param>
    /// <param name="arguments"> The function arguments. </param>
    /// <returns>
    ///     The amount of values pushed onto the stack.
    /// </returns>
    public abstract int Call(LuaThread lua, LuaStackValue userData, LuaStackValueRange arguments);

    /// <inheritdoc/>
    public override void OnMetatableCreated(LuaThread lua, LuaStackValue metatable)
    {
        var L = lua.GetStatePointer();
        var metatableIndex = metatable.Index;
        using (lua.Stack.SnapshotCount())
        {
            lua_pushstring(L, LuaMetatableKeysUtf8.__call);
            lua_pushcfunction(L, _call);
            lua_rawset(L, metatableIndex);
        }
    }
}
