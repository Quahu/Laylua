using System;

namespace Laylua.Marshaling;

/// <summary>
///     Marks a method parameter that should receive the current calling <see cref="Laylua.LuaThread"/>.
/// </summary>
/// <remarks>
///     The source-generated wrapper injects the thread for this parameter and does not consume
///     a Lua stack argument for it. This attribute only affects parameter binding; it does not
///     change return-value semantics. Use <see cref="LuaReturnCountAttribute"/> on the return value
///     to opt into raw Lua return-count behavior.
///     See <a href="https://www.lua.org/manual/5.4/manual.html#4.1">Lua manual</a> for information on how
///     stack arguments are passed to functions.
/// </remarks>
/// <example>
/// <code>
///     public void MyMethod([LuaCallingThread] LuaThread lua, string arg)
///     {
///         // 'lua' is the calling thread, 'arg' comes from the Lua stack
///     }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class LuaCallingThreadAttribute : Attribute;
