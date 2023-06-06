using System;

namespace Laylua.Marshaling;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Constructor)]
public class LuaIgnoreAttribute : Attribute
{ }
