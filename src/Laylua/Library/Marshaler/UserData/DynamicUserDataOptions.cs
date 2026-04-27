using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Laylua.Marshaling;

/// <summary>
///     Configuration options for dynamic (reflection-based) userdata types.
///     Applied via <see cref="LuaTypeRegistry.DynamicDefaults"/> for global defaults,
///     or <see cref="LuaTypeRegistry.ConfigureDynamic{T}"/> for per-type overrides.
///     These options do <b>not</b> affect <see cref="LuaTypeAttribute"/>-annotated types, which use
///     source-generated handlers configured via their attribute and assembly-level <see cref="LuaTypeDefaultsAttribute"/>.
/// </summary>
public class DynamicUserDataOptions
{
    // Dynamic defaults intentionally feel closer to direct C# runtime usage than generated defaults do.
    // E.g., dynamic includes System.Object members and obsolete members by default, while generated keeps both off unless explicitly enabled.

    /// <summary>
    ///     The naming policy applied to C# member names when exposing them to Lua.
    ///     Defaults to <see cref="LuaNamingPolicy.Original"/> (preserves PascalCase).
    /// </summary>
    public LuaNamingPolicy NamingPolicy { get; set; } = LuaNamingPolicy.Original;

    /// <summary>
    ///     Controls which member categories are exposed for the type.
    /// </summary>
    public LuaTypeMembers Members { get; set; } = LuaTypeMembers.All;

    /// <summary>
    ///     Controls which behavioral and operator metamethod features are enabled for the type.
    /// </summary>
    public LuaTypeFeatures Features { get; set; } = LuaTypeFeatures.All;

    /// <summary>
    ///     Controls the <c>__pairs</c> behavior.
    ///     Defaults to <see cref="LuaPairsBehavior.Enumerable"/>.
    /// </summary>
    public LuaPairsBehavior PairsBehavior { get; set; } = LuaPairsBehavior.Enumerable;

    /// <summary>
    ///     Controls what happens when Lua accesses an unknown property key.
    ///     Defaults to <see cref="LuaUnknownPropertyBehavior.Silent"/>.
    /// </summary>
    public LuaUnknownPropertyBehavior UnknownPropertyBehavior { get; set; } = LuaUnknownPropertyBehavior.Silent;

    /// <summary>
    ///     Controls how overloaded methods are resolved when multiple overloads match.
    ///     Defaults to <see cref="LuaOverloadResolution.BestMatch"/>.
    /// </summary>
    public LuaOverloadResolution OverloadResolution { get; set; } = LuaOverloadResolution.BestMatch;

    /// <summary>
    ///     Controls which member categories are exposed through <c>__index</c>/<c>__newindex</c>.
    ///     Defaults to <see cref="LuaIndexMembers.All"/>.
    /// </summary>
    public LuaIndexMembers IndexMembers { get; set; } = LuaIndexMembers.All;

    /// <summary>
    ///     Controls the dispatch priority order for <c>__index</c>/<c>__newindex</c>.
    ///     Defaults to <see cref="LuaIndexPriority.MembersFirst"/>.
    /// </summary>
    public LuaIndexPriority IndexPriority { get; set; } = LuaIndexPriority.MembersFirst;

    /// <summary>
    ///     Controls which <see cref="object"/>-inherited members are exposed to Lua.
    ///     Defaults to <see cref="LuaSystemObjectMembers.All"/>.
    ///     This intentionally differs from generated defaults, which keep these members hidden unless opted into.
    /// </summary>
    public LuaSystemObjectMembers ObjectMembers { get; set; } = LuaSystemObjectMembers.All;

    /// <summary>
    ///     Controls argument validation behavior.
    ///     Defaults to <see cref="LuaArgumentValidation.Strict"/>.
    /// </summary>
    public LuaArgumentValidation ArgumentValidation { get; set; } = LuaArgumentValidation.Strict;

    /// <summary>
    ///     Controls how members marked with <see cref="System.ObsoleteAttribute"/> are handled.
    ///     Defaults to <see cref="LuaObsoleteMemberHandling.Include"/>.
    ///     This intentionally differs from generated defaults, which ignore obsolete members unless opted into.
    /// </summary>
    public LuaObsoleteMemberHandling ObsoleteMembers { get; set; } = LuaObsoleteMemberHandling.Include;

    /// <summary>
    ///     Controls which value conversions are permitted during argument binding and overload resolution.
    ///     Defaults to <see cref="LuaAllowedValueConversions.Default"/>.
    /// </summary>
    public LuaAllowedValueConversions BindingConversions { get; set; } = LuaAllowedValueConversions.Default;

    /// <summary>
    ///     An optional predicate that decides whether a reflected member should be exposed to Lua.
    ///     Return <see langword="true"/> to keep the member, or <see langword="false"/> to skip it.
    ///     Applies to properties, fields, methods, operators, indexers, events, constructors,
    ///     and length-member discovery.
    /// </summary>
    public Predicate<MemberInfo>? MemberFilter { get; set; }

    /// <summary>
    ///     Controls how instances are constructed from Lua.
    ///     Defaults to <see cref="LuaConstructionMode.Direct"/>.
    /// </summary>
    public LuaConstructionMode ConstructionMode { get; set; } = LuaConstructionMode.Direct;

