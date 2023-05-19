using System;

namespace Laylua.Moon;

/// <summary>
///     Represents a Lua hook, i.e. a type that can trigger on various events based on the <see cref="LuaEventMask"/>.
/// </summary>
public abstract unsafe class LuaHook : IDisposable
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
    ///     Executes this hook.
    /// </summary>
    /// <remarks>
    ///     The code must not throw any exceptions.
    /// </remarks>
    /// <param name="L"> The Lua state. </param>
    /// <param name="ar"> The pointer to the debug information. </param>
    protected internal abstract void Execute(lua_State* L, lua_Debug* ar);

    protected virtual void Dispose(bool disposing)
    { }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
