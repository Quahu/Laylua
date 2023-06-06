using System.ComponentModel;
using Qommon;

namespace Laylua.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class LuaMarshalerExtensions
{
    public static T? GetValue<T>(this LuaMarshaler marshaler, int stackIndex)
    {
        if (!marshaler.TryGetValue<T>(stackIndex, out var value))
        {
            Throw.InvalidOperationException($"Failed to convert the value at stack index {stackIndex} to type {typeof(T).Name}.");
        }

        return value;
    }

    public static object? GetValue(this LuaMarshaler marshaler, int stackIndex)
    {
        return marshaler.GetValue<object>(stackIndex);
    }

    public static bool TryPopValue<T>(this LuaMarshaler marshaler, out T? value)
    {
        if (!marshaler.TryGetValue(-1, out value))
        {
            value = default;
            return false;
        }

        marshaler.Lua.Stack.Pop();
        return true;
    }

    public static T? PopValue<T>(this LuaMarshaler marshaler)
    {
        if (!marshaler.TryGetValue<T>(-1, out var value))
        {
            Throw.InvalidOperationException($"Failed to pop and convert the stack value to type {typeof(T).Name}.");
        }

        marshaler.Lua.Stack.Pop();
        return value;
    }

    public static object? PopValue(this LuaMarshaler marshaler)
    {
        return marshaler.PopValue<object>();
    }
}
