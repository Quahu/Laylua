using System;
using Laylua.Moon;
using Microsoft.Extensions.Logging;

namespace Laylua;

public static class NativeMemoryLuaAllocatorLogging
{
    public static EventId AllocEventId => new(1, "Alloc");

    public static EventId ReallocEventId => new(2, "Realloc");

    public static EventId FreeEventId => new(3, "Free");

    public static EventId DenyEventId => new(4, "Deny");

    public static EventId OutOfMemoryEventId => new(5, "OutOfMemory");

    public static readonly Action<ILogger, nuint, IntPtr, Exception?> Alloc = LoggerMessage.Define<nuint, IntPtr>(
        LogLevel.Trace, AllocEventId, "Lua allocated {ByteCount} bytes at address 0x{Address:X}.");

    public static readonly Action<ILogger, nuint, IntPtr, nuint, IntPtr, Exception?> Realloc = LoggerMessage.Define<nuint, IntPtr, nuint, IntPtr>(
        LogLevel.Trace, ReallocEventId, "Lua reallocated {OldByteCount} bytes at address 0x{OldAddress:X} to {NewByteCount} bytes at address 0x{NewAddress:X}.");

    public static readonly Action<ILogger, nuint, IntPtr, Exception?> Free = LoggerMessage.Define<nuint, IntPtr>(
        LogLevel.Trace, FreeEventId, "Lua freed {ByteCount} bytes at address 0x{Address:X}.");

    public static readonly Action<ILogger, nuint, Exception?> Deny = LoggerMessage.Define<nuint>(
        LogLevel.Trace, DenyEventId, "Lua was denied {ByteCount} bytes - too much memory allocated.");

    public static void Hook(NativeMemoryLuaAllocator allocator, ILogger logger)
    {
        allocator.MemoryAllocated += (sender, e) => Alloc(logger, e.Bytes, e.Address, null);
        allocator.MemoryReallocated += (sender, e) => Realloc(logger, e.OldBytes, e.OldAddress, e.NewBytes, e.NewAddress, null);
        allocator.MemoryFreed += (sender, e) => Free(logger, e.Bytes, e.Address, null);
        allocator.MemoryDenied += (sender, e) => Deny(logger, e.Bytes, null);
        allocator.MemoryOutOfMemory += (sender, e) => logger.LogWarning(OutOfMemoryEventId, "Out of memory.");
    }
}
