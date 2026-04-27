using System;

namespace Laylua.Marshaling;

/// <summary>
///     Controls which member categories are exposed through <c>__index</c> and <c>__newindex</c>.
/// </summary>
/// <remarks>
///     Combine flags to expose multiple categories. Resolution order is controlled by
///     <see cref="LuaTypeAttribute.IndexPriority"/>.
/// </remarks>
[Flags]
public enum LuaIndexMembers
{
    /// <summary>
    ///     Expose named properties and fields.
    /// </summary>
    Properties = 1 << 0,

    /// <summary>
    ///     Expose named methods.
    /// </summary>
    Methods = 1 << 1,

    /// <summary>
    ///     Expose the C# indexer, if present.
    /// </summary>
    Indexer = 1 << 2,

    /// <summary>
    ///     Expose events.
    /// </summary>
    Events = 1 << 3,

    /// <summary>
    ///     Expose all supported member categories.
    /// </summary>
    All = Properties | Methods | Indexer | Events,
}
