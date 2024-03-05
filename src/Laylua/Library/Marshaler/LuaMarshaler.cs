using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using Laylua.Moon;
using Qommon;

namespace Laylua.Marshaling;

public abstract partial class LuaMarshaler : IDisposable
{
    /// <summary>
    ///     Gets the Lua instance of this marshaler.
    /// </summary>
    public Lua Lua { get; }

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
    ///     Fired when a <see cref="LuaReference"/> is disposed by this marshaler.
    ///     This happens when your application does not have any references to an alive <see cref="LuaReference"/>.
    /// </summary>
    /// <remarks>
    ///     You can utilize this event to find out if your application
    ///     is failing to correctly dispose all <see cref="LuaReference"/> instances.
    ///     <para/>
    ///     Subscribed event handlers should be lightweight and must not throw exceptions.
    ///     <para/>
    ///     Subscribed handlers must not perform any Lua interactions,
    ///     as they might corrupt the Lua state.
    /// </remarks>
    public event EventHandler<LuaReferenceLeakedEventArgs>? ReferenceLeaked;

    /// <summary>
    ///     Sets the entity pool configuration determining how many <see cref="LuaReference"/> instances can be pooled.
    /// </summary>
    public LuaMarshalerEntityPoolConfiguration EntityPoolConfiguration
    {
        set
        {
            Guard.IsNotNull(value);
            Interlocked.Exchange(ref _entityPool, new LuaReferencePool(Lua, value));
        }
    }

    private UserDataDescriptorProvider _userDataDescriptorProvider = UserDataDescriptorProvider.Default;
    private LuaReferencePool _entityPool = null!;
    private readonly ConcurrentStack<LuaReference> _leakedReferences = new();

    /// <summary>
    ///     Instantiates a new marshaler with the specified Lua instance.
    /// </summary>
    protected LuaMarshaler(Lua lua)
    {
        Lua = lua;
    }

    ~LuaMarshaler()
    {
        Dispose(false);
    }

    /// <summary>
    ///     Instantiates a new <see cref="LuaTable"/>.
    /// </summary>
    /// <param name="reference"> The Lua reference. </param>
    /// <returns>
    ///     The created <see cref="LuaTable"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected LuaTable CreateTable(int reference)
    {
        DisposeLeakedReferences();
        return _entityPool.RentTable(reference);
    }

    /// <summary>
    ///     Instantiates a new <see cref="LuaFunction"/>.
    /// </summary>
    /// <param name="reference"> The Lua reference. </param>
    /// <returns>
    ///     The created <see cref="LuaFunction"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected LuaFunction CreateFunction(int reference)
    {
        DisposeLeakedReferences();
        return _entityPool.RentFunction(reference);
    }

    /// <summary>
    ///     Instantiates a new <see cref="LuaUserData"/>.
    /// </summary>
    /// <param name="reference"> The Lua reference. </param>
    /// <param name="ptr"> The pointer to the allocated memory of the user data. </param>
    /// <returns>
    ///     The created <see cref="LuaUserData"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected LuaUserData CreateUserData(int reference, IntPtr ptr)
    {
        DisposeLeakedReferences();
        return _entityPool.RentUserData(reference, ptr);
    }

    /// <summary>
    ///     Instantiates a new <see cref="LuaThread"/>.
    /// </summary>
    /// <param name="reference"> The Lua reference. </param>
    /// <param name="L"> The Lua state pointer. </param>
    /// <returns>
    ///     The created <see cref="LuaThread"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected unsafe LuaThread CreateThread(int reference, lua_State* L)
    {
        DisposeLeakedReferences();
        return _entityPool.RentThread(reference, L);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DisposeLeakedReferences()
    {
        while (_leakedReferences.TryPop(out var reference))
        {
            reference.Dispose();

            ReturnReference(reference);
        }
    }

    /// <summary>
    ///     Tries to convert the Lua value at the specified stack index
    ///     to a .NET value of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="stackIndex"> The index on the Lua stack. </param>
    /// <param name="obj"> The output value. </param>
    /// <typeparam name="T"> The type of the value. </typeparam>
    /// <returns>
    ///     <see langword="true"/> if successful.
    /// </returns>
    public abstract bool TryGetValue<T>(int stackIndex, out T? obj);

    /// <summary>
    ///     Pushes the specified .NET value onto the Lua stack,
    ///     converting it to a Lua value appropriately.
    /// </summary>
    /// <remarks>
    ///     The marshaler expects space on the stack for the value to be pushed.
    ///     Use <see cref="LuaStack.EnsureFreeCapacity"/> prior to calling this method.
    /// </remarks>
    /// <param name="obj"> The value to push. </param>
    /// <typeparam name="T"> The type of the value. </typeparam>
    public abstract void PushValue<T>(T obj);

    /// <summary>
    ///     Called by <see cref="Laylua.Lua"/> when it is disposed
    ///     or by the <see cref="LuaMarshaler"/> finalizer.
    /// </summary>
    /// <param name="disposing"> <see langword="true"/> if the marshaler is being disposed. </param>
    protected virtual void Dispose(bool disposing)
    { }

    /// <summary>
    ///     Called by <see cref="Laylua.Lua"/> when it is disposed.
    /// </summary>
    public void Dispose()
    {
        DisposeLeakedReferences();

        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
