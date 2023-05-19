namespace Laylua.Moon;

/// <summary>
///     Represents event data for <see cref="LuaState.Panicked"/>.
/// </summary>
public readonly struct LuaPanickedEventArgs
{
    /// <summary>
    ///     Gets the panic exception.
    /// </summary>
    public LuaPanicException Exception { get; }

    internal LuaPanickedEventArgs(LuaPanicException exception)
    {
        Exception = exception;
    }
}
