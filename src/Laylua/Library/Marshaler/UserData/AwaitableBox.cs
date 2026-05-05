using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Laylua.Marshaling;

internal sealed class AwaitableBox
{
    private delegate Task<int> AwaitAwaitableDelegate(object awaitable, LuaThread coroutineThread, bool continueOnCapturedContext, CancellationToken cancellationToken);

    private static readonly MethodInfo AwaitTaskResultAsyncMethod = typeof(AwaitableBox).GetMethod(nameof(AwaitTaskResultAsync),
        BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly MethodInfo AwaitValueTaskResultAsyncMethod = typeof(AwaitableBox).GetMethod(nameof(AwaitValueTaskResultAsync),
        BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly ConditionalWeakTable<Type, AwaitAwaitableDelegate> AwaitTaskResultDelegates = new();
    private static readonly ConditionalWeakTable<Type, AwaitAwaitableDelegate> AwaitValueTaskResultDelegates = new();

    private readonly object _awaitable;
    private readonly AwaitAwaitableDelegate _awaitAwaitable;

    public AwaitableBox(object awaitable, Type awaitableType)
    {
        ArgumentNullException.ThrowIfNull(awaitable);
        ArgumentNullException.ThrowIfNull(awaitableType);

        _awaitable = awaitable;
        _awaitAwaitable = CreateAwaitDelegate(awaitableType);
    }

    public async Task<int> AwaitAndPushResultAsync(LuaThread coroutineThread, bool continueOnCapturedContext, CancellationToken cancellationToken)
    {
        return await _awaitAwaitable(_awaitable, coroutineThread, continueOnCapturedContext, cancellationToken).ConfigureAwait(continueOnCapturedContext);
    }

    private static AwaitAwaitableDelegate CreateAwaitDelegate(Type awaitableType)
    {
        if (awaitableType == typeof(Task))
        {
            return AwaitTaskAsync;
        }

        if (awaitableType.IsGenericType && awaitableType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = awaitableType.GetGenericArguments()[0];
            return AwaitTaskResultDelegates.GetValue(resultType, static type =>
            {
                var method = AwaitTaskResultAsyncMethod.MakeGenericMethod(type);
                return method.CreateDelegate<AwaitAwaitableDelegate>();
            });
        }

        if (awaitableType == typeof(ValueTask))
        {
            return AwaitValueTaskAsync;
        }

        if (awaitableType.IsGenericType && awaitableType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            var resultType = awaitableType.GetGenericArguments()[0];
            return AwaitValueTaskResultDelegates.GetValue(resultType, static type =>
            {
                var method = AwaitValueTaskResultAsyncMethod.MakeGenericMethod(type);
                return method.CreateDelegate<AwaitAwaitableDelegate>();
            });
        }

        throw new InvalidOperationException($"Unsupported awaitable type '{awaitableType}'.");
    }

    private static async Task<int> AwaitTaskAsync(object awaitable, LuaThread coroutineThread, bool continueOnCapturedContext, CancellationToken cancellationToken)
    {
        await ((Task) awaitable).WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext);
        return 0;
    }

    private static async Task<int> AwaitTaskResultAsync<TResult>(object awaitable, LuaThread coroutineThread, bool continueOnCapturedContext, CancellationToken cancellationToken)
    {
        var result = await ((Task<TResult>) awaitable).WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext);

        coroutineThread.Stack.EnsureFreeCapacity(1);
        coroutineThread.MainThread.Marshaler.PushValue(coroutineThread, result);
        return 1;
    }

    private static async Task<int> AwaitValueTaskAsync(object awaitable, LuaThread coroutineThread, bool continueOnCapturedContext, CancellationToken cancellationToken)
    {
        var valueTask = (ValueTask) awaitable;
        if (cancellationToken.CanBeCanceled)
        {
            await valueTask.AsTask().WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext);
        }
        else
        {
            await valueTask.ConfigureAwait(continueOnCapturedContext);
        }

        return 0;
    }

    private static async Task<int> AwaitValueTaskResultAsync<TResult>(object awaitable, LuaThread coroutineThread, bool continueOnCapturedContext, CancellationToken cancellationToken)
    {
        var valueTask = (ValueTask<TResult>) awaitable;
        TResult result;
        if (cancellationToken.CanBeCanceled)
        {
            result = await valueTask.AsTask().WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext);
        }
        else
        {
            result = await valueTask.ConfigureAwait(continueOnCapturedContext);
        }

        coroutineThread.Stack.EnsureFreeCapacity(1);
        coroutineThread.MainThread.Marshaler.PushValue(coroutineThread, result);
        return 1;
    }
}
