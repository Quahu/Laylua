namespace Laylua.Marshaling;

/// <summary>
///     Controls how a <see cref="LuaTypeAttribute"/>-annotated type is constructed from Lua.
/// </summary>
/// <remarks>
///     Construction is only available when <see cref="LuaTypeMembers.Constructors"/> is enabled.
///     When that flag is cleared, the type cannot be constructed from Lua regardless of this setting.
/// </remarks>
public enum LuaConstructionMode
{
    /// <summary>
    ///     Make the type directly callable from Lua.
    /// </summary>
    /// <remarks>
    ///     The type definition itself acts as a constructor (e.g. <c>Player(...)</c>).
    /// </remarks>
    Direct = 1,

    /// <summary>
    ///     Construct through a named factory method on the type definition.
    /// </summary>
    /// <remarks>
    ///     The factory method name is configurable via <see cref="LuaTypeAttribute.FactoryMethodName"/>
    ///     (e.g. <c>Player.new(...)</c>).
    /// </remarks>
    Factory = 2,
}
