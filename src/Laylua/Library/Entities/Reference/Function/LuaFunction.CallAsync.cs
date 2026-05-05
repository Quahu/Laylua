using System;
using System.Threading;
using System.Threading.Tasks;

namespace Laylua;

public sealed unsafe partial class LuaFunction
{
    /// <summary>
    ///     Invokes this Lua function with support for awaitables (e.g., <see cref="Task"/>).
    /// </summary>
    /// <param name="options"> Configures async behavior for this invocation. </param>
    /// <param name="cancellationToken"> The cancellation token to observe. </param>
    /// <returns>
    ///     The Lua results produced by the function call.
    /// </returns>
    /// <remarks>
    ///     This API does not make Lua execution concurrent. The owning Lua state still executes on one thread at a time.
    /// </remarks>
    public ValueTask<LuaFunctionResults> CallAsync(LuaAsyncOptions options = default, CancellationToken cancellationToken = default)
    {
        return CallAsync(Array.Empty<object?>(), options, cancellationToken);
    }

    /// <inheritdoc cref="CallAsync(LuaAsyncOptions,CancellationToken)"/>
    /// <param name="arguments"> The arguments to pass to the function. </param>
    /// <param name="cancellationToken"> The cancellation token to observe. </param>
    public ValueTask<LuaFunctionResults> CallAsync(object?[] arguments, LuaAsyncOptions options = default, CancellationToken cancellationToken = default)
    {
        ThrowIfInvalid();

        return new ValueTask<LuaFunctionResults>(LuaThread.RunFunctionAsyncCore(this, arguments, options, cancellationToken));
    }

    /// <inheritdoc cref="CallAsync(object?[],LuaAsyncOptions,CancellationToken)"/>
    public ValueTask<LuaFunctionResults> CallAsync<T>(T? argument, LuaAsyncOptions options = default, CancellationToken cancellationToken = default)
    {
        return CallAsync(
            [
                argument
            ],
            options,
            cancellationToken);
    }

    /// <inheritdoc cref="CallAsync(object?[],LuaAsyncOptions,CancellationToken)"/>
    public ValueTask<LuaFunctionResults> CallAsync<T1, T2>(T1? argument1, T2? argument2, LuaAsyncOptions options = default, CancellationToken cancellationToken = default)
    {
        return CallAsync(
            [
                argument1,
                argument2
            ],
            options,
            cancellationToken);
    }

    /// <inheritdoc cref="CallAsync(object?[],LuaAsyncOptions,CancellationToken)"/>
    public ValueTask<LuaFunctionResults> CallAsync<T1, T2, T3>(T1? argument1, T2? argument2, T3? argument3, LuaAsyncOptions options = default, CancellationToken cancellationToken = default)
    {
        return CallAsync(
            [
                argument1,
                argument2,
                argument3
            ],
            options,
            cancellationToken);
    }

    /// <inheritdoc cref="CallAsync(object?[],LuaAsyncOptions,CancellationToken)"/>
    public ValueTask<LuaFunctionResults> CallAsync<T1, T2, T3, T4>(T1? argument1, T2? argument2, T3? argument3, T4? argument4, LuaAsyncOptions options = default, CancellationToken cancellationToken = default)
    {
        return CallAsync(
            [
                argument1,
                argument2,
                argument3,
                argument4
            ],
            options,
            cancellationToken);
    }

    /// <inheritdoc cref="CallAsync(object?[],LuaAsyncOptions,CancellationToken)"/>
    public ValueTask<LuaFunctionResults> CallAsync(LuaStackValue[] arguments, LuaAsyncOptions options = default, CancellationToken cancellationToken = default)
    {
        return CallAsync(arguments.AsSpan(), options, cancellationToken);
    }

    /// <inheritdoc cref="CallAsync(object?[],LuaAsyncOptions,CancellationToken)"/>
    public ValueTask<LuaFunctionResults> CallAsync(ReadOnlySpan<LuaStackValue> arguments, LuaAsyncOptions options = default, CancellationToken cancellationToken = default)
    {
        ThrowIfInvalid();

        var lua = Lua;
        LuaAsyncLibrary.EnsureInitialized(lua);

        var coroutineThread = lua.CreateThread();
        try
        {
            var argumentCount = LuaCoroutineRuntime.PushFunctionAndStackArguments(coroutineThread, this, arguments);
            return new ValueTask<LuaFunctionResults>(LuaThread.RunPreparedFunctionAsyncCore(coroutineThread, lua, argumentCount, options, cancellationToken));
        }
        catch
        {
            coroutineThread.Dispose();
            throw;
        }
    }

    /// <inheritdoc cref="CallAsync(object?[],LuaAsyncOptions,CancellationToken)"/>
    public ValueTask<LuaFunctionResults> CallAsync(LuaStackValueRange arguments, LuaAsyncOptions options = default, CancellationToken cancellationToken = default)
    {
        ThrowIfInvalid();

        var lua = Lua;
        LuaAsyncLibrary.EnsureInitialized(lua);

        var coroutineThread = lua.CreateThread();
        try
        {
            var argumentCount = LuaCoroutineRuntime.PushFunctionAndStackArguments(coroutineThread, this, arguments);
            return new ValueTask<LuaFunctionResults>(LuaThread.RunPreparedFunctionAsyncCore(coroutineThread, lua, argumentCount, options, cancellationToken));
        }
        catch
        {
            coroutineThread.Dispose();
            throw;
        }
    }
}
