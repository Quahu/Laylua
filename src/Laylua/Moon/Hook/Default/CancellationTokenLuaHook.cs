using System.Threading;

namespace Laylua.Moon;

/// <summary>
///     Represents a Lua hook that raises an error
///     when the specified cancellation token signals cancellation.
/// </summary>
public sealed unsafe class CancellationTokenLuaHook : LuaHook
{
    /// <inheritdoc/>
    protected internal override LuaEventMask EventMask => LuaEventMask.Call | LuaEventMask.Return | LuaEventMask.Line | LuaEventMask.Count;

    /// <inheritdoc/>
    protected internal override int InstructionCount => 1;

    private readonly CancellationToken _cancellationToken;

    /// <summary>
    ///     Instantiates a new <see cref="CancellationTokenLuaHook"/>.
    /// </summary>
    /// <param name="cancellationToken"> The cancellation token to observe. </param>
    public CancellationTokenLuaHook(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
    }

    /// <inheritdoc/>
    protected internal override void Execute(lua_State* L, lua_Debug* ar)
    {
        if (!_cancellationToken.IsCancellationRequested)
        {
            return;
        }

        luaL_error(L, "The execution has been cancelled.");
    }
}
