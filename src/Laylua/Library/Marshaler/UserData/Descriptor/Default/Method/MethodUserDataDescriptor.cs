using System;
using System.Reflection;
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

        if (AwaitableUserData.IsAwaitableType(method.ReturnType))
        {
            foreach (var parameter in method.GetParameters())
            {
                if (parameter.IsOut || (parameter.ParameterType.IsByRef && !parameter.IsIn))
                {
                    throw new NotSupportedException("Awaitable-returning userdata methods cannot expose out or ref Lua return values.");
                }

                if (GetLuaBoundaryType(parameter.ParameterType) == typeof(LuaStackValueRange))
                {
                    throw new NotSupportedException($"{nameof(LuaStackValueRange)} values are stack-bound and cannot be captured across awaits.");
                }
            }
        }

        var invoker = _invokers.GetValue(method, UserDataDescriptorUtilities.CreateMethodInvoker);
        var returnCount = invoker(thread, arguments);
        if (returnCount == 1
            && AwaitableUserData.ShouldImplicitAwait(thread)
            && AwaitableUserData.IsAwaitableType(method.ReturnType))
        {
            unsafe
            {
                return LuaAsyncLibrary.YieldAwaitable(thread.State.L);
            }
        }

        return returnCount;
    }

    private static Type GetLuaBoundaryType(Type type)
    {
        return type.IsByRef
            ? type.GetElementType()!
            : type;
    }
}
