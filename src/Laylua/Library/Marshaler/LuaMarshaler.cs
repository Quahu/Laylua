using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Laylua.Moon;

namespace Laylua.Marshaling;

public abstract partial class LuaMarshaler
{
    private volatile LuaReferencePool? _entityPool;

    /// <summary>
    ///     Gets the default shared instance of <see cref="DefaultLuaMarshaler"/>.
    /// </summary>
    public static DefaultLuaMarshaler Default { get; } = new();

    /// <summary>
    ///     Sets the entity pool configuration determining how many <see cref="LuaReference"/> instances can be pooled.
    ///     Setting <see langword="null"/> indicates entity pooling is disabled. Defaults to <see langword="null"/>.
    /// </summary>
    /// <remarks>
    ///     When entity pooling is enabled, once you have disposed a <see cref="LuaReference"/> instance,
    ///     you must not continue using that same object instance - it will be pooled and reused for other entities.
    ///     <example>
    ///     Pooling <see cref="LuaReference"/> instances correctly:
    ///     <code language="csharp">
    ///     using (var table = Lua.Evaluate&lt;LuaTable&gt;(...)!)
    ///     {
    ///         // do stuff
    ///     }
    ///     <para/>
    ///     // `table` must not be used after it is disposed!
    ///     </code>
    ///     </example>
    /// </remarks>
    public LuaMarshalerEntityPoolConfiguration? EntityPoolConfiguration
    {
        set => _entityPool = value != null
            ? new LuaReferencePool(value)
            : null;
    }

    /// <summary>
    ///     Instantiates a new marshaler with the specified Lua instance.
    /// </summary>
    protected LuaMarshaler()
    { }

    /// <summary>
    ///     Invoked when the specified Lua state is being disposed.
    /// </summary>
    /// <param name="lua"> The Lua state. </param>
    protected internal virtual void OnLuaDisposing(Lua lua)
    { }

    /// <summary>
    ///     Invoked after the specified Lua state was disposed.
    /// </summary>
    /// <param name="lua"> The Lua state. </param>
    protected internal virtual void OnLuaDisposed(Lua lua)
    { }

    /// <summary>
    ///     Attempts to create a <see cref="LuaTable"/> reference.
    /// </summary>
    /// <param name="thread"> The Lua thread. </param>
    /// <param name="stackIndex"> The index on the Lua stack. </param>
    /// <param name="table"> The referenced Lua table. </param>
    /// <returns>
    ///     <see langword="true"/> if succeeded, <see langword="false"/> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe bool TryCreateTableReference(LuaThread thread, int stackIndex, [MaybeNullWhen(false)] out LuaTable table)
    {
        var lua = Lua.FromThread(thread);
        lua.UnrefLeakedReferences();
        var L = thread.State.L;

        if (!LuaReference.TryCreate(L, stackIndex, out var reference))
        {
            table = default;
            return false;
        }

        try
        {
            table = _entityPool?.RentTable() ?? new();
            table.Lua = lua;
            table.Reference = reference;
            return true;
        }
        catch
        {
            luaL_unref(L, LuaRegistry.Index, reference);
            throw;
        }
    }

    /// <summary>
    ///     Attempts to create a <see cref="LuaFunction"/> reference.
    /// </summary>
    /// <param name="thread"> The Lua thread. </param>
    /// <param name="stackIndex"> The index on the Lua stack. </param>
    /// <param name="function"> The referenced Lua function. </param>
    /// <returns>
    ///     <see langword="true"/> if succeeded, <see langword="false"/> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe bool TryCreateFunctionReference(LuaThread thread, int stackIndex, [MaybeNullWhen(false)] out LuaFunction function)
    {
        var lua = Lua.FromThread(thread);
        lua.UnrefLeakedReferences();
        var L = thread.State.L;

        if (!LuaReference.TryCreate(L, stackIndex, out var reference))
        {
            function = default;
            return false;
        }

        try
        {
            function = _entityPool?.RentFunction() ?? new();
            function.Lua = lua;
            function.Reference = reference;
            return true;
        }
        catch
        {
            luaL_unref(L, LuaRegistry.Index, reference);
            throw;
        }
    }

    /// <summary>
    ///     Attempts to create a <see cref="LuaUserData"/> reference.
    /// </summary>
    /// <param name="thread"> The Lua thread. </param>
    /// <param name="stackIndex"> The index on the Lua stack. </param>
    /// <param name="pointer"> The pointer to the user data. </param>
    /// <param name="userData"> The referenced Lua user data. </param>
    /// <returns>
    ///     <see langword="true"/> if succeeded, <see langword="false"/> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe bool TryCreateUserDataReference(LuaThread thread, int stackIndex, IntPtr pointer, [MaybeNullWhen(false)] out LuaUserData userData)
    {
        var lua = Lua.FromThread(thread);
        lua.UnrefLeakedReferences();
        var L = thread.State.L;

        if (!LuaReference.TryCreate(L, stackIndex, out var reference))
        {
            userData = default;
            return false;
        }

        try
        {
            userData = _entityPool?.RentUserData() ?? new();
            userData.Lua = lua;
            userData.Reference = reference;
            userData.Pointer = pointer;
            return true;
        }
        catch
        {
            luaL_unref(L, LuaRegistry.Index, reference);
            throw;
        }
    }

