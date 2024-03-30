using System;
using System.Collections.Generic;
using System.Globalization;

namespace Laylua.Marshaling;

public partial class DefaultLuaMarshaler : LuaMarshaler
{
    /// <summary>
    ///     Gets or sets the format provider of this marshaler.
    /// </summary>
    /// <remarks>
    ///     This is used to determine how conversion and comparison of values is performed.
    ///     <br/>
    ///     Defaults to <see cref="CultureInfo.CurrentCulture"/>.
    /// </remarks>
    public IFormatProvider FormatProvider { get; set; } = CultureInfo.CurrentCulture;

    /// <summary>
    ///     Gets the user data descriptor provider of this marshaler.
    /// </summary>
    protected UserDataDescriptorProvider UserDataDescriptorProvider { get; }

    private readonly Dictionary<IntPtr, Dictionary<(object Value, UserDataDescriptor Descriptor), UserDataHandle>> _userDataHandleCaches;

    public DefaultLuaMarshaler(UserDataDescriptorProvider userDataDescriptorProvider)
    {
        UserDataDescriptorProvider = userDataDescriptorProvider;
        _userDataHandleCaches = new();
    }

    protected internal override unsafe void OnLuaDisposing(Lua lua)
    {
        lock (_userDataHandleCaches)
        {
            _userDataHandleCaches.Remove((IntPtr) lua.MainThread.L);
        }

        base.OnLuaDisposing(lua);
    }

    protected internal sealed override unsafe void RemoveUserDataHandle(UserDataHandle handle)
    {
        Dictionary<(object Value, UserDataDescriptor Descriptor), UserDataHandle>? userDataHandleCache;
        lock (_userDataHandleCaches)
        {
            if (!_userDataHandleCaches.TryGetValue((IntPtr) handle.Lua.MainThread.L, out userDataHandleCache))
                return;
        }

        if (!handle.TryGetType(out var type) || type.IsValueType || !handle.TryGetValue<object>(out var value))
            return;

        userDataHandleCache.Remove((value, handle.Descriptor));
    }
}
