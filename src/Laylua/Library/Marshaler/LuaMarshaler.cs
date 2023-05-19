using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Laylua.Moon;

namespace Laylua.Marshalling;

public abstract partial class LuaMarshaler : IDisposable
{
    /// <summary>
    ///     Gets the Lua instance of this marshaler.
    /// </summary>
    public Lua Lua { get; }

    /// <summary>
    ///     Gets the user data descriptor provider of this marshaler.
    /// </summary>
    public UserDataDescriptorProvider UserDataDescriptorProvider { get; }

    private readonly EntityPool _entityPool;
    private readonly ConcurrentStack<LuaReference> _leakedEntities;

    /// <summary>
    ///     Instantiates a new marshaler with the specified Lua instance.
    /// </summary>
    /// <param name="lua"> The Lua instance. </param>
    /// <param name="userDataDescriptorProvider"> The user data descriptor provider. </param>
    protected LuaMarshaler(Lua lua, UserDataDescriptorProvider userDataDescriptorProvider)
    {
        Lua = lua;
        UserDataDescriptorProvider = userDataDescriptorProvider;
        _entityPool = new EntityPool(lua);
        _leakedEntities = new();
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
        while (_leakedEntities.TryPop(out var reference))
        {
            reference.Dispose();

            ResetReference(reference);
        }
    }

    internal void ReturnReference(LuaReference reference)
    {
        if (LuaReference.IsAlive(reference))
        {
            _leakedEntities.Push(reference);
        }
        else
        {
            ResetReference(reference);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void ResetReference(LuaReference reference)
    {
        reference.Reset();

        if (_entityPool.Return(reference))
        {
            GC.ReRegisterForFinalize(reference);
        }
    }

    internal abstract void RemoveUserDataHandle(UserDataHandle handle);

    /// <summary>
    ///     Tries to convert the Lua value at the specified stack index
    ///     to a .NET object of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="stackIndex"> The index on the Lua stack. </param>
    /// <param name="obj"> The output object. </param>
    /// <typeparam name="T"> The type of the object. </typeparam>
    /// <returns>
    ///     <see langword="true"/> if successful.
    /// </returns>
    public abstract bool TryToObject<T>(int stackIndex, out T? obj);

    /// <summary>
    ///     Pushes the specified .NET object onto the Lua stack,
    ///     converting it to a Lua value appropriately.
    /// </summary>
    /// <remarks>
    ///     The marshaler expects space on the stack for the value to be pushed.
    ///     Use <see cref="LuaStack.EnsureFreeCapacity"/> prior to calling this method.
    /// </remarks>
    /// <param name="obj"> The object to push. </param>
    /// <typeparam name="T"> The type of the object. </typeparam>
    public abstract void PushObject<T>(T obj);

    /// <summary>
    ///     Called by <see cref="Laylua.Lua"/> when it is disposed
    ///     or by the <see cref="LuaMarshaler"/> finalizer.
    /// </summary>
    /// <param name="disposing"> <see langword="true"/> if the marshaler is being disposed. </param>
    protected virtual void Dispose(bool disposing)
    {
        DisposeLeakedReferences();
    }

    /// <summary>
    ///     Called by <see cref="Laylua.Lua"/> when it is disposed.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
