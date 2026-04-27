using System;

namespace Laylua.Marshaling;

/// <summary>
///     Selects which member categories are exposed for a Lua-exposed type.
/// </summary>
[Flags]
public enum LuaTypeMembers
{
    /// <summary>
    ///     No member categories enabled.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Expose instance properties.
    /// </summary>
    Properties = 1 << 0,

    /// <summary>
    ///     Expose instance fields.
    /// </summary>
    Fields = 1 << 1,

    /// <summary>
    ///     Expose instance methods.
    /// </summary>
    Methods = 1 << 2,

    /// <summary>
    ///     Expose instance events.
    /// </summary>
    Events = 1 << 3,

    /// <summary>
    ///     Expose the C# indexer.
    /// </summary>
    Indexers = 1 << 4,

    /// <summary>
    ///     Expose constructors for Lua-side instantiation.
    /// </summary>
    Constructors = 1 << 5,

    /// <summary>
    ///     Expose static properties on the type definition.
    /// </summary>
    StaticProperties = 1 << 6,

    /// <summary>
    ///     Expose static fields on the type definition.
    /// </summary>
    StaticFields = 1 << 7,

    /// <summary>
    ///     Expose static methods on the type definition.
    /// </summary>
    StaticMethods = 1 << 8,

    /// <summary>
    ///     Expose static events on the type definition.
    /// </summary>
    StaticEvents = 1 << 9,

    /// <summary>
    ///     Expose constants on the type definition.
    /// </summary>
    Constants = 1 << 10,

    /// <summary>
    ///     All instance member categories.
    /// </summary>
    InstanceMembers = Properties | Fields | Methods | Events | Indexers | Constructors,

    /// <summary>
    ///     All static member categories.
    /// </summary>
    StaticMembers = StaticProperties | StaticFields | StaticMethods | StaticEvents | Constants,

    /// <summary>
    ///     All member categories.
    /// </summary>
    All = InstanceMembers | StaticMembers,
}
