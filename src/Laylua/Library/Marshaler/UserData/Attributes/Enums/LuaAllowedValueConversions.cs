using System;

namespace Laylua.Marshaling;

/// <summary>
///     Controls which value conversions are permitted during userdata argument binding,
///     overload resolution, constructor resolution, indexer dispatch, and explicit marshaler reads.
/// </summary>
[Flags]
public enum LuaAllowedValueConversions
{
    /// <summary>
    ///     No conversions are permitted. Only direct type matches are accepted.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Allow Lua strings to bind to built-in primitive CLR parameter types: numeric primitives
    ///     and <see cref="bool"/>. Numeric values are parsed using invariant formatting
    ///     (e.g., <c>"42"</c> binding to an <see cref="int"/> parameter), and booleans are parsed
    ///     case-insensitively from <c>"true"</c> / <c>"false"</c>.
    /// </summary>
    StringToPrimitive = 1 << 0,

    /// <summary>
    ///     Allow Lua tables to bind to concrete registered or generated userdata target types
    ///     using handler capability (e.g., table materialization into a CLR type with a parameterless constructor).
    /// </summary>
    TableToObject = 1 << 1,

    /// <summary>
    ///     Allow Lua functions to bind to <see cref="System.Delegate"/> and
    ///     <see cref="System.Reflection.MethodInfo"/> parameter types.
    /// </summary>
    FunctionToCallableTarget = 1 << 2,

    /// <summary>
    ///     Allow <see cref="object"/> parameters to accept any Lua value as a weak catch-all match.
    /// </summary>
    ObjectFallback = 1 << 3,

    /// <summary>
    ///     Allow Lua booleans and numbers to bind to <see cref="string"/> parameters.
    /// </summary>
    PrimitiveToString = 1 << 4,

    /// <summary>
    ///     Allow single-character Lua strings to bind to <see cref="char"/> parameters.
    /// </summary>
    StringToChar = 1 << 5,

    /// <summary>
    ///     Allow Lua strings to bind to enum parameters by exact declared member name.
    /// </summary>
    StringToEnum = 1 << 6,

    /// <summary>
    ///     Allow Lua numbers to bind to enum parameters when the numeric value
    ///     maps to a defined enum member.
    /// </summary>
    NumberToEnum = 1 << 7,

    /// <summary>
    ///     Allow floating-point values with a non-zero fractional part to be converted to
    ///     an integral CLR type by discarding the fraction.
    ///     For example, <c>1.9</c> binding to an <see cref="int"/> parameter truncates to <c>1</c>.
    ///     When this flag is absent, values with a fractional part are rejected.
    /// </summary>
    TruncateFraction = 1 << 8,

    /// <summary>
    ///     Allow numeric values that do not exactly fit the target integral CLR type to be
    ///     converted using unchecked (wrapping) semantics.
    /// </summary>
    /// <remarks>
    ///     For example, <c>300</c> binding to a <see cref="byte"/> parameter wraps to <c>44</c>,
    ///     and a negative Lua integer binding to a <see cref="ulong"/> parameter is
    ///     bit-reinterpreted - enabling roundtrip of values such as <see cref="ulong.MaxValue"/>
    ///     through Lua's signed 64-bit integer representation.
    ///     When this flag is absent, out-of-range integer values are rejected.
    ///     Note: this flag only guarantees defined behavior for integer-to-integer conversions.
    ///     Floating-point sources with out-of-range values have implementation-defined results
    ///     and should be sanitized before this path is reached.
    /// </remarks>
    AllowOverflow = 1 << 9,

    /// <summary>
    ///     The default policy: all conversions except <see cref="TableToObject"/>.
    /// </summary>
    Default = All & ~TableToObject,

    /// <summary>
    ///     All conversions enabled.
    /// </summary>
    All = StringToPrimitive | TableToObject | FunctionToCallableTarget | ObjectFallback | PrimitiveToString | StringToChar | StringToEnum | NumberToEnum | TruncateFraction | AllowOverflow,
}
