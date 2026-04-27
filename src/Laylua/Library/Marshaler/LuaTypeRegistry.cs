using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Laylua.Moon;

namespace Laylua.Marshaling;

/// <summary>
///     Central registry for userdata handlers.
///     Source-generated handlers register via module initializers;
///     reflection-built handlers are created lazily on first push.
/// </summary>
public static unsafe class LuaTypeRegistry
{
    private static readonly ConcurrentDictionary<Type, ILuaUserDataHandler> Handlers = new();
    private static readonly ConcurrentDictionary<Type, ILuaUserDataHandler[]> AssignableHandlerCache = new();
    private static readonly ConcurrentDictionary<nint, MethodPointerInfo> MethodPointers = new();
    private static readonly ConcurrentDictionary<Type, DynamicUserDataOptions> PerTypeDynamicOptions = new();
    private static readonly ConcurrentDictionary<Type, (FieldInfo InstanceField, FieldInfo TypedInstanceField)> TypedHandlerFields = new();
    private static readonly object HandlerLock = new();

    private static DynamicUserDataOptions _dynamicDefaults = new();

    /// <summary>
    ///     Global default options for all dynamic (reflection-based) userdata types.
    ///     Does <b>not</b> affect <see cref="LuaTypeAttribute"/>-annotated types, which use source-generated handlers.
    /// </summary>
    public static DynamicUserDataOptions DynamicDefaults
    {
        get => _dynamicDefaults;
        set => _dynamicDefaults = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    ///     Configures dynamic userdata options for a specific type.
    ///     Uses reflection at runtime to expose members; for high-performance pre-defined types,
    ///     prefer annotating with <see cref="LuaTypeAttribute"/> instead.
    ///     The provided options override a snapshot of <see cref="DynamicDefaults"/> captured when this method is called.
    ///     Must be called before the type is first pushed to Lua.
    /// </summary>
    public static void ConfigureDynamic<T>(Action<DynamicUserDataOptions>? configure = null)
    {
        ConfigureDynamic(typeof(T), configure);
    }

    /// <summary>
    ///     Configures dynamic userdata options for a specific type.
    ///     Uses reflection at runtime to expose members; for high-performance pre-defined types,
    ///     prefer annotating with <see cref="LuaTypeAttribute"/> instead.
    ///     The provided options override a snapshot of <see cref="DynamicDefaults"/> captured when this method is called.
    ///     Eagerly registers the handler so configured types are marshaled as userdata
    ///     even if they implement <see cref="System.Collections.IEnumerable"/>.
    /// </summary>
    public static void ConfigureDynamic(Type type, Action<DynamicUserDataOptions>? configure = null)
    {
        lock (HandlerLock)
        {
            var currentOptions = PerTypeDynamicOptions.GetValueOrDefault(type, _dynamicDefaults);
            var configuredOptions = currentOptions.Clone();
            configure?.Invoke(configuredOptions);

            var storedOptions = configuredOptions.Clone();
            var handler = new DynamicUserDataHandler(type, configuredOptions);
            PerTypeDynamicOptions[type] = storedOptions;
            Handlers[type] = handler;
            SetTypedHandlerCache(type, handler);
            AssignableHandlerCache.Clear();
        }
    }

    internal static DynamicUserDataOptions GetDynamicOptions(Type type)
    {
        return PerTypeDynamicOptions.GetValueOrDefault(type, _dynamicDefaults);
    }

    /// <summary>
    ///     Metadata about a typed userdata method, keyed by its wrapped native function pointer.
    /// </summary>
    public readonly struct MethodPointerInfo
    {
        /// <summary>
        ///     The CLR type that declares the wrapped method.
        /// </summary>
        public readonly Type DeclaringType;

        /// <summary>
        ///     The CLR method names that map to the wrapped function pointer.
        /// </summary>
        public readonly IReadOnlyList<string> ClrMethodNames;

        /// <summary>
        ///     The resolved CLR methods for the wrapped function pointer.
        /// </summary>
        public readonly IReadOnlyList<MethodInfo> Methods;

        private readonly ConcurrentDictionary<Type, MethodInfo?> _openDelegateMethods;

        /// <summary>
        ///     Initializes method-pointer metadata and resolves its CLR methods once.
        /// </summary>
        public MethodPointerInfo(Type declaringType, string[] clrMethodNames, bool isStatic)
        {
            DeclaringType = declaringType;
            ClrMethodNames = clrMethodNames;
            Methods = ResolveMethods(declaringType, clrMethodNames, isStatic);
            _openDelegateMethods = [];
        }

        /// <summary>
        ///     Attempts to find the CLR method that matches the requested open delegate type.
        /// </summary>
        public bool TryGetOpenDelegateMethod(Type delegateType, out MethodInfo? method)
        {
            method = _openDelegateMethods.GetOrAdd(delegateType, static (currentDelegateType, methods) =>
            {
                for (var i = 0; i < methods.Count; i++)
                {
                    if (Delegate.CreateDelegate(currentDelegateType, null, methods[i], throwOnBindFailure: false) != null)
                    {
                        return methods[i];
                    }
                }

                return null;
            }, Methods);

            return method is not null;
        }

        private static MethodInfo[] ResolveMethods(Type declaringType, string[] clrMethodNames, bool isStatic)
        {
            var bindingFlags = (isStatic ? BindingFlags.Static | BindingFlags.FlattenHierarchy : BindingFlags.Instance)
                | BindingFlags.Public | BindingFlags.NonPublic;

            var methods = new List<MethodInfo>();
            var seenMethods = new HashSet<MethodInfo>();
            for (var i = 0; i < clrMethodNames.Length; i++)
            {
                foreach (var member in declaringType.GetMember(clrMethodNames[i], MemberTypes.Method, bindingFlags))
                {
                    var method = (MethodInfo) member;
                    if (seenMethods.Add(method))
                    {
                        methods.Add(method);
                    }
                }
            }

            return methods.ToArray();
        }
    }

    /// <summary>
    ///     Static generic cache for zero-alloc handler lookup.
    ///     When <c>T</c> matches the runtime type, this avoids dictionary lookup entirely.
    /// </summary>
    private static class TypedHandler<T>
    {
        public static ILuaUserDataHandler? Instance;
        public static ILuaUserDataHandler<T>? TypedInstance;
    }

    private static void SetTypedHandlerCache(Type type, ILuaUserDataHandler? handler)
    {
        var (instanceField, typedInstanceField) = TypedHandlerFields.GetOrAdd(type, static t =>
        {
            var typedHandlerType = typeof(TypedHandler<>).MakeGenericType(t);
            return (
                typedHandlerType.GetField(nameof(TypedHandler<>.Instance), BindingFlags.Public | BindingFlags.Static)!,
                typedHandlerType.GetField(nameof(TypedHandler<>.TypedInstance), BindingFlags.Public | BindingFlags.Static)!);
        });

        instanceField.SetValue(null, handler);
        typedInstanceField.SetValue(null, handler != null && typedInstanceField.FieldType.IsInstanceOfType(handler) ? handler : null);
    }

    /// <summary>
    ///     Registers a handler for the given type with the generic fast-path cache.
    /// </summary>
    public static void Register<T>(ILuaUserDataHandler handler)
    {
        lock (HandlerLock)
        {
            Handlers[typeof(T)] = handler;
            AssignableHandlerCache.Clear();
            TypedHandler<T>.Instance = handler;
            TypedHandler<T>.TypedInstance = handler as ILuaUserDataHandler<T>;
        }
    }

    /// <summary>
    ///     Registers a handler for the given type via its runtime <see cref="Type"/>.
    ///     Does not populate the generic fast-path cache; any stale generic cache entry is cleared
    ///     so subsequent generic operations fall back to the dictionary path.
    ///     Callers requiring maximum throughput should use <see cref="Register{T}"/> instead.
    /// </summary>
    public static void Register(Type type, ILuaUserDataHandler handler)
    {
        lock (HandlerLock)
        {
            SetTypedHandlerCache(type, null);
            TypedHandlerFields.TryRemove(type, out _);
            Handlers[type] = handler;
            AssignableHandlerCache.Clear();
        }
    }

    /// <summary>
    ///     Unregisters the handler for the given type, clearing both the dictionary
    ///     and the generic fast-path cache.
    ///     Returns <c>true</c> if a handler was removed; <c>false</c> if none was registered.
    /// </summary>
    public static bool Unregister<T>()
    {
        ILuaUserDataHandler? handler = null;
        lock (HandlerLock)
        {
            var type = typeof(T);
            TypedHandler<T>.Instance = null;
            TypedHandler<T>.TypedInstance = null;
            TypedHandlerFields.TryRemove(type, out _);
            PerTypeDynamicOptions.TryRemove(type, out _);
            RemoveMethodPointersForType(type);

            if (Handlers.TryRemove(type, out handler))
            {
                AssignableHandlerCache.Clear();
            }
        }

        return handler != null;
    }

    /// <summary>
    ///     Unregisters the handler for the given type via its runtime <see cref="Type"/>.
    ///     Clears both the dictionary entry and the generic fast-path cache.
    ///     Returns <c>true</c> if a handler was removed; <c>false</c> if none was registered.
    /// </summary>
    public static bool Unregister(Type type)
    {
        ILuaUserDataHandler? handler = null;
        lock (HandlerLock)
        {
            SetTypedHandlerCache(type, null);
            TypedHandlerFields.TryRemove(type, out _);
            PerTypeDynamicOptions.TryRemove(type, out _);
            RemoveMethodPointersForType(type);

            if (Handlers.TryRemove(type, out handler))
            {
                AssignableHandlerCache.Clear();
            }
        }

        return handler != null;
    }

    private static void RemoveMethodPointersForType(Type type)
    {
        foreach (var kvp in MethodPointers)
        {
            if (kvp.Value.DeclaringType == type)
            {
                MethodPointers.TryRemove(kvp.Key, out _);
            }
        }
    }

    /// <summary>
    ///     Attempts to push a value using the generic fast-path.
    ///     If <c>T</c> matches the runtime type, no dictionary lookup is needed.
    /// </summary>
    internal static bool TryPush<T>(lua_State* L, T value)
        where T : notnull
    {
        var typedHandler = TypedHandler<T>.TypedInstance;
        if (typedHandler != null)
        {
            typedHandler.Push(L, value);
            return true;
        }

        var handler = TypedHandler<T>.Instance;
        if (handler != null)
        {
            handler.Push(L, value);
            return true;
        }

        return TryPush(L, value.GetType(), value);
    }

    /// <summary>
    ///     Attempts to push a value using its runtime type for handler lookup.
    /// </summary>
    internal static bool TryPush(lua_State* L, Type type, object value)
    {
        if (Handlers.TryGetValue(type, out var handler))
        {
            handler.Push(L, value);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Attempts to push a type definition using a registered handler.
    ///     Used when pushing <c>typeof(T)</c> to expose static members in Lua.
    /// </summary>
    internal static bool TryPushDefinition(lua_State* L, Type type)
    {
        if (Handlers.TryGetValue(type, out var handler))
        {
            handler.PushDefinition(L);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Gets or creates a dynamic handler, then pushes its type definition.
    ///     Used when dynamic (reflection-based) marshaling is enabled.
    /// </summary>
    internal static void PushDynamicDefinition(lua_State* L, Type type)
    {
        var handler = GetOrCreate(type);
        handler.PushDefinition(L);
    }

    /// <summary>
    ///     Attempts to materialize a CLR object from a Lua table using a registered handler.
    /// </summary>
    internal static bool TryGetFromTable(lua_State* L, int stackIndex, Type type, out object? value)
    {
        if (Handlers.TryGetValue(type, out var handler))
        {
            return handler.TryGetFromTable(L, stackIndex, out value);
        }

        value = null;
        return false;
    }

    /// <summary>
    ///     Attempts to materialize a <typeparamref name="T"/> from a Lua table
    ///     using the generic fast-path to avoid boxing for value types.
    /// </summary>
    internal static bool TryGetFromTable<T>(lua_State* L, int stackIndex, out T? value)
    {
        var typedHandler = TypedHandler<T>.TypedInstance;
        if (typedHandler != null)
        {
            return typedHandler.TryGetFromTable(L, stackIndex, out value);
        }

        if (Handlers.TryGetValue(typeof(T), out var handler))
        {
            if (handler is ILuaUserDataHandler<T> typed)
            {
                return typed.TryGetFromTable(L, stackIndex, out value);
            }

            if (handler.TryGetFromTable(L, stackIndex, out var boxed))
            {
                value = (T) boxed!;
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    ///     Attempts to get a value from the Lua stack using the generic fast-path.
    ///     For interface and abstract types, falls back to scanning all registered handlers
    ///     for an assignable match.
    /// </summary>
    internal static bool TryGet<T>(lua_State* L, int stackIndex, out T? value)
    {
        var typedHandler = TypedHandler<T>.TypedInstance;
        if (typedHandler != null && typedHandler.TryGet(L, stackIndex, out value))
        {
            return true;
        }

        var handler = TypedHandler<T>.Instance;
        if (handler != null && handler.TryGet(L, stackIndex, out var obj))
        {
            value = (T) obj!;
            return true;
        }

        if (Handlers.TryGetValue(typeof(T), out var dictionaryHandler) && dictionaryHandler.TryGet(L, stackIndex, out obj))
        {
            value = (T) obj!;
            return true;
        }

        var targetType = typeof(T);
        if ((targetType == typeof(object) || targetType.IsInterface || targetType.IsAbstract) && TryGetAssignable(L, stackIndex, out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    private static ILuaUserDataHandler[] GetAssignableHandlers(Type type)
    {
        // ReSharper disable once InconsistentlySynchronizedField
        if (AssignableHandlerCache.TryGetValue(type, out var handlers))
        {
            return handlers;
        }

        lock (HandlerLock)
        {
            if (AssignableHandlerCache.TryGetValue(type, out handlers))
            {
                return handlers;
            }

            var assignableHandlers = new List<ILuaUserDataHandler>();
            foreach (var handler in Handlers.Values)
            {
                if (type.IsAssignableFrom(handler.Type))
                {
                    assignableHandlers.Add(handler);
                }
            }

            handlers = assignableHandlers.ToArray();
            AssignableHandlerCache[type] = handlers;
            return handlers;
        }
    }

    private static bool TryGetAssignable<T>(lua_State* L, int stackIndex, out T? value)
    {
        foreach (var handler in GetAssignableHandlers(typeof(T)))
        {
            if (handler.TryGet(L, stackIndex, out var obj))
            {
                value = (T) obj!;
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    ///     Attempts to get a value from the Lua stack using a registered handler.
    ///     For <see cref="object"/>, interface, and abstract types, falls back to scanning
    ///     all registered handlers for an assignable match.
    /// </summary>
    internal static bool TryGet(lua_State* L, int stackIndex, Type type, out object? value)
    {
        if (Handlers.TryGetValue(type, out var handler))
        {
            return handler.TryGet(L, stackIndex, out value);
        }

        if (type == typeof(object) || type.IsInterface || type.IsAbstract)
        {
            foreach (var assignableHandler in GetAssignableHandlers(type))
            {
                if (assignableHandler.TryGet(L, stackIndex, out value))
                {
                    return true;
                }
            }
        }

        value = null;
        return false;
    }

    /// <summary>
    ///     Returns the handler registered for the given type, or <c>null</c>.
    /// </summary>
    internal static ILuaUserDataHandler? GetHandler(Type type)
    {
        Handlers.TryGetValue(type, out var handler);
        return handler;
    }

    /// <summary>
    ///     Gets or creates a handler for the given type.
    ///     For types without a source-generated handler, a <see cref="DynamicUserDataHandler"/> is created
    ///     using reflection. For high-performance pre-defined types, prefer <see cref="LuaTypeAttribute"/> instead.
    /// </summary>
    internal static ILuaUserDataHandler GetOrCreate(Type type)
    {
        if (Handlers.TryGetValue(type, out var existing))
        {
            return existing;
        }

        lock (HandlerLock)
        {
            if (Handlers.TryGetValue(type, out existing))
            {
                return existing;
            }

            var options = GetDynamicOptions(type).Clone();
            var handler = new DynamicUserDataHandler(type, options);
            Handlers[type] = handler;
            SetTypedHandlerCache(type, handler);
            AssignableHandlerCache.Clear();
            return handler;
        }
    }

    /// <summary>
    ///     Returns whether a given type has a registered handler.
    /// </summary>
    public static bool IsRegistered(Type type)
    {
        return Handlers.ContainsKey(type);
    }

    /// <summary>
    ///     Registers a native function pointer as corresponding to a typed userdata method.
    ///     Called by generated code during initialization.
    /// </summary>
    internal static void RegisterMethodPointer(nint functionPtr, Type declaringType, string[] clrMethodNames, bool isStatic)
    {
        MethodPointers[functionPtr] = new MethodPointerInfo(declaringType, clrMethodNames, isStatic);
    }

    /// <summary>
    ///     Attempts to look up method metadata for a native function pointer.
    /// </summary>
    internal static bool TryGetMethodPointerInfo(nint functionPtr, out MethodPointerInfo info)
    {
        return MethodPointers.TryGetValue(functionPtr, out info);
    }
}
