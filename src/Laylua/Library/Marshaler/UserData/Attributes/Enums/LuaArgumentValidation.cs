using System;

namespace Laylua.Marshaling;

/// <summary>
///     Controls argument validation behavior for generated and dynamic userdata methods.
/// </summary>
[Flags]
public enum LuaArgumentValidation
{
    /// <summary>
    ///     Skip all argument validation (fastest, but may produce confusing errors on misuse).
    /// </summary>
    None = 0,

    /// <summary>
    ///     Enforce nullability annotations.
    /// </summary>
    /// <remarks>
    ///     Non-nullable reference parameters reject <c>nil</c> with a Lua error.
    /// </remarks>
    Nullability = 1 << 0,

    /// <summary>
    ///     Validate argument types against expected parameter types.
    /// </summary>
    Type = 1 << 1,

    /// <summary>
    ///     Validate argument count against the expected parameter count.
    /// </summary>
    Count = 1 << 2,

    /// <summary>
    ///     Validate that numeric arguments fit within the target CLR type's range.
    /// </summary>
    /// <remarks>
    ///     For example, a Lua number exceeding <see cref="byte.MaxValue"/> when passed to a <c>byte</c> parameter
    ///     will raise a Lua error instead of silently truncating.
    /// </remarks>
    Range = 1 << 3,

    /// <summary>
    ///     Enable all validation checks (<see cref="Nullability"/> | <see cref="Type"/> | <see cref="Count"/> | <see cref="Range"/>).
    /// </summary>
    Strict = Nullability | Type | Count | Range,
}
