namespace Laylua.Marshaling;

public sealed class DescriptorUserDataHandle<T> : UserDataHandle<T>
{
    public UserDataDescriptor Descriptor { get; }

    internal DescriptorUserDataHandle(Lua lua, T value, UserDataDescriptor descriptor)
        : base(lua, value)
    {
        Descriptor = descriptor;
        Value = value;
    }

    internal override UserDataDescriptor? GetDescriptor()
    {
        return Descriptor;
    }
}
