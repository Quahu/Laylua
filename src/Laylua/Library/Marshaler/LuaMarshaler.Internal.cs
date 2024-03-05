using System;

namespace Laylua.Marshaling;

public abstract partial class LuaMarshaler
{
    protected internal abstract void RemoveUserDataHandle(UserDataHandle handle);

    internal void OnReferenceCollected(LuaReference reference)
    {
        if (LuaReference.IsAlive(reference))
        {
            ReferenceLeaked?.Invoke(this, new LuaReferenceLeakedEventArgs(reference));

            _leakedReferences.Push(reference);
        }
        else
        {
            ReturnReference(reference);
        }
    }

    private void ReturnReference(LuaReference reference)
    {
        if (_entityPool.Return(reference))
        {
            GC.ReRegisterForFinalize(reference);
        }
    }
}
