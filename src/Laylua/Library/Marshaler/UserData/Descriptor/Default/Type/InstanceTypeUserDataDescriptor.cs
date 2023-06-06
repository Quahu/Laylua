using System;

namespace Laylua.Marshaling;

public class InstanceTypeUserDataDescriptor : TypeUserDataDescriptor
{
    /// <inheritdoc/>
    public override string MetatableName => base.MetatableName + "__instance";

    public InstanceTypeUserDataDescriptor(Type type,
        TypeMemberProvider? memberProvider = null,
        UserDataNamingPolicy? namingPolicy = null,
        CallbackUserDataDescriptorFlags disabledCallbacks = CallbackUserDataDescriptorFlags.None)
        : base(type, false, memberProvider, namingPolicy, disabledCallbacks)
    { }
}
