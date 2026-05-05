using System;
using System.Diagnostics;
using Laylua.Marshaling;
using Laylua.Moon;

namespace Laylua;

internal static unsafe class LuaCoroutineRuntime
{
    public static int PushFunctionAndArguments(LuaThread coroutineThread, LuaFunction function, object?[] arguments)
    {
        PrepareFunctionCall(coroutineThread, function, arguments.Length);

        foreach (var argument in arguments)
        {
            coroutineThread.Stack.Push(argument);
        }

        return arguments.Length;
    }

    public static int PushFunctionAndStackArguments(LuaThread coroutineThread, LuaFunction function, ReadOnlySpan<LuaStackValue> arguments)
    {
        PrepareFunctionCall(coroutineThread, function, arguments.Length);

        foreach (var argument in arguments)
        {
            PushStackArgument(coroutineThread, argument);
        }

        return arguments.Length;
    }

    public static int PushFunctionAndStackArguments(LuaThread coroutineThread, LuaFunction function, LuaStackValueRange arguments)
    {
        PrepareFunctionCall(coroutineThread, function, arguments.Count);

        foreach (var argument in arguments)
        {
            PushStackArgument(coroutineThread, argument);
        }

        return arguments.Count;
    }

    private static void PrepareFunctionCall(LuaThread coroutineThread, LuaFunction function, int argumentCount)
    {
        coroutineThread.Stack.EnsureFreeCapacity(1 + argumentCount);
        coroutineThread.Stack.Push(function);
    }

    private static void PushStackArgument(LuaThread coroutineThread, LuaStackValue argument)
    {
        var sourceThread = argument.Thread;
        if (argument.Type == LuaType.None)
        {
            throw new InvalidOperationException("The given stack value is not valid.");
        }

        if (sourceThread.MainThread.State.L != coroutineThread.MainThread.State.L)
        {
            throw new InvalidOperationException("The given stack value is owned by a different Lua state.");
        }

        sourceThread.Stack.EnsureFreeCapacity(1);
        coroutineThread.Stack.EnsureFreeCapacity(1);

        lua_pushvalue(sourceThread.State.L, argument.Index);
        lua_xmove(sourceThread.State.L, coroutineThread.State.L, 1);
    }

    public static ResumeStepResult ResumeStep(LuaThread coroutineThread, LuaThread fromThread, int argumentCount)
    {
        var coroutineL = coroutineThread.State.L;
        var fromL = fromThread.State.L;
        var laylua = LayluaState.FromExtraSpace(coroutineL);
        var initialPanic = laylua.Panic;

        var status = lua_resume(coroutineL, fromL, argumentCount, out var resultCount);

        if (status == LuaStatus.Yield)
        {
            // Inside the yielding C function, lua_yield long-jumps directly back into
            // lua_resume, bypassing the assembly trampoline that wraps every native Lua
            // export and would normally pop the panic frame it pushed around lua_yieldk.
            // Drain that orphaned frame here so the panic stack stays balanced.
            while (!ReferenceEquals(laylua.Panic, initialPanic))
            {
                var leakedPanic = laylua.PopPanic();
                if (leakedPanic == null)
                {
                    break;
                }

                laylua.ResetPanic(leakedPanic);
            }

            Debug.Assert(ReferenceEquals(laylua.Panic, initialPanic));

            if (resultCount != 1 || !AwaitableUserData.IsAwaitable(coroutineL, -1))
            {
                lua_settop(coroutineL, 0);
                throw new LuaException("The Lua coroutine yielded a value that is not an awaitable.");
            }

            if (!AwaitableUserData.TryGetBox(coroutineL, -1, out var box) || box is null)
            {
                lua_settop(coroutineL, 0);
                throw new LuaException("Failed to extract the awaitable from the yielded user data.");
            }

            lua_pop(coroutineL);
            return new ResumeStepResult(LuaStatus.Yield, box, resultCount: 0);
        }

        if (status == LuaStatus.Ok)
        {
            Debug.Assert(ReferenceEquals(laylua.Panic, initialPanic));
            return new ResumeStepResult(LuaStatus.Ok, awaitableBox: null, resultCount);
        }

        Debug.Assert(ReferenceEquals(laylua.Panic, initialPanic));
        LuaThread.ThrowLuaException(coroutineThread, status);
        return default;
    }

    public static void ClearStack(LuaThread coroutineThread)
    {
        lua_settop(coroutineThread.State.L, 0);
    }

    public static T? ReadFirstResult<T>(LuaThread coroutineThread, int resultCount)
    {
        try
        {
            if (resultCount == 0)
            {
                throw new InvalidOperationException("The code evaluation succeeded, but returned no results.");
            }

            return coroutineThread.MainThread.Marshaler.GetValue<T>(coroutineThread, 1);
        }
        finally
        {
            ClearStack(coroutineThread);
        }
    }

    public static LuaFunctionResults CollectResults(LuaThread coroutineThread, LuaThread destinationThread, int resultCount)
    {
        if (resultCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(resultCount), "The result count cannot be negative.");
        }

        var oldTop = destinationThread.Stack.Count;
        coroutineThread.Stack.XMove(destinationThread, resultCount);

        var range = LuaStackValueRange.FromTop(destinationThread.Stack, oldTop, destinationThread.Stack.Count);
        return new LuaFunctionResults(range);
    }
}
