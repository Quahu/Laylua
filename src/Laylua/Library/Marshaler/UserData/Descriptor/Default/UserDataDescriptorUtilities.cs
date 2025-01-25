using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Laylua.Moon;
using Qommon;

namespace Laylua.Marshaling;

public static class UserDataDescriptorUtilities
{
    public delegate int MethodInvokerDelegate(LuaThread lua, LuaStackValueRange arguments);

    public delegate int IndexDelegate(LuaThread lua, LuaStackValue userData, LuaStackValue key);

    public delegate int NewIndexDelegate(LuaThread lua, LuaStackValue userData, LuaStackValue key, LuaStackValue value);

    private static readonly MethodInfo _pushMethod;

    private static readonly ConstructorInfo _luaStackValueRangeConstructor;
    private static readonly PropertyInfo _luaStackValueRangeGetItemProperty;
    private static readonly PropertyInfo _luaStackValueRangeCountProperty;
    private static readonly MethodInfo _luaStackValueRangePushValuesMethod;
    private static readonly PropertyInfo _luaFunctionResultsRangeProperty;

    private static readonly MethodInfo _luaMarshalerTryToObjectMethod;

    private static readonly MethodInfo _getParamsArgument;

    private static readonly MethodInfo _luaReferenceDisposeSingle;
    private static readonly MethodInfo _luaReferenceDisposeArray;
    private static readonly MethodInfo _luaReferenceDisposeEnumerable;
    private static readonly MethodInfo _luaReferenceDisposeDictionary;
    private static readonly MethodInfo _luaReferenceDisposeKvpEnumerable;

    private static readonly MethodUserDataDescriptor _methodUserDataDescriptor = new();
    private static readonly OverloadedMethodUserDataDescriptor _overloadedMethodUserDataDescriptor = new();

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

        var luaStackValueRangeCountProperty = typeof(LuaStackValueRange).GetProperty(nameof(LuaStackValueRange.Count), BindingFlags.Instance | BindingFlags.Public);
        Guard.IsNotNull(luaStackValueRangeCountProperty);
        _luaStackValueRangeCountProperty = luaStackValueRangeCountProperty;

        var luaStackValueRangePushValuesMethod = typeof(LuaStackValueRange).GetMethod(nameof(LuaStackValueRange.PushValues), BindingFlags.Instance | BindingFlags.Public);
        Guard.IsNotNull(luaStackValueRangePushValuesMethod);
        _luaStackValueRangePushValuesMethod = luaStackValueRangePushValuesMethod;

        var luaFunctionResultsRangeProperty = typeof(LuaFunctionResults).GetProperty(nameof(LuaFunctionResults.Range), BindingFlags.Instance | BindingFlags.Public);
        Guard.IsNotNull(luaFunctionResultsRangeProperty);
        _luaFunctionResultsRangeProperty = luaFunctionResultsRangeProperty;

        var luaMarshalerTryToObjectMethod = typeof(LuaMarshaler).GetMethod(nameof(LuaMarshaler.TryGetValue), BindingFlags.Instance | BindingFlags.Public);
        Guard.IsNotNull(luaMarshalerTryToObjectMethod);
        _luaMarshalerTryToObjectMethod = luaMarshalerTryToObjectMethod;

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

    private static T?[] GetParamsArgument<T>(LuaThread lua, int argumentCount, int argumentIndex)
    {
        var remainingArgumentCount = argumentCount - (argumentIndex - 1);
        if (remainingArgumentCount == 0)
            return Array.Empty<T>();

        var paramsArgument = new T?[remainingArgumentCount];
        for (var i = 0; i < remainingArgumentCount; i++)
        {
            if (!lua.Marshaler.TryGetValue<T>(lua, argumentIndex + i, out var convertedArgument))
            {
                lua.RaiseArgumentTypeError(argumentIndex + i, typeof(T).Name);
            }

            paramsArgument[i] = convertedArgument;
        }

        return paramsArgument;
    }

    public static MethodInvokerDelegate CreateDelegateInvoker(Delegate @delegate)
    {
        return CreateInvoker(@delegate.Target, @delegate.Method);
    }

    public static MethodInvokerDelegate CreateMethodInvoker(MethodInfo methodInfo)
    {
        return CreateInvoker(null, methodInfo);
    }

    private static MethodInvokerDelegate CreateInvoker(object? instance, MethodInfo methodInfo)
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

        // T1 arg1
        // T2 arg2
        // T3 arg3
        var argumentVariableExpressions = new ParameterExpression[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            argumentVariableExpressions[i] = Expression.Variable(parameters[i].ParameterType, parameters[i].Name);
        }

