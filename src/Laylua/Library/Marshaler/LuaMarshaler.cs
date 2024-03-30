using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Laylua.Moon;
using Qommon;

namespace Laylua.Marshaling;

public abstract partial class LuaMarshaler
{
    /// <summary>
    ///     Gets the default shared instance of <see cref="LuaMarshaler"/>.
    /// </summary>
    public static LuaMarshaler Default { get; } = new DefaultLuaMarshaler();

    /// <summary>
    ///     Gets the user data descriptor provider of this marshaler.
    /// </summary>
    public UserDataDescriptorProvider UserDataDescriptorProvider
    {
        get => _userDataDescriptorProvider;
        set
        {
            Guard.IsNotNull(value);
            _userDataDescriptorProvider = value;
        }
    }

    /// <summary>
    ///     Sets the entity pool configuration determining how many <see cref="LuaReference"/> instances can be pooled.
    ///     Setting <see langword="null"/> indicates entity pooling is disabled. Defaults to <see langword="null"/>.
    /// </summary>
    /// <remarks>
    ///     When entity pooling is enabled, you should invoke <see cref="LuaReferenceExtensions.PoolOnDispose{TReference}"/> whenever
    ///     you retrieve a new <see cref="LuaReference"/> instance.
    ///     Once you have disposed the given <see cref="LuaReference"/>,
    ///     you must not continue using that same object instance - it will be pooled and used for other entities.
    ///     <example>
    ///     Pooling <see cref="LuaReference"/> instances correctly.
    ///     <code language="csharp">
    ///     using (var table = Lua.Evaluate&lt;LuaTable&gt;("return {}").PoolOnDispose()!)
    ///     {
    ///         // do stuff
    ///     }
    ///     <para/>
    ///     // `table` must not be used after it is disposed!
    ///     </code>
    ///     </example>
    /// </remarks>
    /// <seealso cref="LuaReferenceExtensions.PoolOnDispose{TReference}"/>
    public LuaMarshalerEntityPoolConfiguration? EntityPoolConfiguration
    {
        set
        {
            var pool = value != null
                ? new LuaReferencePool(value)
                : null;

            Interlocked.Exchange(ref _entityPool, pool);
        }
    }

    private UserDataDescriptorProvider _userDataDescriptorProvider = UserDataDescriptorProvider.Default;
    private LuaReferencePool? _entityPool;

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
    ///     Instantiates a new <see cref="LuaTable"/>.
    /// </summary>
    /// <param name="lua"> The Lua state. </param>
    /// <param name="reference"> The Lua reference. </param>
    /// <returns>
    ///     The created <see cref="LuaTable"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected LuaTable CreateTable(Lua lua, int reference)
    {
        var table = _entityPool?.RentTable() ?? new();
        table.Lua = lua;
        table.Reference = reference;
        return table;
    }

    /// <summary>
    ///     Instantiates a new <see cref="LuaFunction"/>.
    /// </summary>
    /// <param name="lua"> The Lua state. </param>
    /// <param name="reference"> The Lua reference. </param>
    /// <returns>
    ///     The created <see cref="LuaFunction"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected LuaFunction CreateFunction(Lua lua, int reference)
    {
        var function = _entityPool?.RentFunction() ?? new();
        function.Lua = lua;
        function.Reference = reference;
        return function;
    }

    /// <summary>
    ///     Instantiates a new <see cref="LuaUserData"/>.
    /// </summary>
    /// <param name="lua"> The Lua state. </param>
    /// <param name="reference"> The Lua reference. </param>
    /// <param name="pointer"> The pointer to the allocated memory of the user data. </param>
    /// <returns>
    ///     The created <see cref="LuaUserData"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected LuaUserData CreateUserData(Lua lua, int reference, IntPtr pointer)
    {
        var userData = _entityPool?.RentUserData() ?? new();
        userData.Lua = lua;
        userData.Reference = reference;
        userData.Pointer = pointer;
        return userData;
    }

    /// <summary>
    ///     Instantiates a new <see cref="LuaThread"/>.
    /// </summary>
    /// <param name="lua"> The Lua state. </param>
    /// <param name="reference"> The Lua reference. </param>
    /// <param name="L"> The Lua state pointer. </param>
    /// <returns>
    ///     The created <see cref="LuaThread"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe LuaThread CreateThread(Lua lua, int reference, lua_State* L)
    {
        var thread = _entityPool?.RentThread() ?? new();
        thread.Lua = lua;
        thread.Reference = reference;
        thread.L = L;
        return thread;
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
