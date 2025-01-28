using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Laylua.Moon;
using Qommon;

namespace Laylua;

/// <summary>
///     Represents a reference to a Lua object.
/// </summary>
/// <remarks>
///     If an instance of this type is not given to you via a method parameter
///     you must ensure that you dispose of it manually after you are done working with it,
///     because as long as that instance exists the Lua garbage collector will not free
///     the memory allocated by the referenced Lua object.
///     <para/>
///     The exception to this are the main <see cref="LuaThread"/> and the globals <see cref="LuaTable"/>.
///     These are always referenced by the Lua state and do not have to be disposed.
///     <para/>
///     This type is not thread-safe; operations on it are not thread-safe.
///     Ensure neither you nor Lua modify the object concurrently.
/// </remarks>
public abstract unsafe partial class LuaReference : IEquatable<LuaReference>, IDisposable
{
    internal LuaThread Lua
    {
        get
        {
            ThrowIfInvalid();
            return LuaCore;
        }
        set => LuaCore = value;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    protected internal abstract LuaThread? LuaCore { get; set; }

    internal int Reference
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _reference = value;
    }

    internal virtual bool IsDisposed
    {
        get => _reference == LUA_NOREF;
        set => _reference = value ? LUA_NOREF : LUA_REFNIL;
    }

    /// <remarks>
    ///     Ensure you dispose of all <see cref="LuaReference"/>s returned.
    ///     See the remarks on <see cref="LuaReference"/> for details.
    /// </remarks>
    private int _reference;

    private protected LuaReference()
    { }

    ~LuaReference()
    {
        if (!IsAlive(this))
            return;

        if (!LuaRegistry.IsPersistentReference(_reference))
        {
            Laylua.Lua.FromThread(Lua).PushLeakedReference(_reference);
        }

        if (Lua!.Marshaler.ReturnReference(this))
        {
            GC.ReRegisterForFinalize(this);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Reset()
    {
        IsDisposed = false;
    }

    [MemberNotNull(nameof(LuaCore))]
    protected void ThrowIfInvalid()
    {
        if (LuaCore == null)
        {
            throw new InvalidOperationException($"This {GetType().Name.SingleQuoted()} has not been initialized.");
        }

        if (IsDisposed)
        {
            throw new ObjectDisposedException(GetType().FullName, $"This {GetType().Name.SingleQuoted()} has been disposed.");
        }
    }

    /// <summary>
    ///     Clones this reference.
    /// </summary>
    /// <remarks>
    ///     By using this method you can hold onto a Lua object as long as you want.
    ///     This means that you must dispose of the cloned reference, even if the original reference
    ///     was disposed automatically by the library.
    ///     See the remarks on <see cref="LuaReference"/> for details.
    /// </remarks>
    /// <returns>
    ///     A new <see cref="LuaReference"/> to the <b>same</b> Lua object.
    /// </returns>
    protected T Clone<T>()
        where T : LuaReference
    {
        ThrowIfInvalid();

        Lua.Stack.EnsureFreeCapacity(1);

        using (Lua.Stack.SnapshotCount())
        {
            PushValue(this);
            return Lua.Stack[-1].GetValue<T>()!;
        }
    }

    /// <summary>
    ///     Creates a weak reference to the object of this reference.
    /// </summary>
    /// <returns>
    ///     The created weak reference.
    /// </returns>
    private protected LuaWeakReference<TReference> CreateWeakReference<TReference>()
        where TReference : LuaReference
    {
        ThrowIfInvalid();

        Lua.Stack.EnsureFreeCapacity(1);

        using (Lua.Stack.SnapshotCount())
        {
            PushValue(this);
            if (!LuaWeakReference.TryCreate<TReference>(Lua, -1, out var weakReference))
            {
                LuaThread.ThrowLuaException("Failed to create the weak reference.");
            }

            return weakReference;
        }
    }

    public bool Equals(LuaReference? other)
    {
        if (other == null)
            return false;

        if (!IsAlive(this) || !IsAlive(other))
            return false;

        if (_reference == other._reference)
            return true;

        Lua!.Stack.EnsureFreeCapacity(2);

        using (Lua.Stack.SnapshotCount())
        {
            PushValue(this);
            PushValue(other);
            var L = Lua.GetStatePointer();
            if (lua_rawequal(L, -2, -1))
                return true;
        }

        return false;
    }

    public sealed override bool Equals(object? obj)
    {
        if (obj is not LuaReference other)
            return false;

        return Equals(other);
    }

    public sealed override int GetHashCode()
    {
        return RuntimeHelpers.GetHashCode(this);
    }

    /// <summary>
    ///     Converts the referenced Lua object to a <see cref="string"/> representation.
    /// </summary>
    /// <returns>
    ///     A <see cref="string"/> representing the Lua object.
    /// </returns>
    public override string ToString()
    {
        if (!IsAlive(this))
            return $"<disposed {GetType().ToTypeString()}>";

        Lua.Stack.EnsureFreeCapacity(2);

        using (Lua.Stack.SnapshotCount())
        {
            PushValue(this);
            var L = Lua.GetStatePointer();
            return luaL_tostring(L, -1).ToString();
        }
    }

    /// <summary>
    ///     Disposes this <see cref="LuaReference"/>, dereferencing the Lua object.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual void Dispose()
    {
        if (LuaRegistry.IsPersistentReference(_reference))
            return;

        if (!IsAlive(this))
            return;

        var L = Lua!.GetStatePointer();
        if (L == null)
            return;

        luaL_unref(L, LuaRegistry.Index, _reference);
        if (!Lua!.Marshaler.ReturnReference(this))
        {
            GC.SuppressFinalize(this);
        }

        IsDisposed = true;
    }

    public static int GetReference(LuaReference reference)
    {
        reference.ThrowIfInvalid();

        return reference._reference;
    }

    internal static void PushValue(LuaReference reference)
    {
        reference.ThrowIfInvalid();

        var L = reference.Lua.GetStatePointer();
        if (lua_rawgeti(L, LuaRegistry.Index, reference._reference).IsNoneOrNil())
        {
            lua_pop(L);
            LuaThread.ThrowLuaException("Failed to push the referenced object.");
        }
    }

    internal static bool TryCreate(lua_State* L, int stackIndex, out int reference)
    {
        if (!lua_checkstack(L, 1))
        {
            reference = default;
            return false;
        }

        lua_pushvalue(L, stackIndex);
        reference = luaL_ref(L, LuaRegistry.Index);
        if (reference is LUA_REFNIL or LUA_NOREF)
            return false;

        return true;
    }

    internal static void ValidateOwnership(LuaThread lua, LuaReference reference)
    {
        if (lua.MainThread.State.L != reference.Lua.MainThread.State.L)
        {
            throw new InvalidOperationException($"The given {reference.GetType().Name.SingleQuoted()} is owned by a different Lua state.");
        }
    }

    internal static bool IsAlive(LuaReference reference)
    {
        var lua = reference.LuaCore;
        return lua != null && !lua.IsDisposed && !reference.IsDisposed && !lua.MainThread.IsDisposed;
    }
}
