using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Laylua.Marshaling;

namespace Laylua;

public static class LuaReferenceExtensions
{
    /// <summary>
    ///     Specifies that the given Lua reference should be returned to the
    ///     entity pool of the marshaler when it is disposed.
    /// </summary>
    /// <remarks>
    ///     <inheritdoc cref="LuaMarshaler.EntityPoolConfiguration"/>
    /// </remarks>
    /// <param name="reference"> The Lua reference. </param>
    /// <seealso cref="LuaMarshaler.EntityPoolConfiguration"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(reference))]
    public static TReference? PoolOnDispose<TReference>(this TReference? reference)
        where TReference : LuaReference
    {
        if (reference != null)
        {
            reference.PoolOnDispose = true;
        }

        return reference;
    }
}
