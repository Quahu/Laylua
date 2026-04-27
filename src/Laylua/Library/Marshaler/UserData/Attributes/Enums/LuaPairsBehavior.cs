namespace Laylua.Marshaling;

/// <summary>
///     Controls which member source is used for <c>pairs()</c> iteration.
/// </summary>
/// <remarks>
///     This setting is used only when <see cref="LuaTypeFeatures.Pairs"/> is enabled.
/// </remarks>
public enum LuaPairsBehavior
{
    /// <summary>
    ///     Enumerate elements when the type implements <see cref="System.Collections.IEnumerable"/>.
    /// </summary>
    Enumerable = 1,

    /// <summary>
    ///     Enumerate named properties and fields via <c>pairs()</c>.
    /// </summary>
    Properties = 2,
}
