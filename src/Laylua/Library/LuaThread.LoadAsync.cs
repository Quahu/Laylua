using System;
using System.Threading;
using System.Threading.Tasks;
using Laylua.Marshaling;
using Laylua.Moon;
using Qommon;

namespace Laylua;

public partial class LuaThread
{
    /// <summary>
    ///     Loads a Lua chunk and runs it with support for awaitables (e.g., <see cref="Task"/>).
    /// </summary>
    /// <param name="code"> The Lua chunk source to load and run. </param>
    /// <param name="chunkName"> The debug name used for chunk load errors and stack traces. </param>
    /// <param name="options"> Configures async behavior for this execution. </param>
    /// <param name="cancellationToken"> The cancellation token to observe. </param>
    /// <remarks>
    ///     This API does not make Lua execution concurrent. The owning Lua state still executes on one thread at a time.
    /// </remarks>
    public ValueTask ExecuteAsync(string code, string? chunkName = null, LuaAsyncOptions options = default, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(code);

        return ExecuteAsync(code.AsMemory(), chunkName.AsMemory(), options, cancellationToken);
    }

    /// <inheritdoc cref="ExecuteAsync(string,string,LuaAsyncOptions,CancellationToken)"/>
    public ValueTask ExecuteAsync(ReadOnlyMemory<char> code, ReadOnlyMemory<char> chunkName = default, LuaAsyncOptions options = default, CancellationToken cancellationToken = default)
    {
        ThrowIfInvalid();

        return new ValueTask(ExecuteChunkAsyncCore(this, code, chunkName, options, cancellationToken));
    }

    /// <summary>
    ///     Loads a UTF-8 Lua chunk and runs it with support for awaitables (e.g., <see cref="Task"/>).
    /// </summary>
    /// <param name="utf8Code"> The UTF-8 Lua chunk source to load and run. </param>
    /// <param name="chunkName"> The debug name used for chunk load errors and stack traces. </param>
    /// <param name="options"> Configures async behavior for this execution. </param>
    /// <param name="cancellationToken"> The cancellation token to observe. </param>
    /// <remarks>
    ///     This API does not make Lua execution concurrent. The owning Lua state still executes on one thread at a time.
    /// </remarks>
    public ValueTask ExecuteAsync(ReadOnlyMemory<byte> utf8Code, ReadOnlyMemory<char> chunkName = default, LuaAsyncOptions options = default, CancellationToken cancellationToken = default)
    {
        ThrowIfInvalid();

        return new ValueTask(ExecuteUtf8ChunkAsyncCore(this, utf8Code, chunkName, options, cancellationToken));
    }

    /// <summary>
    ///     Loads a Lua chunk, runs it with support for awaitables (e.g., <see cref="Task"/>), and returns all Lua results.
    /// </summary>
    /// <param name="code"> The Lua chunk source to load and run. </param>
    /// <param name="chunkName"> The debug name used for chunk load errors and stack traces. </param>
    /// <param name="options"> Configures async behavior for this execution. </param>
    /// <param name="cancellationToken"> The cancellation token to observe. </param>
    /// <remarks>
    ///     This API does not make Lua execution concurrent. The owning Lua state still executes on one thread at a time.
    /// </remarks>
    public ValueTask<LuaFunctionResults> EvaluateAsync(string code, string? chunkName = null, LuaAsyncOptions options = default, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(code);

        return EvaluateAsync(code.AsMemory(), chunkName.AsMemory(), options, cancellationToken);
    }

    /// <inheritdoc cref="EvaluateAsync(string,string,LuaAsyncOptions,CancellationToken)"/>
    public ValueTask<LuaFunctionResults> EvaluateAsync(ReadOnlyMemory<char> code, ReadOnlyMemory<char> chunkName = default, LuaAsyncOptions options = default, CancellationToken cancellationToken = default)
    {
        ThrowIfInvalid();

        return new ValueTask<LuaFunctionResults>(EvaluateChunkAsyncCore(this, code, chunkName, options, cancellationToken));
    }

