using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Laylua.Moon;

/// <summary>
///     Represents extension methods for <see cref="LuaStatus"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class LuaStatusExtensions
{
    /// <summary>
    ///     Checks if the status represents an error.
    /// </summary>
    /// <param name="status"> The status to check. </param>
    /// <returns>
    ///     <see langword="true"/> if the status represents an error.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsError(this LuaStatus status)
    {
        return status > LuaStatus.Yield;
    }
}
