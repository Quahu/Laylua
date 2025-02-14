using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Laylua.Moon;
using Qommon;

namespace Laylua;

/// <summary>
///     Represents a reference to a Lua function.
/// </summary>
/// <remarks>
///     <inheritdoc cref="LuaReference"/>
/// </remarks>
public sealed unsafe partial class LuaFunction : LuaReference
{
    /// <summary>
    ///     Gets the upvalues of this function.
    /// </summary>
    public UpvalueCollection Upvalues => new(this);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    protected override Lua? LuaCore { get; set; }

    internal LuaFunction()
    { }

    /// <inheritdoc cref="LuaReference.Clone{T}"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public LuaFunction CloneReference()
    {
        return Clone<LuaFunction>();
    }

    /// <inheritdoc cref="LuaReference.CreateWeakReference{TReference}"/>
    public LuaWeakReference<LuaFunction> CreateWeakReference()
    {
        return CreateWeakReference<LuaFunction>();
    }

    private static void PrepareForCall(LuaThread caller, LuaReference reference, int nargs)
    {
        caller.Stack.EnsureFreeCapacity(1 + nargs);

        caller.Stack.Push(reference);
    }

    private static void PrepareForCall(LuaThread caller, LuaReference reference, int nargs, out int top)
    {
        PrepareForCall(caller, reference, nargs);

        top = caller.Stack.Count - 1;
    }

    internal static void PCallCore(LuaThread caller, int nargs, int nresults)
    {
        var L = caller.State.L;
        var status = lua_pcall(L, nargs, nresults, 0);
        if (status.IsError())
        {
            LuaThread.ThrowLuaException(caller, status);
        }
    }

    internal static LuaFunctionResults PCall(LuaThread caller, int nargs, int oldTop)
    {
        PCallCore(caller, nargs, LUA_MULTRET);

        var range = LuaStackValueRange.FromTop(caller.Stack, oldTop, caller.Stack.Count);
        return new LuaFunctionResults(range);
    }

    /// <summary>
    ///     Calls (invokes) this function from the thread specified by <see cref="LuaFunctionCallParameters.Caller"/>.
    /// </summary>
    /// <seealso cref="LuaFunctionCallParameters.ArgumentCount"/>
    /// <seealso cref="LuaFunctionCallParameters.ResultCount"/>
    public void Call(in LuaFunctionCallParameters parameters)
    {
        ThrowIfInvalid();

        if (parameters.Caller != null)
        {
            ValidateOwnership(parameters.Caller, this);
        }

        Guard.IsNotNegative(parameters.ArgumentCount);

        if (parameters.ResultCount != null)
        {
            Guard.IsNotNegative(parameters.ResultCount.Value);
        }

        var caller = parameters.Caller ?? Lua;
        var nargs = parameters.ArgumentCount;
        var nresults = parameters.ResultCount ?? LUA_MULTRET;

        PrepareForCall(caller, this, nargs);
        PCallCore(caller, nargs, nresults);
    }

    /// <summary>
    ///     Calls (invokes) this function from the main thread without any arguments.
    /// </summary>
    /// <returns>
    ///     The results returned from the function.
    /// </returns>
    public LuaFunctionResults Call()
    {
        ThrowIfInvalid();

        PrepareForCall(Lua, this, nargs: 0, out var top);
        return PCall(Lua, nargs: 0, top);
    }

    /// <summary>
    ///     Calls (invokes) this function from the main thread with the specified argument.
    /// </summary>
    /// <typeparam name="T"> The type of the argument. </typeparam>
    /// <param name="argument"> The argument to pass to the function. </param>
    /// <returns>
    ///     The results returned from the function.
    /// </returns>
    public LuaFunctionResults Call<T>(T? argument)
    {
        ThrowIfInvalid();

        PrepareForCall(Lua, this, nargs: 1, out var top);
        try
        {
            Lua.Stack.Push(argument);

            return PCall(Lua, nargs: 1, top);
        }
        catch
        {
            lua_settop(Lua.State.L, top);
            throw;
        }
    }

    /// <summary>
    ///     Calls (invokes) this function from the main thread with the specified arguments.
    /// </summary>
    /// <typeparam name="T1"> The type of the first argument. </typeparam>
    /// <typeparam name="T2"> The type of the second argument. </typeparam>
    /// <param name="argument1"> The first argument to pass to the function. </param>
    /// <param name="argument2"> The second argument to pass to the function. </param>
    /// <returns>
    ///     The results returned from the function.
    /// </returns>
    public LuaFunctionResults Call<T1, T2>(T1? argument1, T2? argument2)
    {
        ThrowIfInvalid();

        PrepareForCall(Lua, this, nargs: 2, out var top);
        try
        {
            Lua.Stack.Push(argument1);
            Lua.Stack.Push(argument2);

            return PCall(Lua, nargs: 2, top);
        }
        catch
        {
            lua_settop(Lua.State.L, top);
            throw;
        }
    }

