using System;

namespace Laylua.Marshaling;

/// <summary>
///     Exposes a non-public member to Lua.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Event)]
public class LuaIncludeAttribute : Attribute;
