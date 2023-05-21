using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Qommon;

namespace Laylua.Marshalling;

public static class UserDataDescriptorUtilities
{
    private static readonly MethodInfo _pushMethod;

    private static readonly ConstructorInfo _luaStackValueRangeConstructor;
    private static readonly PropertyInfo _luaStackValueRangeGetItemProperty;

    private static readonly MethodInfo _getParamsArgument;

    private static readonly MethodInfo _luaReferenceDisposeSingle;
    private static readonly MethodInfo _luaReferenceDisposeArray;
    private static readonly MethodInfo _luaReferenceDisposeEnumerable;
    private static readonly MethodInfo _luaReferenceDisposeDictionary;
    private static readonly MethodInfo _luaReferenceDisposeKvpEnumerable;

    static UserDataDescriptorUtilities()
    {
        Guard.IsTrue(RuntimeFeature.IsDynamicCodeSupported);

        var pushMethod = typeof(LuaStack).GetMethod(nameof(LuaStack.Push));
        Guard.IsNotNull(pushMethod);
        _pushMethod = pushMethod;

        var luaStackValueRangeConstructor = Array.Find(typeof(LuaStackValueRange).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic), constructorInfo => constructorInfo.GetParameters().Length == 3);
        Guard.IsNotNull(luaStackValueRangeConstructor);
        _luaStackValueRangeConstructor = luaStackValueRangeConstructor;

        var luaStackValueRangeGetItemProperty = Array.Find(typeof(LuaStackValueRange).GetProperties(), propertyInfo => propertyInfo.GetIndexParameters().Length == 1);
        Guard.IsNotNull(luaStackValueRangeGetItemProperty);
        _luaStackValueRangeGetItemProperty = luaStackValueRangeGetItemProperty;

        var getParamsArgument = typeof(UserDataDescriptorUtilities).GetMethod(nameof(GetParamsArgument), BindingFlags.Static | BindingFlags.NonPublic);
        Guard.IsNotNull(getParamsArgument);
        _getParamsArgument = getParamsArgument;

        MethodInfo? luaReferenceDisposeSingle = null;
        MethodInfo? luaReferenceDisposeArray = null;
        MethodInfo? luaReferenceDisposeEnumerable = null;
        MethodInfo? luaReferenceDisposeDictionary = null;
        MethodInfo? luaReferenceDisposeKvpEnumerable = null;
        var luaReferenceMethods = typeof(LuaReference).GetMethods(BindingFlags.Static | BindingFlags.Public);
        foreach (var luaReferenceMethod in luaReferenceMethods)
        {
            if (luaReferenceMethod.Name != nameof(LuaReference.Dispose))
                continue;

            var parameters = luaReferenceMethod.GetParameters();
            if (parameters.Length != 1)
                continue;

            if (!luaReferenceMethod.IsGenericMethod)
                continue;

            var genericParameterCount = luaReferenceMethod.GetGenericArguments().Length;
            var parameter = parameters[0];
            if (genericParameterCount == 1)
            {
                if (parameter.ParameterType.IsGenericMethodParameter)
                {
                    Guard.IsNull(luaReferenceDisposeSingle);
                    luaReferenceDisposeSingle = luaReferenceMethod;
                    continue;
                }

                if (parameter.ParameterType.IsArray && parameter.ParameterType.GetElementType()!.IsGenericMethodParameter)
                {
                    Guard.IsNull(luaReferenceDisposeArray);
                    luaReferenceDisposeArray = luaReferenceMethod;
                    continue;
                }

                if (parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    && parameter.ParameterType.GetGenericArguments()[0].IsGenericMethodParameter)
                {
                    Guard.IsNull(luaReferenceDisposeEnumerable);
                    luaReferenceDisposeEnumerable = luaReferenceMethod;
                    continue;
                }
            }
            else if (genericParameterCount == 2)
            {
                if (parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    Guard.IsNull(luaReferenceDisposeDictionary);
                    luaReferenceDisposeDictionary = luaReferenceMethod;
                    continue;
                }

                if (parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    Guard.IsNull(luaReferenceDisposeKvpEnumerable);
                    luaReferenceDisposeKvpEnumerable = luaReferenceMethod;
                    continue;
                }
            }
        }

