using System;
using System.Collections.Generic;

namespace Laylua.Marshaling;

public partial class DefaultLuaMarshaler : LuaMarshaler
{
    private readonly Dictionary<IntPtr, Dictionary<(object Value, UserDataDescriptor Descriptor), UserDataHandle>> _userDataHandleCaches;

    public DefaultLuaMarshaler()
    {
        _userDataHandleCaches = new();
    }

    protected internal override unsafe void OnLuaDisposed(Lua lua)
    {
        lock (_userDataHandleCaches)
        {
            _userDataHandleCaches.Remove((IntPtr) lua.MainThread.State);
        }
    }

    protected internal sealed override unsafe void RemoveUserDataHandle(UserDataHandle handle)
    {
        Dictionary<(object Value, UserDataDescriptor Descriptor), UserDataHandle>? userDataHandleCache;
        lock (_userDataHandleCaches)
        {
            if (!_userDataHandleCaches.TryGetValue((IntPtr) handle.Lua.MainThread.State, out userDataHandleCache))
                return;
        }

        if (!handle.TryGetType(out var type) || type.IsValueType || !handle.TryGetValue<object>(out var value))
            return;

        userDataHandleCache.Remove((value, handle.Descriptor));
    }
}
