namespace Laylua.Marshaling;

public sealed class DescriptorUserDataHandle<T> : UserDataHandle<T>
{
    public UserDataDescriptor Descriptor { get; }

    internal DescriptorUserDataHandle(LuaThread thread, T value, UserDataDescriptor descriptor)
        : base(thread, value)
    {
        Descriptor = descriptor;
        Value = value;
    }

    internal override UserDataDescriptor? GetDescriptor()
    {
        return Descriptor;
    }
}
