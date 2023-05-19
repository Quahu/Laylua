using System;
using System.Runtime.InteropServices;

namespace Laylua.Moon;

/// <summary>
///     Represents the default implementation of <see cref="LuaAllocator"/>.
/// </summary>
/// <remarks>
///     This is a managed equivalent of the default Lua memory allocator
///     with the addition of a maximum allocation limit and tracking of allocations.
/// </remarks>
public sealed unsafe class NativeMemoryLuaAllocator : LuaAllocator
{
    /// <summary>
    ///     Gets the maximum amount of bytes this allocator can allocate.
    ///     <c>0</c> indicates no limit.
    /// </summary>
    public nuint MaxBytes { get; }

    /// <summary>
    ///     Gets the current amount of bytes allocated by this allocator.
    /// </summary>
    public nuint CurrentlyAllocatedBytes => _currentlyAllocatedBytes;

    /// <summary>
    ///     Gets the total amount of bytes allocated by this allocator.
    /// </summary>
    public nuint TotalAllocatedBytes => _totalAllocatedBytes;

    /// <summary>
    ///     Gets the total amount of times this allocator allocated memory.
    /// </summary>
    public nuint TimesAllocated => _timesAllocated;

    /// <summary>
    ///     Fired when Lua allocates memory.
    /// </summary>
    /// <remarks>
    ///     Subscribed event handlers should be as lightweight as possible
    ///     and must not throw any exceptions.
    /// </remarks>
    public event EventHandler<MemoryAllocatedEventArgs>? MemoryAllocated;

    /// <summary>
    ///     Fired when Lua reallocates memory.
    /// </summary>
    /// <remarks>
    ///     <inheritdoc cref="MemoryAllocated"/>
    /// </remarks>
    public event EventHandler<MemoryReallocatedEventArgs>? MemoryReallocated;

    /// <summary>
    ///     Fired when Lua frees memory.
    /// </summary>
    /// <remarks>
    ///     <inheritdoc cref="MemoryAllocated"/>
    /// </remarks>
    public event EventHandler<MemoryFreedEventArgs>? MemoryFreed;

    /// <summary>
    ///     Fired when Lua fails to allocate memory due to exceeding <see cref="MaxBytes"/>.
    /// </summary>
    /// <remarks>
    ///     <inheritdoc cref="MemoryAllocated"/>
    /// </remarks>
    public event EventHandler<MemoryDeniedEventArgs>? MemoryDenied;

    /// <summary>
    ///     Fired when Lua fails to allocate memory due to the machine not having enough free memory left.
    /// </summary>
    /// <remarks>
    ///     <inheritdoc cref="MemoryAllocated"/>
    /// </remarks>
    public event EventHandler<MemoryOutOfMemoryEventArgs>? MemoryOutOfMemory;

    private nuint _currentlyAllocatedBytes;
    private nuint _totalAllocatedBytes;
    private nuint _timesAllocated;

    /// <summary>
    ///     Instantiates a new <see cref="NativeMemoryLuaAllocator"/> with the specified
    ///     maximum amount of bytes Lua is allowed to allocate.
    /// </summary>
    /// <param name="maxBytes"> The maximum amount of bytes. <c>0</c> indicates no limit. </param>
    public NativeMemoryLuaAllocator(nuint maxBytes = 0)
    {
        MaxBytes = maxBytes;
    }

    /// <inheritdoc/>
    protected internal override void* Allocate( /*void* userDataPtr,*/ void* ptr, nuint oldSizeOrType, nuint size)
    {
        if (ptr == null)
        {
            oldSizeOrType = 0;
        }

        if (size == 0)
        {
            NativeMemory.Free(ptr);
            _currentlyAllocatedBytes -= oldSizeOrType;

            if (ptr != null)
            {
                MemoryFreed?.Invoke(this, new MemoryFreedEventArgs((IntPtr) ptr, oldSizeOrType));
            }

            return null;
        }

        if (MaxBytes != 0 && _currentlyAllocatedBytes + (size - oldSizeOrType) > MaxBytes)
        {
            MemoryDenied?.Invoke(this, new MemoryDeniedEventArgs(size));

            return null;
        }

        var oldPtr = ptr;
        try
        {
            ptr = NativeMemory.Realloc(ptr, size);
        }
        catch (OutOfMemoryException)
        {
            MemoryOutOfMemory?.Invoke(this, new MemoryOutOfMemoryEventArgs(size));

            return null;
        }

        _currentlyAllocatedBytes += size - oldSizeOrType;
        _totalAllocatedBytes += size - oldSizeOrType;
        _timesAllocated++;

        if (oldPtr == null)
        {
            MemoryAllocated?.Invoke(this, new MemoryAllocatedEventArgs((IntPtr) ptr, size));
        }
        else
        {
            MemoryReallocated?.Invoke(this, new MemoryReallocatedEventArgs((IntPtr) oldPtr, oldSizeOrType, (IntPtr) ptr, size));
        }

        return ptr;
    }
}
