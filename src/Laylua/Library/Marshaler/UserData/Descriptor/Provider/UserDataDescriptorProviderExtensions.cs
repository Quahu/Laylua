namespace Laylua.Marshaling;

public static class UserDataDescriptorProviderExtensions
{
    public static void SetDescriptor<T>(this UserDataDescriptorProvider provider, UserDataDescriptor descriptor)
    {
        provider.SetDescriptor(typeof(T), descriptor);
    }
}