        Guard.IsNotNull(luaReferenceDisposeSingle);
        Guard.IsNotNull(luaReferenceDisposeArray);
        Guard.IsNotNull(luaReferenceDisposeEnumerable);
        Guard.IsNotNull(luaReferenceDisposeDictionary);
        Guard.IsNotNull(luaReferenceDisposeKvpEnumerable);
        _luaReferenceDisposeSingle = luaReferenceDisposeSingle;
        _luaReferenceDisposeArray = luaReferenceDisposeArray;
        _luaReferenceDisposeEnumerable = luaReferenceDisposeEnumerable;
        _luaReferenceDisposeDictionary = luaReferenceDisposeDictionary;
        _luaReferenceDisposeKvpEnumerable = luaReferenceDisposeKvpEnumerable;
    }

    private static T?[] GetParamsArgument<T>(Lua lua, LuaStackValueRange arguments, int argumentIndex)
    {
        var remainingArgumentCount = arguments.Count - (argumentIndex - 1);
        if (remainingArgumentCount == 0)
            return Array.Empty<T>();

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

        var luaParameterExpression = Expression.Parameter(typeof(Lua), "lua");
        var argumentsParameterExpression = Expression.Parameter(typeof(LuaStackValueRange), "arguments");
        var requiredParameterCount = 0;
        for (var i = 0; i < parameterInfos.Length; i++)
        {
            var parameterInfo = parameterInfos[i];
            if (parameterInfo.IsOptional)
                break;

            if (i == parameterInfos.Length - 1 && (parameterInfo.ParameterType.IsArray && parameterInfo.GetCustomAttribute<ParamArrayAttribute>() != null
                || parameterInfo.ParameterType == typeof(LuaStackValueRange)))
                break;

            requiredParameterCount++;
        }

        var bodyExpressions = new List<Expression>();

        // T1 arg1
        // T2 arg2
        // T3 arg3
        var argumentVariableExpressions = new ParameterExpression[parameterInfos.Length];
        for (var i = 0; i < parameterInfos.Length; i++)
        {
            argumentVariableExpressions[i] = Expression.Variable(parameterInfos[i].ParameterType, parameterInfos[i].Name);
        }

        // if (arguments.Count < requiredParameterCount)
        //     lua.RaiseArgumentError(requiredParameterCount - arguments.Count, "Missing argument.");
        bodyExpressions.Add(Expression.IfThen(
            Expression.LessThan(
                Expression.Property(argumentsParameterExpression, nameof(LuaStackValueRange.Count)),
                Expression.Constant(requiredParameterCount)),
            Expression.Call(luaParameterExpression, nameof(Lua.RaiseArgumentError), null,
                Expression.Subtract(
                    Expression.Constant(requiredParameterCount),
                    Expression.Property(argumentsParameterExpression, nameof(LuaStackValueRange.Count))),
                Expression.Constant("Missing argument."))));

        for (var i = 0; i < argumentVariableExpressions.Length; i++)
        {
            var parameterInfo = parameterInfos[i];
            var parameterType = parameterInfo.ParameterType;
            var argumentVariableExpression = argumentVariableExpressions[i];
            var argumentIndexExpression = Expression.Constant(i + 1);

            if (i != parameterInfos.Length - 1 || ((!parameterInfo.ParameterType.IsArray || parameterInfo.GetCustomAttribute<ParamArrayAttribute>() == null)
                && parameterInfo.ParameterType != typeof(LuaStackValueRange)))
            {
                /*
                    if (!arguments[1].TryGetValue<T1>(out arg1))
                    {
                        lua.RaiseArgumentTypeError(1, typeof(T1).Name);
                    }
                */
                var getArgumentExpression = Expression.IfThen(
                    Expression.IsFalse(
                        Expression.Call(
                            Expression.Property(argumentsParameterExpression, _luaStackValueRangeGetItemProperty, argumentIndexExpression),
                            nameof(LuaStackValue.TryGetValue), new[] { parameterType },
                            argumentVariableExpression)),
                    Expression.Call(
                        luaParameterExpression,
                        nameof(Lua.RaiseArgumentTypeError), Type.EmptyTypes,
                        Expression.Constant(i + 2), Expression.Constant(parameterType.Name)));

                if (i >= requiredParameterCount)
                {
                    // If the parameter is optional the logic code above becomes optional.
                    /*
                        if (arguments.Count < 3)
                        {
                            arg3 = defaultValue;
                        }
                        else
                        {
                            // Code from above.
                        }
                    */
                    getArgumentExpression = Expression.IfThenElse(
                        Expression.LessThan(
                            Expression.Property(argumentsParameterExpression, nameof(LuaStackValueRange.Count)),
                            argumentIndexExpression),
                        Expression.Assign(argumentVariableExpression, parameterType.IsArray && parameterType.GetCustomAttribute<ParamArrayAttribute>() != null
                            ? Expression.Call(typeof(Array), "Empty", new[] { parameterType.GetElementType()! })
                            : Expression.Constant(parameterInfo.DefaultValue)),
                        getArgumentExpression);
                }

                bodyExpressions.Add(getArgumentExpression);
            }
            else
            {
                if (parameterInfo.ParameterType.IsArray)
                {
                    var elementType = parameterType.GetElementType()!;
                    var getParamsArgument = _getParamsArgument.MakeGenericMethod(elementType);
                    var getArgumentExpression = Expression.Assign(argumentVariableExpression,
                        Expression.Call(getParamsArgument, luaParameterExpression, argumentsParameterExpression, argumentIndexExpression));

                    bodyExpressions.Add(getArgumentExpression);
                }
                else
                {
                    // arg1 = new LuaStackValueRange(lua.Stack, argumentIndex, arguments.Count - (argumentIndex - 1))
                    var getArgumentExpression = Expression.Assign(argumentVariableExpression,
                        Expression.New(_luaStackValueRangeConstructor,
                            Expression.Property(luaParameterExpression, nameof(Lua.Stack)),
                            argumentIndexExpression,
                            Expression.Subtract(
                                Expression.Property(argumentsParameterExpression, nameof(LuaStackValueRange.Count)),
                                Expression.Subtract(
                                    argumentIndexExpression,
                                    Expression.Constant(1)))));

                    bodyExpressions.Add(getArgumentExpression);
                }
            }
        }

        // @delegate(arg1, arg2, arg3);
        var callDelegateExpression = Expression.Call(methodInfo.IsStatic ? null : Expression.Constant(instance), methodInfo, argumentVariableExpressions);

        Expression returnCountExpression;
        if (methodInfo.ReturnType != typeof(void))
        {
            // lua.Stack.Push(@delegate(...));
            var pushMethod = _pushMethod.MakeGenericMethod(methodInfo.ReturnType);
            var luaStack = Expression.Property(luaParameterExpression, nameof(Lua.Stack));
            callDelegateExpression = Expression.Call(luaStack, pushMethod, callDelegateExpression);
            returnCountExpression = Expression.Constant(1, typeof(int));
        }
        else
        {
            returnCountExpression = Expression.Constant(0, typeof(int));
        }

        var disposeArgumentExpressions = new List<Expression>();
        foreach (var argumentVariableExpression in argumentVariableExpressions)
        {
            var argumentType = argumentVariableExpression.Type;
            if (argumentType.IsValueType)
                continue;

            Expression disposeArgumentExpression;
            if (argumentType == typeof(object) || argumentType.IsAssignableTo(typeof(LuaReference)))
            {
                // LuaReference.Dispose<T1>(arg1);
                disposeArgumentExpression = Expression.Call(null, _luaReferenceDisposeSingle.MakeGenericMethod(argumentType), argumentVariableExpression);
            }

            // Note: the code above takes care of any singular LuaReference arguments.
            // The code below handles collections of references, with a depth of 1.
            // There's a lot of code here, but it's all just choosing the appropriate LuaReference.Dispose() overload
            // Currently, this is only used for the params argument.
            // In the future, if marshaling from LuaTable to .NET collections is made possible,
            // this will handle those parameters as well.
            else if (argumentType.IsArray || argumentType.IsAssignableTo(typeof(IEnumerable)))
            {
                Type? elementType = null;
                if (argumentType.IsArray)
                {
                    elementType = argumentType.GetElementType()!;
                }
                else
                {
                    if (argumentType.IsGenericType && argumentType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        elementType = argumentType.GenericTypeArguments[0];
                    }
                    else
                    {
                        var interfaces = argumentType.GetInterfaces();
                        foreach (var @interface in interfaces)
                        {
                            if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                            {
                                elementType = @interface.GenericTypeArguments[0];
                                break;
                            }
                        }
                    }
                }

                if (elementType == null)
                    continue;

                if (elementType.IsValueType && elementType.IsGenericType)
                {
                    var genericTypeDefinition = elementType.GetGenericTypeDefinition();
                    if (genericTypeDefinition != typeof(KeyValuePair<,>))
                        continue;

                    var genericTypeArguments = elementType.GetGenericArguments();
                    if (genericTypeArguments[0] != typeof(object) && !genericTypeArguments[0].IsAssignableTo(typeof(LuaReference))
                        && genericTypeArguments[1] != typeof(object) && !genericTypeArguments[1].IsAssignableTo(typeof(LuaReference)))
                        continue;

                    var method = argumentType.IsGenericType && argumentType.GetGenericTypeDefinition() == typeof(Dictionary<,>)
                        ? _luaReferenceDisposeDictionary
                        : _luaReferenceDisposeKvpEnumerable;

                    // LuaReference.Dispose<TKey, TValue>(arg1);
                    disposeArgumentExpression = Expression.Call(null, method.MakeGenericMethod(genericTypeArguments[0], genericTypeArguments[1]), argumentVariableExpression);
                }
                else
                {
                    if (elementType.IsValueType)
                        continue;

                    if (elementType != typeof(object) && !elementType.IsAssignableTo(typeof(LuaReference)))
                        continue;

                    var method = argumentType.IsArray
                        ? _luaReferenceDisposeArray
                        : _luaReferenceDisposeEnumerable;

                    // LuaReference.Dispose<T1>(arg1);
                    disposeArgumentExpression = Expression.Call(null, method.MakeGenericMethod(elementType), argumentVariableExpression);
                }
            }
            else
            {
                continue;
            }

            disposeArgumentExpressions.Add(disposeArgumentExpression);
        }

        bodyExpressions.Add(callDelegateExpression);
        bodyExpressions.Add(returnCountExpression);

        Expression bodyExpression = Expression.Block(argumentVariableExpressions, bodyExpressions);

        if (disposeArgumentExpressions.Count > 0)
        {
            bodyExpression = Expression.TryFinally(bodyExpression, Expression.Block(argumentVariableExpressions, disposeArgumentExpressions));
        }

        var lambdaExpression = Expression.Lambda<Func<Lua, LuaStackValueRange, int>>(bodyExpression, luaParameterExpression, argumentsParameterExpression);
        return lambdaExpression.Compile();
    }
}
