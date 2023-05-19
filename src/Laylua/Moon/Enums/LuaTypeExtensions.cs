using System.ComponentModel;

namespace Laylua.Moon;

/// <summary>
///     Represents extension methods for <see cref="LuaType"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class LuaTypeExtensions
{
    /// <summary>
    ///     Checks if the type represents no value or nil.
    /// </summary>
    /// <param name="type"> The type to check. </param>
    /// <returns>
    ///     <see langword="true"/> if the type represents no value or nil.
    /// </returns>
    public static bool IsNoneOrNil(this LuaType type)
    {
        return type <= LuaType.Nil;
    }

    /// <summary>
    ///     Checks if the type represents a value.
    /// </summary>
    /// <param name="type"> The type to check. </param>
    /// <returns>
    ///     <see langword="true"/> if the type represents a value.
    /// </returns>
    public static bool IsValue(this LuaType type)
    {
        return type > LuaType.Nil;
    }
}
