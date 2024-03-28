using System.ComponentModel;
using Qommon;

namespace Laylua.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class LuaMarshalerExtensions
{
    public static T? GetValue<T>(this LuaMarshaler marshaler, Lua lua, int stackIndex)
    {
        if (!marshaler.TryGetValue<T>(lua, stackIndex, out var value))
        {
            Throw.InvalidOperationException($"Failed to convert the value at stack index {stackIndex} to type {typeof(T).Name}.");
        }

        return value;
    }

    public static object? GetValue(this LuaMarshaler marshaler, Lua lua, int stackIndex)
    {
        return marshaler.GetValue<object>(lua, stackIndex);
    }

    public static bool TryPopValue<T>(this LuaMarshaler marshaler, Lua lua, out T? value)
    {
        if (!marshaler.TryGetValue(lua, -1, out value))
        {
            value = default;
            return false;
        }

        lua.Stack.Pop();
        return true;
    }

    public static T? PopValue<T>(this LuaMarshaler marshaler, Lua lua)
    {
        if (!marshaler.TryGetValue<T>(lua, -1, out var value))
        {
            Throw.InvalidOperationException($"Failed to pop and convert the stack value to type {typeof(T).Name}.");
        }

        lua.Stack.Pop();
        return value;
    }

    public static object? PopValue(this LuaMarshaler marshaler, Lua lua)
    {
        return marshaler.PopValue<object>(lua);
    }
}
