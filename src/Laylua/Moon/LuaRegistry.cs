using System.Runtime.CompilerServices;

namespace Laylua.Moon;

/// <summary>
///     Defines the Lua registry pseudo-indices.
/// </summary>
public static class LuaRegistry
{
    /// <summary>
    ///     Represents the pseudo-index on the stack at which the Lua registry is available.
    /// </summary>
    public const int Index = -LUAI_MAXSTACK - 1000;

    /// <summary>
    ///     Defines the indices accessible in the Lua registry.
    /// </summary>
    public static class Indices
    {
        /// <summary>
        ///     Represents the registry index at which the main thread of the Lua state is stored.
        /// </summary>
        public const int MainThread = 1;

        /// <summary>
        ///     Represents the registry index at which the globals of the Lua state are stored.
        /// </summary>
        public const int Globals = 2;
    }

    /// <summary>
    ///     Checks whether the reference is a persistent reference,
    ///     i.e. whether it is always available in the registry.
    /// </summary>
    /// <param name="reference"> The reference to check. </param>
    /// <returns>
    ///     <see langword="true"/> if the reference is persistent.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPersistentReference(int reference)
    {
        return reference >= Indices.MainThread && reference <= Indices.Globals;
    }
}
