using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Laylua.Marshaling;
using Laylua.Moon;
using Qommon;

namespace Laylua;

/// <summary>
///     Represents a reference to a Lua thread.
/// </summary>
/// <remarks>
///     <inheritdoc cref="LuaReference"/>
/// </remarks>
public abstract unsafe partial class LuaThread : LuaReference
{
    /// <summary>
    ///     Gets the low-level state of this thread.
    /// </summary>
    public LuaState State { get; protected set; } = null!;

    /// <summary>
    ///     Gets the Lua stack of this thread.
    /// </summary>
    /// <remarks>
    ///     <inheritdoc cref="LuaStack"/>
    /// </remarks>
    public LuaStack Stack { get; protected set; } = null!;

    /// <summary>
    ///     Gets the main Lua thread, i.e. the parent of this thread.
    /// </summary>
    public abstract LuaThread MainThread { get; }

    /// <summary>
    ///     Gets the table containing the global variables.
    /// </summary>
    /// <remarks>
    ///     The returned object does not have to be disposed.
    /// </remarks>
    public abstract LuaTable Globals { get; }

    /// <summary>
    ///     Gets the status of this thread.
    ///     For the main thread, this will always return <see cref="LuaStatus.Ok"/>.
    /// </summary>
    public LuaStatus Status
    {
        get
        {
            ThrowIfInvalid();
            return lua_status(State.L);
        }
    }

    /// <summary>
    ///     Gets or sets the value of the global variable with the specified name.
    /// </summary>
    /// <remarks>
    ///     For type safety (especially so you can dispose any <see cref="LuaReference"/>s returned)
    ///     and for performance reasons you should prefer the generic methods, i.e.
    ///     <see cref="TryGetGlobal{TValue}"/> and <see cref="SetGlobal{TValue}"/>.
    /// </remarks>
    /// <param name="name"> The name of the global variable. </param>
    public object? this[string name]
    {
        get => GetGlobal<object>(name);
        set => SetGlobal(name, value);
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    protected override LuaThread? ThreadCore
    {
        get => this;
        set => throw new NotSupportedException();
    }

    internal abstract LuaMarshaler Marshaler { get; }

    internal LuaThread()
    { }

    [DoesNotReturn]
    internal static void ThrowLuaException(LuaThread thread)
    {
        throw LuaException.ConstructFromStack(thread);
    }

    [DoesNotReturn]
    internal static void ThrowLuaException(LuaThread thread, LuaStatus status)
    {
        throw LuaException.ConstructFromStack(thread).WithStatus(status);
    }

    [DoesNotReturn]
    internal static void ThrowLuaException(string message)
    {
        throw new LuaException(message);
    }

    /// <summary>
    ///     Raises a Lua error.
    /// </summary>
    /// <remarks>
    ///     This should be used to propagate errors
    ///     from .NET to Lua.
    ///     <para/>
    ///     For example, if Lua calls a .NET method
    ///     with unexpected arguments, this method can be used
    ///     to indicate the method was called incorrectly.
    /// </remarks>
    /// <param name="message"> The error message. </param>
    /// <returns>
    ///     This method does not actually return.
    /// </returns>
    /// <exception cref="LuaPanicException">
    ///     The exception thrown if the caller was not called
    ///     from a protected Lua context.
    /// </exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public int RaiseError(ReadOnlySpan<char> message)
    {
        ThrowIfInvalid();

        return luaL_error(State.L, message);
    }

    /// <inheritdoc cref="RaiseError(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public int RaiseError(string message)
    {
        ThrowIfInvalid();

        return luaL_error(State.L, message);
    }

    /// <summary>
    ///     Raises a Lua error using an exception.
    /// </summary>
    /// <remarks>
    ///     <inheritdoc cref="RaiseError(System.ReadOnlySpan{char})"/>
    /// </remarks>
    /// <param name="exception"> The inner exception to propagate. </param>
    /// <returns>
    ///     This method does not actually return.
    /// </returns>
    /// <exception cref="LuaPanicException">
    ///     The exception thrown if the caller was not called
    ///     from a protected Lua context.
    /// </exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public int RaiseError(Exception exception)
    {
        return RaiseError(null, exception);
    }