        variableExpressions.AddRange(argumentVariableExpressions);

        var isInstanceMethodInvoker = false;
        Expression? instanceExpression;
        ParameterExpression? instanceExpressionVariable = null;
        if (methodInfo.IsStatic)
        {
            instanceExpression = null;
        }
        else
        {
            // Instance being null here means this is a colon syntax invoker:
            // obj:method()
            // This means the first argument is 'obj'.
            if (instance == null)
            {
                isInstanceMethodInvoker = true;
                requiredParameterCount++;

                /*
                    if (!Lua.Marshaler.TryToObject<T>(2, out var instance))
                    {
                        lua.RaiseArgumentTypeError(2, typeof(T).Name);
                    }
                */
                var userDataHandleType = typeof(UserDataHandle<>).MakeGenericType(methodInfo.ReflectedType!);
                instanceExpressionVariable = Expression.Variable(userDataHandleType, "instance");
                instanceExpression = Expression.Field(instanceExpressionVariable, "Value");
                bodyExpressions.Add(Expression.IfThen(
                    Expression.IsFalse(
                        Expression.Call(
                            Expression.Property(luaParameterExpression, nameof(Lua.Marshaler)),
                            _luaMarshalerTryToObjectMethod.MakeGenericMethod(userDataHandleType),
                            luaParameterExpression, Expression.Constant(2), instanceExpressionVariable)),
                    Expression.Call(
                        luaParameterExpression,
                        nameof(Lua.RaiseArgumentTypeError), Type.EmptyTypes,
                        Expression.Constant(2), Expression.Constant(methodInfo.ReflectedType!.ToTypeString()))));
            }
            else
            {
                instanceExpression = Expression.Constant(instance);
            }
        }

        if (instanceExpressionVariable != null)
        {
            variableExpressions.Add(instanceExpressionVariable);
        }

        // @delegate(arg1, arg2, arg3);
        var callDelegateExpression = Expression.Call(instanceExpression, methodInfo, argumentVariableExpressions);

        // // if (arguments.Count < requiredParameterCount)
        // //     lua.RaiseArgumentError(requiredParameterCount - arguments.Count, "Missing argument.");
        // bodyExpressions.Insert(0, Expression.IfThen(
        //     Expression.LessThan(
        //         Expression.Property(argumentsParameterExpression, nameof(LuaStackValueRange.Count)),
        //         Expression.Constant(requiredParameterCount)),
        //     Expression.Call(luaParameterExpression, nameof(Lua.RaiseArgumentError), null,
        //         Expression.Subtract(
        //             Expression.Constant(requiredParameterCount),
        //             Expression.Property(argumentsParameterExpression, nameof(LuaStackValueRange.Count))),
        //         Expression.Constant("Missing argument."))));