    /// <summary>
    ///     Calls (invokes) this function from the main thread with the specified arguments.
    /// </summary>
    /// <typeparam name="T1"> The type of the first argument. </typeparam>
    /// <typeparam name="T2"> The type of the second argument. </typeparam>
    /// <typeparam name="T3"> The type of the third argument. </typeparam>
    /// <param name="argument1"> The first argument to pass to the function. </param>
    /// <param name="argument2"> The second argument to pass to the function. </param>
    /// <param name="argument3"> The third argument to pass to the function. </param>
    /// <returns>
    ///     The results returned from the function.
    /// </returns>
    public LuaFunctionResults Call<T1, T2, T3>(T1? argument1, T2? argument2, T3? argument3)
    {
        ThrowIfInvalid();

        PrepareForCall(Lua, this, nargs: 3, out var top);
        try
        {
            Lua.Stack.Push(argument1);
            Lua.Stack.Push(argument2);
            Lua.Stack.Push(argument3);

            return PCall(Lua, nargs: 3, top);
        }
        catch
        {
            lua_settop(Lua.State.L, top);
            throw;
        }
    }

    /// <summary>
    ///     Calls (invokes) this function from the main thread with the specified arguments.
    /// </summary>
    /// <typeparam name="T1"> The type of the first argument. </typeparam>
    /// <typeparam name="T2"> The type of the second argument. </typeparam>
    /// <typeparam name="T3"> The type of the third argument. </typeparam>
    /// <typeparam name="T4"> The type of the fourth argument. </typeparam>
    /// <param name="argument1"> The first argument to pass to the function. </param>
    /// <param name="argument2"> The second argument to pass to the function. </param>
    /// <param name="argument3"> The third argument to pass to the function. </param>
    /// <param name="argument4"> The fourth argument to pass to the function. </param>
    /// <returns>
    ///     The results returned from the function.
    /// </returns>
    public LuaFunctionResults Call<T1, T2, T3, T4>(T1? argument1, T2? argument2, T3? argument3, T4? argument4)
    {
        ThrowIfInvalid();

        PrepareForCall(Lua, this, nargs: 4, out var top);
        try
        {
            Lua.Stack.Push(argument1);
            Lua.Stack.Push(argument2);
            Lua.Stack.Push(argument3);
            Lua.Stack.Push(argument4);

            return PCall(Lua, nargs: 4, top);
        }
        catch
        {
            lua_settop(Lua.State.L, top);
            throw;
        }
    }

    /// <summary>
    ///     Calls (invokes) this function from the main thread with the specified arguments.
    /// </summary>
    /// <param name="arguments"> The arguments to pass to the function. </param>
    /// <returns>
    ///     The results returned from the function.
    /// </returns>
    public LuaFunctionResults Call(params object?[] arguments)
    {
        return Call(arguments.AsSpan());
    }

    /// <summary>
    ///     Calls (invokes) this function from the main thread with the specified arguments.
    /// </summary>
    /// <param name="arguments"> The arguments to pass to the function. </param>
    /// <returns>
    ///     The results returned from the function.
    /// </returns>
    public LuaFunctionResults Call(params ReadOnlySpan<object?> arguments)
    {
        ThrowIfInvalid();

        var argumentCount = arguments.Length;
        PrepareForCall(Lua, this, argumentCount, out var top);
        try
        {
            foreach (var argument in arguments)
            {
                Lua.Stack.Push(argument);
            }

            return PCall(Lua, argumentCount, top);
        }
        catch
        {
            lua_settop(Lua.State.L, top);
            throw;
        }
    }

    /// <summary>
    ///     Calls (invokes) this function from the main thread with the specified arguments.
    /// </summary>
    /// <param name="arguments"> The arguments to pass to the function. </param>
    /// <returns>
    ///     The results returned from the function.
    /// </returns>
    public LuaFunctionResults Call(params LuaStackValue[] arguments)
    {
        return Call(arguments.AsSpan());
    }

    /// <summary>
    ///     Calls (invokes) this function from the main thread with the specified arguments.
    /// </summary>
    /// <param name="arguments"> The arguments to pass to the function. </param>
    /// <returns>
    ///     The results returned from the function.
    /// </returns>
    public LuaFunctionResults Call(params ReadOnlySpan<LuaStackValue> arguments)
    {
        ThrowIfInvalid();

        var argumentCount = arguments.Length;

        PrepareForCall(Lua, this, argumentCount, out var top);

        try
        {
            foreach (var argument in arguments)
            {
                Lua.Stack.Push(argument);
            }

            return PCall(Lua, argumentCount, top);
        }
        catch
        {
            lua_settop(Lua.State.L, top);
            throw;
        }
    }

    /// <summary>
    ///     Calls (invokes) this function from the main thread with the specified arguments.
    /// </summary>
    /// <param name="arguments"> The arguments to pass to the function. </param>
    /// <returns>
    ///     The results returned from the function.
    /// </returns>
    public LuaFunctionResults Call(LuaStackValueRange arguments)
    {
        ThrowIfInvalid();

        var argumentCount = arguments.Count;

        PrepareForCall(Lua, this, argumentCount, out var top);

        try
        {
            foreach (var argument in arguments)
            {
                Lua.Stack.Push(argument);
            }

            return PCall(Lua, argumentCount, top);
        }
        catch
        {
            lua_settop(Lua.State.L, top);
            throw;
        }
    }
}
