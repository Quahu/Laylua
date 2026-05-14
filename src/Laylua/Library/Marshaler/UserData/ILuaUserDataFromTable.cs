using System.Diagnostics.CodeAnalysis;

namespace Laylua.Marshaling;

/// <summary>
///     Defines a static conversion contract for reading a CLR value from a Lua table.
/// </summary>
/// <typeparam name="TUserData"> The CLR type being materialized. </typeparam>
public interface ILuaUserDataFromTable<TUserData>
{
    /// <summary>
    ///     Attempts to materialize a value from the specified Lua table.
    /// </summary>
    /// <param name="thread"> The owning Lua thread. </param>
    /// <param name="table"> The source Lua table. </param>
    /// <param name="value"> The materialized value when conversion succeeds. </param>
    /// <param name="error"> The conversion error, if conversion failed. </param>
    /// <returns> <see langword="true"/> when conversion succeeded; otherwise <see langword="false"/>. </returns>
    static abstract bool TryGetFromTable(
        LuaThread thread,
        LuaTable table,
        [MaybeNullWhen(false)] out TUserData value,
        [NotNullWhen(false)] out LuaConversionError? error);
}
