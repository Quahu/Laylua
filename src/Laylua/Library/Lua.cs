using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Laylua.Marshalling;
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
    ///     Gets the culture of this instance.
    /// </summary>
    /// <remarks>
    ///     This can be used to determine conversion and comparison behavior of values.
    ///     <br/>
    ///     Defaults to <see cref="CultureInfo.CurrentCulture"/> if not specified through the constructor.
    /// </remarks>
    public CultureInfo Culture { get; }

    /// <summary>
    ///     Gets the comparer of this instance pre-configured with the <see cref="Culture"/>.
    /// </summary>
    public LuaComparer Comparer { get; }

    /// <summary>
    ///     Gets the main Lua thread.
    /// </summary>
    /// <remarks>
    ///     The returned object does not have to be disposed.
    /// </remarks>
    public LuaThread MainThread => _mainThread ??= LuaThread.CreateMainThread(this);

    /// <summary>
    ///     Gets the table containing the global variables.
    /// </summary>
    /// <remarks>
    ///     The returned object does not have to be disposed.
    /// </remarks>
    public LuaTable Globals => _globals ??= LuaTable.CreateGlobalsTable(this);

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

    private LuaThread? _mainThread;
    private LuaTable? _globals;
    private readonly List<ILuaLibrary> _openLibraries = new();

    public Lua()
        : this(CultureInfo.CurrentCulture)
    { }

    public Lua(LuaAllocator allocator)
        : this(CultureInfo.CurrentCulture, new LuaState(allocator))
    { }

    public Lua(CultureInfo culture)
        : this(culture, new LuaState())
    { }

    public Lua(CultureInfo culture, LuaAllocator allocator)
        : this(culture, new LuaState(allocator))
    { }

    public Lua(CultureInfo culture, LuaState state)
        : this(culture, state, LuaMarshalerProvider.Default)
    { }

    public Lua(
        CultureInfo culture,
        LuaState state,
        LuaMarshalerProvider marshalerProvider)
    {
        Culture = culture;
        Stack = new LuaStack(this);
        State = state;
        State.State = this;
        Marshaler = marshalerProvider.GetMarshaler(this);
        Comparer = new LuaComparer(Culture);
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
            if (!lua_rawgetglobal(L, name).IsNoneOrNil() && Marshaler.TryToObject(-1, out value))
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
                throw new KeyNotFoundException();

            return Marshaler.ToObject<TValue>(-1)!;
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
            Marshaler.PushObject(value);
            var L = this.GetStatePointer();
            lua_rawsetglobal(L, name);
        }
    }

    public bool OpenLibrary(ILuaLibrary library)
    {
        foreach (var openlibrary in _openLibraries)
        {
            if (openlibrary.Name == library.Name)
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
            if (openlibrary.Name == libraryName)
            {
                openlibrary.Close(this);
                _openLibraries.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public T? Evaluate<T>(string code)
    {
        return Evaluate<T>(code.AsSpan());
    }

    public T? Evaluate<T>(ReadOnlySpan<char> code)
    {
        using (var results = Evaluate(code))
        {
            if (results.Count == 0)
            {
                Throw.InvalidOperationException("The evaluation returned no results.");
            }

            return results.First.GetValue<T>();
        }
    }

    public LuaFunctionResults Evaluate(string code)
    {
        return Evaluate(code.AsSpan());
    }

    public LuaFunctionResults Evaluate(ReadOnlySpan<char> code)
    {
        var top = Stack.Count;
        LoadString(code);
        return LuaFunction.PCall(this, top, 0);
    }

    public void Execute(string code)
    {
        Execute(code.AsSpan());
    }

    public void Execute(ReadOnlySpan<char> code)
    {
        var L = State.L;
        var oldTop = lua_gettop(L);
        LoadString(code);
        var status = lua_pcall(L, 0, LUA_MULTRET, 0);
        var newTop = lua_gettop(L);
        try
        {
            if (status.IsError())
            {
                throw new LuaException(this, status);
            }
        }
        finally
        {
            if (oldTop != newTop)
                lua_settop(L, oldTop);
        }
    }

    public LuaFunction Compile(string code)
    {
        return Compile(code.AsSpan());
    }

    public LuaFunction Compile(ReadOnlySpan<char> code)
    {
        LoadString(code);
        return Marshaler.PopObject<LuaFunction>()!;
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
            return Marshaler.PopObject<LuaTable>()!;
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
            return Marshaler.PopObject<LuaUserData>()!;
        }
    }

    private void LoadString(ReadOnlySpan<char> code)
    {
        Stack.EnsureFreeCapacity(1);

        using (var bytes = RentedArray<byte>.Rent(Encoding.UTF8.GetByteCount(code)))
        {
            Encoding.UTF8.GetBytes(code, bytes);
            LuaStatus status;
            fixed (byte* bytesPtr = bytes)
            {
                status = luaL_loadbuffer(State.L, bytesPtr, (nuint) bytes.Length, Id);
            }

            if (status.IsError())
            {
                throw new LuaException(this, status);
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
        if (State.IsDisposed)
            return;

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
