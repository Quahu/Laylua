﻿namespace Laylua.Marshaling;

/// <summary>
///     Represents a userdata descriptor.
///     This type is responsible for describing the metatable of a userdata value.
/// </summary>
public abstract class UserDataDescriptor
{
    /// <summary>
    ///     Gets the name of the metatable of this descriptor.
    /// </summary>
    /// <remarks>
    ///     This must be unique per userdata type described.
    /// </remarks>
    public abstract string MetatableName { get; }

    /// <summary>
    ///     Instantiates a new <see cref="UserDataDescriptor"/>.
    /// </summary>
    protected UserDataDescriptor()
    { }

    /// <summary>
    ///     Invoked upon metatable creation of the described userdata.
    /// </summary>
    /// <param name="thread"> The Lua thread. </param>
    /// <param name="metatable"> The metatable on the Lua stack of the described userdata. </param>
    public abstract void OnMetatableCreated(LuaThread thread, LuaStackValue metatable);
}