    /// <summary>
    ///     Loads a UTF-8 Lua chunk, runs it with support for awaitables (e.g., <see cref="Task"/>), and returns all Lua results.
    /// </summary>
    /// <param name="utf8Code"> The UTF-8 Lua chunk source to load and run. </param>
    /// <param name="chunkName"> The debug name used for chunk load errors and stack traces. </param>
    /// <param name="options"> Configures async behavior for this execution. </param>
    /// <param name="cancellationToken"> The cancellation token to observe. </param>
    /// <remarks>
    ///     This API does not make Lua execution concurrent. The owning Lua state still executes on one thread at a time.
    /// </remarks>
    public ValueTask<LuaFunctionResults> EvaluateAsync(ReadOnlyMemory<byte> utf8Code, ReadOnlyMemory<char> chunkName = default, LuaAsyncOptions options = default, CancellationToken cancellationToken = default)
    {
        ThrowIfInvalid();

        return new ValueTask<LuaFunctionResults>(EvaluateUtf8ChunkAsyncCore(this, utf8Code, chunkName, options, cancellationToken));
    }

    /// <summary>
    ///     Loads a Lua chunk, runs it with support for awaitables (e.g., <see cref="Task"/>), and converts the first Lua result to <typeparamref name="T"/>.
    /// </summary>
    /// <param name="code"> The Lua chunk source to load and run. </param>
    /// <param name="chunkName"> The debug name used for chunk load errors and stack traces. </param>
    /// <param name="options"> Configures async behavior for this execution. </param>
    /// <param name="cancellationToken"> The cancellation token to observe. </param>
    /// <returns> The first Lua result converted to <typeparamref name="T"/>. </returns>
    /// <remarks>
    ///     This API does not make Lua execution concurrent. The owning Lua state still executes on one thread at a time.
    /// </remarks>
    public ValueTask<T?> EvaluateAsync<T>(string code, string? chunkName = null, LuaAsyncOptions options = default, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(code);

        return EvaluateAsync<T>(code.AsMemory(), chunkName.AsMemory(), options, cancellationToken);
    }

    /// <inheritdoc cref="EvaluateAsync{T}(string,string,LuaAsyncOptions,CancellationToken)"/>
    public ValueTask<T?> EvaluateAsync<T>(ReadOnlyMemory<char> code, ReadOnlyMemory<char> chunkName = default, LuaAsyncOptions options = default, CancellationToken cancellationToken = default)
    {
        ThrowIfInvalid();

        return new ValueTask<T?>(EvaluateChunkAsyncCore<T>(this, code, chunkName, options, cancellationToken));
    }

    /// <summary>
    ///     Loads a UTF-8 Lua chunk, runs it with support for awaitables (e.g., <see cref="Task"/>), and converts the first Lua result to <typeparamref name="T"/>.
    /// </summary>
    /// <param name="utf8Code"> The UTF-8 Lua chunk source to load and run. </param>
    /// <param name="chunkName"> The debug name used for chunk load errors and stack traces. </param>
    /// <param name="options"> Configures async behavior for this execution. </param>
    /// <param name="cancellationToken"> The cancellation token to observe. </param>
    /// <returns> The first Lua result converted to <typeparamref name="T"/>. </returns>
    /// <remarks>
    ///     This API does not make Lua execution concurrent. The owning Lua state still executes on one thread at a time.
    /// </remarks>
    public ValueTask<T?> EvaluateAsync<T>(ReadOnlyMemory<byte> utf8Code, ReadOnlyMemory<char> chunkName = default, LuaAsyncOptions options = default, CancellationToken cancellationToken = default)
    {
        ThrowIfInvalid();

        return new ValueTask<T?>(EvaluateUtf8ChunkAsyncCore<T>(this, utf8Code, chunkName, options, cancellationToken));
    }

