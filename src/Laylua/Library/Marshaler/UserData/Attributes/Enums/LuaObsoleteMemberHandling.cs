namespace Laylua.Marshaling;

/// <summary>
///     Controls how members marked with <see cref="System.ObsoleteAttribute"/> are handled.
/// </summary>
public enum LuaObsoleteMemberHandling
{
    /// <summary>
    ///     Obsolete members are not exposed to Lua.
    /// </summary>
    Ignore = 1,

    /// <summary>
    ///     Obsolete members are exposed to Lua.
    /// </summary>
    Include = 2,
}
