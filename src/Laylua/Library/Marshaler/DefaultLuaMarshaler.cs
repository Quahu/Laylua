using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;

namespace Laylua.Marshaling;

public unsafe partial class DefaultLuaMarshaler : LuaMarshaler
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

    private static readonly ConcurrentDictionary<Lua, Dictionary<(object Value, UserDataDescriptor? Descriptor), UserDataHandle>> _userDataHandleCaches = [];

    public DefaultLuaMarshaler(UserDataDescriptorProvider userDataDescriptorProvider)
    {
        UserDataDescriptorProvider = userDataDescriptorProvider;
    }

    /// <inheritdoc/>
    protected internal override void OnLuaDisposing(Lua lua)
    {
        foreach (var kvp in GetUserDataCache(lua))
        {
            kvp.Value.IsCollectible = false;
        }

        base.OnLuaDisposing(lua);
    }

    /// <inheritdoc/>
    protected internal override void OnLuaDisposed(Lua lua)
    {
        if (_userDataHandleCaches.TryRemove(lua, out var cache))
        {
            foreach (var kvp in cache)
            {
                kvp.Value.Free();
            }
        }

        base.OnLuaDisposed(lua);
    }

    private static Dictionary<(object Value, UserDataDescriptor? Descriptor), UserDataHandle> GetUserDataCache(Lua lua)
    {
        return _userDataHandleCaches.GetOrAdd(lua, static _ => []);
    }

    protected internal sealed override void RemoveUserDataHandle(UserDataHandle handle)
    {
        var cache = GetUserDataCache(handle.Lua);
        if (!handle.TryGetType(out var type) || type.IsValueType || !handle.TryGetValue<object>(out var value))
            return;

        cache.Remove((value, handle.GetDescriptor()));
    }
}
