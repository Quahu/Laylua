namespace Laylua.Marshaling;

public static class UserDataDescriptorProviderExtensions
{
    public static void SetDescriptor<T>(this UserDataDescriptorProvider provider, UserDataDescriptor descriptor)
    {
        provider.SetDescriptor(typeof(T), descriptor);
    }

    public static void SetInstanceDescriptor<T>(this UserDataDescriptorProvider provider,
        TypeMemberProvider? memberProvider = null,
        UserDataNamingPolicy? namingPolicy = null,
        CallbackUserDataDescriptorFlags disabledCallbacks = CallbackUserDataDescriptorFlags.None)
    {
        provider.SetDescriptor(typeof(T), new InstanceTypeUserDataDescriptor(typeof(T), memberProvider, namingPolicy, disabledCallbacks));
    }

    public static void SetDefinitionDescriptor<T>(this UserDataDescriptorProvider provider,
        TypeMemberProvider? memberProvider = null,
        UserDataNamingPolicy? namingPolicy = null,
        CallbackUserDataDescriptorFlags disabledCallbacks = CallbackUserDataDescriptorFlags.None)
    {
        provider.SetDescriptor(typeof(T), new DefinitionTypeUserDataDescriptor(typeof(T), memberProvider, namingPolicy, disabledCallbacks));
    }
}
