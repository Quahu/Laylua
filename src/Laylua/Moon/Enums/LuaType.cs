namespace Laylua.Moon;

/// <summary>
///     Defines the types of Lua objects.
/// </summary>
public enum LuaType
{
    /// <summary>
    ///     Represents no type, i.e. an invalid value index.
    /// </summary>
    None = -1,

    /// <summary>
    ///     Represents a Lua <see langword="null"/> value.
    /// </summary>
    Nil = 0,

    /// <summary>
    ///     Represents a Lua <see langword="true"/> or <see langword="false"/>, i.e. a <see cref="bool"/> value.
    /// </summary>
    Boolean = 1,

    /// <summary>
    ///     Represents a Lua light userdata, i.e. a pointer to userdata that is not managed by Lua.
    /// </summary>
    LightUserData = 2,

    /// <summary>
    ///     Represents a Lua number, i.e. a <see cref="double"/> or a <see cref="long"/> value.
    /// </summary>
    Number = 3,

    /// <summary>
    ///     Represents a Lua string, i.e. a <see cref="string"/> value.
    /// </summary>
    String = 4,

    /// <summary>
    ///     Represents a Lua table.
    /// </summary>
    Table = 5,

    /// <summary>
    ///     Represents a Lua function.
    /// </summary>
    Function = 6,

    /// <summary>
    ///     Represents a Lua user data.
    /// </summary>
    UserData = 7,

    /// <summary>
    ///     Represents a Lua thread.
    /// </summary>
    Thread = 8
}
