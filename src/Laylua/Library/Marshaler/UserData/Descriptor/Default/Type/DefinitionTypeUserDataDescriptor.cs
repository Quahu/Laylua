using System;

namespace Laylua.Marshaling;

public class DefinitionTypeUserDataDescriptor : TypeUserDataDescriptor
{
    /// <inheritdoc/>
    public override string MetatableName => base.MetatableName + "__definition";

    public DefinitionTypeUserDataDescriptor(Type type,
        TypeMemberProvider? memberProvider = null,
        UserDataNamingPolicy? namingPolicy = null,
        CallbackUserDataDescriptorFlags disabledCallbacks = CallbackUserDataDescriptorFlags.None)
        : base(type, true, memberProvider, namingPolicy, disabledCallbacks)
    { }
}
