using System;
using Laylua.Marshaling;
using Laylua.Moon;

namespace Laylua;

internal readonly struct ResumeStepResult
{
    public LuaStatus Status { get; }

    public AwaitableBox? AwaitableBox { get; }

    public int ResultCount { get; }

    public ResumeStepResult(LuaStatus status, AwaitableBox? awaitableBox, int resultCount)
    {
        if (resultCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(resultCount), "The result count cannot be negative.");
        }

        if (status == LuaStatus.Yield && awaitableBox == null)
        {
            throw new ArgumentNullException(nameof(awaitableBox), "A yielded coroutine step must provide an awaitable.");
        }

        if (status != LuaStatus.Yield && awaitableBox != null)
        {
            throw new ArgumentException("Only a yielded coroutine step can provide an awaitable.", nameof(awaitableBox));
        }

        Status = status;
        AwaitableBox = awaitableBox;
        ResultCount = resultCount;
    }
}