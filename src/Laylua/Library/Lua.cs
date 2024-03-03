using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Laylua.Marshaling;
using Laylua.Moon;
using Qommon;
using Qommon.Pooling;

namespace Laylua;

/// <summary>
///     Represents a high-level Lua state.
/// </summary>
public unsafe class Lua : IDisposable, ISpanFormattable
{
    /// <summary>
    ///     Gets the ID of this instance.
    /// </summary>
    /// <remarks>
    ///     This is purely for developer use.
    /// </remarks>
    public string? Id { get; set; }

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

    private readonly List<LuaLibrary> _openLibraries = new();

    public Lua()
        : this(new LuaState())
    { }

    public Lua(LuaAllocator allocator)
        : this(new LuaState(allocator))
    { }

    public Lua(LuaState state)
        : this(state, LuaMarshalerProvider.Default)
    { }

    public Lua(
        LuaState state,
        LuaMarshalerProvider marshalerProvider)
    {
        Stack = new LuaStack(this);
        State = state;
        State.State = this;
        Marshaler = marshalerProvider.Create(this);

        MainThread = LuaThread.CreateMainThread(this);
        Globals = LuaTable.CreateGlobalsTable(this);
    }

    ~Lua()
    {
        Dispose(false);
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
        using (Stack.SnapshotCount())
        {
            var L = this.GetStatePointer();
            if (!lua_rawgetglobal(L, name).IsNoneOrNil() && Marshaler.TryGetValue(-1, out value))
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
        using (Stack.SnapshotCount())
        {
            var L = this.GetStatePointer();
            if (lua_rawgetglobal(L, name).IsNoneOrNil())
            {
                Throw.KeyNotFoundException();
            }

            return Marshaler.GetValue<TValue>(-1)!;
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

        using (Stack.SnapshotCount())
        {
            Marshaler.PushValue(value);
            var L = this.GetStatePointer();
            lua_rawsetglobal(L, name);
        }
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

    public T? Evaluate<T>(string code, string? chunkName = null)
    {
        return Evaluate<T>(code.AsSpan(), chunkName.AsSpan());
    }

    public T? Evaluate<T>(ReadOnlySpan<char> code, ReadOnlySpan<char> chunkName = default)
    {
        using (var results = Evaluate(code, chunkName))
        {
            if (results.Count == 0)
            {
                Throw.InvalidOperationException("The evaluation returned no results.");
            }

            return results.First.GetValue<T>();
        }
    }

    public LuaFunctionResults Evaluate(string code, string? chunkName = null)
    {
        return Evaluate(code.AsSpan(), chunkName.AsSpan());
    }

    public LuaFunctionResults Evaluate(ReadOnlySpan<char> code, ReadOnlySpan<char> chunkName = default)
    {
        var top = Stack.Count;
        LoadString(code, chunkName);
        return LuaFunction.PCall(this, top, 0);
    }

    public void Execute(string code, string? chunkName = null)
    {
        Execute(code.AsSpan(), chunkName.AsSpan());
    }

    public void Execute(ReadOnlySpan<char> code, ReadOnlySpan<char> chunkName = default)
    {
        var L = State.L;
        LoadString(code, chunkName);
        var status = lua_pcall(L, 0, 0, 0);
        if (status.IsError())
        {
            ThrowLuaException(status);
        }
    }

    public LuaFunction Compile(string code, string? chunkName = null)
    {
        return Compile(code.AsSpan(), chunkName.AsSpan());
    }

    public LuaFunction Compile(ReadOnlySpan<char> code, ReadOnlySpan<char> chunkName = default)
    {
        LoadString(code, chunkName);
        return Marshaler.PopValue<LuaFunction>()!;
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

        using (Stack.SnapshotCount())
        {
            var L = this.GetStatePointer();
            lua_createtable(L, sequenceCapacity, tableCapacity);
            return Marshaler.PopValue<LuaTable>()!;
        }
    }

    /// <summary>
    ///     Instantiates a new <see cref="LuaUserData"/>, referencing it in the Lua registry.
    /// </summary>
    /// <remarks>
    ///     Note that this creates a native Lua user data,
    ///     which probably won't be very useful for your application
    ///     and you should instead let the marshaler handle user data creation
    ///     for .NET objects.
    /// </remarks>
    /// <param name="size"> The size of the memory to allocate for this user data. </param>
    /// <param name="userValueCount"> The amount of additional user values to be stored alongside this user data. </param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public LuaUserData CreateUserData(nuint size, int userValueCount = 0)
    {
        Stack.EnsureFreeCapacity(1);

        using (Stack.SnapshotCount())
        {
            var L = this.GetStatePointer();
            _ = lua_newuserdatauv(L, size, userValueCount);
            return Marshaler.PopValue<LuaUserData>()!;
        }
    }

    private void LoadString(ReadOnlySpan<char> code, ReadOnlySpan<char> chunkName)
    {
        Stack.EnsureFreeCapacity(1);

        using (var bytes = RentedArray<byte>.Rent(Encoding.UTF8.GetByteCount(code)))
        {
            Encoding.UTF8.GetBytes(code, bytes);
            LuaStatus status;
            fixed (byte* bytesPtr = bytes)
            {
                status = luaL_loadbuffer(State.L, bytesPtr, (nuint) bytes.Length, chunkName);
            }

            if (status.IsError())
            {
                ThrowLuaException(status);
            }
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

    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed)
            return;

        Marshaler.Dispose();
        State.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
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