    /// <summary>
    ///     The Lua name used for the factory method when
    ///     <see cref="ConstructionMode"/> is <see cref="LuaConstructionMode.Factory"/>.
    ///     Defaults to <c>"new"</c>.
    /// </summary>
    public string FactoryMethodName { get; set; } = "new";

    /// <summary>
    ///     Called after the instance metatable is first created, allowing custom metamethods to be added.
    ///     The metatable is at the top of the Lua stack.
    /// </summary>
    public Action<LuaThread, LuaStackValue>? OnMetatableCreated { get; set; }

    /// <summary>
    ///     Called after the type definition metatable is first created, allowing custom metamethods to be added.
    ///     The metatable is at the top of the Lua stack.
    /// </summary>
    public Action<LuaThread, LuaStackValue>? OnDefinitionMetatableCreated { get; set; }

    private HashSet<string>? _ignoredMembers;

    /// <summary>
    ///     Marks the specified C# member names as ignored (they will not be exposed to Lua).
    ///     Uses the original CLR member name, not the Lua-transformed name.
    /// </summary>
    /// <remarks>
    ///     When called on <see cref="LuaTypeRegistry.DynamicDefaults"/>, the ignored members
    ///     propagate to all types that haven't been explicitly configured via
    ///     <see cref="LuaTypeRegistry.ConfigureDynamic{T}"/>. This can be useful for
    ///     blanket-ignoring members like <c>"ToString"</c>, but is otherwise a footgun.
    /// </remarks>
    public DynamicUserDataOptions Ignore(params string[] members)
    {
        _ignoredMembers ??= new HashSet<string>(StringComparer.Ordinal);

        foreach (var member in members)
        {
            _ignoredMembers.Add(member);
        }

        return this;
    }

    internal bool ShouldExposeMember(MemberInfo member)
    {
        if (_ignoredMembers != null && _ignoredMembers.Contains(member.Name))
        {
            return false;
        }

        if (MemberFilter != null && !MemberFilter(member))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Creates a copy of this options instance.
    /// </summary>
    /// <returns>
    ///     A copy of the current options instance. Collection state is copied, while delegate-valued members are reused.
    /// </returns>
    public DynamicUserDataOptions Clone()
    {
        var clone = new DynamicUserDataOptions
        {
            NamingPolicy = NamingPolicy,
            Members = Members,
            Features = Features,
            PairsBehavior = PairsBehavior,
            UnknownPropertyBehavior = UnknownPropertyBehavior,
            OverloadResolution = OverloadResolution,
            IndexMembers = IndexMembers,
            IndexPriority = IndexPriority,
            ObjectMembers = ObjectMembers,
            ArgumentValidation = ArgumentValidation,
            ObsoleteMembers = ObsoleteMembers,
            BindingConversions = BindingConversions,
            MemberFilter = MemberFilter,
            ConstructionMode = ConstructionMode,
            FactoryMethodName = FactoryMethodName,
            OnMetatableCreated = OnMetatableCreated,
            OnDefinitionMetatableCreated = OnDefinitionMetatableCreated,
        };

        if (_ignoredMembers != null)
        {
            clone._ignoredMembers = new HashSet<string>(_ignoredMembers, StringComparer.Ordinal);
        }

        return clone;
    }

    internal static string ApplyNamingPolicy(string name, LuaNamingPolicy policy)
    {
        return policy switch
        {
            LuaNamingPolicy.Original => name,
            LuaNamingPolicy.LowerCase => name.ToLowerInvariant(),
            LuaNamingPolicy.UpperCase => name.ToUpperInvariant(),
            LuaNamingPolicy.CamelCase => ToCamelCase(name),
            LuaNamingPolicy.LowerSnakeCase => ToSnakeCase(name, uppercase: false),
            LuaNamingPolicy.UpperSnakeCase => ToSnakeCase(name, uppercase: true),
            LuaNamingPolicy.PascalCase => ToPascalCase(name),
            _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, null),
        };
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        if (char.IsLower(name[0]))
        {
            return name;
        }

        var uppercasePrefixLength = 0;
        for (var i = 0; i < name.Length && char.IsUpper(name[i]); i++)
        {
            uppercasePrefixLength++;
        }

        if (uppercasePrefixLength <= 1)
        {
            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        var loweredLength = uppercasePrefixLength < name.Length
            ? uppercasePrefixLength - 1
            : uppercasePrefixLength;

        return name.Substring(0, loweredLength).ToLowerInvariant() + name.Substring(loweredLength);
    }

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        if (char.IsUpper(name[0]))
        {
            return name;
        }

        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    private static string ToSnakeCase(string name, bool uppercase)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var builder = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var character = name[i];
            if (char.IsUpper(character))
            {
                if (i > 0)
                {
                    var previousIsLower = char.IsLower(name[i - 1]);
                    var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);

                    if (previousIsLower || nextIsLower)
                    {
                        builder.Append('_');
                    }
                }

                builder.Append(uppercase
                    ? char.ToUpperInvariant(character)
                    : char.ToLowerInvariant(character));
            }
            else
            {
                builder.Append(uppercase
                    ? char.ToUpperInvariant(character)
                    : character);
            }
        }

        return builder.ToString();
    }
}
