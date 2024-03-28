namespace Laylua.Marshaling;

/// <summary>
///     Represents a type responsible for providing <see cref="LuaMarshaler"/> instances.
/// </summary>
public abstract class LuaMarshalerProvider
{
    /// <summary>
    ///     Gets the default marshaler provider instance.
    /// </summary>
    public static LuaMarshalerProvider Default => new DefaultLuaMarshalerProvider();

    /// <summary>
    ///     Gets or sets the user data descriptor provider for the marshaler.
    ///     If <see langword="null"/>, a default value will be used.
    /// </summary>
    public UserDataDescriptorProvider? UserDataDescriptorProvider { get; set; }

    /// <summary>
    ///     Gets or sets the entity pool configuration provider for the marshaler.
    ///     If <see langword="null"/>, a default value will be used.
    /// </summary>
    public LuaMarshalerEntityPoolConfiguration? EntityPoolConfiguration { get; set; }

    /// <summary>
    ///     Creates a marshaler for the specified Lua instance.
    /// </summary>
    /// <remarks>
    ///     After you instantiate your implementation of <see cref="LuaMarshaler"/>,
    ///     the instance will not yet be valid for usage.
    ///     It will be valid when the creation flow completes and the <see cref="Lua"/>
    ///     instance wrapping it is constructed.
    /// </remarks>
    /// <returns>
    ///     The created marshaler.
    /// </returns>
    protected abstract LuaMarshaler CreateCore();

    internal LuaMarshaler Create()
    {
        var marshaler = CreateCore();

        marshaler.EntityPoolConfiguration = EntityPoolConfiguration ?? new();

        if (UserDataDescriptorProvider != null)
        {
            marshaler.UserDataDescriptorProvider = UserDataDescriptorProvider;
        }

        return marshaler;
    }
}
