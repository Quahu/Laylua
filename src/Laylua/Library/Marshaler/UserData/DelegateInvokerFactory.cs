using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Qommon;

namespace Laylua.Marshaling;

/// <summary>
///     Builds invokers for CLR delegates exposed to Lua.
///     Uses compiled expression trees when JIT is available, falls back to reflection-based invocation for NativeAOT.
/// </summary>
internal static class DelegateInvokerFactory
{
    public delegate int MethodInvokerDelegate(LuaThread thread, LuaStackValueRange arguments);

    private static readonly MethodInfo PushMethod;
    private static readonly ConstructorInfo LuaStackValueRangeConstructor;
    private static readonly PropertyInfo LuaStackValueRangeCountProperty;
    private static readonly MethodInfo LuaStackValueRangePushValuesMethod;
    private static readonly PropertyInfo LuaFunctionResultsRangeProperty;
    private static readonly MethodInfo LuaMarshalerTryToObjectMethod;
    private static readonly MethodInfo GetParamsArgumentMethod;

    private static readonly MethodInfo LuaReferenceDisposeSingle;
    private static readonly MethodInfo LuaReferenceDisposeArray;
    private static readonly MethodInfo LuaReferenceDisposeEnumerable;
    private static readonly MethodInfo LuaReferenceDisposeDictionary;
    private static readonly MethodInfo LuaReferenceDisposeKvpEnumerable;

    static DelegateInvokerFactory()
    {
        var pushMethod = typeof(LuaStack).GetMethod(nameof(LuaStack.Push));
        Guard.IsNotNull(pushMethod);
        PushMethod = pushMethod;

        var luaStackValueRangeConstructor = Array.Find(
            typeof(LuaStackValueRange).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic), static c => c.GetParameters().Length == 3);

        Guard.IsNotNull(luaStackValueRangeConstructor);
        LuaStackValueRangeConstructor = luaStackValueRangeConstructor;

        var luaStackValueRangeCountProperty = typeof(LuaStackValueRange).GetProperty(nameof(LuaStackValueRange.Count));
        Guard.IsNotNull(luaStackValueRangeCountProperty);
        LuaStackValueRangeCountProperty = luaStackValueRangeCountProperty;

        var luaStackValueRangePushValuesMethod = typeof(LuaStackValueRange).GetMethod(nameof(LuaStackValueRange.PushValues));
        Guard.IsNotNull(luaStackValueRangePushValuesMethod);
        LuaStackValueRangePushValuesMethod = luaStackValueRangePushValuesMethod;

        var luaFunctionResultsRangeProperty = typeof(LuaFunctionResults).GetProperty(nameof(LuaFunctionResults.Range));
        Guard.IsNotNull(luaFunctionResultsRangeProperty);
        LuaFunctionResultsRangeProperty = luaFunctionResultsRangeProperty;

        var luaMarshalerTryToObjectMethod = Array.Find(
            typeof(LuaMarshaler).GetMethods(BindingFlags.Instance | BindingFlags.Public),
            static method =>
            {
                if (method.Name != nameof(LuaMarshaler.TryGetValue) || !method.IsGenericMethodDefinition)
                {
                    return false;
                }

                var parameters = method.GetParameters();
                return parameters.Length == 3
                    && parameters[0].ParameterType == typeof(LuaThread)
                    && parameters[1].ParameterType == typeof(int)
                    && parameters[2].IsOut;
            });
        Guard.IsNotNull(luaMarshalerTryToObjectMethod);
        LuaMarshalerTryToObjectMethod = luaMarshalerTryToObjectMethod;

        var getParamsArgumentMethod = typeof(DelegateInvokerFactory).GetMethod(nameof(GetParamsArgument), BindingFlags.Static | BindingFlags.NonPublic);
        Guard.IsNotNull(getParamsArgumentMethod);
        GetParamsArgumentMethod = getParamsArgumentMethod;

        MethodInfo? luaReferenceDisposeSingle = null;
        MethodInfo? luaReferenceDisposeArray = null;
        MethodInfo? luaReferenceDisposeEnumerable = null;
        MethodInfo? luaReferenceDisposeDictionary = null;
        MethodInfo? luaReferenceDisposeKvpEnumerable = null;