    /// <summary>
    ///     Raises a Lua error using an exception.
    /// </summary>
    /// <remarks>
    ///     <inheritdoc cref="RaiseError(System.ReadOnlySpan{char})"/>
    /// </remarks>
    /// <param name="message"> The error message. </param>
    /// <param name="exception"> The inner exception to propagate. </param>
    /// <returns>
    ///     This method does not actually return.
    /// </returns>
    /// <exception cref="LuaPanicException">
    ///     The exception thrown if the caller was not called
    ///     from a protected Lua context.
    /// </exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public int RaiseError(string? message, Exception exception)
    {
        ThrowIfInvalid();

        return LuaException.RaiseErrorInfo(State.L, message, exception);
    }

    /// <inheritdoc cref="RaiseError(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public int RaiseArgumentError(int argumentIndex, ReadOnlySpan<char> extraMessage = default)
    {
        ThrowIfInvalid();

        return luaL_argerror(State.L, argumentIndex, extraMessage);
    }

    /// <inheritdoc cref="RaiseError(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public int RaiseArgumentError(int argumentIndex, string? extraMessage = default)
    {
        ThrowIfInvalid();

        return luaL_argerror(State.L, argumentIndex, extraMessage);
    }

    /// <inheritdoc cref="RaiseError(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public int RaiseArgumentTypeError(int argumentIndex, ReadOnlySpan<char> typeName)
    {
        ThrowIfInvalid();

        return luaL_typeerror(State.L, argumentIndex, typeName);
    }

    /// <inheritdoc cref="RaiseError(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public int RaiseArgumentTypeError(int argumentIndex, string typeName)
    {
        ThrowIfInvalid();

        return luaL_typeerror(State.L, argumentIndex, typeName);
    }

    /// <inheritdoc cref="EmitWarning(ReadOnlySpan{char})"/>
    public void EmitWarning(string? message)
    {
        EmitWarning(message.AsSpan());
    }

    /// <summary>
    ///     Emits a Lua warning that can fire <see cref="WarningEmitted"/>. <br/>
    ///     See <a href="https://www.lua.org/manual/5.4/manual.html#2.3">Error Handling (Lua manual)</a> and
    ///     <a href="https://www.lua.org/manual/5.4/manual.html#pdf-warn"><c>warn (msg1, ···) (Lua Manual)</c></a> for more information about warnings.
    /// </summary>
    /// <param name="message"> The warning message. </param>
    public void EmitWarning(ReadOnlySpan<char> message)
    {
        lua_warning(State.L, message, false);
    }

    /// <summary>
    ///     Gets the value of a global variable.
    /// </summary>
    /// <remarks>
    ///     <inheritdoc cref="LuaReference._reference"/>
    ///     <para/>
    ///     This method uses raw table access.
    /// </remarks>
    /// <param name="name"> The name of the global variable. </param>
    /// <param name="value"> The result out value. </param>
    /// <typeparam name="TValue"> The type of the value. </typeparam>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool TryGetGlobal<TValue>(ReadOnlySpan<char> name, [MaybeNullWhen(false)] out TValue value)
        where TValue : notnull
    {
        ThrowIfInvalid();

        var L = State.L;
        using (Stack.SnapshotCount())
        {
            if (!lua_rawgetglobal(L, name).IsNoneOrNil() && Stack[-1].TryGetValue(out value))
            {
                Debug.Assert(value != null);
                return true;
            }

            value = default;
            return false;
        }
    }

    /// <summary>
    ///     Gets the value of a global variable.
    /// </summary>
    /// <remarks>
    ///     <inheritdoc cref="LuaReference._reference"/>
    ///     <para/>
    ///     This method uses raw table access.
    /// </remarks>
    /// <param name="name"> The name of the global variable. </param>
    /// <typeparam name="TValue"> The type of the value. </typeparam>
    /// <exception cref="KeyNotFoundException">
    ///     Thrown when no global with the specified name is set.
    /// </exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public TValue GetGlobal<TValue>(ReadOnlySpan<char> name)
        where TValue : notnull
    {
        ThrowIfInvalid();

        var L = State.L;
        using (Stack.SnapshotCount())
        {
            if (lua_rawgetglobal(L, name).IsNoneOrNil())
            {
                Throw.KeyNotFoundException();
            }

            return Stack[-1].GetValue<TValue>()!;
        }
    }

