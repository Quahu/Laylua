using System;

namespace Laylua.Marshaling;

/// <summary>
///     Configures how a partial type is exposed to Lua via source-generated typed userdata.
/// </summary>
/// <remarks>
///     The generator emits type-specific marshaling and metatable callbacks for the annotated type.
///     Properties not explicitly set in the attribute application inherit their values from
///     assembly-level <see cref="LuaTypeDefaultsAttribute"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class LuaTypeAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets the naming policy for Lua-visible member names.
    /// </summary>
    /// <remarks>
    ///     When not set, inherits from <see cref="LuaTypeDefaultsAttribute.NamingPolicy"/>
    ///     (built-in default: <see cref="LuaNamingPolicy.Original"/>).
    ///     <see cref="LuaNameAttribute"/> takes precedence on individual members.
    /// </remarks>
    public LuaNamingPolicy NamingPolicy { get; set; }

    /// <summary>
    ///     Gets or sets which member categories are exposed.
    /// </summary>
    /// <remarks>
    ///     When not set, inherits from <see cref="LuaTypeDefaultsAttribute.Members"/>
    ///     (built-in default: <see cref="LuaTypeMembers.All"/>).
    /// </remarks>
    public LuaTypeMembers Members { get; set; }

    /// <summary>
    ///     Gets or sets which generated interop features are enabled.
    /// </summary>
    /// <remarks>
    ///     When not set, inherits from <see cref="LuaTypeDefaultsAttribute.Features"/>
    ///     (built-in default: <see cref="LuaTypeFeatures.All"/>).
    /// </remarks>
    public LuaTypeFeatures Features { get; set; }

    /// <summary>
    ///     Gets or sets which source is used for <c>pairs()</c> iteration.
    /// </summary>
    /// <remarks>
    ///     When not set, inherits from <see cref="LuaTypeDefaultsAttribute.PairsBehavior"/>
    ///     (built-in default: <see cref="LuaPairsBehavior.Enumerable"/>).
    ///     Only applies when <see cref="LuaTypeFeatures.Pairs"/> is enabled in <see cref="Features"/>.
    /// </remarks>
    public LuaPairsBehavior PairsBehavior { get; set; }

    /// <summary>
    ///     Gets or sets how unknown property keys are handled.
    /// </summary>
    /// <remarks>
    ///     When not set, inherits from <see cref="LuaTypeDefaultsAttribute.UnknownPropertyBehavior"/>
    ///     (built-in default: <see cref="LuaUnknownPropertyBehavior.Silent"/>).
    /// </remarks>
    public LuaUnknownPropertyBehavior UnknownPropertyBehavior { get; set; }

    /// <summary>
    ///     Gets or sets overload resolution behavior for methods with multiple overloads.
    /// </summary>
    /// <remarks>
    ///     When not set, inherits from <see cref="LuaTypeDefaultsAttribute.OverloadResolution"/>
    ///     (built-in default: <see cref="LuaOverloadResolution.BestMatch"/>).
    /// </remarks>
    public LuaOverloadResolution OverloadResolution { get; set; }

    /// <summary>
    ///     Gets or sets which member categories are exposed through <c>__index</c>/<c>__newindex</c>.
    /// </summary>
    /// <remarks>
    ///     When not set, inherits from <see cref="LuaTypeDefaultsAttribute.IndexMembers"/>
    ///     (built-in default: <see cref="LuaIndexMembers.All"/>).
    /// </remarks>
    public LuaIndexMembers IndexMembers { get; set; }

    /// <summary>
    ///     Gets or sets dispatch priority for <c>__index</c>/<c>__newindex</c>.
    /// </summary>
    /// <remarks>
    ///     When not set, inherits from <see cref="LuaTypeDefaultsAttribute.IndexPriority"/>
    ///     (built-in default: <see cref="LuaIndexPriority.MembersFirst"/>).
    /// </remarks>
    public LuaIndexPriority IndexPriority { get; set; }

    /// <summary>
    ///     Gets or sets the enabled optimizations for generated callbacks.
    /// </summary>
    /// <remarks>
    ///     When not set, inherits from <see cref="LuaTypeDefaultsAttribute.Optimizations"/>
    ///     (built-in default: <see cref="LuaTypeOptimizations.All"/>).
    /// </remarks>
    public LuaTypeOptimizations Optimizations { get; set; }

    /// <summary>
    ///     Gets or sets how the type is constructed from Lua.
    /// </summary>
    /// <remarks>
    ///     When not set, inherits from <see cref="LuaTypeDefaultsAttribute.Construction"/>
    ///     (built-in default: <see cref="LuaConstructionMode.Direct"/>).
    /// </remarks>
    public LuaConstructionMode Construction { get; set; }

    /// <summary>
    ///     Gets or sets the Lua-facing factory method name used in <see cref="LuaConstructionMode.Factory"/> mode.
    /// </summary>
    /// <remarks>
    ///     The name is used as-is (naming policy is not applied). Defaults to <c>"new"</c>.
    /// </remarks>
    public string? FactoryMethodName { get; set; }

    /// <summary>
    ///     Gets or sets argument validation behavior for generated callbacks.
    /// </summary>
    /// <remarks>
    ///     When not set, inherits from <see cref="LuaTypeDefaultsAttribute.ArgumentValidation"/>
    ///     (built-in default: <see cref="LuaArgumentValidation.Strict"/>).
    /// </remarks>
    public LuaArgumentValidation ArgumentValidation { get; set; }

    /// <summary>
    ///     Gets or sets which <see cref="object"/>-inherited members are exposed to Lua.
    /// </summary>
    /// <remarks>
    ///     When not set, inherits from <see cref="LuaTypeDefaultsAttribute.ObjectMembers"/>
    ///     (built-in default: <see cref="LuaSystemObjectMembers.None"/>).
    /// </remarks>
    public LuaSystemObjectMembers ObjectMembers { get; set; }

    /// <summary>
    ///     Gets or sets how members marked with <see cref="System.ObsoleteAttribute"/> are handled.
    /// </summary>
    /// <remarks>
    ///     When not set, inherits from <see cref="LuaTypeDefaultsAttribute.ObsoleteMembers"/>
    ///     (built-in default: <see cref="LuaObsoleteMemberHandling.Ignore"/>).
    /// </remarks>
    public LuaObsoleteMemberHandling ObsoleteMembers { get; set; }

    /// <summary>
    ///     Gets or sets how generated callbacks return awaitable CLR method results.
    /// </summary>
    /// <remarks>
    ///     When not set, inherits from <see cref="LuaTypeDefaultsAttribute.AwaitableReturnHandling"/>
    ///     (built-in default: <see cref="LuaAwaitableReturnHandling.ImplicitAwait"/>).
    /// </remarks>
    public LuaAwaitableReturnHandling AwaitableReturnHandling { get; set; }

    /// <summary>
    ///     Gets or sets which value conversions are permitted during argument binding
    ///     and overload resolution for this type's generated callbacks.
    /// </summary>
    /// <remarks>
    ///     When not set, inherits from <see cref="LuaTypeDefaultsAttribute.BindingConversions"/>
    ///     (built-in default: <see cref="LuaAllowedValueConversions.Default"/>).
    /// </remarks>
    public LuaAllowedValueConversions BindingConversions { get; set; }
}
