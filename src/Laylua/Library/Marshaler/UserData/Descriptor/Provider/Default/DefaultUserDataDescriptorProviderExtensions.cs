namespace Laylua.Marshaling;

public static class DefaultUserDataDescriptorProviderExtensions
{
    public static void SetDescriptor<T>(this DefaultUserDataDescriptorProvider provider, UserDataDescriptor descriptor)
    {
        provider.SetDescriptorForValuesOfType(typeof(T), descriptor);
    }

    public static void SetDefinitionDescriptor<T>(this DefaultUserDataDescriptorProvider provider,
        TypeMemberProvider? memberProvider = null,
        UserDataNamingPolicy? namingPolicy = null,
        CallbackUserDataDescriptorFlags disabledCallbacks = CallbackUserDataDescriptorFlags.None)
    {
        provider.SetDescriptorForType(typeof(T), new DefinitionTypeUserDataDescriptor(typeof(T), memberProvider, namingPolicy, disabledCallbacks));
    }

    public static void SetInstanceDescriptor<T>(this DefaultUserDataDescriptorProvider provider,
        TypeMemberProvider? memberProvider = null,
        UserDataNamingPolicy? namingPolicy = null,
        CallbackUserDataDescriptorFlags disabledCallbacks = CallbackUserDataDescriptorFlags.None)
    {
        provider.SetDescriptorForValuesOfType(typeof(T), new InstanceTypeUserDataDescriptor(typeof(T), memberProvider, namingPolicy, disabledCallbacks));
    }
}
