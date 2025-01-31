using System;

namespace Laylua.Moon;

/// <summary>
///     Represents a Lua hook, i.e. a type that can trigger on various events based on the <see cref="LuaEventMask"/>.
/// </summary>
public abstract class LuaHook
{
    /// <summary>
    ///     Gets the events this hook is subscribed to.
    /// </summary>
    protected internal abstract LuaEventMask EventMask { get; }

    /// <summary>
    ///     Gets the instruction count this hook should be executed on.
    /// </summary>
    /// <remarks>
    ///     Only applicable if <see cref="EventMask"/> contains the <see cref="LuaEventMask.Count"/> flag.
    /// </remarks>
    protected internal abstract int InstructionCount { get; }

    /// <summary>
    ///     Invoked when this hook is triggered.
    /// </summary>
    /// <param name="thread"> The Lua thread. </param>
    /// <param name="debug"> The debug information. </param>
    protected internal abstract void Execute(LuaThread thread, ref LuaDebug debug);

    /// <summary>
    ///     Combines multiple hooks into one.
    /// </summary>
    /// <param name="hooks"> The hooks to combine. </param>
    /// <returns>
    ///     A hook representing the combined hooks.
    /// </returns>
    public static CombinedLuaHook Combine(params Span<LuaHook> hooks)
    {
        return new CombinedLuaHook(hooks);
    }
}
