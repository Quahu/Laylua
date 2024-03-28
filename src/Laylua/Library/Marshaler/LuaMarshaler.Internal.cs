using System;
using System.Collections.Concurrent;

namespace Laylua.Marshaling;

public abstract partial class LuaMarshaler
{
    protected internal abstract void RemoveUserDataHandle(UserDataHandle handle);

    internal unsafe void OnReferenceCollected(LuaReference reference)
    {
        if (LuaReference.IsAlive(reference))
        {
            ReferenceLeaked?.Invoke(this, new LuaReferenceLeakedEventArgs(reference));

            ConcurrentStack<LuaReference>? leakedReferences;
            lock (_leakedReferences)
            {
                if (!_leakedReferences.TryGetValue((IntPtr) reference.Lua.MainThread.State, out leakedReferences))
                    return;
            }

            leakedReferences.Push(reference);
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
