using System.ComponentModel;
using Qommon;

namespace Laylua.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class LuaMarshalerExtensions
{
    public static T? GetValue<T>(this LuaMarshaler marshaler, LuaThread thread, int stackIndex)
    {
        if (!marshaler.TryGetValue<T>(thread, stackIndex, out var value))
        {
            Throw.InvalidOperationException($"Failed to convert the value at stack index {stackIndex} to type {typeof(T).Name}.");
        }

        return value;
    }
}
