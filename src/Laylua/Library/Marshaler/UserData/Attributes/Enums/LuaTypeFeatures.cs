using System;

namespace Laylua.Marshaling;

/// <summary>
///     Selects which behavioral and operator metamethod features are enabled for a Lua-exposed type.
/// </summary>
[Flags]
public enum LuaTypeFeatures
{
    /// <summary>
    ///     No features enabled.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Enable generated <c>__tostring</c> support.
    /// </summary>
    ToString = 1 << 0,

    /// <summary>
    ///     Enable disposal support for types that implement <see cref="IDisposable"/>.
    /// </summary>
    /// <remarks>
    ///     Implemented through both <c>__close</c> (deterministic scope-based cleanup) and
    ///     <c>__gc</c> (fallback cleanup during garbage collection).
    ///     See <a href="https://www.lua.org/manual/5.4/manual.html#3.3.8">Lua manual</a> for information
    ///     on to-be-closed variables.
    /// </remarks>
    Dispose = 1 << 1,

    /// <summary>
    ///     Enable generated <c>__len</c> support.
    /// </summary>
    /// <remarks>
    ///     The generator uses a suitable <c>Count</c>/<c>Length</c> source when available.
    /// </remarks>
    Length = 1 << 2,

    /// <summary>
    ///     Enable generated <c>__pairs</c> support.
    /// </summary>
    /// <remarks>
    ///     Member source selection is configured by <see cref="LuaTypeAttribute.PairsBehavior"/>.
    /// </remarks>
    Pairs = 1 << 3,

    /// <summary>
    ///     <c>operator +</c> -> <c>__add</c>
    /// </summary>
    Add = 1 << 4,

    /// <summary>
    ///     <c>operator -</c> (binary) -> <c>__sub</c>
    /// </summary>
    Subtract = 1 << 5,

    /// <summary>
    ///     <c>operator *</c> -> <c>__mul</c>
    /// </summary>
    Multiply = 1 << 6,

    /// <summary>
    ///     <c>operator /</c> -> <c>__div</c>
    /// </summary>
    Divide = 1 << 7,

    /// <summary>
    ///     <c>operator %</c> -> <c>__mod</c>
    /// </summary>
    Modulus = 1 << 8,

    /// <summary>
    ///     <c>operator -</c> (unary) -> <c>__unm</c>
    /// </summary>
    UnaryNegation = 1 << 9,

    /// <summary>
    ///     <c>operator ==</c> -> <c>__eq</c>
    /// </summary>
    Equality = 1 << 10,

    /// <summary>
    ///     <c>operator &lt;</c> -> <c>__lt</c>
    /// </summary>
    LessThan = 1 << 11,

    /// <summary>
    ///     <c>operator &lt;=</c> -> <c>__le</c>
    /// </summary>
    LessThanOrEqual = 1 << 12,

    /// <summary>
    ///     <c>operator &amp;</c> -> <c>__band</c>
    /// </summary>
    BitwiseAnd = 1 << 13,

    /// <summary>
    ///     <c>operator |</c> -> <c>__bor</c>
    /// </summary>
    BitwiseOr = 1 << 14,

    /// <summary>
    ///     <c>operator ^</c> -> <c>__bxor</c>
    /// </summary>
    ExclusiveOr = 1 << 15,

    /// <summary>
    ///     <c>operator ~</c> -> <c>__bnot</c>
    /// </summary>
    OnesComplement = 1 << 16,

    /// <summary>
    ///     <c>operator &lt;&lt;</c> -> <c>__shl</c>
    /// </summary>
    LeftShift = 1 << 17,

    /// <summary>
    ///     <c>operator &gt;&gt;</c> -> <c>__shr</c>
    /// </summary>
    RightShift = 1 << 18,

    /// <summary>
    ///     All arithmetic operator metamethods.
    /// </summary>
    ArithmeticOperators = Add | Subtract | Multiply | Divide | Modulus | UnaryNegation,

    /// <summary>
    ///     All comparison operator metamethods.
    /// </summary>
    ComparisonOperators = Equality | LessThan | LessThanOrEqual,

    /// <summary>
    ///     All bitwise operator metamethods.
    /// </summary>
    BitwiseOperators = BitwiseAnd | BitwiseOr | ExclusiveOr | OnesComplement | LeftShift | RightShift,

    /// <summary>
    ///     All operator metamethods.
    /// </summary>
    Operators = ArithmeticOperators | ComparisonOperators | BitwiseOperators,

    /// <summary>
    ///     All non-operator behaviors.
    /// </summary>
    Behaviors = ToString | Dispose | Length | Pairs,

    /// <summary>
     ///     All features enabled.
     /// </summary>
    All = Behaviors | Operators,
}
