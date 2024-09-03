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
    internal static readonly ConcurrentDictionary<IntPtr, GCHandle> ErrorInfoHandles = new();

    private class ErrorInfo(string? message, Exception? exception)
    {
        public string? Message { get; } = message;

        public Exception? Exception { get; } = exception;
    }

    private const string UnknownMessage = "An unknown error occurred.";

    /// <summary>
    ///     Gets the status associated with this exception.
    ///     This usually comes from methods such as <see cref="Lua.Execute(string,string?)"/>.
    /// </summary>
    public LuaStatus? Status { get; private set; }

    internal LuaException(string? message, Exception? innerException = null)
        : base(message, innerException)
    { }

    private static string GetMessage(string? message, Exception? innerException)
    {
        return message ?? (innerException != null ? "An exception occurred, but was caught and raised as an error." : UnknownMessage);
    }

    internal LuaException WithStatus(LuaStatus status)
    {
        Status = status;
        return this;
    }

    internal static unsafe LuaException ConstructFromStack(Lua lua)
    {
        var L = lua.GetStatePointer();
        if (TryGetError(L, out var error))
        {
            lua_pop(L);
            return new LuaException(error.Message, error.Exception);
        }

        return new LuaException("Error object is neither a string nor an exception.");
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
            error = (lua_tostring(L, -1).ToString().Replace("\r", "").Replace("\n", ""), Exception: null);
            return true;
        }

        var ptr = (IntPtr) lua_touserdata(L, -1);
        if (!ErrorInfoHandles.TryRemove(ptr, out var handle))
        {
            error = default;
            return false;
        }

        var errorInfo = Unsafe.As<ErrorInfo>(handle.Target)!;
        error = (errorInfo.Message, errorInfo.Exception);
        handle.Free();
        return true;
    }

    [DoesNotReturn]
    internal static unsafe int RaiseErrorInfo(lua_State* L, string? message, Exception? exception)
    {
        luaL_checkstack(L, 1, "No space on the stack to push error info.");

        var handle = GCHandle.Alloc(new ErrorInfo(message, exception));
        var ptr = (IntPtr) handle;
        ErrorInfoHandles[ptr] = handle;

        lua_pushlightuserdata(L, ptr.ToPointer());
        return lua_error(L);
    }
}
