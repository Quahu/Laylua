using System;
using System.Runtime.InteropServices;
using Qommon;

namespace Laylua.Moon;

/// <summary>
///     Represents an implementation of <see cref="LuaAllocator"/> using <see cref="NativeMemory"/>.
/// </summary>
/// <remarks>
///     This type serves as a managed counterpart to the default Lua memory allocator.
///     It includes features such as a maximum allocation limit and allocation tracking.
///     <para/>
///     This type is not thread-safe; it is not suitable for concurrent use.
///     Do not use a single instance of this type with multiple Lua states;
///     instantiate a new allocator for each Lua state.
/// </remarks>
public sealed unsafe class NativeMemoryLuaAllocator : LuaAllocator
{
    /// <summary>
    ///     Gets the maximum amount of bytes this allocator can allocate.
    ///     <c>0</c> indicates no limit.
    /// </summary>
    /// <remarks>
    ///     This cannot be set to a lower value than <see cref="CurrentlyAllocatedBytes"/>.
    /// </remarks>
    public nuint MaxBytes
    {
        get => _maxBytes;
        set
        {
            if (value != 0 && value < CurrentlyAllocatedBytes)
            {
                Throw.ArgumentOutOfRangeException(nameof(value), value, $"Value cannot be lower than {nameof(CurrentlyAllocatedBytes)}.");
            }

            _maxBytes = value;
        }
    }

    /// <summary>
    ///     Gets the current amount of bytes allocated by this allocator.
    /// </summary>
    public nuint CurrentlyAllocatedBytes => _currentlyAllocatedBytes;

    /// <summary>
    ///     Gets the total amount of bytes allocated by this allocator.
    /// </summary>
    public nuint TotalAllocatedBytes => _totalAllocatedBytes;

    /// <summary>
    ///     Gets the total amount of times this allocator allocated and reallocated memory.
    /// </summary>
    public nuint TimesAllocated => _timesAllocated;

    /// <summary>
    ///     Fired when Lua allocates memory.
    /// </summary>
    /// <remarks>
    ///     Subscribed event handlers must not throw any exceptions.
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

    private nuint _maxBytes;
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
        _maxBytes = maxBytes;
    }

    /// <inheritdoc/>
    protected internal override void* Allocate(void* ptr, nuint oldSizeOrType, nuint size)
    {
        if (ptr == null)
        {
            oldSizeOrType = 0;
        }
        else if (oldSizeOrType == size)
        {
            return ptr;
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

        if (_maxBytes > 0 && size > oldSizeOrType && _currentlyAllocatedBytes + (size - oldSizeOrType) > _maxBytes)
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

        var allocatedBytes = size - oldSizeOrType;
        _currentlyAllocatedBytes += allocatedBytes;
        _totalAllocatedBytes += allocatedBytes;
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
