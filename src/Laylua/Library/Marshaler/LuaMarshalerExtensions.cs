using System.ComponentModel;
using Qommon;

namespace Laylua.Marshalling;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class LuaMarshalerExtensions
{
    public static T? ToObject<T>(this LuaMarshaler marshaler, int stackIndex)
    {
        if (marshaler.TryToObject<T>(stackIndex, out var value))
        {
            return value;
        }

        Throw.InvalidOperationException($"Failed to convert the value at stack index {stackIndex} to type {typeof(T).Name}.");
        return default!;
    }

    public static object? ToObject(this LuaMarshaler marshaler, int stackIndex)
    {
        return marshaler.ToObject<object>(stackIndex);
    }

    public static bool TryPopObject<T>(this LuaMarshaler marshaler, out T? value)
    {
        if (marshaler.TryToObject(-1, out value))
        {
            marshaler.Lua.Stack.Pop();
            return true;
        }

        value = default;
        return false;
    }

    public static T? PopObject<T>(this LuaMarshaler marshaler)
    {
        if (marshaler.TryToObject<T>(-1, out var value))
        {
            marshaler.Lua.Stack.Pop();
            return value;
        }

        Throw.InvalidOperationException("Failed to pop the object.");
        return default!;
    }

    public static object? PopObject(this LuaMarshaler marshaler)
    {
        return marshaler.PopObject<object>();
    }
}