        for (var i = 0; i < argumentVariableExpressions.Length; i++)
        {
            var parameter = parameters[i];
            var parameterType = parameter.ParameterType;
            var argumentVariableExpression = argumentVariableExpressions[i];
            var argumentIndexExpression = Expression.Constant(i + (isInstanceMethodInvoker ? 3 : 1));

            if (i != parameters.Length - 1 || ((!parameter.ParameterType.IsArray || parameter.GetCustomAttribute<ParamArrayAttribute>() == null)
                && parameter.ParameterType != typeof(LuaStackValueRange)))
            {
                /*
                    if (!Lua.Marshaler.TryToObject<T1>(argIndex1, out arg1))
                    {
                        lua.RaiseArgumentTypeError(argIndex1, typeof(T1).Name);
                    }
                */
                var getArgumentExpression = Expression.IfThen(
                    Expression.IsFalse(
                        Expression.Call(
                            Expression.Property(luaParameterExpression, nameof(Lua.Marshaler)),
                            _luaMarshalerTryToObjectMethod.MakeGenericMethod(parameterType),
                            luaParameterExpression, argumentIndexExpression, argumentVariableExpression)),
                    Expression.Call(
                        luaParameterExpression,
                        nameof(Lua.RaiseArgumentTypeError), Type.EmptyTypes,
                        argumentIndexExpression, Expression.Constant(parameterType.ToTypeString())));

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
                            : Expression.Constant(parameter.DefaultValue)),
                        getArgumentExpression);
                }

                bodyExpressions.Add(getArgumentExpression);
            }
            else
            {
                if (parameter.ParameterType.IsArray)
                {
                    var elementType = parameterType.GetElementType()!;
                    var getParamsArgument = _getParamsArgument.MakeGenericMethod(elementType);
                    var getArgumentExpression = Expression.Assign(argumentVariableExpression,
                        Expression.Call(getParamsArgument, luaParameterExpression, Expression.Property(argumentsParameterExpression, "Count"), argumentIndexExpression));

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

        Expression returnCountExpression;
        if (methodInfo.ReturnType != typeof(void))
        {
            if (methodInfo.ReturnType == typeof(LuaStackValueRange)
                || methodInfo.ReturnType == typeof(LuaFunctionResults))
            {
                // @delegate(...).PushValues();
                var returnValueVariableExpression = Expression.Variable(typeof(LuaStackValueRange), "returnValue");
                variableExpressions.Add(returnValueVariableExpression);

                callDelegateExpression = Expression.Call(
                    Expression.Assign(returnValueVariableExpression, methodInfo.ReturnType == typeof(LuaStackValueRange)
                        ? callDelegateExpression
                        : Expression.Property(callDelegateExpression, _luaFunctionResultsRangeProperty)),
                    _luaStackValueRangePushValuesMethod);

                returnCountExpression = Expression.Property(returnValueVariableExpression, _luaStackValueRangeCountProperty);
            }
            else
            {
                // lua.Stack.Push(@delegate(...));
                var pushMethod = _pushMethod.MakeGenericMethod(methodInfo.ReturnType);
                var luaStack = Expression.Property(luaParameterExpression, nameof(Lua.Stack));
                callDelegateExpression = Expression.Call(luaStack, pushMethod, callDelegateExpression);
                returnCountExpression = Expression.Constant(1, typeof(int));
            }
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

        Expression bodyExpression = Expression.Block(variableExpressions, bodyExpressions);

        if (disposeArgumentExpressions.Count > 0)
        {
            bodyExpression = Expression.TryFinally(bodyExpression, Expression.Block(variableExpressions, disposeArgumentExpressions));
        }

        var lambdaExpression = Expression.Lambda<MethodInvokerDelegate>(bodyExpression, luaParameterExpression, argumentsParameterExpression);
        return lambdaExpression.Compile();
    }

    public static IndexDelegate CreateIndex(Type type, bool isTypeDefinition, TypeMemberProvider memberProvider, UserDataNamingPolicy namingPolicy)
    {
        // Index
        var returnLabelTarget = Expression.Label(typeof(int), "returnLabel");

        var luaParameterExpression = Expression.Parameter(typeof(LuaThread), "lua");
        var userDataParameterExpression = Expression.Parameter(typeof(LuaStackValue), "userData");
        var keyParameterExpression = Expression.Parameter(typeof(LuaStackValue), "key");

        Expression? instanceExpression = isTypeDefinition
            ? null
            : Expression.Field(
                Expression.Call(userDataParameterExpression, nameof(LuaStackValue.GetValue), new[] { typeof(UserDataHandle<>).MakeGenericType(type) }),
                "Value");

        // string name;
        var nameVariableExpression = Expression.Variable(typeof(string), "name");

        // name = key.GetValue<string>();
        var nameVariableAssignExpression = Expression.Assign(
            nameVariableExpression,
            Expression.Call(keyParameterExpression, nameof(LuaStackValue.GetValue), new[] { typeof(string) }));

        // lua.Stack
        var getLuaStackExpression = Expression.Property(luaParameterExpression, nameof(Lua.Stack));
        var nameSwitchCases = new List<SwitchCase>();

        // Properties
        {
            foreach (var property in memberProvider.EnumerateReadableProperties(type, isTypeDefinition))
            {
                var indexParameters = property.GetIndexParameters();
                if (indexParameters.Length > 0)
                    continue;

                var propertyName = property.GetCustomAttribute<LuaNameAttribute>()?.Name ?? property.Name;
                propertyName = namingPolicy.ConvertName(propertyName);

                /*
                    case "propertyName":
                    {
                        Lua.Stack.Push(instance.Property); // instance is either the type or the value
                        return 1;
                    }
                */
                var switchCaseExpression = Expression.SwitchCase(
                    Expression.Block(
                        Expression.Call(getLuaStackExpression, nameof(LuaStack.Push), new[] { property.PropertyType }, Expression.Property(instanceExpression, property)),
                        Expression.Return(returnLabelTarget, Expression.Constant(1))),
                    Expression.Constant(propertyName));

                nameSwitchCases.Add(switchCaseExpression);
            }
        }

        // Fields
        {
            foreach (var field in memberProvider.EnumerateFields(type, isTypeDefinition))
            {
                var fieldName = field.GetCustomAttribute<LuaNameAttribute>()?.Name ?? field.Name;
                fieldName = namingPolicy.ConvertName(fieldName);

                /*
                    case "fieldName":
                    {
                        Lua.Stack.Push(instance.Field); // instance is either the type or the value
                        return 1;
                    }
                */
                var switchCaseExpression = Expression.SwitchCase(
                    Expression.Block(
                        Expression.Call(getLuaStackExpression, nameof(LuaStack.Push), new[] { field.FieldType }, Expression.Field(instanceExpression, field)),
                        Expression.Return(returnLabelTarget, Expression.Constant(1))),
                    Expression.Constant(fieldName));

                nameSwitchCases.Add(switchCaseExpression);
            }
        }

        // Methods
        {
            foreach (var methodGroup in memberProvider.EnumerateMethods(type, isTypeDefinition)
                .GroupBy(static method => method.GetCustomAttribute<LuaNameAttribute>()?.Name ?? method.Name))
            {
                var methodName = namingPolicy.ConvertName(methodGroup.Key);
                /*
                    case "methodName":
                    {
                        Lua.Stack.Push(instance.Method); // instance is either the type or the value; method is either the method or an array of methods
                        return 1;
                    }
                */

                var methods = methodGroup.ToArray();
                var switchCaseExpression = Expression.SwitchCase(
                    Expression.Block(
                        methods.Length == 1
                            ? Expression.Call(getLuaStackExpression, nameof(LuaStack.Push), new[] { typeof(DescribedUserData<MethodInfo>) },
                                Expression.Constant(new DescribedUserData<MethodInfo>(methods[0], _methodUserDataDescriptor)))
                            : Expression.Call(getLuaStackExpression, nameof(LuaStack.Push), new[] { typeof(DescribedUserData<MethodInfo[]>) },
                                Expression.Constant(new DescribedUserData<MethodInfo[]>(methods, _overloadedMethodUserDataDescriptor))),
                        Expression.Return(returnLabelTarget, Expression.Constant(1))),
                    Expression.Constant(methodName));

                nameSwitchCases.Add(switchCaseExpression);
            }
        }

        var nameSwitchExpression = Expression.Switch(nameVariableExpression, nameSwitchCases.ToArray());
        var nameBlockExpression = Expression.Block(new[] { nameVariableExpression }, nameVariableAssignExpression, nameSwitchExpression);

        /*
            if (key.Type == LuaType.String)
            {
                // check fields, properties, methods
            }
            else
            {
                // check indexer
            }
        */
        var ifKeyTypeStringThenElseExpression = Expression.IfThen(
            Expression.Equal(
                Expression.Property(keyParameterExpression, nameof(LuaStackValue.Type)),
                Expression.Constant(LuaType.String)),
            nameBlockExpression);

        var labelExpression = Expression.Label(returnLabelTarget, Expression.Constant(0));
        var lambdaBlockExpression = Expression.Block(ifKeyTypeStringThenElseExpression, labelExpression);

        var lambdaExpression = Expression.Lambda<IndexDelegate>(lambdaBlockExpression,
            luaParameterExpression, userDataParameterExpression, keyParameterExpression);

        return lambdaExpression.Compile();
    }

    public static NewIndexDelegate CreateNewIndex(Type type, bool isTypeDefinition, TypeMemberProvider memberProvider, UserDataNamingPolicy namingPolicy)
    {
        var returnLabelTarget = Expression.Label(typeof(int), "returnLabel");

        var luaParameterExpression = Expression.Parameter(typeof(LuaThread), "lua");
        var userDataParameterExpression = Expression.Parameter(typeof(LuaStackValue), "userData");
        var keyParameterExpression = Expression.Parameter(typeof(LuaStackValue), "key");
        var valueParameterExpression = Expression.Parameter(typeof(LuaStackValue), "value");

        Expression? instanceExpression = isTypeDefinition
            ? null
            : Expression.Field(
                Expression.Call(userDataParameterExpression, nameof(LuaStackValue.GetValue), new[] { typeof(UserDataHandle<>).MakeGenericType(type) }),
                "Value");

        // string name;
        var nameVariableExpression = Expression.Variable(typeof(string), "name");

        // name = key.GetValue<string>();
        var nameVariableAssignExpression = Expression.Assign(
            nameVariableExpression,
            Expression.Call(keyParameterExpression, nameof(LuaStackValue.GetValue), new[] { typeof(string) }));

        var switchCases = new List<SwitchCase>();

        // Properties
        {
            foreach (var property in memberProvider.EnumerateWritableProperties(type, isTypeDefinition))
            {
                var indexParameters = property.GetIndexParameters();
                if (indexParameters.Length > 0)
                    continue;

                var propertyName = property.GetCustomAttribute<LuaNameAttribute>()?.Name ?? property.Name;
                propertyName = namingPolicy.ConvertName(propertyName);

                /*
                    case "propertyName":
                    {
                        if (!value.TryGetValue<TProperty>(out var typedValue))
                        {
                            lua.RaiseArgumentTypeError(value.Index, "TProperty");
                        }

                        instance.Property = typedValue; // instance is either the type or the value
                        return 0;
                    }
                */
                var typedValueVariableExpression = Expression.Variable(property.PropertyType, "typedValue");
                var switchCaseExpression = Expression.SwitchCase(
                    Expression.Block(new[] { typedValueVariableExpression },
                        Expression.IfThen(
                            Expression.IsFalse(
                                Expression.Call(valueParameterExpression, nameof(LuaStackValue.TryGetValue), new[] { property.PropertyType }, typedValueVariableExpression)),
                            Expression.Call(
                                luaParameterExpression,
                                nameof(Lua.RaiseArgumentTypeError), Type.EmptyTypes,
                                Expression.Property(valueParameterExpression, nameof(LuaStackValue.Index)), Expression.Constant(property.PropertyType.ToTypeString()))),
                        Expression.Assign(Expression.Property(instanceExpression, property), typedValueVariableExpression),
                        Expression.Return(returnLabelTarget, Expression.Constant(0))),
                    Expression.Constant(propertyName));

                switchCases.Add(switchCaseExpression);
            }
        }

        // Fields
        {
            foreach (var field in memberProvider.EnumerateFields(type, isTypeDefinition))
            {
                var fieldName = field.GetCustomAttribute<LuaNameAttribute>()?.Name ?? field.Name;
                fieldName = namingPolicy.ConvertName(fieldName);
                /*
                    case "fieldName":
                    {
                        if (!value.TryGetValue<TField>(out var typedValue))
                        {
                            lua.RaiseArgumentTypeError(value.Index, "TField");
                        }

                        instance.Field = typedValue; // instance is either the type or the value
                        return 0;
                    }
                */
                var typedValueVariableExpression = Expression.Variable(field.FieldType, "typedValue");
                var switchCaseExpression = Expression.SwitchCase(
                    Expression.Block(new[] { typedValueVariableExpression },
                        Expression.IfThen(
                            Expression.IsFalse(
                                Expression.Call(valueParameterExpression, nameof(LuaStackValue.TryGetValue), new[] { field.FieldType }, typedValueVariableExpression)),
                            Expression.Call(
                                luaParameterExpression,
                                nameof(Lua.RaiseArgumentTypeError), Type.EmptyTypes,
                                Expression.Property(valueParameterExpression, nameof(LuaStackValue.Index)), Expression.Constant(field.FieldType.ToTypeString()))),
                        Expression.Assign(
                            Expression.Field(instanceExpression, field),
                            typedValueVariableExpression),
                        Expression.Return(returnLabelTarget, Expression.Constant(0))),
                    Expression.Constant(fieldName));

                switchCases.Add(switchCaseExpression);
            }
        }

        var nameSwitchExpression = Expression.Switch(nameVariableExpression, switchCases.ToArray());
        var nameBlockExpression = Expression.Block(new[] { nameVariableExpression }, nameVariableAssignExpression, nameSwitchExpression);

        /*
            if (key.Type == LuaType.String)
            {
                // check fields, properties, methods
            }
            else
            {
                // check indexer
            }
        */
        var ifKeyTypeStringThenElseExpression = Expression.IfThen(
            Expression.Equal(
                Expression.Property(keyParameterExpression, nameof(LuaStackValue.Type)),
                Expression.Constant(LuaType.String)),
            nameBlockExpression);

        var labelExpression = Expression.Label(returnLabelTarget, Expression.Constant(0));
        var lambdaBlockExpression = Expression.Block(ifKeyTypeStringThenElseExpression, labelExpression);

        var lambdaExpression = Expression.Lambda<NewIndexDelegate>(lambdaBlockExpression,
            luaParameterExpression, userDataParameterExpression, keyParameterExpression, valueParameterExpression);

        return lambdaExpression.Compile();
    }
}
