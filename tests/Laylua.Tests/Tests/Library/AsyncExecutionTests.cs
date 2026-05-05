using System;
using System.Threading;
using System.Threading.Tasks;
using Laylua.Marshaling;
using Laylua.Moon;
using NUnit.Framework;

namespace Laylua.Tests;

[TestFixture]
public class AsyncExecutionTests : LuaTestBase
{
    [Test]
    public async Task ExecuteAsync_RunsSynchronousChunk()
    {
        // Arrange
        await Lua.ExecuteAsync("x = 1 + 2");

        // Act
        var x = Lua.Evaluate<int>("return x");

        // Assert
        Assert.That(x, Is.EqualTo(3));
    }

    [Test]
    public async Task EvaluateAsync_ReturnsSynchronousResult()
    {
        // Act
        var result = await Lua.EvaluateAsync<int>("return 40 + 2");

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void EvaluateAsync_AfterAwaitingTask_LuaError_ThrowsLuaException()
    {
        // Arrange
        OpenAsyncLibrary();
        Lua["pending"] = Task.FromResult(0);

        // Act & Assert
        Assert.That(async () => await Lua.EvaluateAsync<int>("""
                async.await(pending)
                return nil + 1
                """),
            Throws.InstanceOf<LuaException>());
    }

    [Test]
    public async Task EvaluateAsync_ReadOnlyMemoryChunk_LoadsBeforeFirstSuspension()
    {
        // Arrange
        OpenAsyncLibrary();
        var taskCompletionSource = new TaskCompletionSource<int>();
        var code = "return async.await(pending)".ToCharArray();
        Lua["pending"] = taskCompletionSource.Task;

        // Act
        var task = Lua.EvaluateAsync<int>(code.AsMemory());
        Array.Fill(code, ' ');
        taskCompletionSource.SetResult(42);

        var result = await task;

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void EvaluateAsync_NoReturnedValues_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Assert.That(async () => await Lua.EvaluateAsync<int>("return"), Throws.InvalidOperationException);
    }

    [Test]
    public async Task EvaluateAsync_MultipleReturnedValues_ReturnsStackBackedResults()
    {
        // Act
        using var results = await Lua.EvaluateAsync("return 1, 'two', true");

        // Assert
        Assert.That(results, Is.TypeOf<LuaFunctionResults>());
        Assert.That(results.Count, Is.EqualTo(3));
        Assert.That(results[1].GetValue<int>(), Is.EqualTo(1));
        Assert.That(results[2].GetValue<string>(), Is.EqualTo("two"));
        Assert.That(results[3].GetValue<bool>(), Is.True);
    }

    [Test]
    public async Task EvaluateAsync_ResultsDisposed_InvalidatesResultAccess()
    {
        // Arrange
        var results = await Lua.EvaluateAsync("return function() return 1 end");

        // Act
        results.Dispose();

        // Assert
        Assert.That(() => _ = results.Count, Throws.InstanceOf<ObjectDisposedException>());
    }

    [Test]
    public async Task EvaluateAsync_ResultReferenceRemainsValidAfterResultsDisposed()
    {
        // Arrange
        var results = await Lua.EvaluateAsync("return function() return 42 end");
        var function = results.First.GetValue<LuaFunction>()!;

        // Act
        results.Dispose();

        // Assert
        using (function)
        using (var callResults = function.Call())
        {
            Assert.That(callResults.First.GetValue<int>(), Is.EqualTo(42));
        }
    }

    [Test]
    public async Task EvaluateAsync_Dispose_RemovesOnlyReturnedSliceFromCallerStack()
    {
        // Arrange
        using var stackSnapshot = Lua.Stack.SnapshotCount();
        Lua.Stack.Push("sentinel");
        var sentinelIndex = Lua.Stack.Count;

        // Act
        var results = await Lua.EvaluateAsync("return 1, 2");

        // Assert
        Assert.That(Lua.Stack.Count, Is.EqualTo(sentinelIndex + 2));
        Assert.That(Lua.Stack[sentinelIndex].GetValue<string>(), Is.EqualTo("sentinel"));
        Assert.That(results.Count, Is.EqualTo(2));
        Assert.That(results[1].GetValue<int>(), Is.EqualTo(1));
        Assert.That(results[2].GetValue<int>(), Is.EqualTo(2));

        results.Dispose();

        Assert.That(Lua.Stack.Count, Is.EqualTo(sentinelIndex));
        Assert.That(Lua.Stack[sentinelIndex].GetValue<string>(), Is.EqualTo("sentinel"));
    }

    [Test]
    public void PushValue_TaskValue_DoesNotInitializeAsyncHelpers()
    {
        // Arrange
        Lua.OpenLibrary(LuaLibraries.Standard.Coroutine);
        Lua.Execute("original_resume = coroutine.resume");

        // Act
        Lua["pending"] = Task.CompletedTask;

        var helpersAreAbsent = Lua.Evaluate<bool>("return async == nil");
        var isOriginalResume = Lua.Evaluate<bool>("return coroutine.resume == original_resume");

        // Assert
        Assert.That(helpersAreAbsent, Is.True);
        Assert.That(isOriginalResume, Is.True);
    }

    [Test]
    public void PushValue_TaskBaseWithRegisteredRuntimeHandler_UsesRegisteredHandler()
    {
        // Arrange
        var handler = new StringUserDataHandler(typeof(Task<int>), "registered-task");
        LuaTypeRegistry.Register<Task<int>>(handler);
        Task pending = Task.FromResult(123);

        try
        {
            // Act
            Lua.SetGlobal("pending", pending);
            var result = Lua.Evaluate<string>("return pending");

            // Assert
            Assert.That(result, Is.EqualTo("registered-task"));
        }
        finally
        {
            LuaTypeRegistry.Unregister<Task<int>>();
        }
    }

    [Test]
    public void PushValue_ValueTaskWithRegisteredHandler_UsesRegisteredHandler()
    {
        // Arrange
        var handler = new StringUserDataHandler(typeof(ValueTask<int>), "registered-value-task");
        LuaTypeRegistry.Register<ValueTask<int>>(handler);
        var pending = new ValueTask<int>(123);

        try
        {
            // Act
            Lua.SetGlobal("pending", pending);
            var result = Lua.Evaluate<string>("return pending");

            // Assert
            Assert.That(result, Is.EqualTo("registered-value-task"));
        }
        finally
        {
            LuaTypeRegistry.Unregister<ValueTask<int>>();
        }
    }

    [Test]
    public async Task EvaluateAsync_ImplicitAwaitedDelegate_IgnoresRegisteredTaskHandler()
    {
        // Arrange
        var handler = new StringUserDataHandler(typeof(Task<int>), "registered-task");
        LuaTypeRegistry.Register<Task<int>>(handler);
        Lua["fn"] = (Func<Task<int>>) (static async () =>
        {
            await Task.Yield();
            return 7;
        });

        try
        {
            // Act
            var result = await Lua.EvaluateAsync<int>("return fn()");

            // Assert
            Assert.That(result, Is.EqualTo(7));
        }
        finally
        {
            LuaTypeRegistry.Unregister<Task<int>>();
        }
    }

    [Test]
    public async Task EvaluateAsync_ImplicitAwaitedDynamicUserDataMethod_IgnoresRegisteredTaskHandler()
    {
        // Arrange
        var handler = new StringUserDataHandler(typeof(Task<int>), "registered-task");
        LuaTypeRegistry.Register<Task<int>>(handler);
        Marshaler.AllowsDynamicMarshaling = true;
        Lua["target"] = new DynamicAsyncUserDataTarget();

        try
        {
            // Act
            var result = await Lua.EvaluateAsync<int>("return target:GetAsync()");

            // Assert
            Assert.That(result, Is.EqualTo(42));
        }
        finally
        {
            LuaTypeRegistry.Unregister<Task<int>>();
        }
    }

    [Test]
    public async Task EvaluateAsync_ImplicitAwaitedTypedUserDataMethod_IgnoresRegisteredValueTaskHandler()
    {
        // Arrange
        var handler = new StringUserDataHandler(typeof(ValueTask<int>), "registered-value-task");
        LuaTypeRegistry.Register<ValueTask<int>>(handler);
        Lua["target"] = new TypedAsyncUserDataTarget();

        try
        {
            // Act
            var result = await Lua.EvaluateAsync<int>("return target:GetAsync()");

            // Assert
            Assert.That(result, Is.EqualTo(42));
        }
        finally
        {
            LuaTypeRegistry.Unregister<ValueTask<int>>();
        }
    }

    [Test]
    public async Task EvaluateAsync_ReturnAwaitableDelegate_IgnoresRegisteredTaskHandler()
    {
        // Arrange
        var handler = new StringUserDataHandler(typeof(Task<int>), "registered-task");
        LuaTypeRegistry.Register<Task<int>>(handler);
        Marshaler.AwaitableReturnHandling = LuaAwaitableReturnHandling.ReturnAwaitable;
        Lua["fn"] = (Func<Task<int>>) (static async () =>
        {
            await Task.Yield();
            return 7;
        });

        try
        {
            // Act
            var result = await Lua.EvaluateAsync<Task<int>>("return fn()");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(await result!, Is.EqualTo(7));
        }
        finally
        {
            LuaTypeRegistry.Unregister<Task<int>>();
        }
    }

    [Test]
    public async Task EvaluateAsync_ReturnAwaitableDynamicUserDataMethod_IgnoresRegisteredTaskHandler()
    {
        // Arrange
        var handler = new StringUserDataHandler(typeof(Task<int>), "registered-task");
        LuaTypeRegistry.Register<Task<int>>(handler);
        LuaTypeRegistry.ConfigureDynamic<DynamicAsyncUserDataTarget>(static options =>
        {
            options.AwaitableReturnHandling = LuaAwaitableReturnHandling.ReturnAwaitable;
        });
        Lua["target"] = new DynamicAsyncUserDataTarget();

        try
        {
            // Act
            var result = await Lua.EvaluateAsync<Task<int>>("return target:GetAsync()");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(await result!, Is.EqualTo(42));
        }
        finally
        {
            LuaTypeRegistry.Unregister<DynamicAsyncUserDataTarget>();
            LuaTypeRegistry.Unregister<Task<int>>();
        }
    }

    [Test]
    public async Task EvaluateAsync_ReturnAwaitableTypedUserDataMethod_IgnoresRegisteredValueTaskHandler()
    {
        // Arrange
        var handler = new StringUserDataHandler(typeof(ValueTask<int>), "registered-value-task");
        LuaTypeRegistry.Register<ValueTask<int>>(handler);
        Lua["target"] = new TypedExplicitAwaitAsyncUserDataTarget();

        try
        {
            // Act
            var result = await Lua.EvaluateAsync<ValueTask<int>>("return target:GetAsync()");

            // Assert
            Assert.That(await result, Is.EqualTo(42));
        }
        finally
        {
            LuaTypeRegistry.Unregister<ValueTask<int>>();
        }
    }

    [Test]
    public async Task EvaluateAsync_AsyncDelegateDoesNotExposeAsyncGlobalWithoutStandardLibraries()
    {
        // Arrange
        Lua["fn"] = (Func<Task<int>>) (static () => Task.FromResult(123));

        // Act
        var result = await Lua.EvaluateAsync<int>("return fn()");
        var asyncGlobalIsAbsent = Lua.Evaluate<bool>("return async == nil");
        var standardLibrariesAreAbsent = Lua.Evaluate<bool>("return coroutine == nil and table == nil");

        // Assert
        Assert.That(result, Is.EqualTo(123));
        Assert.That(asyncGlobalIsAbsent, Is.True);
        Assert.That(standardLibrariesAreAbsent, Is.True);
    }

    [Test]
    public void OpenLibrary_Async_InstallsAsyncGlobal()
    {
        // Act
        var opened = Lua.OpenLibrary(LuaLibraries.Laylua.Async);

        // Assert
        Assert.That(opened, Is.True);
        Assert.That(Lua.Evaluate<bool>("return async ~= nil"), Is.True);
    }

    [Test]
    public async Task ExecuteAsync_PatchesCoroutineOnlyWhenCoroutineAlreadyOpen()
    {
        // Arrange
        Lua.OpenLibrary(LuaLibraries.Standard.Coroutine);
        Lua.Execute("original_resume = coroutine.resume");

        // Act
        await Lua.ExecuteAsync("");

        var isPatched = Lua.Evaluate<bool>("return coroutine.resume ~= original_resume");

        // Assert
        Assert.That(isPatched, Is.True);
    }

    [Test]
    public async Task ExecuteAsync_CoroutineOpenedAfterAsyncInitialization_GuardsLayluaAwaitableYields()
    {
        // Arrange
        await Lua.ExecuteAsync("");
        Lua.OpenLibrary(LuaLibraries.Standard.Coroutine);
        OpenAsyncLibrary();
        Lua["pending"] = Task.FromResult(1);

        // Act & Assert
        Assert.That(() => Lua.Execute("""
                local co = coroutine.create(function()
                    return async.await(pending)
                end)

                coroutine.resume(co)
                """),
            Throws.InstanceOf<LuaException>().With.Message.Contains("Implicit await requires"));
    }

    [Test]
    public async Task ExecuteAsync_CoroutinePatch_DoesNotExposeInternalStateOnCoroutineTable()
    {
        // Arrange
        Lua.OpenLibrary(LuaLibraries.Standard.Coroutine);

        // Act
        await Lua.ExecuteAsync("");

        var exposesInternalState = Lua.Evaluate<bool>("return coroutine.__laylua_async_guarded ~= nil or coroutine.__laylua_original_resume ~= nil");

        // Assert
        Assert.That(exposesInternalState, Is.False);
    }

    [Test]
    public async Task ExecuteAsync_CoroutineWrap_UsesCapturedOriginalResume()
    {
        // Arrange
        Lua.OpenLibrary(LuaLibraries.Standard.Coroutine);

        // Act
        await Lua.ExecuteAsync("");

        var result = Lua.Evaluate<int>("""
            coroutine.resume = function()
                error('wrong resume')
            end

            local wrapped = coroutine.wrap(function()
                return 12
            end)

            return wrapped()
            """);

        // Assert
        Assert.That(result, Is.EqualTo(12));
    }

    [Test]
    public async Task CoroutineWrap_WithLayluaAwaitable_ReportsClearError()
    {
        // Arrange
        Lua.OpenLibrary(LuaLibraries.Standard.Coroutine);
        OpenAsyncLibrary();
        await Lua.ExecuteAsync("");
        Lua["pending"] = Task.FromResult(1);

        // Act & Assert
        Assert.That(() => Lua.Execute("""
                local wrapped = coroutine.wrap(function()
                    return async.await(pending)
                end)

                return wrapped()
                """),
            Throws.InstanceOf<LuaException>().With.Message.Contains("Implicit await requires"));
    }

    [Test]
    public async Task CoroutineResume_ReturnedAwaitable_IsAllowed()
    {
        // Arrange
        Lua.OpenLibrary(LuaLibraries.Standard.Coroutine);
        OpenAsyncLibrary();
        await Lua.ExecuteAsync("");
        Lua["pending"] = Task.FromResult(7);

        // Act
        using var results = Lua.Evaluate("""
            local co = coroutine.create(function()
                return pending
            end)

            local ok, value = coroutine.resume(co)
            return ok, async.is_awaitable(value), coroutine.status(co)
            """);

        // Assert
        Assert.That(results[1].GetValue<bool>(), Is.True);
        Assert.That(results[2].GetValue<bool>(), Is.True);
        Assert.That(results[3].GetValue<string>(), Is.EqualTo("dead"));
    }

    [Test]
    public async Task EvaluateAsync_AsyncDelegateReturningTask_ReturnsAwaitedValue()
    {
        // Arrange
        Lua["fn"] = (Func<Task<int>>) (static async () =>
        {
            await Task.Yield();
            return 7;
        });

        // Act
        var result = await Lua.EvaluateAsync<int>("return fn()");

        // Assert
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public async Task EvaluateAsync_AsyncDelegateWithReturnAwaitable_ReturnsAwaitable()
    {
        // Arrange
        OpenAsyncLibrary();
        Marshaler.AwaitableReturnHandling = LuaAwaitableReturnHandling.ReturnAwaitable;
        Lua["fn"] = (Func<Task<int>>) (static () => Task.FromResult(7));

        // Act
        var isAwaitable = await Lua.EvaluateAsync<bool>("return async.is_awaitable(fn())");
        var result = await Lua.EvaluateAsync<int>("return async.await(fn())");

        // Assert
        Assert.That(isAwaitable, Is.True);
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public async Task EvaluateAsync_AsyncDelegateWithReturnAwaitable_RoundTripsTaskResult()
    {
        // Arrange
        Marshaler.AwaitableReturnHandling = LuaAwaitableReturnHandling.ReturnAwaitable;
        Lua["fn"] = (Func<Task<int>>) (static async () =>
        {
            await Task.Yield();
            return 7;
        });

        // Act
        var result = await Lua.EvaluateAsync<Task<int>>("return fn()");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(await result!, Is.EqualTo(7));
    }

    [Test]
    public async Task EvaluateAsync_AsyncDelegateWrapper_IgnoresOverwrittenAsyncGlobal()
    {
        // Arrange
        Lua["fn"] = (Func<Task<int>>) (static async () =>
        {
            await Task.Yield();
            return 7;
        });

        // Act
        Lua.Execute("""
            async = {
                await = function()
                    error('wrong await')
                end,
                is_awaitable = function()
                    return true
                end
            }
            """);

        var result = await Lua.EvaluateAsync<int>("return fn()");

        // Assert
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public async Task EvaluateAsync_AsyncDelegateReturningValueTask_ReturnsAwaitedValue()
    {
        // Arrange
        Lua["fn"] = (Func<ValueTask<string>>) (static async () =>
        {
            await Task.Yield();
            return "hello";
        });

        // Act
        var result = await Lua.EvaluateAsync<string>("return fn()");

        // Assert
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public async Task EvaluateAsync_AwaitOnPushedTask_ReturnsAwaitedValue()
    {
        // Arrange
        OpenAsyncLibrary();
        Lua["pending"] = Task.FromResult(123);

        // Act
        var result = await Lua.EvaluateAsync<int>("return async.await(pending)");

        // Assert
        Assert.That(result, Is.EqualTo(123));
    }

    [Test]
    public void Evaluate_PushedTask_RoundTripsOriginalTask()
    {
        // Arrange
        var pending = Task.FromResult(123);
        Lua["pending"] = pending;

        // Act
        var result = Lua.Evaluate<Task<int>>("return pending");

        // Assert
        Assert.That(result, Is.SameAs(pending));
    }

    [Test]
    public async Task EvaluateAsync_RawCompletedValueTaskPush_CanBeAwaitedMoreThanOnce()
    {
        // Arrange
        OpenAsyncLibrary();
        Lua["pending"] = new ValueTask<int>(12);

        // Act
        using var results = await Lua.EvaluateAsync("return async.await(pending), async.await(pending)");

        // Assert
        Assert.That(results.Count, Is.EqualTo(2));
        Assert.That(results[1].GetValue<int>(), Is.EqualTo(12));
        Assert.That(results[2].GetValue<int>(), Is.EqualTo(12));
    }

    [Test]
    public async Task Evaluate_PushedValueTask_RoundTripsOriginalValueTask()
    {
        // Arrange
        var pending = new ValueTask<int>(13);
        Lua["pending"] = pending;

        // Act
        var result = Lua.Evaluate<ValueTask<int>>("return pending");

        // Assert
        Assert.That(await result, Is.EqualTo(13));
    }

    [Test]
    public async Task EvaluateAsync_BoxedValueTaskPush_ReturnsAwaitedValue()
    {
        // Arrange
        OpenAsyncLibrary();
        object pending = new ValueTask<int>(13);
        Lua["pending"] = pending;

        // Act
        var result = await Lua.EvaluateAsync<int>("return async.await(pending)");

        // Assert
        Assert.That(result, Is.EqualTo(13));
    }

    [Test]
    public async Task EvaluateAsync_BoxedTaskPush_ReturnsAwaitedValue()
    {
        // Arrange
        OpenAsyncLibrary();
        object pending = Task.FromResult(21);
        Lua["pending"] = pending;

        // Act
        var result = await Lua.EvaluateAsync<int>("return async.await(pending)");

        // Assert
        Assert.That(result, Is.EqualTo(21));
    }

    [Test]
    public async Task EvaluateAsync_BoxedNonGenericValueTaskPush_CompletesWithoutResult()
    {
        // Arrange
        OpenAsyncLibrary();
        object pending = default(ValueTask);
        Lua["pending"] = pending;

        // Act
        using var results = await Lua.EvaluateAsync("return async.await(pending)");

        // Assert
        Assert.That(results.Count, Is.EqualTo(0));
    }

    [Test]
    public void EvaluateAsync_FaultedValueTask_ThrowsLuaExceptionWithOriginalInnerException()
    {
        // Arrange
        OpenAsyncLibrary();
        Lua["pending"] = new ValueTask<int>(Task.FromException<int>(new InvalidOperationException("value task boom")));

        // Act & Assert
        Assert.That(async () => await Lua.EvaluateAsync<int>("return async.await(pending)"),
            Throws.InstanceOf<LuaException>()
                .With.InnerException.TypeOf<InvalidOperationException>()
                .And.InnerException.Message.EqualTo("value task boom"));
    }

    [Test]
    public async Task EvaluateAsync_NoResultAwait_ResumesWithZeroValues()
    {
        // Arrange
        OpenAsyncLibrary();
        Lua["pending"] = Task.CompletedTask;

        // Act
        using var results = await Lua.EvaluateAsync("return async.await(pending)");

        // Assert
        Assert.That(results.Count, Is.EqualTo(0));
    }

    [Test]
    public void EvaluateAsync_FaultedTask_ThrowsLuaExceptionWithOriginalInnerException()
    {
        // Arrange
        Lua["fn"] = (Func<Task<int>>) (static async () =>
        {
            await Task.Yield();
            throw new InvalidOperationException("boom");
        });

        // Act & Assert
        Assert.That(async () => await Lua.EvaluateAsync<int>("return fn()"),
            Throws.InstanceOf<LuaException>()
                .With.InnerException.TypeOf<InvalidOperationException>()
                .And.InnerException.Message.EqualTo("boom"));
    }

    [Test]
    public void EvaluateAsync_CancellationDuringAwait_ThrowsOperationCanceled()
    {
        // Arrange
        OpenAsyncLibrary();
        var cancellationTokenSource = new CancellationTokenSource();
        var taskCompletionSource = new TaskCompletionSource<int>();
        Lua["pending"] = taskCompletionSource.Task;

        // Act
        var task = Lua.EvaluateAsync<int>("return async.await(pending)", cancellationToken: cancellationTokenSource.Token);
        cancellationTokenSource.Cancel();

        // Assert
        Assert.That(async () => await task, Throws.InstanceOf<OperationCanceledException>());

        taskCompletionSource.TrySetCanceled();
        cancellationTokenSource.Dispose();
    }

    [Test]
    public async Task CallAsync_OnLuaFunction_RunsWithYields()
    {
        // Arrange
        Lua["fn"] = (Func<Task<int>>) (static async () =>
        {
            await Task.Yield();
            return 21;
        });

        // Act
        await Lua.ExecuteAsync("function double_via_async() return fn() * 2 end");

        using (var doubler = Lua.Evaluate<LuaFunction>("return double_via_async")!)
        {
            using var result = await doubler.CallAsync();

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.First.GetValue<int>(), Is.EqualTo(42));
        }
    }

    [Test]
    public async Task CallAsync_OnLuaFunction_AcceptsClrArguments()
    {
        // Arrange
        await Lua.ExecuteAsync("function add(a, b) return a + b end");

        // Act & Assert
        using (var add = Lua.Evaluate<LuaFunction>("return add")!)
        using (var result = await add.CallAsync(20, 22))
        {
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.First.GetValue<int>(), Is.EqualTo(42));
        }
    }

    [Test]
    public async Task CallAsync_OnLuaFunction_AcceptsStackArguments()
    {
        // Arrange
        await Lua.ExecuteAsync("function add(a, b) return a + b end");

        // Act
        using (var add = Lua.Evaluate<LuaFunction>("return add")!)
        using (Lua.Stack.SnapshotCount())
        {
            var firstArgumentIndex = Lua.Stack.Count + 1;
            Lua.Stack.Push(19);
            Lua.Stack.Push(23);

            var arguments = Lua.Stack.GetRange(firstArgumentIndex, 2);

            using var result = await add.CallAsync(arguments);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.First.GetValue<int>(), Is.EqualTo(42));
        }
    }

    [Test]
    public async Task EvaluateAsync_OnChildThread_ReturnsResultsOnChildThreadStack()
    {
        // Arrange
        using var thread = Lua.CreateThread();
        thread.Stack.Push("sentinel");

        // Act
        var results = await thread.EvaluateAsync("return 1, 2");

        // Assert
        Assert.That(thread.Stack.Count, Is.EqualTo(3));
        Assert.That(thread.Stack[1].GetValue<string>(), Is.EqualTo("sentinel"));
        Assert.That(results.Count, Is.EqualTo(2));
        Assert.That(results[1].GetValue<int>(), Is.EqualTo(1));
        Assert.That(results[2].GetValue<int>(), Is.EqualTo(2));
        Assert.That(Lua.Stack.Count, Is.EqualTo(0));

        results.Dispose();

        Assert.That(thread.Stack.Count, Is.EqualTo(1));
        Assert.That(thread.Stack[1].GetValue<string>(), Is.EqualTo("sentinel"));
    }

    [Test]
    public async Task EvaluateAsync_DynamicUserDataAsyncMethod_ImplicitlyAwaitsReturnedTask()
    {
        // Arrange
        Marshaler.AllowsDynamicMarshaling = true;
        Lua["target"] = new DynamicAsyncUserDataTarget();

        // Act
        var result = await Lua.EvaluateAsync<int>("return target:GetAsync()");

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public async Task EvaluateAsync_DynamicUserDataAsyncMethodWithReturnAwaitable_ReturnsAwaitable()
    {
        // Arrange
        OpenAsyncLibrary();
        LuaTypeRegistry.ConfigureDynamic<DynamicAsyncUserDataTarget>(static options =>
        {
            options.AwaitableReturnHandling = LuaAwaitableReturnHandling.ReturnAwaitable;
        });

        Lua["target"] = new DynamicAsyncUserDataTarget();

        try
        {
            // Act
            var isAwaitable = await Lua.EvaluateAsync<bool>("return async.is_awaitable(target:GetAsync())");
            var result = await Lua.EvaluateAsync<int>("return async.await(target:GetAsync())");

            // Assert
            Assert.That(isAwaitable, Is.True);
            Assert.That(result, Is.EqualTo(42));
        }
        finally
        {
            LuaTypeRegistry.Unregister<DynamicAsyncUserDataTarget>();
        }
    }

    [Test]
    public async Task EvaluateAsync_DynamicUserDataAsyncMethodWithReturnAwaitable_RoundTripsTaskResult()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicAsyncUserDataTarget>(static options =>
        {
            options.AwaitableReturnHandling = LuaAwaitableReturnHandling.ReturnAwaitable;
        });
        Lua["target"] = new DynamicAsyncUserDataTarget();

        try
        {
            // Act
            var result = await Lua.EvaluateAsync<Task<int>>("return target:GetAsync()");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(await result!, Is.EqualTo(42));
        }
        finally
        {
            LuaTypeRegistry.Unregister<DynamicAsyncUserDataTarget>();
        }
    }

    [Test]
    public void DynamicUserData_AwaitableMethodWithOutReturn_IsNotExposed()
    {
        // Arrange
        Marshaler.AllowsDynamicMarshaling = true;
        Lua["target"] = new DynamicAsyncUserDataTarget();

        // Act
        var methodIsHidden = Lua.Evaluate<bool>("return target.GetAsyncWithOut == nil");

        // Assert
        Assert.That(methodIsHidden, Is.True);
    }

    [Test]
    public void DynamicUserData_AwaitableMethodWithLuaStackValueRange_IsNotExposed()
    {
        // Arrange
        Marshaler.AllowsDynamicMarshaling = true;
        Lua["target"] = new DynamicAsyncUserDataTarget();

        // Act
        var methodIsHidden = Lua.Evaluate<bool>("return target.GetAsyncWithRange == nil");

        // Assert
        Assert.That(methodIsHidden, Is.True);
    }

    [Test]
    public async Task EvaluateAsync_TypedUserDataAsyncMethod_ImplicitlyAwaitsReturnedValueTask()
    {
        // Arrange
        Lua["target"] = new TypedAsyncUserDataTarget();

        // Act
        var result = await Lua.EvaluateAsync<int>("return target:GetAsync()");

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public async Task EvaluateAsync_TypedUserDataAsyncMethodWithReturnAwaitable_ReturnsAwaitable()
    {
        // Arrange
        OpenAsyncLibrary();
        Lua["target"] = new TypedExplicitAwaitAsyncUserDataTarget();

        // Act
        var isAwaitable = await Lua.EvaluateAsync<bool>("return async.is_awaitable(target:GetAsync())");
        var result = await Lua.EvaluateAsync<int>("return async.await(target:GetAsync())");

        // Assert
        Assert.That(isAwaitable, Is.True);
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public async Task EvaluateAsync_TypedUserDataAsyncMethodWithReturnAwaitable_RoundTripsValueTaskResult()
    {
        // Arrange
        Lua["target"] = new TypedExplicitAwaitAsyncUserDataTarget();

        // Act
        var result = await Lua.EvaluateAsync<ValueTask<int>>("return target:GetAsync()");

        // Assert
        Assert.That(await result, Is.EqualTo(42));
    }

    [Test]
    public void Execute_SyncAsyncDelegateCall_ReportsImplicitAwaitRequirementWithoutDuplicateInnerException()
    {
        // Arrange
        Lua["fn"] = (Func<Task<int>>) (static () => Task.FromResult(7));

        // Act
        var exception = Assert.Throws<LuaException>(() => Lua.Execute("return fn()"));

        // Assert
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("Implicit await requires"));
        Assert.That(exception.Message, Does.Contain("ReturnAwaitable"));
        Assert.That(exception.InnerException, Is.Null);
    }

    [Test]
    public void Delegate_AwaitableReturnWithLuaStackValueRangeParameter_ThrowsNotSupportedException()
    {
        // Arrange
        Lua["fn"] = (Func<LuaStackValueRange, Task<int>>) (static _ => Task.FromResult(1));

        // Act & Assert
        Assert.That(() => Lua.Execute("return fn(1)"),
            Throws.InstanceOf<LuaException>()
                .With.InnerException.TypeOf<NotSupportedException>());
    }

    [Test]
    public void Execute_SyncDynamicUserDataAsyncMethod_ReportsImplicitAwaitRequirementWithoutDuplicateInnerException()
    {
        // Arrange
        Marshaler.AllowsDynamicMarshaling = true;
        Lua["target"] = new DynamicAsyncUserDataTarget();

        // Act
        var exception = Assert.Throws<LuaException>(() => Lua.Execute("return target:GetAsync()"));

        // Assert
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("Implicit await requires"));
        Assert.That(exception.Message, Does.Contain("ReturnAwaitable"));
        Assert.That(exception.InnerException, Is.Null);
    }

    [Test]
    public void Execute_SyncTypedUserDataAsyncMethod_ReportsImplicitAwaitRequirementWithoutDuplicateInnerException()
    {
        // Arrange
        Lua["target"] = new TypedAsyncUserDataTarget();

        // Act
        var exception = Assert.Throws<LuaException>(() => Lua.Execute("return target:GetAsync()"));

        // Assert
        Assert.That(exception, Is.Not.Null);
        Assert.That(exception!.Message, Does.Contain("Implicit await requires"));
        Assert.That(exception.Message, Does.Contain("ReturnAwaitable"));
        Assert.That(exception.InnerException, Is.Null);
    }

    [Test]
    public async Task CoroutineResume_WithLayluaAwaitable_ReportsClearError()
    {
        // Arrange
        OpenAsyncLibrary();
        Lua.OpenLibrary(LuaLibraries.Standard.Coroutine);
        await Lua.ExecuteAsync("");
        Lua["pending"] = Task.FromResult(1);

        // Act & Assert
        Assert.That(() => Lua.Execute("""
                local co = coroutine.create(function()
                    return async.await(pending)
                end)

                coroutine.resume(co)
                """),
            Throws.InstanceOf<LuaException>().With.Message.Contains("Implicit await requires"));
    }

    private void OpenAsyncLibrary()
    {
        Lua.OpenLibrary(LuaLibraries.Laylua.Async);
    }
}

public sealed class DynamicAsyncUserDataTarget
{
    public Task<int> GetAsync()
    {
        return Task.FromResult(42);
    }

    public Task<int> GetAsyncWithOut(out int value)
    {
        value = 1;
        return Task.FromResult(42);
    }

    public Task<int> GetAsyncWithRange(LuaStackValueRange arguments)
    {
        return Task.FromResult(arguments.Count);
    }
}

[LuaType]
public partial class TypedAsyncUserDataTarget
{
    public ValueTask<int> GetAsync()
    {
        return new ValueTask<int>(42);
    }
}

[LuaType(AwaitableReturnHandling = LuaAwaitableReturnHandling.ReturnAwaitable)]
public partial class TypedExplicitAwaitAsyncUserDataTarget
{
    public ValueTask<int> GetAsync()
    {
        return new ValueTask<int>(42);
    }
}

internal sealed unsafe class StringUserDataHandler(Type type, string value) : ILuaUserDataHandler
{
    public Type Type { get; } = type;

    public void Push(lua_State* L, object value1)
    {
        lua_pushstring(L, value);
    }

    public bool TryGet(lua_State* L, int stackIndex, out object? value)
    {
        value = null;
        return false;
    }

    public bool TryGetFromTable(lua_State* L, int stackIndex, out object? value)
    {
        value = null;
        return false;
    }

    public void PushDefinition(lua_State* L)
    {
        lua_pushnil(L);
    }

    public void CleanUpState(lua_State* L)
    { }
}
