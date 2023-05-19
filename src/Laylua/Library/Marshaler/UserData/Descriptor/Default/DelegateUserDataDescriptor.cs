using System;
using System.Runtime.CompilerServices;
using Laylua.Moon;

namespace Laylua.Marshalling;

public unsafe class DelegateUserDataDescriptor : UserDataDescriptor
{
    /// <inheritdoc/>
    public override string MetatableName => "delegate";

    private readonly ConditionalWeakTable<Delegate, Func<Lua, LuaStackValueRange, int>> _invokers;
    private readonly LuaCFunction _call;

    public DelegateUserDataDescriptor()
    {
        _invokers = new();
        _call ??= L =>
        {
            var lua = Lua.FromExtraSpace(L);
            var top = lua_gettop(L);
            var arguments = top == 1
                ? LuaStackValueRange.Empty
                : lua.Stack.GetRange(2);

            return Call(lua, lua.Stack[1], arguments);
        };
    }

    /// <summary>
    ///     By default, calls the delegate.
    /// </summary>
    /// <param name="lua"> The Lua state. </param>
    /// <param name="userData"> The user data. </param>
    /// <param name="arguments"> The function arguments. </param>
    /// <returns>
    ///     The amount of values pushed onto the stack.
    /// </returns>
    public virtual int Call(Lua lua, LuaStackValue userData, LuaStackValueRange arguments)
    {
        var @delegate = userData.GetValue<Delegate>();
        if (@delegate == null)
        {
            lua.RaiseArgumentError(userData.Index, "The argument must be a delegate.");
        }

        var invoker = _invokers.GetValue(@delegate, static @delegate => UserDataDescriptorUtilities.CreateCallInvoker(@delegate.Target, @delegate.Method));
        return invoker(lua, arguments);
    }

    /// <inheritdoc/>
    public override void OnMetatableCreated(Lua lua, LuaStackValue metatable)
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
