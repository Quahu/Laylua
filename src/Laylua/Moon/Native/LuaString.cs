using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Laylua.Moon;

/// <summary>
///     Represents a Lua string, i.e. a C string pointer.
/// </summary>
/// <remarks>
///     For Lua strings that exist on the Lua stack,
///     they are valid only as long as they remain on the stack.
///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#6">Lua manual</a> for other scenarios. </para>
/// </remarks>
public readonly unsafe struct LuaString : IEquatable<LuaString>
{
    /// <summary>
    ///     Gets the span containing the string.
    /// </summary>
    public ReadOnlySpan<byte> Span
    {
        get
        {
            if (Pointer == null)
                return ReadOnlySpan<byte>.Empty;

            return new(Pointer, (int) Length);
        }
    }

    /// <summary>
    ///     Gets the pointer to the C string.
    /// </summary>
    public byte* Pointer { get; }

    /// <summary>
    ///     Gets the length of the C string.
    /// </summary>
    public nuint Length { get; }

    /// <summary>
    ///     Gets the <see cref="char"/> length of the C string.
    /// </summary>
    public int CharLength => Encoding.UTF8.GetCharCount(Pointer, (int) Length);

    /// <summary>
    ///     Instantiates a new <see cref="LuaString"/> with a pointer to a <see langword="null"/>-terminated C string.
    /// </summary>
    /// <param name="pointer"> The pointer to the string. </param>
    public LuaString(byte* pointer)
    {
        Pointer = pointer;
        Length = (nuint) MemoryMarshal.CreateReadOnlySpanFromNullTerminated(pointer).Length;
    }

    /// <summary>
    ///     Instantiates a new <see cref="LuaString"/> with a pointer to a C string of the given length.
    /// </summary>
    /// <param name="pointer"> The pointer to the string. </param>
    /// <param name="length"> The length of the string. </param>
    public LuaString(byte* pointer, nuint length)
    {
        Pointer = pointer;
        Length = length;
    }

    /// <summary>
    ///     Decodes the C string into the destination span.
    /// </summary>
    /// <param name="destination"> The destination span. </param>
    /// <returns>
    ///     The number of decoded bytes.
    /// </returns>
    public int GetChars(Span<char> destination)
    {
        return Encoding.UTF8.GetChars(Span, destination);
    }

    /// <summary>
    ///     Returns a <see cref="string"/> created from this <see cref="LuaString"/>.
    /// </summary>
    /// <returns> The created <see cref="string"/>. </returns>
    public override string ToString()
    {
        return Pointer != null
            ? Encoding.UTF8.GetString(Pointer, (int) Length)
            : "<invalid string pointer>";
    }

    /// <inheritdoc/>
    public bool Equals(LuaString other)
    {
        if (Pointer == other.Pointer && Length == other.Length)
            return true;

        return Span.SequenceEqual(other.Span);
    }

    /// <summary>
    ///     Checks whether this <see cref="LuaString"/>
    ///     is equal to the specified UTF-8 sequence.
    /// </summary>
    /// <param name="other"> The UTF-8 sequence. </param>
    /// <returns>
    ///     <see langword="true"/> if equal.
    /// </returns>
    public bool Equals(ReadOnlySpan<byte> other)
    {
        return Span.SequenceEqual(other);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is LuaString other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine((IntPtr) Pointer, Length);
    }

    public static bool operator ==(LuaString left, LuaString right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LuaString left, LuaString right)
    {
        return !left.Equals(right);
    }

    public static bool operator ==(LuaString left, ReadOnlySpan<byte> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LuaString left, ReadOnlySpan<byte> right)
    {
        return !left.Equals(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<byte>(LuaString value)
    {
        return value.Span;
    }
}