    private static async Task ExecuteChunkAsyncCore(LuaThread parentThread, ReadOnlyMemory<char> code, ReadOnlyMemory<char> chunkName, LuaAsyncOptions options, CancellationToken cancellationToken)
    {
        await RunChunkAsync(
                parentThread,
                code,
                chunkName,
                static (coroutineThread, chunkCode, chunkDebugName) => coroutineThread.LoadString(chunkCode.Span, chunkDebugName.Span),
                CompleteDiscard,
                options,
                cancellationToken)
            .ConfigureAwait(options.ContinueOnCapturedContext);
    }

    private static async Task ExecuteUtf8ChunkAsyncCore(LuaThread parentThread, ReadOnlyMemory<byte> code, ReadOnlyMemory<char> chunkName, LuaAsyncOptions options, CancellationToken cancellationToken)
    {
        await RunChunkAsync(
                parentThread,
                code,
                chunkName,
                static (coroutineThread, chunkCode, chunkDebugName) => coroutineThread.LoadUtf8String(chunkCode.Span, chunkDebugName.Span),
                CompleteDiscard,
                options,
                cancellationToken)
            .ConfigureAwait(options.ContinueOnCapturedContext);
    }

    private static Task<T?> EvaluateChunkAsyncCore<T>(LuaThread parentThread, ReadOnlyMemory<char> code, ReadOnlyMemory<char> chunkName, LuaAsyncOptions options, CancellationToken cancellationToken)
    {
        return RunChunkAsync(
            parentThread,
            code,
            chunkName,
            static (coroutineThread, chunkCode, chunkDebugName) => coroutineThread.LoadString(chunkCode.Span, chunkDebugName.Span),
            CompleteFirstResult<T>,
            options,
            cancellationToken);
    }

    private static Task<T?> EvaluateUtf8ChunkAsyncCore<T>(LuaThread parentThread, ReadOnlyMemory<byte> code, ReadOnlyMemory<char> chunkName, LuaAsyncOptions options, CancellationToken cancellationToken)
    {
        return RunChunkAsync(
            parentThread,
            code,
            chunkName,
            static (coroutineThread, chunkCode, chunkDebugName) => coroutineThread.LoadUtf8String(chunkCode.Span, chunkDebugName.Span),
            CompleteFirstResult<T>,
            options,
            cancellationToken);
    }

    private static Task<LuaFunctionResults> EvaluateChunkAsyncCore(LuaThread parentThread, ReadOnlyMemory<char> code, ReadOnlyMemory<char> chunkName, LuaAsyncOptions options, CancellationToken cancellationToken)
    {
        return RunChunkAsync(
            parentThread,
            code,
            chunkName,
            static (coroutineThread, chunkCode, chunkDebugName) => coroutineThread.LoadString(chunkCode.Span, chunkDebugName.Span),
            CompleteResults,
            options,
            cancellationToken);
    }

    private static Task<LuaFunctionResults> EvaluateUtf8ChunkAsyncCore(LuaThread parentThread, ReadOnlyMemory<byte> code, ReadOnlyMemory<char> chunkName, LuaAsyncOptions options, CancellationToken cancellationToken)
    {
        return RunChunkAsync(
            parentThread,
            code,
            chunkName,
            static (coroutineThread, chunkCode, chunkDebugName) => coroutineThread.LoadUtf8String(chunkCode.Span, chunkDebugName.Span),
            CompleteResults,
            options,
            cancellationToken);
    }

    internal static async Task<LuaFunctionResults> RunFunctionAsyncCore(LuaFunction function, object?[] arguments, LuaAsyncOptions options, CancellationToken cancellationToken)
    {
        var lua = function.Lua;
        LuaAsyncLibrary.EnsureInitialized(lua);

        var coroutineThread = lua.CreateThread();
        var isPrepared = false;
        var argumentCount = 0;

        try
        {
            argumentCount = LuaCoroutineRuntime.PushFunctionAndArguments(coroutineThread, function, arguments);
            isPrepared = true;
        }
        finally
        {
            if (!isPrepared)
            {
                coroutineThread.Dispose();
            }
        }

        return await RunPreparedFunctionAsyncCore(coroutineThread, lua, argumentCount, options, cancellationToken).ConfigureAwait(options.ContinueOnCapturedContext);
    }

