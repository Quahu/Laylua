using System;

namespace Laylua.Marshaling;

/// <summary>
///     Selects generated interop fast paths for typed userdata.
/// </summary>
[Flags]
public enum LuaTypeOptimizations
{
    /// <summary>
    ///     No optional optimizations enabled.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Use direct Lua C API fast paths for exact primitive boundary values when that preserves
    ///     the same conversion semantics as the marshaler.
    /// </summary>
    PrimitiveMarshalerBypass = 1 << 0,

    /// <summary>
    ///     All supported optimizations enabled.
    /// </summary>
    All = PrimitiveMarshalerBypass,
}
