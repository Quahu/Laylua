using System.Collections.Generic;

namespace Laylua.Marshaling;

public partial class DefaultLuaMarshaler : LuaMarshaler
{
    private readonly Dictionary<(object Value, UserDataDescriptor Descriptor), UserDataHandle> _userDataHandleCache;

    public DefaultLuaMarshaler(Lua lua)
        : base(lua)
    {
        _userDataHandleCache = new();
    }

    protected internal sealed override void RemoveUserDataHandle(UserDataHandle handle)
    {
        if (!handle.TryGetType(out var type) || type.IsValueType || !handle.TryGetValue<object>(out var value))
            return;

        _userDataHandleCache.Remove((value, handle.Descriptor));
    }
}
