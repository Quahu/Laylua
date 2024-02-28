namespace Laylua.Marshaling;

/// <summary>
///     Defines extension methods for <see cref="LuaMarshaler"/>.
/// </summary>
public static class LuaMarshalerProviderExtensions
{
    /// <summary>
    ///     Configures the marshaler with the specified user data descriptor provider.
    /// </summary>
    /// <param name="marshalerProvider"> The marshaler provider. </param>
    /// <param name="userDataDescriptorProvider"> The user data descriptor provider. </param>
    /// <returns>
    ///     This provider instance.
    /// </returns>
    public static TMarshalerProvider SetUserDataDescriptorProvider<TMarshalerProvider>(
        this TMarshalerProvider marshalerProvider,
        UserDataDescriptorProvider userDataDescriptorProvider)
        where TMarshalerProvider : LuaMarshalerProvider
    {
        marshalerProvider.UserDataDescriptorProvider = userDataDescriptorProvider;
        return marshalerProvider;
    }

    /// <summary>
    ///     Configures the marshaler with the specified entity pool configuration.
    /// </summary>
    /// <param name="marshalerProvider"> The marshaler provider. </param>
    /// <param name="entityPoolConfiguration"> The entity pool configuration. </param>
    /// <returns>
    ///     This provider instance.
    /// </returns>
    public static TMarshalerProvider ConfigureEntityPool<TMarshalerProvider>(
        this TMarshalerProvider marshalerProvider,
        LuaMarshalerEntityPoolConfiguration entityPoolConfiguration)
        where TMarshalerProvider : LuaMarshalerProvider
    {
        marshalerProvider.EntityPoolConfiguration = entityPoolConfiguration;
        return marshalerProvider;
    }
}
