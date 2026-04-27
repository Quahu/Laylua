using System;

namespace Laylua.Marshaling;

/// <summary>
///     Flags that describe additional characteristics of a marshaling compatibility result.
/// </summary>
[Flags]
public enum LuaMarshalingCompatibilityFlags : ushort
{
    /// <summary>
    ///     No additional characteristics.
    /// </summary>
    None = 0,

    /// <summary>
    ///     The compatibility result depends on the actual runtime value, not just the Lua type.
    /// </summary>
    ValueDependent = 1 << 0,

    /// <summary>
    ///     The target type may reject out-of-range values at conversion time.
    /// </summary>
    NeedsRangeValidation = 1 << 1,

    /// <summary>
    ///     The match uses the catch-all <see cref="object"/> parameter fallback.
    /// </summary>
    UsesObjectFallback = 1 << 2,

    /// <summary>
    ///     The match requires Lua table to CLR object materialization.
    /// </summary>
    UsesTableMaterialization = 1 << 3,

    /// <summary>
    ///     The match wraps the Lua reference in a <c>LuaWeakReference&lt;T&gt;</c>.
    /// </summary>
    UsesWeakReferenceWrapper = 1 << 4,

    /// <summary>
    ///     The match converts a Lua function to a .NET delegate or <see cref="System.Reflection.MethodInfo"/>.
    /// </summary>
    UsesDelegateBinding = 1 << 5,

    /// <summary>
    ///     The match requires parsing a Lua string to a built-in primitive type
    ///     (numeric or <see cref="bool"/>).
    /// </summary>
    UsesStringParsing = 1 << 6,

    /// <summary>
    ///     The match converts a Lua boolean or number to a string representation.
    /// </summary>
    UsesPrimitiveToString = 1 << 7,

    /// <summary>
    ///     The match converts a single-character Lua string to <see cref="char"/>.
    /// </summary>
    UsesStringToChar = 1 << 8,

    /// <summary>
    ///     The match converts a Lua string to an enum value by name.
    /// </summary>
    UsesStringToEnum = 1 << 9,

    /// <summary>
    ///     The match converts a Lua number to an enum value.
    /// </summary>
    UsesNumberToEnum = 1 << 10,
}