    internal static async Task<LuaFunctionResults> RunPreparedFunctionAsyncCore(LuaThread coroutineThread, LuaThread fromThread, int argumentCount, LuaAsyncOptions options, CancellationToken cancellationToken)
    {
        try
        {
            return await ResumeLoopAsync(
                    coroutineThread,
                    fromThread,
                    argumentCount,
                    CompleteResults,
                    options,
                    cancellationToken)
                .ConfigureAwait(options.ContinueOnCapturedContext);
        }
        finally
        {
            coroutineThread.Dispose();
        }
    }

    private static async Task<TResult> RunChunkAsync<TCode, TResult>(
        LuaThread parentThread,
        TCode code,
        ReadOnlyMemory<char> chunkName,
        Action<LuaThread, TCode, ReadOnlyMemory<char>> loadChunk,
        Func<LuaThread, LuaThread, int, TResult> complete,
        LuaAsyncOptions options,
        CancellationToken cancellationToken)
    {
        var lua = Lua.FromThread(parentThread);
        LuaAsyncLibrary.EnsureInitialized(lua);

        var coroutineThread = parentThread.CreateThread();

        try
        {
            loadChunk(coroutineThread, code, chunkName);

            return await ResumeLoopAsync(coroutineThread, parentThread, initialArgumentCount: 0, complete, options, cancellationToken).ConfigureAwait(options.ContinueOnCapturedContext);
        }
        finally
        {
            coroutineThread.Dispose();
        }
    }

    private static async Task<TResult> ResumeLoopAsync<TResult>(
        LuaThread coroutineThread,
        LuaThread fromThread,
        int initialArgumentCount,
        Func<LuaThread, LuaThread, int, TResult> complete,
        LuaAsyncOptions options,
        CancellationToken cancellationToken)
    {
        var argumentCount = initialArgumentCount;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var step = LuaCoroutineRuntime.ResumeStep(coroutineThread, fromThread, argumentCount);

            if (step.Status == LuaStatus.Ok)
            {
                return complete(coroutineThread, fromThread, step.ResultCount);
            }

            argumentCount = await AwaitAndPushResultAsync(coroutineThread, step.AwaitableBox!, options, cancellationToken).ConfigureAwait(options.ContinueOnCapturedContext);
        }
    }

    private static bool CompleteDiscard(LuaThread coroutineThread, LuaThread fromThread, int resultCount)
    {
        LuaCoroutineRuntime.ClearStack(coroutineThread);
        return true;
    }

    private static T? CompleteFirstResult<T>(LuaThread coroutineThread, LuaThread fromThread, int resultCount)
    {
        return LuaCoroutineRuntime.ReadFirstResult<T>(coroutineThread, resultCount);
    }

    private static LuaFunctionResults CompleteResults(LuaThread coroutineThread, LuaThread fromThread, int resultCount)
    {
        return LuaCoroutineRuntime.CollectResults(coroutineThread, fromThread, resultCount);
    }

    private static async Task<int> AwaitAndPushResultAsync(LuaThread coroutineThread, AwaitableBox box, LuaAsyncOptions options, CancellationToken cancellationToken)
    {
        try
        {
            return await box.AwaitAndPushResultAsync(coroutineThread, options.ContinueOnCapturedContext, cancellationToken).ConfigureAwait(options.ContinueOnCapturedContext);
        }
        catch (AggregateException ex) when (ex.InnerExceptions.Count == 1)
        {
            var innerException = ex.InnerExceptions[0];
            throw new LuaException(innerException.Message, innerException);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not LuaException)
        {
            throw new LuaException(ex.Message, ex);
        }
    }
}
