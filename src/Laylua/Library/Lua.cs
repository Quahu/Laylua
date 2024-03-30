using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using Laylua.Marshaling;
using Laylua.Moon;
using Qommon;

namespace Laylua;

/// <summary>
///     Represents a high-level Lua state.
/// </summary>
/// <remarks>
///     This type is not thread-safe; operations on it are not thread-safe.
/// </remarks>
public sealed unsafe partial class Lua : IDisposable, ISpanFormattable
{
    /// <summary>
    ///     Gets the stack of this instance.
    /// </summary>
    /// <remarks>
    ///     <inheritdoc cref="LuaStack"/>
    /// </remarks>
    public LuaStack Stack { get; }

    /// <summary>
    ///     Gets the low-level state of this instance.
    /// </summary>
    public LuaState State { get; }

    /// <summary>
    ///     Gets the marshaler of this instance.
    /// </summary>
    public LuaMarshaler Marshaler { get; }

    /// <summary>
    ///     Gets or sets the format provider of this instance.
    /// </summary>
    /// <remarks>
    ///     This is used to determine how conversion and comparison of values is performed.
    ///     <br/>
    ///     Defaults to <see cref="CultureInfo.CurrentCulture"/>.
    /// </remarks>
    public IFormatProvider FormatProvider { get; set; } = CultureInfo.CurrentCulture;

    /// <summary>
    ///     Gets the main Lua thread.
    /// </summary>
    /// <remarks>
    ///     The returned object does not have to be disposed.
    /// </remarks>
    public LuaThread MainThread { get; }

    /// <summary>
    ///     Gets the table containing the global variables.
    /// </summary>
    /// <remarks>
    ///     The returned object does not have to be disposed.
    /// </remarks>
    public LuaTable Globals { get; }

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

    /// <summary>
    ///     Gets whether this instance is disposed.
    /// </summary>
    public bool IsDisposed => State.IsDisposed;

    private readonly List<LuaLibrary> _openLibraries;

    public Lua()
        : this(LuaMarshaler.Default)
    { }

    public Lua(LuaMarshaler marshaler)
        : this(new LuaState(), marshaler)
    { }

    public Lua(LuaAllocator allocator)
        : this(allocator, LuaMarshaler.Default)
    { }

    public Lua(LuaAllocator allocator, LuaMarshaler marshaler)
        : this(new LuaState(allocator), marshaler)
    { }

    private Lua(
        LuaState state,
        LuaMarshaler marshaler)
    {
        Stack = new LuaStack(this);
        State = state;
        State.State = this;
        Marshaler = marshaler;

        _openLibraries = new List<LuaLibrary>();
        MainThread = LuaThread.CreateMainThread(this);
        Globals = LuaTable.CreateGlobalsTable(this);
    }

    private Lua(
        Lua parent,
        lua_State* L,
        int threadReference)
    {
        Stack = new LuaStack(this);
        State = new LuaState(L, threadReference);
        Marshaler = parent.Marshaler;
        FormatProvider = parent.FormatProvider;

        _openLibraries = parent._openLibraries;
        MainThread = parent.MainThread;
        Globals = parent.Globals;
    }

    public bool OpenLibrary(LuaLibrary library)
    {
        foreach (var openlibrary in _openLibraries)
        {
            if (string.Equals(openlibrary.Name, library.Name, StringComparison.Ordinal))
                return false;
        }

        library.Open(this, false);
        _openLibraries.Add(library);
        return true;
    }

