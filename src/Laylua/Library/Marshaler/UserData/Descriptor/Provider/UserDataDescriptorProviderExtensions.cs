namespace Laylua.Marshaling;

public static class UserDataDescriptorProviderExtensions
{
    public static void SetDescriptor<T>(this UserDataDescriptorProvider provider, TypeUserDataDescriptor<T> descriptor)
    {
        provider.SetDescriptor(typeof(T), descriptor);
    }
}
