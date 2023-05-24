namespace Laylua.Marshaling;

/// <summary>
///     Represents the default <see cref="LuaMarshalerProvider"/> implementation.
/// </summary>
public class DefaultLuaMarshalerProvider : LuaMarshalerProvider
{
    /// <summary>
    ///     Gets the user data descriptor provider of this marshaler provider.
    /// </summary>
    public UserDataDescriptorProvider UserDataDescriptorProvider { get; }

    /// <summary>
    ///     Instantiates a new <see cref="DefaultLuaMarshalerProvider"/>.
    /// </summary>
    /// <param name="userDataDescriptorProvider"> The user data descriptor provider. </param>
    public DefaultLuaMarshalerProvider(UserDataDescriptorProvider userDataDescriptorProvider)
    {
        UserDataDescriptorProvider = userDataDescriptorProvider;
    }

    /// <inheritdoc/>
    public override LuaMarshaler GetMarshaler(Lua lua)
    {
        return new DefaultLuaMarshaler(lua, UserDataDescriptorProvider);
    }
}
