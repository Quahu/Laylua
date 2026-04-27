using System;

namespace Laylua.Marshaling;

/// <summary>
///     Excludes a member from Lua.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Event)]
public class LuaIgnoreAttribute : Attribute;
