using System;

namespace Laylua.Marshaling;

/// <summary>
///     Overrides the Lua-visible name for a member.
/// </summary>
/// <remarks>
///     When present, this takes precedence over naming policy transformations.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Event)]
public class LuaNameAttribute(string name) : Attribute
{
    /// <summary>
    ///     Gets the explicit Lua-visible name.
    /// </summary>
    public string Name { get; } = name;
}
