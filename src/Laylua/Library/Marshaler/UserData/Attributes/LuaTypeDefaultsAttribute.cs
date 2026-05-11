using System;

namespace Laylua.Marshaling;

/// <summary>
///     Defines assembly-wide defaults for source-generated Lua type configuration.
/// </summary>
/// <remarks>
///     Per-type settings on <see cref="LuaTypeAttribute"/> override these defaults.
///     Generated defaults intentionally stay conservative compared to <see cref="DynamicUserDataOptions"/>.
///     Dynamic exposure defaults to including <see cref="object"/> members and obsolete members to feel more like
///     direct C# runtime usage, while generated exposure defaults to hiding both unless explicitly enabled.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class LuaTypeDefaultsAttribute : Attribute
{
    /// <summary>
    ///     Gets or sets the default naming policy for Lua-visible member names.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see cref="LuaNamingPolicy.Original"/>.
    ///     Applied when <see cref="LuaTypeAttribute.NamingPolicy"/> is not set.
    /// </remarks>
    public LuaNamingPolicy NamingPolicy { get; set; } = LuaNamingPolicy.Original;

    /// <summary>
    ///     Gets or sets the default exposed member categories.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see cref="LuaTypeMembers.All"/>.
    ///     Applied when <see cref="LuaTypeAttribute.Members"/> is not set.
    /// </remarks>
    public LuaTypeMembers Members { get; set; } = LuaTypeMembers.All;

    /// <summary>
    ///     Gets or sets the default generated interop features.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see cref="LuaTypeFeatures.All"/>.
    ///     Applied when <see cref="LuaTypeAttribute.Features"/> is not set.
    /// </remarks>
    public LuaTypeFeatures Features { get; set; } = LuaTypeFeatures.All;

    /// <summary>
    ///     Gets or sets the default source used for <c>pairs()</c> iteration.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see cref="LuaPairsBehavior.Enumerable"/>.
    ///     Applied when <see cref="LuaTypeAttribute.PairsBehavior"/> is not set.
    /// </remarks>
    public LuaPairsBehavior PairsBehavior { get; set; } = LuaPairsBehavior.Enumerable;

    /// <summary>
    ///     Gets or sets the default behavior for unknown property keys.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see cref="LuaUnknownPropertyBehavior.Silent"/>.
    ///     Applied when <see cref="LuaTypeAttribute.UnknownPropertyBehavior"/> is not set.
    /// </remarks>
    public LuaUnknownPropertyBehavior UnknownPropertyBehavior { get; set; } = LuaUnknownPropertyBehavior.Silent;

    /// <summary>
    ///     Gets or sets the default overload resolution strategy.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see cref="LuaOverloadResolution.BestMatch"/>.
    ///     Applied when <see cref="LuaTypeAttribute.OverloadResolution"/> is not set.
    /// </remarks>
    public LuaOverloadResolution OverloadResolution { get; set; } = LuaOverloadResolution.BestMatch;

    /// <summary>
    ///     Gets or sets default member categories exposed through <c>__index</c>/<c>__newindex</c>.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see cref="LuaIndexMembers.All"/>.
    ///     Applied when <see cref="LuaTypeAttribute.IndexMembers"/> is not set.
    /// </remarks>
    public LuaIndexMembers IndexMembers { get; set; } = LuaIndexMembers.All;

    /// <summary>
    ///     Gets or sets default dispatch priority for <c>__index</c>/<c>__newindex</c>.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see cref="LuaIndexPriority.MembersFirst"/>.
    ///     Applied when <see cref="LuaTypeAttribute.IndexPriority"/> is not set.
    /// </remarks>
    public LuaIndexPriority IndexPriority { get; set; } = LuaIndexPriority.MembersFirst;

    /// <summary>
    ///     Gets or sets the default optimization set for generated callbacks.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see cref="LuaTypeOptimizations.All"/>.
    ///     Applied when <see cref="LuaTypeAttribute.Optimizations"/> is not set.
    /// </remarks>
    public LuaTypeOptimizations Optimizations { get; set; } = LuaTypeOptimizations.All;

    /// <summary>
    ///     Gets or sets the default construction mode for Lua-exposed types.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see cref="LuaConstructionMode.Direct"/>.
    ///     Applied when <see cref="LuaTypeAttribute.Construction"/> is not set.
    /// </remarks>
    public LuaConstructionMode Construction { get; set; } = LuaConstructionMode.Direct;

    /// <summary>
    ///     Gets or sets the default factory method name used in <see cref="LuaConstructionMode.Factory"/> mode.
    /// </summary>
    /// <remarks>
    ///     Defaults to <c>"new"</c>.
    /// </remarks>
    public string? FactoryMethodName { get; set; }

    /// <summary>
    ///     Gets or sets the default argument validation behavior.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see cref="LuaArgumentValidation.Strict"/>.
    ///     Applied when <see cref="LuaTypeAttribute.ArgumentValidation"/> is not set.
    /// </remarks>
    public LuaArgumentValidation ArgumentValidation { get; set; } = LuaArgumentValidation.Strict;

    /// <summary>
     ///     Gets or sets the default set of <see cref="object"/>-inherited members exposed to Lua.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see cref="LuaSystemObjectMembers.None"/>.
    ///     This is intentionally stricter than <see cref="DynamicUserDataOptions.ObjectMembers"/>,
    ///     which defaults to exposing <see cref="LuaSystemObjectMembers.All"/> for runtime-configured types.
    ///     Applied when <see cref="LuaTypeAttribute.ObjectMembers"/> is not set.
    /// </remarks>
    public LuaSystemObjectMembers ObjectMembers { get; set; } = LuaSystemObjectMembers.None;

    /// <summary>
     ///     Gets or sets the default handling of <see cref="System.ObsoleteAttribute"/>-marked members.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see cref="LuaObsoleteMemberHandling.Ignore"/>.
    ///     This is intentionally stricter than <see cref="DynamicUserDataOptions.ObsoleteMembers"/>,
    ///     which defaults to including obsolete members for runtime-configured types.
    ///     Applied when <see cref="LuaTypeAttribute.ObsoleteMembers"/> is not set.
    /// </remarks>
    public LuaObsoleteMemberHandling ObsoleteMembers { get; set; } = LuaObsoleteMemberHandling.Ignore;

    /// <summary>
    ///     Gets or sets the default handling of awaitable CLR method results.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see cref="LuaAwaitableReturnHandling.ImplicitAwait"/>.
    ///     Applied when <see cref="LuaTypeAttribute.AwaitableReturnHandling"/> is not set.
    /// </remarks>
    public LuaAwaitableReturnHandling AwaitableReturnHandling { get; set; } = LuaAwaitableReturnHandling.ImplicitAwait;

    /// <summary>
    ///     Gets or sets the default source-generated table-to-object conversion support mode.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see cref="LuaFromTableConversion.WhenTypeHasParameterlessConstructor"/>.
    ///     Applied when <see cref="LuaTypeAttribute.FromTableConversion"/> is not set.
    ///     This controls whether generated handlers support reading their own type from a Lua table;
    ///     <see cref="BindingConversions"/> still controls whether generated member binding is allowed to attempt table-to-object conversion.
    /// </remarks>
    public LuaFromTableConversion FromTableConversion { get; set; } = LuaFromTableConversion.WhenTypeHasParameterlessConstructor;

    /// <summary>
    ///     Gets or sets the default binding conversion policy for generated callbacks.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see cref="LuaAllowedValueConversions.Default"/>.
    ///     Applied when <see cref="LuaTypeAttribute.BindingConversions"/> is not set.
    /// </remarks>
    public LuaAllowedValueConversions BindingConversions { get; set; } = LuaAllowedValueConversions.Default;
}
