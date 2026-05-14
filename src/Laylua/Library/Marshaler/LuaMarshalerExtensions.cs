using Qommon;

namespace Laylua.Marshaling;

public static class LuaMarshalerExtensions
{
    public static T? GetValue<T>(this LuaMarshaler marshaler, LuaThread thread, int stackIndex)
    {
        if (!marshaler.TryGetValue<T>(thread, stackIndex, out var value, out var error))
        {
            throw new LuaConversionException(error);
        }

        return value;
    }

    public static T? GetValue<T>(this LuaMarshaler marshaler, LuaThread thread, int stackIndex, LuaAllowedValueConversions allowedConversions)
    {
        if (!marshaler.TryGetValue<T>(thread, stackIndex, allowedConversions, out var value, out var error))
        {
            throw new LuaConversionException(error);
        }

        return value;
    }
}