        foreach (var method in typeof(LuaReference).GetMethods(BindingFlags.Static | BindingFlags.Public))
        {
            if (method.Name != "Dispose")
                continue;

            var parameters = method.GetParameters();
            if (parameters.Length != 1 || !method.IsGenericMethod)
                continue;

            var genericParameterCount = method.GetGenericArguments().Length;
            var parameter = parameters[0];
            if (genericParameterCount == 1)
            {
                if (parameter.ParameterType.IsGenericMethodParameter)
                {
                    luaReferenceDisposeSingle = method;
                    continue;
                }

                if (parameter.ParameterType.IsArray && parameter.ParameterType.GetElementType()!.IsGenericMethodParameter)
                {
                    luaReferenceDisposeArray = method;
                    continue;
                }

                if (parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    && parameter.ParameterType.GetGenericArguments()[0].IsGenericMethodParameter)
                {
                    luaReferenceDisposeEnumerable = method;
                }
            }
            else if (genericParameterCount == 2)
            {
                if (parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    luaReferenceDisposeDictionary = method;
                    continue;
                }

                if (parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    luaReferenceDisposeKvpEnumerable = method;
                }
            }
        }

        Guard.IsNotNull(luaReferenceDisposeSingle);
        Guard.IsNotNull(luaReferenceDisposeArray);
        Guard.IsNotNull(luaReferenceDisposeEnumerable);
        Guard.IsNotNull(luaReferenceDisposeDictionary);
        Guard.IsNotNull(luaReferenceDisposeKvpEnumerable);
        LuaReferenceDisposeSingle = luaReferenceDisposeSingle;
        LuaReferenceDisposeArray = luaReferenceDisposeArray;
        LuaReferenceDisposeEnumerable = luaReferenceDisposeEnumerable;
        LuaReferenceDisposeDictionary = luaReferenceDisposeDictionary;
        LuaReferenceDisposeKvpEnumerable = luaReferenceDisposeKvpEnumerable;
    }

    private static T?[] GetParamsArgument<T>(LuaThread thread, int argumentCount, int argumentIndex)
    {
        var remainingArgumentCount = argumentCount - (argumentIndex - 1);
        if (remainingArgumentCount == 0)
            return [];

        var paramsArgument = new T?[remainingArgumentCount];
        for (var i = 0; i < remainingArgumentCount; i++)
        {
            if (!thread.Stack[argumentIndex + i].TryGetValue<T>(out var convertedArgument))
            {
                thread.RaiseArgumentTypeError(argumentIndex + i, typeof(T).Name);
            }

            paramsArgument[i] = convertedArgument;
        }

        return paramsArgument;
    }

    private static void RethrowIfNotNull(Exception? exception)
    {
        if (exception != null)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    /// <summary>
    ///     Creates an invoker for the given delegate.
    ///     Uses compiled expression trees when JIT is available, falls back to reflection otherwise (e.g., NativeAOT).
    /// </summary>
    public static MethodInvokerDelegate CreateInvoker(Delegate @delegate)
    {
        var method = @delegate.Method;
        var parameters = method.GetParameters();

        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType.IsByRef)
            {
                throw new NotSupportedException(
                    $"Delegate parameter '{parameters[i].Name}' of '{method.Name}' is a by-reference parameter ({parameters[i].ParameterType.Name}). " +
                    "Delegates with ref, out, or in parameters cannot be marshaled to Lua.");
            }
        }

        if (method.ReturnType.IsByRef || method.ReturnType.IsByRefLike || method.ReturnType.IsPointer)
        {
            throw new NotSupportedException(
                $"Delegate return type '{method.ReturnType.Name}' of '{method.Name}' cannot be marshaled to Lua.");
        }

        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            return CreateExpressionInvoker(@delegate, @delegate.Method);
        }

        return CreateReflectionInvoker(@delegate);
    }

    private static MethodInvokerDelegate CreateReflectionInvoker(Delegate @delegate)
    {
        var method = @delegate.Method;
        var parameters = method.GetParameters();
        var paramCount = parameters.Length;

        // Pre-compute per-parameter metadata
        var tryGetMethods = new MethodInfo[paramCount];
        var paramKinds = new byte[paramCount]; // 0 = normal, 1 = params array, 2 = LuaStackValueRange
        var isLuaRef = new bool[paramCount];
        var isLuaRefArray = new bool[paramCount];

        for (var i = 0; i < paramCount; i++)
        {
            var param = parameters[i];
            var paramType = param.ParameterType;

            if (i == paramCount - 1 && paramType == typeof(LuaStackValueRange))
            {
                paramKinds[i] = 2;
            }
            else if (i == paramCount - 1 && paramType.IsArray && param.GetCustomAttribute<ParamArrayAttribute>() != null)
            {
                paramKinds[i] = 1;
                tryGetMethods[i] = GetParamsArgumentMethod.MakeGenericMethod(paramType.GetElementType()!);
            }
            else
            {
                tryGetMethods[i] = LuaMarshalerTryToObjectMethod.MakeGenericMethod(paramType);
            }

            if (!paramType.IsValueType)
            {
                if (paramType.IsAssignableTo(typeof(LuaReference)))
                {
                    isLuaRef[i] = true;
                }
                else if (paramType.IsArray && paramType.GetElementType()!.IsAssignableTo(typeof(LuaReference)))
                {
                    isLuaRefArray[i] = true;
                }
            }
        }

        var returnType = method.ReturnType;
        var pushReturnMethod = returnType != typeof(void)
            && returnType != typeof(LuaFunctionResults)
            && returnType != typeof(LuaStackValueRange)
                ? PushMethod.MakeGenericMethod(returnType)
                : null;

        return (thread, arguments) =>
        {
            var args = new object?[paramCount];
            Exception? invocationException = null;
            try
            {
                for (var i = 0; i < paramCount; i++)
                {
                    switch (paramKinds[i])
                    {
                        case 2: // LuaStackValueRange
                        {
                            args[i] = LuaStackValueRangeConstructor.Invoke([
                                thread.Stack,
                                i + 1,
                                arguments.Count - i
                            ]);

                            break;
                        }
                        case 1: // params array
                        {
                            args[i] = tryGetMethods[i].Invoke(null, [thread, arguments.Count, i + 1]);
                            break;
                        }
                        default: // normal parameter
                        {
                            if (arguments.Count < i + 1)
                            {
                                if (parameters[i].IsOptional)
                                {
                                    args[i] = parameters[i].DefaultValue;
                                }
                                else
                                {
                                    thread.RaiseArgumentTypeError(i + 1, parameters[i].ParameterType.ToTypeString());
                                }
                            }
                            else
                            {
                                var outArgs = new object?[] { thread, i + 1, null };
                                if (!(bool) tryGetMethods[i].Invoke(thread.Marshaler, outArgs)!)
                                {
                                    thread.RaiseArgumentTypeError(i + 1, parameters[i].ParameterType.ToTypeString());
                                }

                                args[i] = outArgs[2];
                            }

                            break;
                        }
                    }
                }

                var result = @delegate.DynamicInvoke(args);
                if (returnType == typeof(void))
                {
                    return 0;
                }

                if (returnType == typeof(LuaFunctionResults))
                {
                    var range = ((LuaFunctionResults) result!).Range;
                    range.PushValues();
                    return range.Count;
                }

                if (returnType == typeof(LuaStackValueRange))
                {
                    var range = (LuaStackValueRange) result!;
                    range.PushValues();
                    return range.Count;
                }

                pushReturnMethod!.Invoke(thread.Stack, [result]);
                return 1;
            }
            catch (Exception ex)
            {
                invocationException = ex;
                throw;
            }
            finally
            {
                Exception? disposeException = null;
                for (var i = 0; i < paramCount; i++)
                {
                    if (args[i] == null)
                    {
                        continue;
                    }

                    if (isLuaRef[i])
                    {
                        try
                        {
                            ((LuaReference) args[i]!).Dispose();
                        }
                        catch (Exception ex)
                        {
                            disposeException ??= ex;
                        }
                    }
                    else if (isLuaRefArray[i])
                    {
                        foreach (var element in (Array) args[i]!)
                        {
                            if (element is not LuaReference reference)
                            {
                                continue;
                            }

                            try
                            {
                                reference.Dispose();
                            }
                            catch (Exception ex)
                            {
                                disposeException ??= ex;
                            }
                        }
                    }
                    else if (args[i] is LuaReference untypedRef)
                    {
                        // Catch LuaReferences received through loosely-typed parameters (e.g., object)
                        try
                        {
                            untypedRef.Dispose();
                        }
                        catch (Exception ex)
                        {
                            disposeException ??= ex;
                        }
                    }
                }

                if (disposeException != null && invocationException == null)
                {
                    ExceptionDispatchInfo.Capture(disposeException).Throw();
                }
            }
        };
    }

    private static MethodInvokerDelegate CreateExpressionInvoker(Delegate @delegate, MethodInfo methodInfo)
    {
        Guard.IsNotNull(methodInfo);

        var parameters = methodInfo.GetParameters();
        var luaParameterExpression = Expression.Parameter(typeof(LuaThread), "lua");
        var argumentsParameterExpression = Expression.Parameter(typeof(LuaStackValueRange), "arguments");
        var requiredParameterCount = 0;
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            if (parameter.IsOptional)
                break;

            if (i == parameters.Length - 1 && (parameter.ParameterType.IsArray && parameter.GetCustomAttribute<ParamArrayAttribute>() != null
                || parameter.ParameterType == typeof(LuaStackValueRange)))
                break;

            requiredParameterCount++;
        }

        var bodyExpressions = new List<Expression>();
        var variableExpressions = new List<ParameterExpression>();

        var argumentVariableExpressions = new ParameterExpression[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            argumentVariableExpressions[i] = Expression.Variable(parameters[i].ParameterType, parameters[i].Name);
        }

        variableExpressions.AddRange(argumentVariableExpressions);

        // Invoke through the delegate itself (not Method/Target) to support multicast delegates
        var delegateConstant = Expression.Constant(@delegate);
        var invokeMethod = @delegate.GetType().GetMethod("Invoke")!;
        var callDelegateExpression = Expression.Call(delegateConstant, invokeMethod, argumentVariableExpressions);

        for (var i = 0; i < argumentVariableExpressions.Length; i++)
        {
            var parameter = parameters[i];
            var parameterType = parameter.ParameterType;
            var argumentVariableExpression = argumentVariableExpressions[i];
            var argumentIndexExpression = Expression.Constant(i + 1);

            if (i != parameters.Length - 1 || ((!parameter.ParameterType.IsArray || parameter.GetCustomAttribute<ParamArrayAttribute>() == null)
                && parameter.ParameterType != typeof(LuaStackValueRange)))
            {
                var getArgumentExpression = Expression.IfThen(
                    Expression.IsFalse(
                        Expression.Call(
                            Expression.Property(luaParameterExpression, nameof(Lua.Marshaler)),
                            LuaMarshalerTryToObjectMethod.MakeGenericMethod(parameterType),
                            luaParameterExpression, argumentIndexExpression, argumentVariableExpression)),
                    Expression.Call(
                        luaParameterExpression,
                        nameof(Lua.RaiseArgumentTypeError), Type.EmptyTypes,
                        argumentIndexExpression, Expression.Constant(parameterType.ToTypeString())));

                if (i >= requiredParameterCount)
                {
                    getArgumentExpression = Expression.IfThenElse(
                        Expression.LessThan(
                            Expression.Property(argumentsParameterExpression, nameof(LuaStackValueRange.Count)),
                            argumentIndexExpression),
                        Expression.Assign(argumentVariableExpression, parameterType.IsArray && parameterType.GetCustomAttribute<ParamArrayAttribute>() != null
                            ? Expression.Call(typeof(Array), "Empty", [parameterType.GetElementType()!])
                            : Expression.Constant(parameter.DefaultValue, parameterType)),
                        getArgumentExpression);
                }

                bodyExpressions.Add(getArgumentExpression);
            }
            else
            {
                if (parameter.ParameterType.IsArray)
                {
                    var elementType = parameterType.GetElementType()!;
                    var getParamsArgument = GetParamsArgumentMethod.MakeGenericMethod(elementType);
                    var getArgumentExpression = Expression.Assign(argumentVariableExpression,
                        Expression.Call(getParamsArgument, luaParameterExpression, Expression.Property(argumentsParameterExpression, "Count"), argumentIndexExpression));

                    bodyExpressions.Add(getArgumentExpression);
                }
                else
                {
                    var getArgumentExpression = Expression.Assign(argumentVariableExpression,
                        Expression.New(LuaStackValueRangeConstructor,
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

        Expression returnCountExpression;
        if (methodInfo.ReturnType != typeof(void))
        {
            if (methodInfo.ReturnType == typeof(LuaStackValueRange)
                || methodInfo.ReturnType == typeof(LuaFunctionResults))
            {
                var returnValueVariableExpression = Expression.Variable(typeof(LuaStackValueRange), "returnValue");
                variableExpressions.Add(returnValueVariableExpression);

                callDelegateExpression = Expression.Call(
                    Expression.Assign(returnValueVariableExpression, methodInfo.ReturnType == typeof(LuaStackValueRange)
                        ? callDelegateExpression
                        : Expression.Property(callDelegateExpression, LuaFunctionResultsRangeProperty)),
                    LuaStackValueRangePushValuesMethod);

                returnCountExpression = Expression.Property(returnValueVariableExpression, LuaStackValueRangeCountProperty);
            }
            else
            {
                var pushMethod = PushMethod.MakeGenericMethod(methodInfo.ReturnType);
                var luaStack = Expression.Property(luaParameterExpression, nameof(Lua.Stack));
                callDelegateExpression = Expression.Call(luaStack, pushMethod, callDelegateExpression);
                returnCountExpression = Expression.Constant(1, typeof(int));
            }
        }
        else
        {
            returnCountExpression = Expression.Constant(0, typeof(int));
        }

        var disposeExceptionVariableExpression = Expression.Variable(typeof(Exception), "__disposeException");
        var disposeArgumentExpressions = BuildDisposeExpressions(argumentVariableExpressions, disposeExceptionVariableExpression);

        bodyExpressions.Add(callDelegateExpression);
        bodyExpressions.Add(returnCountExpression);

        Expression bodyExpression = Expression.Block(bodyExpressions);

        if (disposeArgumentExpressions.Count > 0)
        {
            var invocationFailedVariableExpression = Expression.Variable(typeof(bool), "__invocationFailed");
            var disposeBlockExpressions = new List<Expression>
            {
                Expression.Assign(disposeExceptionVariableExpression, Expression.Constant(null, typeof(Exception))),
            };

            disposeBlockExpressions.AddRange(disposeArgumentExpressions);
            disposeBlockExpressions.Add(Expression.IfThen(
                Expression.IsFalse(invocationFailedVariableExpression),
                Expression.Call(
                    null,
                    typeof(DelegateInvokerFactory).GetMethod(nameof(RethrowIfNotNull), BindingFlags.Static | BindingFlags.NonPublic)!,
                    disposeExceptionVariableExpression)));

            bodyExpression = Expression.Block(
                [invocationFailedVariableExpression],
                Expression.Assign(invocationFailedVariableExpression, Expression.Constant(false)),
                Expression.TryFinally(
                    Expression.TryCatch(
                        bodyExpression,
                        Expression.Catch(
                            typeof(Exception),
                            Expression.Block(
                                Expression.Assign(invocationFailedVariableExpression, Expression.Constant(true)),
                                Expression.Rethrow(bodyExpression.Type)))),
                    Expression.Block([disposeExceptionVariableExpression], disposeBlockExpressions)));
        }

        bodyExpression = Expression.Block(variableExpressions, bodyExpression);

        var lambdaExpression = Expression.Lambda<MethodInvokerDelegate>(bodyExpression, luaParameterExpression, argumentsParameterExpression);
        return lambdaExpression.Compile();
    }

    private static List<Expression> BuildDisposeExpressions(
        ParameterExpression[] argumentVariableExpressions,
        ParameterExpression disposeExceptionVariableExpression)
    {
        var disposeExpressions = new List<Expression>();
        foreach (var argumentVariableExpression in argumentVariableExpressions)
        {
            var argumentType = argumentVariableExpression.Type;
            if (argumentType.IsValueType)
                continue;

            Expression disposeExpression;
            if (argumentType == typeof(object) || argumentType.IsAssignableTo(typeof(LuaReference)))
            {
                disposeExpression = Expression.Call(null, LuaReferenceDisposeSingle.MakeGenericMethod(argumentType), argumentVariableExpression);
            }
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
                        foreach (var @interface in argumentType.GetInterfaces())
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
                        ? LuaReferenceDisposeDictionary
                        : LuaReferenceDisposeKvpEnumerable;

                    disposeExpression = Expression.Call(null, method.MakeGenericMethod(genericTypeArguments[0], genericTypeArguments[1]), argumentVariableExpression);
                }
                else
                {
                    if (elementType.IsValueType)
                        continue;

                    if (elementType != typeof(object) && !elementType.IsAssignableTo(typeof(LuaReference)))
                        continue;

                    var method = argumentType.IsArray
                        ? LuaReferenceDisposeArray
                        : LuaReferenceDisposeEnumerable;

                    disposeExpression = Expression.Call(null, method.MakeGenericMethod(elementType), argumentVariableExpression);
                }
            }
            else
            {
                continue;
            }

            var disposeCatchException = Expression.Variable(typeof(Exception), "__disposeExceptionCurrent");
            var captureFirstException = Expression.IfThen(
                Expression.Equal(disposeExceptionVariableExpression, Expression.Constant(null, typeof(Exception))),
                Expression.Assign(disposeExceptionVariableExpression, disposeCatchException));

            disposeExpressions.Add(Expression.TryCatch(
                disposeExpression,
                Expression.Catch(disposeCatchException, captureFirstException)));
        }

        return disposeExpressions;
    }
}
