﻿using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Laylua.Moon;
using Qommon;

namespace Laylua;

/// <summary>
///     Represents a reference to a Lua object.
///     As long as an object has any references, it will not be garbage collected by Lua.
/// </summary>
/// <remarks>
///     This type is not thread-safe; operations on it are not thread-safe.
/// </remarks>
public abstract unsafe partial class LuaReference : IEquatable<LuaReference>, IDisposable
{
    internal Lua Lua
    {
        get
        {
            ThrowIfInvalid();
            return LuaCore;
        }
        set => LuaCore = value;
    }

    internal int Reference
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => _reference = value;
    }

    internal virtual bool IsDisposed
    {
        get
        {
            if (_reference is LUA_NOREF)
                return true;

            var thread = LuaCore;
            return thread == null || thread.MainThread.IsDisposed;
        }
        set => _reference = value ? LUA_NOREF : LUA_REFNIL;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    protected abstract Lua? LuaCore { get; set; }

    private int _reference;

    private protected LuaReference()
    { }

    ~LuaReference()
    {
        if (!IsAlive(this, out var lua))
            return;

        if (!LuaRegistry.IsPersistentReference(_reference))
        {
            lua.PushLeakedReference(_reference);
        }

        if (lua.Marshaler.ReturnReference(this))
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
    internal void ThrowIfInvalid()
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
    ///     Clones this reference, creating a new reference to the same object.
    /// </summary>
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
            Lua.Stack.Push(this);
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
            Lua.Stack.Push(this);
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

        if (!IsAlive(this, out var lua) || !IsAlive(other, out var otherLua))
            return false;

        if (lua.State.L != otherLua.State.L)
            return false;

        if (_reference == other._reference)
            return true;

        lua.Stack.EnsureFreeCapacity(2);

        using (lua.Stack.SnapshotCount())
        {
            lua.Stack.Push(this);
            lua.Stack.Push(other);
            var L = lua.State.L;
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
        if (!IsAlive(this, out var lua))
            return $"<disposed {GetType().ToTypeString()}>";

        lua.Stack.EnsureFreeCapacity(2);

        using (lua.Stack.SnapshotCount())
        {
            lua.Stack.Push(this);
            var L = lua.State.L;
            return luaL_tostring(L, -1).ToString() ?? "<invalid>";
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

        if (!IsAlive(this, out var thread))
            return;

        var L = thread.State.L;
        if (L == null)
            return;

        luaL_unref(L, LuaRegistry.Index, _reference);
        if (!Lua.Marshaler.ReturnReference(this))
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

    internal static void ValidateOwnership(LuaThread thread, LuaReference reference)
    {
        if (thread.MainThread.State.L != reference.Lua.MainThread.State.L)
        {
            throw new InvalidOperationException($"The given {reference.GetType().Name.SingleQuoted()} is owned by a different Lua state.");
        }
    }

    internal static bool IsAlive(LuaReference reference, [MaybeNullWhen(false)] out Lua lua)
    {
        lua = reference.LuaCore;
        return lua != null && !lua.IsDisposed && !reference.IsDisposed;
    }
}
