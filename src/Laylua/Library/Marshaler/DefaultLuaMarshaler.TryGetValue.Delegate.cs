using System;
using System.Collections.Concurrent;
using System.Reflection;
using Laylua.Moon;

namespace Laylua.Marshaling;

public unsafe partial class DefaultLuaMarshaler
{
    private const int MaxSupportedDelegateParameterCount = 16;
    private const int MaxSupportedDelegateTypeArgumentCount = MaxSupportedDelegateParameterCount + 1;

    private static readonly ConcurrentDictionary<Type, DelegateWrapperMethodCacheEntry> _delegateWrapperMethods = new();
    private static readonly MethodInfo?[] _actionWrapperMethodDefinitions = BuildDelegateWrapperMethodDefinitions(nameof(LuaFunctionDelegateWrapper.InvokeAction));
    private static readonly MethodInfo?[] _functionWrapperMethodDefinitions = BuildDelegateWrapperMethodDefinitions(nameof(LuaFunctionDelegateWrapper.InvokeFunc));

    /// <summary>
    ///     Wraps a <see cref="LuaFunction"/> so it can be called as a .NET delegate.
    /// </summary>
    private sealed partial class LuaFunctionDelegateWrapper(LuaFunction function);

    private readonly struct DelegateWrapperMethodCacheEntry(MethodInfo? method, int parameterCount, bool hasTooManyParameters)
    {
        public readonly MethodInfo? Method = method;

        public readonly int ParameterCount = parameterCount;

        public readonly bool HasTooManyParameters = hasTooManyParameters;
    }

    internal static Delegate? CreateDelegateFromFunction(LuaFunction function, Type delegateType)
    {
        var cacheEntry = _delegateWrapperMethods.GetOrAdd(delegateType, static currentDelegateType => CreateDelegateWrapperMethodCacheEntry(currentDelegateType));
        if (cacheEntry.HasTooManyParameters)
        {
            throw new NotSupportedException(
                $"Cannot marshal a Lua function to delegate type '{delegateType.Name}' because it has {cacheEntry.ParameterCount} parameters. " +
                $"Marshaling Lua functions to delegates supports at most {MaxSupportedDelegateParameterCount} parameters. " +
                $"Use {nameof(LuaFunction)} directly for higher-arity calls.");
        }

        if (cacheEntry.Method == null)
        {
            return null;
        }

        var wrapper = new LuaFunctionDelegateWrapper(function);
        return cacheEntry.Method.CreateDelegate(delegateType, wrapper);
    }

    internal static bool CanConvertFunctionToDelegate(lua_State* L, int stackIndex, Type delegateType)
    {
        if (delegateType.BaseType != typeof(MulticastDelegate))
        {
            return false;
        }

        if (TryGetMethodInfoFromFunction(L, stackIndex, delegateType, out _))
        {
            return true;
        }

        var cacheEntry = _delegateWrapperMethods.GetOrAdd(delegateType, static currentDelegateType => CreateDelegateWrapperMethodCacheEntry(currentDelegateType));
        return !cacheEntry.HasTooManyParameters && cacheEntry.Method != null;
    }

    internal static bool CanConvertFunctionToMethodInfo(lua_State* L, int stackIndex)
    {
        return TryGetMethodInfoFromFunction(L, stackIndex, typeof(MethodInfo), out _);
    }

    /// <summary>
    ///     Attempts to resolve a typed userdata method CFunction to a <see cref="MethodInfo"/>
    ///     or an open instance delegate. Returns <c>false</c> for plain Lua functions.
    /// </summary>
    private static bool TryGetMethodInfoFromFunction(lua_State* L, int stackIndex, Type targetType, out object? result)
    {
        var rawPtr = (nint) lua_topointer(L, stackIndex);
        if (rawPtr == 0 || !LuaTypeRegistry.TryGetMethodPointerInfo(rawPtr, out var info))
        {
            result = null;
            return false;
        }

        var methods = info.Methods;
        if (methods.Count == 0)
        {
            result = null;
            return false;
        }

        if (targetType == typeof(MethodInfo))
        {
            if (methods.Count == 1)
            {
                result = methods[0];
                return true;
            }

            result = null;
            return false;
        }

        // Specific delegate type -> create open delegate via signature matching
        if (targetType.IsSubclassOf(typeof(MulticastDelegate))
            && info.TryGetOpenDelegateMethod(targetType, out var matchingMethod)
            && matchingMethod != null)
        {
            var @delegate = Delegate.CreateDelegate(targetType, null, matchingMethod, throwOnBindFailure: false);
            if (@delegate != null)
            {
                result = @delegate;
                return true;
            }
        }

        result = null;
        return false;
    }

    private static DelegateWrapperMethodCacheEntry CreateDelegateWrapperMethodCacheEntry(Type delegateType)
    {
        var invokeMethod = delegateType.GetMethod("Invoke");
        if (invokeMethod == null)
        {
            return default;
        }

        var parameters = invokeMethod.GetParameters();
        var parameterCount = parameters.Length;
        if (parameterCount > MaxSupportedDelegateParameterCount)
        {
            return new DelegateWrapperMethodCacheEntry(method: null, parameterCount, hasTooManyParameters: true);
        }

        var returnType = invokeMethod.ReturnType;
        var isVoid = returnType == typeof(void);
        var typeArgumentCount = isVoid
            ? parameterCount
            : parameterCount + 1;

        var typeArguments = new Type[typeArgumentCount];
        for (var i = 0; i < parameterCount; i++)
        {
            var parameterType = parameters[i].ParameterType;
            if (IsUnsupportedDelegateType(parameterType))
            {
                return default;
            }

            typeArguments[i] = parameterType;
        }

        if (!isVoid)
        {
            if (IsUnsupportedDelegateType(returnType))
            {
                return default;
            }

            typeArguments[parameterCount] = returnType;
        }

        var methodDefinition = isVoid
            ? _actionWrapperMethodDefinitions[typeArgumentCount]
            : _functionWrapperMethodDefinitions[typeArgumentCount];

        if (methodDefinition == null)
        {
            return default;
        }

        var method = typeArgumentCount == 0
            ? methodDefinition
            : methodDefinition.MakeGenericMethod(typeArguments);

        return new DelegateWrapperMethodCacheEntry(method, parameterCount, hasTooManyParameters: false);
    }

    private static MethodInfo?[] BuildDelegateWrapperMethodDefinitions(string methodName)
    {
        var methodDefinitions = new MethodInfo?[MaxSupportedDelegateTypeArgumentCount + 1];
        var methods = typeof(LuaFunctionDelegateWrapper).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
        for (var i = 0; i < methods.Length; i++)
        {
            var method = methods[i];
            if (method.Name != methodName)
            {
                continue;
            }

            var genericArgumentCount = method.GetGenericArguments().Length;
            if (genericArgumentCount <= MaxSupportedDelegateTypeArgumentCount)
            {
                methodDefinitions[genericArgumentCount] = method;
            }
        }

        return methodDefinitions;
    }

    private static bool IsUnsupportedDelegateType(Type type)
    {
        return type.IsByRef || type.IsByRefLike || type.IsPointer;
    }
}
