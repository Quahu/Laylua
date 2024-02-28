namespace Laylua.Marshaling;

/// <summary>
///     Represents the default <see cref="LuaMarshalerProvider"/> implementation.
/// </summary>
public class DefaultLuaMarshalerProvider : LuaMarshalerProvider
{
    /// <summary>
    ///     Instantiates a new <see cref="DefaultLuaMarshalerProvider"/>.
    /// </summary>
    public DefaultLuaMarshalerProvider()
    { }

    /// <inheritdoc/>
    protected override LuaMarshaler CreateCore(Lua lua)
    {
        return new DefaultLuaMarshaler(lua);
    }
}
