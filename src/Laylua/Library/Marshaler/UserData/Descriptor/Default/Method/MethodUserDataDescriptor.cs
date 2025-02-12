﻿using System.Reflection;
using System.Runtime.CompilerServices;

namespace Laylua.Marshaling;

public class MethodUserDataDescriptor : CallUserDataDescriptor
{
    /// <inheritdoc/>
    public override string MetatableName => "method";

    private readonly ConditionalWeakTable<MethodInfo, UserDataDescriptorUtilities.MethodInvokerDelegate> _invokers;

    public MethodUserDataDescriptor()
    {
        _invokers = new();
    }

    /// <summary>
    ///     By default, calls the method.
    /// </summary>
    /// <param name="thread"> The Lua thread. </param>
    /// <param name="userData"> The user data. </param>
    /// <param name="arguments"> The function arguments. </param>
    /// <returns>
    ///     The amount of values pushed onto the stack.
    /// </returns>
    public override int Call(LuaThread thread, LuaStackValue userData, LuaStackValueRange arguments)
    {
        if (!userData.TryGetValue<MethodInfo>(out var method) || method == null)
        {
            return thread.RaiseError("The userdata argument must be a method definition.");
        }

        // if (!method.IsConstructedGenericMethod)
        // {
        //     lua.RaiseError("");
        // }

        var invoker = _invokers.GetValue(method, UserDataDescriptorUtilities.CreateMethodInvoker);
        return invoker(thread, arguments);
    }
}
