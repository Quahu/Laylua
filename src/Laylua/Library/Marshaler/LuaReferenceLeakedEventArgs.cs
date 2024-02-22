namespace Laylua.Marshaling;

/// <summary>
///     Represents event data for <see cref="LuaMarshaler.ReferenceLeaked"/>.
/// </summary>
public readonly struct LuaReferenceLeakedEventArgs
{
    /// <summary>
    ///     Gets the leaked reference.
    /// </summary>
    public LuaReference Reference { get; }

    /// <summary>
    ///     Instantiates a new <see cref="LuaReferenceLeakedEventArgs"/>.
    /// </summary>
    /// <param name="reference"> The leaked reference. </param>
    public LuaReferenceLeakedEventArgs(LuaReference reference)
    {
        Reference = reference;
    }
}
