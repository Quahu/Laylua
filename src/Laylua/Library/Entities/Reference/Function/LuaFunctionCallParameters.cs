namespace Laylua;

/// <summary>
///     Represents parameters for calling a <see cref="LuaFunction"/>.
/// </summary>
public struct LuaFunctionCallParameters
{
    /// <summary>
    ///     The thread calling the function. If <see langword="null"/>, defaults to the main thread.
    /// </summary>
    public LuaThread? Caller { get; set; }

    /// <summary>
    ///     The number of arguments to call the function with.
    ///     The arguments are taken from <see cref="Caller"/>'s stack.
    /// </summary>
    public int ArgumentCount { get; set; }

    /// <summary>
    ///     The maximum number of values the function can return.
    ///     The results are pushed onto <see cref="Caller"/>'s stack.
    ///     If <see langword="null"/>, there is no limit.
    /// </summary>
    public int? ResultCount { get; set; }
}
