namespace Laylua.Moon;

/// <summary>
///     Defines the comparison types in Lua.
/// </summary>
public enum LuaComparison
{
    /// <summary>
    ///     The <c>EQ</c> (<c>==</c>) comparison.
    /// </summary>
    Equal = 0,

    /// <summary>
    ///     The <c>LT</c> (<c>&lt;</c>) comparison.
    /// </summary>
    LessThan = 1,

    /// <summary>
    ///     The <c>LE</c> (<c>&lt;=</c>) comparison.
    /// </summary>
    LessThanOrEqual = 2
}
