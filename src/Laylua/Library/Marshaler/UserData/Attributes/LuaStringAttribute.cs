using System;

namespace Laylua.Marshaling;

/// <summary>
///     Marks a parameter or property whose type is <see cref="ReadOnlySpan{T}"/>
///     to be treated as a Lua string for marshaling purposes.
/// </summary>
/// <example>
/// <code>
///     [LuaString]
///     public ReadOnlySpan&lt;byte&gt; Utf8Name => "hello"u8;
///     
///     public int Parse([LuaString] ReadOnlySpan&lt;byte&gt; utf8Text)
///     {
///         return int.Parse(utf8Text);
///     }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
public sealed class LuaStringAttribute : Attribute;
