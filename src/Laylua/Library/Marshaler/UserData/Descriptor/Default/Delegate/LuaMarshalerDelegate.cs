using System;
using System.Runtime.CompilerServices;

namespace Laylua.Marshaling;

public class LuaMarshalerDelegate : CallUserDataDescriptor
{
    /// <inheritdoc/>
    public override string MetatableName => "delegate";

    private readonly ConditionalWeakTable<Delegate, UserDataDescriptorUtilities.MethodInvokerDelegate> _invokers;

    public LuaMarshalerDelegate()
    {
        _invokers = new();
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
    public override int Call(LuaThread lua, LuaStackValue userData, LuaStackValueRange arguments)
    {
        if (!userData.TryGetValue<Delegate>(out var @delegate) || @delegate == null)
        {
            lua.RaiseError("The userdata argument must be a delegate.");
        }

        var invoker = _invokers.GetValue(@delegate, UserDataDescriptorUtilities.CreateDelegateInvoker);
        return invoker(lua, arguments);
    }
}
