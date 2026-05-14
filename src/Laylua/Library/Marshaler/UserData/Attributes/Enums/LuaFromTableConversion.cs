namespace Laylua.Marshaling;

/// <summary>
///     Controls whether source-generated typed userdata can be materialized from Lua tables.
/// </summary>
public enum LuaFromTableConversion
{
    /// <summary>
    ///     Do not emit generated table-to-object conversion support for the type.
    /// </summary>
    Disabled = 1,

    /// <summary>
    ///     Emit generated table-to-object conversion support when the type has a supported parameterless-construction path.
    /// </summary>
    WhenTypeHasParameterlessConstructor = 2,

    /// <summary>
    ///     Emit generated table-to-object conversion support and thus require a parameterless constructor.
    /// </summary>
    Enabled = 3,
}
