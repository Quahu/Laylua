using System;

namespace Laylua.Moon;

/// <summary>
///     Defines the arithmetic Lua operations.
/// </summary>
public enum LuaOperation
{
    /// <summary>
    ///     The <c>ADD</c> operation (<c>+</c>).
    /// </summary>
    Add = 0,

    /// <summary>
    ///     The <c>SUB</c> operation (<c>-</c>).
    /// </summary>
    Subtract = 1,

    /// <summary>
    ///     The <c>MUL</c> operation (<c>*</c>).
    /// </summary>
    Multiply = 2,

    /// <summary>
    ///     The <c>MOD</c> operation (<c>%</c>).
    /// </summary>
    Modulo = 3,

    /// <summary>
    ///     The <c>POW</c> operation (<c>^</c> in Lua, <see cref="Math.Pow"/> in C#).
    /// </summary>
    Power = 4,

    /// <summary>
    ///     The <c>DIV</c> operation (<c>/</c>).
    /// </summary>
    Divide = 5,

    /// <summary>
    ///     The <c>IDIV</c> operation (<c>//</c> in Lua, <c>/</c> combined with <see cref="Math.Floor(double)"/> in C#).
    /// </summary>
    FloorDivide = 6,

    /// <summary>
    ///     The <c>BAND</c> operation (<c>&</c>).
    /// </summary>
    BitwiseAnd = 7,

    /// <summary>
    ///     The <c>BOR</c> operation (<c>|</c>).
    /// </summary>
    BitwiseOr = 8,

    /// <summary>
    ///     The <c>BXOR</c> operation (<c>~</c> in Lua, <c>^</c> in C#).
    /// </summary>
    BitwiseExclusiveOr = 9,

    /// <summary>
    ///     The <c>SHL</c> operation (<c>&lt;&lt;</c>).
    /// </summary>
    ShiftLeft = 10,

    /// <summary>
    ///     The <c>SHR</c> operation (<c>&gt;&gt;</c>).
    /// </summary>
    ShiftRight = 11,

    /// <summary>
    ///     The <c>UNM</c> operation (<c>-</c>).
    /// </summary>
    Negate = 12,

    /// <summary>
    ///     The <c>BNOT</c> operation (<c>~</c>).
    /// </summary>
    BitwiseNot = 13
}
