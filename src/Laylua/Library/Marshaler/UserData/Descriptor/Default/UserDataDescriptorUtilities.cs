using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Qommon;

namespace Laylua.Marshalling;

public static class UserDataDescriptorUtilities
{
    // private static readonly MethodInfo _raiseArgumentError;
    // private static readonly MethodInfo _raiseArgumentTypeError;
    // private static readonly MethodInfo _tryGetValueMethod;
    private static readonly MethodInfo _pushMethod;

    // private static readonly MethodInfo _getItemMethod;
    private static readonly MethodInfo _getArgument;
    private static readonly MethodInfo _getRangeArgument;
    private static readonly MethodInfo _getParamsArgument;

    static UserDataDescriptorUtilities()
    {
        // var raiseArgumentError = typeof(Lua).GetMethod(nameof(Lua.RaiseArgumentError), new[] { typeof(int), typeof(string) });
        // var raiseArgumentTypeError = typeof(Lua).GetMethod(nameof(Lua.RaiseArgumentTypeError), new[] { typeof(int), typeof(string) });
        // var tryGetValueMethod = typeof(LuaStackValue).GetMethod(nameof(LuaStackValue.TryGetValue));
        var pushMethod = typeof(LuaStack).GetMethod(nameof(LuaStack.Push));

        // var getItemMethod = typeof(LuaStackValueRange).GetMethod("get_Item");
        var getArgument = typeof(UserDataDescriptorUtilities).GetMethod(nameof(GetArgument), BindingFlags.Static | BindingFlags.NonPublic);
        var getRangeArgument = typeof(UserDataDescriptorUtilities).GetMethod(nameof(GetRangeArgument), BindingFlags.Static | BindingFlags.NonPublic);
        var getParamsArgument = typeof(UserDataDescriptorUtilities).GetMethod(nameof(GetParamsArgument), BindingFlags.Static | BindingFlags.NonPublic);

        Guard.IsTrue(RuntimeFeature.IsDynamicCodeSupported);

        // Guard.IsNotNull(raiseArgumentError);
        // Guard.IsNotNull(raiseArgumentTypeError);
        // Guard.IsNotNull(tryGetValueMethod);
        Guard.IsNotNull(pushMethod);

        // Guard.IsNotNull(getItemMethod);
        Guard.IsNotNull(getArgument);
        Guard.IsNotNull(getRangeArgument);
        Guard.IsNotNull(getParamsArgument);

        // _raiseArgumentError = raiseArgumentError;
        // _raiseArgumentTypeError = raiseArgumentTypeError;
        // _tryGetValueMethod = tryGetValueMethod;
        _pushMethod = pushMethod;

        // _getItemMethod = getItemMethod;
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

            // var countProperty = Expression.Property(argumentsParameter, nameof(LuaStackValueRange.Count));
            // var lessThanIndex = Expression.LessThan(countProperty, argumentIndex);
            // var argumentVariable = Expression.Variable(parameterType, "convertedArgument" + i);
            //
            // Expression ifCountLessThen = !parameterInfo.IsOptional
            //     ? Expression.Call(luaParameter, _raiseArgumentError, argumentIndex, Expression.Constant("Missing argument."))
            //     : Expression.Assign(argumentVariable, Expression.Constant(parameterInfo.DefaultValue));
            //
            // var stackValue = Expression.Call(argumentsParameter, _getItemMethod, argumentIndex);
            // var tryGetValueMethod = _tryGetValueMethod.MakeGenericMethod(parameterType);
            // var tryGetValueResult = Expression.Call(stackValue, tryGetValueMethod, argumentVariable);
            // var tryGetValueFailed = Expression.IsFalse(tryGetValueResult);
            // var raiseArgumentTypeError = Expression.Call(luaParameter, _raiseArgumentTypeError, argumentIndex, Expression.Constant(parameterType.Name));
            // var ifCountLessElse = Expression.IfThen(tryGetValueFailed, raiseArgumentTypeError);
            // var lessThanIf = Expression.IfThenElse(lessThanIndex, ifCountLessThen, ifCountLessElse);
            // var argumentReturnTarget = Expression.Label(parameterType, "label" + i);
            // var argumentReturnExpression = Expression.Return(argumentReturnTarget, argumentVariable, parameterType);
            // var argumentBlock = Expression.Block(parameterType, new[] { luaParameter, argumentsParameter, argumentVariable }, lessThanIf, argumentReturnExpression);

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
        }

        var call = Expression.Call(methodInfo.IsStatic ? null : Expression.Constant(instance), methodInfo, arguments);

        Expression returnCount;
        if (methodInfo.ReturnType != typeof(void))
        {
            var pushMethod = _pushMethod.MakeGenericMethod(methodInfo.ReturnType);
            var luaStack = Expression.Property(luaParameter, nameof(Lua.Stack));
            call = Expression.Call(luaStack, pushMethod, call);
            returnCount = Expression.Constant(1);
        }
        else
        {
            returnCount = Expression.Constant(0);
        }

        var returnTarget = Expression.Label(typeof(int));
        var returnLabel = Expression.Label(returnTarget, returnCount);

        var block = Expression.Block(call, returnLabel);

        var lambda = Expression.Lambda<Func<Lua, LuaStackValueRange, int>>(block, luaParameter, argumentsParameter);
        return lambda.Compile();
    }
}
