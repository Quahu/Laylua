using System;

namespace Laylua.Marshaling;

/// <summary>
///     Controls which <see cref="object"/>-inherited members are exposed to Lua as regular callable methods.
/// </summary>
/// <remarks>
///     These members are separate from metamethod-based features.
///     For example, <see cref="LuaTypeFeatures.ToString"/> wires <c>__tostring</c> for <c>tostring(obj)</c>,
///     while <see cref="ToString"/> exposes <c>ToString()</c> as a regular method (e.g. <c>obj:toString()</c>).
/// </remarks>
[Flags]
public enum LuaSystemObjectMembers
{
    /// <summary>
    ///     Do not expose any <see cref="object"/>-inherited members.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Expose <see cref="object.ToString"/> as a callable method.
    /// </summary>
    ToString = 1 << 0,

    /// <summary>
    ///     Expose <see cref="object.Equals(object)"/> as a callable method.
    /// </summary>
    Equals = 1 << 1,

    /// <summary>
    ///     Expose <see cref="object.GetHashCode"/> as a callable method.
    /// </summary>
    GetHashCode = 1 << 2,

    /// <summary>
    ///     Expose <see cref="object.ReferenceEquals"/> as a callable static method on the type definition.
    /// </summary>
    ReferenceEquals = 1 << 3,

    /// <summary>
    ///     Expose all instance-level <see cref="object"/> members
    ///     (<see cref="ToString"/>, <see cref="Equals"/>, <see cref="GetHashCode"/>).
    /// </summary>
    Instance = ToString | Equals | GetHashCode,

    /// <summary>
    ///     Expose all <see cref="object"/>-inherited members, including static members.
    /// </summary>
    All = Instance | ReferenceEquals,
}
