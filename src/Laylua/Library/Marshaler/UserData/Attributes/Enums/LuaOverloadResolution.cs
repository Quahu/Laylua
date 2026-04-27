namespace Laylua.Marshaling;

/// <summary>
///     Controls how overloaded methods are resolved when multiple overloads match.
/// </summary>
public enum LuaOverloadResolution
{
    /// <summary>
    ///     Attempt type-based dispatch to find the best matching overload.
    /// </summary>
    BestMatch = 1,

    /// <summary>
    ///     Raise a Lua error when multiple overloads match the given arguments.
    /// </summary>
    Error = 2,
}
