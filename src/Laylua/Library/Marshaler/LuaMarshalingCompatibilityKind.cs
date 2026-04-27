namespace Laylua.Marshaling;

/// <summary>
///     Identifies the kind of conversion a marshaler can perform when binding
///     a Lua value to a CLR target type during marshaling.
/// </summary>
/// <remarks>
///     The numeric value of each member serves as the base cost for that conversion tier.
///     Lower values indicate cheaper (more preferred) conversions.
/// </remarks>
public enum LuaMarshalingCompatibilityKind : byte
{
    /// <summary>
    ///     Sentinel indicating the marshaler cannot convert the value.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Exact primitive match: bool, integer-to-integer, float-to-float/decimal, string-to-string.
    /// </summary>
    ExactPrimitive = 1,

    /// <summary>
    ///     Exact Lua reference match: <see cref="LuaTable"/>, <see cref="LuaFunction"/>,
    ///     <see cref="LuaUserData"/>, <see cref="LuaThread"/>, or weak reference wrappers thereof.
    /// </summary>
    ExactLuaReference = 2,

    /// <summary>
    ///     Exact typed userdata match via a registered or assignable handler.
    /// </summary>
    ExactTypedUserData = 3,

    /// <summary>
    ///     Lua function resolved to <see cref="System.Reflection.MethodInfo"/> or an open delegate.
    /// </summary>
    FunctionToMethodInfo = 4,

    /// <summary>
    ///     Lua function wrapped as a .NET delegate.
    /// </summary>
    FunctionToDelegate = 5,

    /// <summary>
    ///     Lua table materialized to a CLR object via a registered handler.
    /// </summary>
    TableMaterialization = 6,

    /// <summary>
    ///     Non-lossy numeric widening: integer to double/decimal, or similar lifts.
    /// </summary>
    NumericWidening = 7,

    /// <summary>
    ///     Numeric narrowing that passes a runtime range check.
    /// </summary>
    NumericNarrowingChecked = 8,

    /// <summary>
    ///     Lua string (single character) converted to <see cref="char"/>.
    /// </summary>
    StringToChar = 9,

    /// <summary>
    ///     Lua string parsed to a built-in primitive CLR type (numeric or <see cref="bool"/>) by the marshaler.
    /// </summary>
    StringToPrimitive = 10,

    /// <summary>
    ///     Lua string converted to an enum member by exact name match.
    /// </summary>
    StringToEnum = 11,

    /// <summary>
    ///     Lua number converted to an enum member by defined value.
    /// </summary>
    NumberToEnum = 12,

    /// <summary>
    ///     Lua boolean or number converted to string via formatting.
    /// </summary>
    PrimitiveToString = 13,

    /// <summary>
    ///     Catch-all <see cref="object"/> parameter fallback.
    /// </summary>
    ObjectFallback = 100,
}
