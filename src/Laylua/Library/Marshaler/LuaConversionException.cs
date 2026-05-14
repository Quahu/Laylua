using System;

namespace Laylua.Marshaling;

/// <summary>
///     Represents a failed Lua-to-CLR conversion.
/// </summary>
public sealed class LuaConversionException(LuaConversionError error)
    : InvalidOperationException($"Lua conversion failed: {error}")
{
    /// <summary>
    ///     Gets the conversion error.
    /// </summary>
    public LuaConversionError Error { get; } = error ?? throw new ArgumentNullException(nameof(error));
}
