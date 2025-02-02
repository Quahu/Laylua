namespace Laylua.Moon;

/// <summary>
///     Represents a memory allocator used by Lua to manage blocks of memory.
/// </summary>
public abstract unsafe class LuaAllocator
{
    /// <summary>
    ///     Allocates a memory block of the requested size.
    /// </summary>
    /// <remarks>
    ///     The implementation must not throw any exceptions.
    /// </remarks>
    /// <param name="ptr"> The pointer to the memory being allocated, reallocated or freed. </param>
    /// <param name="oldSizeOrType"> The old allocation size if <paramref name="ptr"/> is <see langword="null"/> or the <see cref="LuaType"/> of the allocated object. </param>
    /// <param name="size"> The requested allocation size. </param>
    /// <returns>
    ///     The pointer to the allocated memory.
    /// </returns>
    protected internal abstract void* Allocate(void* ptr, nuint oldSizeOrType, nuint size);
}
