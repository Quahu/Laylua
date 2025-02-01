using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Laylua;

/// <summary>
///     Represents a reference to a Lua user data.
/// </summary>
/// <remarks>
///     <inheritdoc cref="LuaReference"/>
/// </remarks>
public sealed unsafe class LuaUserData : LuaTable
{
    /// <summary>
    ///     Gets the pointer to the allocated memory of this user data.
    /// </summary>
    public IntPtr Pointer
    {
        get => _ptr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal set
        {
            Debug.Assert(_ptr == default);
            _ptr = value;
        }
    }

    /// <summary>
    ///     Gets the size of the allocated memory of this user data.
    /// </summary>
    public lua_Unsigned Size
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        get
        {
            ThrowIfInvalid();

            Thread.Stack.EnsureFreeCapacity(1);

            using (Thread.Stack.SnapshotCount())
            {
                Thread.Stack.Push(this);
                var L = Thread.State.L;
                return lua_rawlen(L, -1);
            }
        }
    }

    private IntPtr _ptr;

    internal LuaUserData()
    { }

    /// <inheritdoc cref="LuaReference.Clone{T}"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public override LuaUserData Clone()
    {
        return Clone<LuaUserData>();
    }

    /// <inheritdoc cref="LuaReference.CreateWeakReference{TReference}"/>
    public new LuaWeakReference<LuaUserData> CreateWeakReference()
    {
        return CreateWeakReference<LuaUserData>();
    }
}
