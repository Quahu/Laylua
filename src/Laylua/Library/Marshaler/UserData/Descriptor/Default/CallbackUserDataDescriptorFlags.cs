using System;
using Laylua.Moon;

namespace Laylua.Marshalling;

/// <summary>
///     Represents the metatable values and methods
///     supported by the <see cref="CallbackBasedUserDataDescriptor"/>.
/// </summary>
[Flags]
public enum CallbackUserDataDescriptorFlags : ulong
{
    None = 0,

    // Name = 1 << 0,

    Pairs = 1 << 1,

    // Metadata = 1 << 2,

    /// <summary>
    ///     The descriptor supports the <see cref="LuaOperation.Add"/>.
    /// </summary>
    Add = 1 << 3,

    /// <summary>
    ///     The descriptor supports the <see cref="LuaOperation.Subtract"/>.
    /// </summary>
    Subtract = 1 << 4,

    /// <summary>
    ///     The descriptor supports the <see cref="LuaOperation.Multiply"/>.
    /// </summary>
    Multiply = 1 << 5,

    /// <summary>
    ///     The descriptor supports the <see cref="LuaOperation.Modulo"/>.
    /// </summary>
    Modulo = 1 << 6,

    /// <summary>
    ///     The descriptor supports the <see cref="LuaOperation.Power"/>.
    /// </summary>
    Power = 1 << 7,

    /// <summary>
    ///     The descriptor supports the <see cref="LuaOperation.Divide"/>.
    /// </summary>
    Divide = 1 << 8,

    /// <summary>
    ///     The descriptor supports the <see cref="LuaOperation.FloorDivide"/>.
    /// </summary>
    FloorDivide = 1 << 9,

    /// <summary>
    ///     The descriptor supports the <see cref="LuaOperation.BitwiseAnd"/>.
    /// </summary>
    BitwiseAnd = 1 << 10,

    /// <summary>
    ///     The descriptor supports the <see cref="LuaOperation.BitwiseOr"/>.
    /// </summary>
    BitwiseOr = 1 << 11,

    /// <summary>
    ///     The descriptor supports the <see cref="LuaOperation.BitwiseExclusiveOr"/>.
    /// </summary>
    BitwiseExclusiveOr = 1 << 12,

    /// <summary>
    ///     The descriptor supports the <see cref="LuaOperation.ShiftLeft"/>.
    /// </summary>
    ShiftLeft = 1 << 13,

    /// <summary>
    ///     The descriptor supports the <see cref="LuaOperation.ShiftRight"/>.
    /// </summary>
    ShiftRight = 1 << 14,

    /// <summary>
    ///     The descriptor supports the <see cref="LuaOperation.Negate"/>.
    /// </summary>
    Negate = 1 << 15,

    /// <summary>
    ///     The descriptor supports the <see cref="LuaOperation.BitwiseNot"/>.
    /// </summary>
    BitwiseNot = 1 << 16,

    Concat = 1 << 17,

    Length = 1 << 18,

    /// <summary>
    ///     The descriptor supports the <see cref="LuaComparison.Equal"/>.
    /// </summary>
    Equal = 1 << 19,

    /// <summary>
    ///     The descriptor supports the <see cref="LuaComparison.LessThan"/>.
    /// </summary>
    LessThan = 1 << 20,

    /// <summary>
    ///     The descriptor supports the <see cref="LuaComparison.LessThanOrEqual"/>.
    /// </summary>
    LessThanOrEqual = 1 << 21,

    Index = 1 << 22,

    NewIndex = 1 << 23,

    Call = 1 << 24,

    Close = 1 << 25,

    ToString = 1 << 27
}
