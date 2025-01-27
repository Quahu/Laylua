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
    /// <remarks>
    ///     The code must not throw any exceptions.
    /// </remarks>
    /// <param name="lua"> The Lua state. </param>
    /// <param name="debug"> The debug information. </param>
    protected internal abstract void Execute(LuaThread lua, LuaDebug debug);
}