    public bool CloseLibrary(string libraryName)
    {
        for (var i = 0; i < _openLibraries.Count; i++)
        {
            var openlibrary = _openLibraries[i];
            if (string.Equals(openlibrary.Name, libraryName, StringComparison.Ordinal))
            {
                openlibrary.Close(this);
                _openLibraries.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    [DoesNotReturn]
    internal void ThrowLuaException()
    {
        throw new LuaException(this);
    }

    [DoesNotReturn]
    internal void ThrowLuaException(LuaStatus status)
    {
        throw new LuaException(this, status);
    }

    [DoesNotReturn]
    internal void ThrowLuaException(string message)
    {
        throw new LuaException(this, message);
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
        return luaL_error(State.L, message);
    }

    /// <inheritdoc cref="RaiseError(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public int RaiseError(string message)
    {
        return luaL_error(State.L, message);
    }

    /// <inheritdoc cref="RaiseError(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public int RaiseArgumentError(int argumentIndex, ReadOnlySpan<char> extraMessage = default)
    {
        return luaL_argerror(State.L, argumentIndex, extraMessage);
    }

    /// <inheritdoc cref="RaiseError(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public int RaiseArgumentError(int argumentIndex, string? extraMessage = default)
    {
        return luaL_argerror(State.L, argumentIndex, extraMessage);
    }

    /// <inheritdoc cref="RaiseError(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public int RaiseArgumentTypeError(int argumentIndex, ReadOnlySpan<char> typeName)
    {
        return luaL_typeerror(State.L, argumentIndex, typeName);
    }

    /// <inheritdoc cref="RaiseError(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    public int RaiseArgumentTypeError(int argumentIndex, string typeName)
    {
        return luaL_typeerror(State.L, argumentIndex, typeName);
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
        var L = this.GetStatePointer();
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
        var L = this.GetStatePointer();
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
        Stack.EnsureFreeCapacity(1);

        var L = this.GetStatePointer();
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
        Stack.EnsureFreeCapacity(1);

        var L = this.GetStatePointer();
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
        Stack.EnsureFreeCapacity(1);

        var L = this.GetStatePointer();
        using (Stack.SnapshotCount())
        {
            _ = lua_newuserdatauv(L, size, userValueCount);
            return Stack[-1].GetValue<LuaUserData>()!;
        }
    }

    /// <summary>
    ///     Gets the thread of this Lua state.
    /// </summary>
    /// <remarks>
    ///     If this instance is not a state created from another existing state,
    ///     the returned object can be the same as <see cref="MainThread"/>.
    /// </remarks>
    /// <returns>
    ///     The thread for this instance.
    /// </returns>
    public LuaThread GetThread()
    {
        var L = this.GetStatePointer();
        if (L == MainThread.L)
        {
            return MainThread;
        }

        Stack.EnsureFreeCapacity(1);

        using (Stack.SnapshotCount())
        {
            _ = lua_pushthread(L);
            var thread = Stack[-1].GetValue<LuaThread>();
            if (thread == null)
            {
                ThrowLuaException("Failed to get the Lua thread.");
            }

            return thread;
        }
    }

    /// <summary>
    ///     Creates a thread from this Lua state.
    /// </summary>
    /// <returns>
    ///     The created thread.
    /// </returns>
    public Lua CreateThread()
    {
        Stack.EnsureFreeCapacity(1);

        var L = this.GetStatePointer();
        using (Stack.SnapshotCount())
        {
            var L1 = lua_newthread(L);
            if (L1 == null || !LuaReference.TryCreate(L, -1, out var threadReference))
            {
                ThrowLuaException("Failed to create the Lua thread.");
                throw null!;
            }

            return new Lua(this, L1, threadReference);
        }
    }

    public override string ToString()
    {
        return ToString(null, null);
    }

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return State.ToString(format, formatProvider);
    }

    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        return (State as ISpanFormattable).TryFormat(destination, out charsWritten, format, provider);
    }

    public void Dispose()
    {
        if (IsDisposed)
            return;

        try
        {
            Marshaler.OnLuaDisposing(this);
        }
        finally
        {
            State.Close();
        }
    }

    public static Lua FromExtraSpace(lua_State* L)
    {
        var lua = LayluaState.FromExtraSpace(L).State as Lua;
        if (lua == null)
        {
            luaL_error(L, "Laylua is not attached to this Lua state.");
        }

        return lua;
    }
}
