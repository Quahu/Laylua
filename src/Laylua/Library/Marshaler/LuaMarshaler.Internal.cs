using System;

namespace Laylua.Marshaling;

public abstract partial class LuaMarshaler
{
    internal abstract void RemoveUserDataHandle(UserDataHandle handle);

    internal void ReturnReference(LuaReference reference)
    {
        if (LuaReference.IsAlive(reference))
        {
            ReferenceLeaked?.Invoke(this, new LuaReferenceLeakedEventArgs(reference));

            reference.Dispose();
        }

        if (_entityPool.Return(reference))
        {
            GC.ReRegisterForFinalize(reference);
        }
    }
}
