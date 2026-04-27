using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Laylua.Moon;

namespace Laylua;

/// <summary>
///     Represents errors that occur when executing Lua operations.
/// </summary>
/// <remarks>
///     If <see cref="Exception.InnerException"/> has a value,
///     it usually means that a callback or another user-provided component threw an exception.
/// </remarks>
/// <seealso cref="LuaPanicException"/>
public class LuaException : Exception
{
    private static readonly ConcurrentDictionary<IntPtr, ErrorInfoHandle> ErrorInfoHandles = new();

    private class ErrorInfo(string? message, Exception? exception)
    {
        public string? Message { get; } = message;

        public Exception? Exception { get; } = exception;
    }

    private readonly struct ErrorInfoHandle(GCHandle handle, IntPtr ownerKey)
    {
        public GCHandle Handle { get; } = handle;

        public IntPtr OwnerKey { get; } = ownerKey;
    }

    /// <summary>
    ///     Gets the status associated with this exception.
    ///     This usually comes from methods such as <see cref="Lua.Execute(string,string?)"/>.
    /// </summary>
    public LuaStatus? Status { get; private set; }

    internal LuaException(string? message, Exception? innerException = null)
        : base(message, innerException)
    { }

    /// <summary>
    ///     Attempts to extract most inner exception that is neither <see cref="LuaException"/> nor <see cref="LuaPanicException"/>.
    /// </summary>
    /// <returns>
    ///     The unwrapped exception or a <see cref="LuaException"/>.
    /// </returns>
    public Exception UnwrapException()
    {
        Exception exception = this;
        do
        {
            if (exception.InnerException == null)
            {
                break;
            }

            exception = exception.InnerException;
        }
        while (exception is LuaException);

        return exception;
    }

    internal LuaException WithStatus(LuaStatus status)
    {
        Status = status;
        return this;
    }

    internal static unsafe LuaException ConstructFromStack(LuaThread thread)
    {
        var L = thread.State.L;
        if (TryGetError(L, out var error))
        {
            lua_pop(L);
            return new LuaException(error.Message, error.Exception);
        }

        var errorType = lua_type(L, -1);
        lua_pop(L);
        return new LuaException($"Error object is a non-string, non-exception value (Lua type: {errorType}).");
    }

    internal static unsafe bool TryGetError(lua_State* L, out (string? Message, Exception? Exception) error)
    {
        var type = lua_type(L, -1);
        if (type != LuaType.String && type != LuaType.LightUserData)
        {
            error = default;
            return false;
        }

        if (type == LuaType.String)
        {
            error = (lua_tostring(L, -1).ToString()?.Replace("\r", "").Replace("\n", ""), Exception: null);
            return true;
        }

        var ptr = (IntPtr) lua_touserdata(L, -1);
        if (!ErrorInfoHandles.TryRemove(ptr, out var handle))
        {
            error = default;
            return false;
        }

        var errorInfo = Unsafe.As<ErrorInfo>(handle.Handle.Target)!;
        error = (errorInfo.Message, errorInfo.Exception);
        handle.Handle.Free();
        return true;
    }

    [DoesNotReturn]
    internal static unsafe int RaiseErrorInfo(lua_State* L, string? message, Exception? exception)
    {
        luaL_checkstack(L, 1, "No space on the stack to push error info.");

        var handle = GCHandle.Alloc(new ErrorInfo(message, exception));
        var ptr = (IntPtr) handle;
        var ownerKey = LayluaState.FromExtraSpace(L).GCPtr;
        ErrorInfoHandles[ptr] = new ErrorInfoHandle(handle, ownerKey);

        lua_pushlightuserdata(L, ptr.ToPointer());
        return lua_error(L);
    }

    /// <summary>
    ///     Pushes error info onto the Lua stack without calling lua_error.
    ///     Used by the IsPCall path so that ManagedPanicAsm can call lua_error
    ///     from assembly (avoiding managed frames between lua_error and pcall's setjmp).
    /// </summary>
    internal static unsafe void PrepareErrorInfo(lua_State* L, string? message, Exception? exception)
    {
        var handle = GCHandle.Alloc(new ErrorInfo(message, exception));
        var ptr = (IntPtr) handle;
        var ownerKey = LayluaState.FromExtraSpace(L).GCPtr;
        ErrorInfoHandles[ptr] = new ErrorInfoHandle(handle, ownerKey);

        lua_pushlightuserdata(L, ptr.ToPointer());
    }

    /// <summary>
    ///     Frees orphaned error info handles for a specific Lua state that were never retrieved by <see cref="TryGetError"/>.
    /// </summary>
    internal static void FreeOrphanedErrorInfoHandles(IntPtr ownerKey)
    {
        if (ownerKey == IntPtr.Zero)
        {
            return;
        }

        foreach (var kvp in ErrorInfoHandles)
        {
            if (kvp.Value.OwnerKey != ownerKey)
            {
                continue;
            }

            if (ErrorInfoHandles.TryRemove(kvp.Key, out var handle))
            {
                handle.Handle.Free();
            }
        }
    }
}
