namespace Laylua.Marshaling;

/// <summary>
///     Specifies how member names are exposed in Lua.
/// </summary>
/// <remarks>
///     Explicit <see cref="LuaNameAttribute"/> values take precedence over this policy.
/// </remarks>
public enum LuaNamingPolicy
{
    /// <summary>
    ///     Keep original C# member names.
    /// </summary>
    Original = 1,

    /// <summary>
    ///     Convert names to lowercase.
    /// </summary>
    LowerCase = 2,

    /// <summary>
    ///     Convert names to UPPERCASE.
    /// </summary>
    UpperCase = 3,

    /// <summary>
    ///     Convert names to camelCase.
    /// </summary>
    CamelCase = 4,

    /// <summary>
    ///     Convert names to lower_snake_case.
    /// </summary>
    LowerSnakeCase = 5,

    /// <summary>
    ///     Convert names to UPPER_SNAKE_CASE.
    /// </summary>
    UpperSnakeCase = 6,

    /// <summary>
    ///     Convert names to PascalCase.
    /// </summary>
    PascalCase = 7,
}