    /// <summary>
    ///     Attempts to create a <see cref="LuaThread"/> reference.
    /// </summary>
    /// <param name="thread"> The Lua thread. </param>
    /// <param name="stackIndex"> The index on the Lua stack. </param>
    /// <param name="L1"> The thread state pointer of the target thread. </param>
    /// <param name="createdThread"> The referenced Lua thread. </param>
    /// <returns>
    ///     <see langword="true"/> if succeeded, <see langword="false"/> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe bool TryCreateThreadReference(LuaThread thread, int stackIndex, lua_State* L1, [MaybeNullWhen(false)] out LuaThread createdThread)
    {
        var lua = Lua.FromThread(thread);
        lua.UnrefLeakedReferences();
        var L = thread.State.L;

        if (!LuaReference.TryCreate(L, stackIndex, out var reference))
        {
            createdThread = default;
            return false;
        }

        try
        {
            var childThread = _entityPool?.RentThread() ?? new();
            childThread.Initialize(lua, L1, reference);
            createdThread = childThread;
            return true;
        }
        catch
        {
            luaL_unref(L, LuaRegistry.Index, reference);
            throw;
        }
    }

    /// <summary>
    ///     Tries to convert the Lua value at the specified stack index
    ///     to a .NET value of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="thread"> The Lua thread. </param>
    /// <param name="stackIndex"> The index on the Lua stack. </param>
    /// <param name="obj"> The output value. </param>
    /// <typeparam name="T"> The type of the value. </typeparam>
    /// <returns>
    ///     <see langword="true"/> if successful.
    /// </returns>
    public bool TryGetValue<T>(LuaThread thread, int stackIndex, out T? obj)
    {
        return TryGetValue(thread, stackIndex, out obj, out _);
    }

    /// <summary>
    ///     Tries to convert the Lua value at the specified stack index
    ///     to a .NET value of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="thread"> The Lua thread. </param>
    /// <param name="stackIndex"> The index on the Lua stack. </param>
    /// <param name="obj"> The output value. </param>
    /// <param name="error"> The conversion error, if conversion failed. </param>
    /// <typeparam name="T"> The type of the value. </typeparam>
    /// <returns>
    ///     <see langword="true"/> if successful.
    /// </returns>
    public abstract bool TryGetValue<T>(
        LuaThread thread,
        int stackIndex,
        out T? obj,
        [NotNullWhen(false)] out LuaConversionError? error);

    /// <summary>
    ///     Tries to convert the Lua value at the specified stack index
    ///     to a .NET value of type <typeparamref name="T"/>, applying the specified conversion rules.
    /// </summary>
    /// <param name="thread"> The Lua thread. </param>
    /// <param name="stackIndex"> The index on the Lua stack. </param>
    /// <param name="allowedConversions"> The conversion rules to apply. </param>
    /// <param name="obj"> The output value. </param>
    /// <typeparam name="T"> The type of the value. </typeparam>
    /// <returns>
    ///     <see langword="true"/> if successful.
    /// </returns>
    public bool TryGetValue<T>(LuaThread thread, int stackIndex, LuaAllowedValueConversions allowedConversions, out T? obj)
    {
        return TryGetValue(thread, stackIndex, allowedConversions, out obj, out _);
    }

    /// <summary>
    ///     Tries to convert the Lua value at the specified stack index
    ///     to a .NET value of type <typeparamref name="T"/>, applying the specified conversion rules.
    /// </summary>
    /// <param name="thread"> The Lua thread. </param>
    /// <param name="stackIndex"> The index on the Lua stack. </param>
    /// <param name="allowedConversions"> The conversion rules to apply. </param>
    /// <param name="obj"> The output value. </param>
    /// <param name="error"> The conversion error, if conversion failed. </param>
    /// <typeparam name="T"> The type of the value. </typeparam>
    /// <returns>
    ///     <see langword="true"/> if successful.
    /// </returns>
    public abstract bool TryGetValue<T>(
        LuaThread thread,
        int stackIndex,
        LuaAllowedValueConversions allowedConversions,
        out T? obj,
        [NotNullWhen(false)] out LuaConversionError? error);

    /// <summary>
    ///     Pushes the specified .NET value onto the Lua stack,
    ///     converting it to a Lua value appropriately.
    /// </summary>
    /// <remarks>
    ///     The marshaler expects space on the stack for the value to be pushed.
    ///     Use <see cref="LuaStack.EnsureFreeCapacity"/> prior to calling this method.
    /// </remarks>
    /// <param name="thread"> The Lua thread. </param>
    /// <param name="obj"> The value to push. </param>
    /// <typeparam name="T"> The type of the value. </typeparam>
    public abstract void PushValue<T>(LuaThread thread, T obj);

    /// <summary>
    ///     Returns the marshaling compatibility for binding the Lua value at the specified stack index
    ///     to a .NET target type. This method declares what conversions the marshaler supports
    ///     and their relative costs, without performing any conversion.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method is the extension point for custom marshalers to advertise their conversion
    ///         capabilities. The returned result describes the marshaler's raw capability;
    ///         handler-level policy filtering (via <see cref="LuaAllowedValueConversions"/>) is applied
    ///         separately by the caller.
    ///     </para>
    ///     <para>
    ///         The caller handles nil values. This method is only invoked for non-nil stack values.
    ///     </para>
    /// </remarks>
    /// <param name="thread">The Lua thread.</param>
    /// <param name="stackIndex">The index on the Lua stack.</param>
    /// <param name="targetType">The CLR target type (already normalized past byref/nullable by the caller).</param>
    /// <returns>
    ///     A <see cref="LuaMarshalingCompatibility"/> describing the conversion capability and cost,
    ///     or <see cref="LuaMarshalingCompatibility.Incompatible"/> if the marshaler cannot convert the value.
    /// </returns>
    public abstract LuaMarshalingCompatibility GetMarshalingCompatibility(LuaThread thread, int stackIndex, Type targetType);
}
