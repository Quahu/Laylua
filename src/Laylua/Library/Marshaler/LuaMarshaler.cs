using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Laylua.Moon;

namespace Laylua.Marshaling;

public abstract partial class LuaMarshaler
{
    /// <summary>
    ///     Gets the default shared instance of <see cref="DefaultLuaMarshaler"/>.
    /// </summary>
    public static DefaultLuaMarshaler Default { get; } = new(UserDataDescriptorProvider.Default);

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

    private volatile LuaReferencePool? _entityPool;

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
    ///     Attempts to create a <see cref="LuaTable"/> reference.
    /// </summary>
    /// <param name="lua"> The Lua state. </param>
    /// <param name="stackIndex"> The index on the Lua stack. </param>
    /// <param name="table"> The referenced Lua table. </param>
    /// <returns>
    ///     <see langword="true"/> if succeeded, <see langword="false"/> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe bool TryCreateTableReference(Lua lua, int stackIndex, [MaybeNullWhen(false)] out LuaTable table)
    {
        lua.UnrefLeakedReferences();

        if (!LuaReference.TryCreate(lua.State.L, stackIndex, out var reference))
        {
            table = default;
            return false;
        }

        table = _entityPool?.RentTable() ?? new();
        table.Lua = lua;
        table.Reference = reference;
        return true;
    }

    /// <summary>
    ///     Attempts to create a <see cref="LuaFunction"/> reference.
    /// </summary>
    /// <param name="lua"> The Lua state. </param>
    /// <param name="stackIndex"> The index on the Lua stack. </param>
    /// <param name="function"> The referenced Lua function. </param>
    /// <returns>
    ///     <see langword="true"/> if succeeded, <see langword="false"/> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe bool TryCreateFunctionReference(Lua lua, int stackIndex, [MaybeNullWhen(false)] out LuaFunction function)
    {
        lua.UnrefLeakedReferences();

        if (!LuaReference.TryCreate(lua.State.L, stackIndex, out var reference))
        {
            function = default;
            return false;
        }

        function = _entityPool?.RentFunction() ?? new();
        function.Lua = lua;
        function.Reference = reference;
        return true;
    }

    /// <summary>
    ///     Attempts to create a <see cref="LuaUserData"/> reference.
    /// </summary>
    /// <param name="lua"> The Lua state. </param>
    /// <param name="stackIndex"> The index on the Lua stack. </param>
    /// <param name="userData"> The referenced Lua user data. </param>
    /// <returns>
    ///     <see langword="true"/> if succeeded, <see langword="false"/> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe bool TryCreateUserDataReference(Lua lua, int stackIndex, IntPtr pointer, [MaybeNullWhen(false)] out LuaUserData userData)
    {
        lua.UnrefLeakedReferences();

        if (!LuaReference.TryCreate(lua.State.L, stackIndex, out var reference))
        {
            userData = default;
            return false;
        }

        userData = _entityPool?.RentUserData() ?? new();
        userData.Lua = lua;
        userData.Reference = reference;
        userData.Pointer = pointer;
        return true;
    }

    /// <summary>
    ///     Attempts to create a <see cref="LuaThread"/> reference.
    /// </summary>
    /// <param name="lua"> The Lua state. </param>
    /// <param name="stackIndex"> The index on the Lua stack. </param>
    /// <param name="thread"> The referenced Lua thread. </param>
    /// <returns>
    ///     <see langword="true"/> if succeeded, <see langword="false"/> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe bool TryCreateThreadReference(Lua lua, int stackIndex, lua_State* L, [MaybeNullWhen(false)] out LuaThread thread)
    {
        lua.UnrefLeakedReferences();

        if (!LuaReference.TryCreate(lua.State.L, stackIndex, out var reference))
        {
            thread = default;
            return false;
        }

        thread = _entityPool?.RentThread() ?? new();
        thread.Lua = lua;
        thread.Reference = reference;
        thread.L = L;
        return true;
    }

    /// <summary>
    ///     Tries to convert the Lua value at the specified stack index
    ///     to a .NET value of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="lua"> The Lua state. </param>
    /// <param name="stackIndex"> The index on the Lua stack. </param>
    /// <param name="obj"> The output value. </param>
    /// <typeparam name="T"> The type of the value. </typeparam>
    /// <returns>
    ///     <see langword="true"/> if successful.
    /// </returns>
    public abstract bool TryGetValue<T>(Lua lua, int stackIndex, out T? obj);

    /// <summary>
    ///     Pushes the specified .NET value onto the Lua stack,
    ///     converting it to a Lua value appropriately.
    /// </summary>
    /// <remarks>
    ///     The marshaler expects space on the stack for the value to be pushed.
    ///     Use <see cref="LuaStack.EnsureFreeCapacity"/> prior to calling this method.
    /// </remarks>
    /// <param name="lua"> The Lua state. </param>
    /// <param name="obj"> The value to push. </param>
    /// <typeparam name="T"> The type of the value. </typeparam>
    public abstract void PushValue<T>(Lua lua, T obj);
}
