using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Qommon;

namespace Laylua.Marshalling;

public static class UserDataDescriptorUtilities
{
    private static readonly MethodInfo _pushMethod;

    private static readonly MethodInfo _getArgument;
    private static readonly MethodInfo _getRangeArgument;
    private static readonly MethodInfo _getParamsArgument;

    static UserDataDescriptorUtilities()
    {
        var pushMethod = typeof(LuaStack).GetMethod(nameof(LuaStack.Push));

        var getArgument = typeof(UserDataDescriptorUtilities).GetMethod(nameof(GetArgument), BindingFlags.Static | BindingFlags.NonPublic);
        var getRangeArgument = typeof(UserDataDescriptorUtilities).GetMethod(nameof(GetRangeArgument), BindingFlags.Static | BindingFlags.NonPublic);
        var getParamsArgument = typeof(UserDataDescriptorUtilities).GetMethod(nameof(GetParamsArgument), BindingFlags.Static | BindingFlags.NonPublic);

        Guard.IsTrue(RuntimeFeature.IsDynamicCodeSupported);

        Guard.IsNotNull(pushMethod);

        Guard.IsNotNull(getArgument);
        Guard.IsNotNull(getRangeArgument);
        Guard.IsNotNull(getParamsArgument);

        _pushMethod = pushMethod;

        _getArgument = getArgument;
        _getRangeArgument = getRangeArgument;
        _getParamsArgument = getParamsArgument;
    }

    private static T? GetArgument<T>(Lua lua, LuaStackValueRange arguments, int argumentIndex)
    {
        if (arguments.Count < argumentIndex)
        {
            lua.RaiseArgumentError(argumentIndex, "Missing argument.");
        }

        var argument = arguments[argumentIndex];
        if (!argument.TryGetValue<T>(out var convertedArgument))
        {
            lua.RaiseArgumentTypeError(argument.Index, typeof(T).Name);
        }

        return convertedArgument;
    }

    private static LuaStackValueRange GetRangeArgument(Lua lua, LuaStackValueRange arguments, int argumentIndex)
    {
        return new(lua.Stack, argumentIndex, arguments.Count);
    }

    private static T?[] GetParamsArgument<T>(Lua lua, LuaStackValueRange arguments, int argumentIndex)
    {
        var remainingArgumentCount = arguments.Count - (argumentIndex - 1);
        var paramsArgument = new T?[remainingArgumentCount];
        for (var i = 0; i < remainingArgumentCount; i++)
        {
            var argument = arguments[argumentIndex + i];
            if (!argument.TryGetValue<T>(out var convertedArgument))
            {
                lua.RaiseArgumentTypeError(argument.Index, typeof(T).Name);
            }

            paramsArgument[i] = convertedArgument;
        }

        return paramsArgument;
    }

    public static Func<Lua, LuaStackValueRange, int> CreateCallInvoker(object? instance, MethodInfo methodInfo)
    {
        Guard.IsNotNull(methodInfo);

        var parameterInfos = methodInfo.GetParameters();

        var luaParameter = Expression.Parameter(typeof(Lua), "lua");
        var argumentsParameter = Expression.Parameter(typeof(LuaStackValueRange), "arguments");

        var arguments = new Expression[parameterInfos.Length];
        for (var i = 0; i < parameterInfos.Length; i++)
        {
            var parameterInfo = parameterInfos[i];
            var parameterType = parameterInfo.ParameterType;
            var argumentIndex = Expression.Constant(i + 1);

            if (i == parameterInfos.Length - 1 && (parameterInfo.GetCustomAttribute<ParamArrayAttribute>() != null || parameterInfo.ParameterType == typeof(LuaStackValueRange)))
            {
                if (parameterInfo.ParameterType.IsArray)
                {
                    var elementType = parameterType.GetElementType()!;
                    var getParamsArgument = _getParamsArgument.MakeGenericMethod(elementType);
                    arguments[i] = Expression.Call(getParamsArgument, luaParameter, argumentsParameter, argumentIndex);
                }
                else
                {
                    arguments[i] = Expression.Call(_getRangeArgument, luaParameter, argumentsParameter, argumentIndex);
                }
            }
            else
            {
                var getArgument = _getArgument.MakeGenericMethod(parameterType);
                arguments[i] = Expression.Call(getArgument, luaParameter, argumentsParameter, argumentIndex);
            }

            var argumentVariable = Expression.Variable(arguments[i].Type, parameterInfo.Name);
            arguments[i] = Expression.Block(new[] { argumentVariable }, Expression.Assign(argumentVariable, arguments[i]));
        }

        Expression call = Expression.Call(methodInfo.IsStatic ? null : Expression.Constant(instance), methodInfo, arguments);

        var disposeArgumentExpressions = new List<Expression>();
        foreach (var argument in arguments)
        {
            if (argument.Type.IsValueType)
                continue;

            if (argument.Type != typeof(object)
                && !argument.Type.IsAssignableTo(typeof(IDisposable)))
                continue;

            var variable = (argument as BlockExpression)!.Variables[0];
            Expression disposeArgumentExpression = Expression.Call(variable, "Dispose", Type.EmptyTypes);
            if (!argument.Type.IsAssignableTo(typeof(LuaReference)))
            {
                disposeArgumentExpression = Expression.IfThen(
                    Expression.TypeIs(variable, typeof(LuaReference)), disposeArgumentExpression);
            }

            disposeArgumentExpressions.Add(disposeArgumentExpression);
        }

        if (disposeArgumentExpressions.Count > 0)
        {
            call = Expression.TryFinally(call,
                Expression.Block(arguments.SelectMany(argument => (argument as BlockExpression)!.Variables), disposeArgumentExpressions));
        }

        Expression returnCount;
        if (methodInfo.ReturnType != typeof(void))
        {
            var pushMethod = _pushMethod.MakeGenericMethod(methodInfo.ReturnType);
            var luaStack = Expression.Property(luaParameter, nameof(Lua.Stack));
            call = Expression.Call(luaStack, pushMethod, call);
            returnCount = Expression.Constant(1, typeof(int));
        }
        else
        {
            returnCount = Expression.Constant(0, typeof(int));
        }

        var returnTarget = Expression.Label(typeof(int));
        var returnLabel = Expression.Label(returnTarget, returnCount);

        var block = Expression.Block(call, returnCount);

        var lambda = Expression.Lambda<Func<Lua, LuaStackValueRange, int>>(block, luaParameter, argumentsParameter);
        return lambda.Compile();
    }
}
