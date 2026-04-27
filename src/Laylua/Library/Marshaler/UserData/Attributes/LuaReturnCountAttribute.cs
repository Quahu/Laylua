using System;

namespace Laylua.Marshaling;

/// <summary>
///     Marks a method return value as a raw Lua return-count.
/// </summary>
/// <remarks>
///     Use this on methods that also accept a <see cref="LuaCallingThreadAttribute"/> parameter when
///     the method pushes results directly onto the Lua stack and returns how many values were pushed.
///     See <a href="https://www.lua.org/manual/5.4/manual.html#4.1">Lua manual</a> for information on
///     Lua function return counts.
/// </remarks>
[AttributeUsage(AttributeTargets.ReturnValue)]
public sealed class LuaReturnCountAttribute : Attribute;