    /// <summary>
    ///     Sets the value of a global variable.
    /// </summary>
    /// <param name="name"> The name of the global variable. </param>
    /// <param name="value"> The value to set. </param>
    /// <typeparam name="TValue"> The type of the value. </typeparam>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void SetGlobal<TValue>(ReadOnlySpan<char> name, TValue? value)
    {
        ThrowIfInvalid();

        Stack.EnsureFreeCapacity(1);

        var L = State.L;
        using (Stack.SnapshotCount())
        {
            Stack.Push(value);
            lua_rawsetglobal(L, name);
        }
    }

    /// <summary>
    ///     Instantiates a new <see cref="LuaTable"/>, referencing it in the Lua registry.
    /// </summary>
    /// <param name="sequenceCapacity"> The size hint for how many items keyed with integers the table will hold. </param>
    /// <param name="tableCapacity"> The size hint for how many items keyed with non-integers the table will hold. </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public LuaTable CreateTable(int sequenceCapacity = 0, int tableCapacity = 0)
    {
        ThrowIfInvalid();

        Stack.EnsureFreeCapacity(1);

        var L = State.L;
        using (Stack.SnapshotCount())
        {
            lua_createtable(L, sequenceCapacity, tableCapacity);
            return Stack[-1].GetValue<LuaTable>()!;
        }
    }

    /// <summary>
    ///     Instantiates a new <see cref="LuaUserData"/>, referencing it in the Lua registry.
    /// </summary>
    /// <remarks>
    ///     Note that this creates a native Lua user data,
    ///     which may not be particularly useful for your application.
    ///     Instead, it is recommended to let the marshaler handle user data creation
    ///     for .NET objects.
    /// </remarks>
    /// <param name="size"> The size of the memory to allocate for this user data. </param>
    /// <param name="userValueCount"> The amount of additional user values to be stored alongside this user data. </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public LuaUserData CreateUserData(nuint size, int userValueCount = 0)
    {
        ThrowIfInvalid();

        Stack.EnsureFreeCapacity(1);

        var L = State.L;
        using (Stack.SnapshotCount())
        {
            _ = lua_newuserdatauv(L, size, userValueCount);
            return Stack[-1].GetValue<LuaUserData>()!;
        }
    }

    /// <summary>
    ///     Creates a child Lua thread.
    ///     <para/>
    ///     See <a href="https://www.lua.org/manual/5.4/manual.html#2.1">Values and Types (Lua manual)</a>
    ///     for more information on threads.
    /// </summary>
    /// <returns>
    ///     The created thread.
    /// </returns>
    public LuaThread CreateThread()
    {
        ThrowIfInvalid();

        Stack.EnsureFreeCapacity(1);

        var L = State.L;
        using (Stack.SnapshotCount())
        {
            var L1 = lua_newthread(L);
            if (L1 == null)
            {
                ThrowLuaException("Failed to create the Lua thread.");
                throw null!;
            }

            return Stack[-1].GetValue<LuaThread>()!;
        }
    }

    public static LuaThread FromExtraSpace(lua_State* L)
    {
        var lua = LayluaState.FromExtraSpace(L).State as Lua;
        if (lua == null)
        {
            luaL_error(L, "Laylua is not attached to this Lua state.");
        }

        var G = lua.State.L;
        if (L == G)
        {
            return lua.MainThread;
        }

        var top = lua_gettop(L);
        try
        {
            lua_pushthread(L);
            lua_xmove(L, G, 1);
            try
            {
                return lua.Stack[-1].GetValue<LuaThread>()!;
            }
            finally
            {
                lua_pop(G);
            }
        }
        finally
        {
            lua_settop(L, top);
        }
    }
}
