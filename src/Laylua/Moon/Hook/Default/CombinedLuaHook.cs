using System;
using System.Collections.Generic;
using System.Linq;

namespace Laylua.Moon;

/// <summary>
///     Represents a hook combined from multiple hooks.
/// </summary>
public sealed class CombinedLuaHook : LuaHook
{
    /// <summary>
    ///     Gets the combined hooks.
    /// </summary>
    public IReadOnlyCollection<LuaHook> Hooks => _hooks;

    /// <inheritdoc/>
    protected internal override LuaEventMask EventMask { get; }

    /// <inheritdoc/>
    protected internal override int InstructionCount { get; }

    private readonly LuaHook[] _hooks;
    private readonly int[] _instructionCounts;

    public CombinedLuaHook(params ReadOnlySpan<LuaHook> hooks)
    {
        _hooks = hooks.ToArray();
        _instructionCounts = new int[_hooks.Length];

        if (_hooks.Length != 0)
        {
            EventMask = _hooks.Aggregate(LuaEventMask.None, static (current, hook) => current | hook.EventMask);
            InstructionCount = _hooks.Select(static hook => hook.InstructionCount).Aggregate(static (a, b) =>
            {
                // GCD
                while (b != 0)
                {
                    (a, b) = (b, a % b);
                }

                return a;
            });
        }
    }

    /// <inheritdoc/>
    protected internal override void Execute(LuaThread thread, LuaEvent @event, ref LuaDebug debug)
    {
        for (var hookIndex = 0; hookIndex < _hooks.Length; hookIndex++)
        {
            var hook = _hooks[hookIndex];
            var maskFlag = GetMaskForEvent(@event);
            if (!hook.EventMask.HasFlag(maskFlag))
                continue;

            if (maskFlag == LuaEventMask.Count)
            {
                _instructionCounts[hookIndex] += InstructionCount;

                if (hook.InstructionCount != _instructionCounts[hookIndex])
                    continue;

                _instructionCounts[hookIndex] = 0;
            }

            hook.Execute(thread, @event, ref debug);
        }
    }

    private static LuaEventMask GetMaskForEvent(LuaEvent @event)
    {
        return @event switch
        {
            LuaEvent.Call or LuaEvent.TailCall => LuaEventMask.Call,
            LuaEvent.Return => LuaEventMask.Return,
            LuaEvent.Line => LuaEventMask.Line,
            LuaEvent.Count => LuaEventMask.Count,
            _ => LuaEventMask.None
        };
    }
}
