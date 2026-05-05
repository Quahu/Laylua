using System;
using System.Runtime.CompilerServices;
using Laylua.Marshaling;
using Laylua.Moon;

namespace Laylua;

internal static unsafe class LuaAsyncLibrary
{
    private const string LibraryRegistryKey = "__Laylua:async_library";
    private const string FactoryRegistryKey = "__Laylua:async_factory";
    private const string FactoryChunkName = "=[laylua-async-factory]";
    private const string CoroutineResumeRegistryKey = "__Laylua:async_coroutine_resume";
    private const string CoroutineWrapRegistryKey = "__Laylua:async_coroutine_wrap";
    private const string CoroutinePatchChunkName = "=[laylua-async-coroutine]";

    private const string AwaitFunctionName = "await";
    private const string IsAwaitableFunctionName = "is_awaitable";
    private const string ErrorFunctionName = "__error";

    private const string CoroutineGlobalName = "coroutine";
    private const string CoroutineResumeFunctionName = "resume";
    private const string CoroutineWrapFunctionName = "wrap";
    private const string CoroutineCreateFunctionName = "create";
    private const string CoroutineStatusFunctionName = "status";
    private const string AsyncExecutionRequiredMessage = "Implicit await requires LuaThread.ExecuteAsync, LuaThread.EvaluateAsync, or LuaFunction.CallAsync. Configure AwaitableReturnHandling.ReturnAwaitable to return the awaitable instead.";

    private static readonly ConditionalWeakTable<Lua, object> _initialized = new();

    private static readonly object _sentinel = new();

    private static ReadOnlySpan<byte> FactorySource => """
        local await = ...
        return function(raw)
            return function(...)
                return await(raw(...))
            end
        end
        """u8;

    private static ReadOnlySpan<byte> CoroutinePatchSource => """
        local original_resume, original_status, original_create, is_awaitable, raise_error, async_message = ...
        local function resume_results(co, ok, value, ...)
            if ok and original_status(co) ~= 'dead' and is_awaitable(value) then
              raise_error(async_message)
            end
            return ok, value, ...
        end
        
        local function wrap_results(ok, ...)
            if not ok then
              raise_error((...))
            end
            return ...
        end
        
        local function guarded_resume(co, ...)
            return resume_results(co, original_resume(co, ...))
        end
        
        local function guarded_wrap(f)
            local co = original_create(f)
            return function(...)
              return wrap_results(resume_results(co, original_resume(co, ...)))
            end
        end
        
        return guarded_resume, guarded_wrap
        """u8;

    private static readonly LuaCFunction AwaitCFunction = static L =>
    {
        if (lua_gettop(L) < 1 || !AwaitableUserData.IsAwaitable(L, 1))
        {
            return luaL_argerror(L, 1, "awaitable expected");
        }

        if (!lua_isyieldable(L))
        {
            return RaiseAsyncExecutionRequired(L);
        }

        lua_settop(L, 1);

        var laylua = LayluaState.FromExtraSpace(L);
        if (laylua.Panic != null)
        {
            var panic = laylua.PopPanic();
            if (panic != null)
            {
                laylua.ResetPanic(panic);
            }
        }

        return lua_yield(L, 1);
    };

    private static readonly LuaCFunction IsAwaitableCFunction = static L =>
    {
        var result = lua_gettop(L) >= 1 && AwaitableUserData.IsAwaitable(L, 1);
        lua_pushboolean(L, result);
        return 1;
    };

    private static readonly LuaCFunction ErrorCFunction = static L =>
    {
        var message = lua_gettop(L) >= 1
            ? lua_tostring(L, 1).ToString()
            : "Laylua async coroutine error.";

        return luaL_error(L, message);
    };

    public static void EnsureInitialized(Lua lua)
    {
        if (!IsRuntimeAvailable(lua.State.L))
        {
            Initialize(lua.State.L);
        }

        _initialized.GetValue(lua, static _ => _sentinel);
    }

    public static void RefreshCoroutinePatchIfInitialized(Lua lua)
    {
        if (!_initialized.TryGetValue(lua, out _) || !IsAsyncCoreAvailable(lua.State.L))
        {
            return;
        }

        PatchCoroutineLibraryIfAvailable(lua.State.L);
    }

    private static bool IsRuntimeAvailable(lua_State* L)
    {
        if (!IsAsyncCoreAvailable(L))
        {
            return false;
        }

        return IsCoroutinePatchAppliedOrNotNeeded(L);
    }

    private static bool IsAsyncCoreAvailable(lua_State* L)
    {
        var stackTop = lua_gettop(L);
        try
        {
            if (lua_getfield(L, LuaRegistry.Index, LibraryRegistryKey) != LuaType.Table)
            {
                return false;
            }

            var hasAwait = lua_getfield(L, -1, AwaitFunctionName) == LuaType.Function;
            lua_pop(L);

            var hasIsAwaitable = lua_getfield(L, -1, IsAwaitableFunctionName) == LuaType.Function;
            lua_pop(L);

            var hasError = lua_getfield(L, -1, ErrorFunctionName) == LuaType.Function;
            lua_pop(L);

            if (!hasAwait || !hasIsAwaitable || !hasError)
            {
                return false;
            }

            lua_pop(L);

            return lua_getfield(L, LuaRegistry.Index, FactoryRegistryKey) == LuaType.Function;
        }
        finally
        {
            lua_settop(L, stackTop);
        }
    }

    private static void Initialize(lua_State* L)
    {
        lua_createtable(L, 0, 3);
        try
        {
            lua_pushcfunction(L, AwaitCFunction);
            lua_setfield(L, -2, AwaitFunctionName);

            lua_pushcfunction(L, IsAwaitableCFunction);
            lua_setfield(L, -2, IsAwaitableFunctionName);

            lua_pushcfunction(L, ErrorCFunction);
            lua_setfield(L, -2, ErrorFunctionName);

            lua_pushvalue(L, -1);
            lua_setfield(L, LuaRegistry.Index, LibraryRegistryKey);
        }
        finally
        {
            lua_pop(L);
        }

        LoadChunkOrThrow(L, FactorySource, FactoryChunkName, "Failed to load the Laylua async wrapper factory");
        PushAsyncFunction(L, AwaitFunctionName);

        var callStatus = lua_pcall(L, 1, 1, 0);
        if (callStatus.IsError())
        {
            var message = ReadErrorMessage(L);
            LuaThread.ThrowLuaException($"Failed to call the Laylua async wrapper factory: {message}");
        }

        lua_setfield(L, LuaRegistry.Index, FactoryRegistryKey);
        PatchCoroutineLibraryIfAvailable(L);
    }

    private static bool IsCoroutinePatchAppliedOrNotNeeded(lua_State* L)
    {
        var stackTop = lua_gettop(L);
        try
        {
            if (lua_getglobal(L, CoroutineGlobalName) != LuaType.Table)
            {
                return true;
            }

            var coroutineIndex = lua_absindex(L, -1);
            if (lua_getfield(L, coroutineIndex, CoroutineResumeFunctionName) != LuaType.Function)
            {
                return true;
            }

            var resumeIndex = lua_absindex(L, -1);
            if (lua_getfield(L, coroutineIndex, CoroutineWrapFunctionName) != LuaType.Function)
            {
                return true;
            }

            var wrapIndex = lua_absindex(L, -1);
            return IsSameAsRegistryFunction(L, resumeIndex, CoroutineResumeRegistryKey)
                && IsSameAsRegistryFunction(L, wrapIndex, CoroutineWrapRegistryKey);
        }
        finally
        {
            lua_settop(L, stackTop);
        }
    }

    private static void PatchCoroutineLibraryIfAvailable(lua_State* L)
    {
        var stackTop = lua_gettop(L);
        try
        {
            if (lua_getglobal(L, CoroutineGlobalName) != LuaType.Table)
            {
                return;
            }

            var coroutineIndex = lua_absindex(L, -1);
            if (lua_getfield(L, coroutineIndex, CoroutineResumeFunctionName) != LuaType.Function)
            {
                return;
            }

            var resumeIndex = lua_absindex(L, -1);
            if (lua_getfield(L, coroutineIndex, CoroutineWrapFunctionName) != LuaType.Function)
            {
                return;
            }

            var wrapIndex = lua_absindex(L, -1);
            if (lua_getfield(L, coroutineIndex, CoroutineStatusFunctionName) != LuaType.Function)
            {
                return;
            }

            var statusIndex = lua_absindex(L, -1);
            if (lua_getfield(L, coroutineIndex, CoroutineCreateFunctionName) != LuaType.Function)
            {
                return;
            }

            var createIndex = lua_absindex(L, -1);
            if (IsSameAsRegistryFunction(L, resumeIndex, CoroutineResumeRegistryKey)
                && IsSameAsRegistryFunction(L, wrapIndex, CoroutineWrapRegistryKey))
            {
                return;
            }

            LoadChunkOrThrow(L, CoroutinePatchSource, CoroutinePatchChunkName, "Failed to load the Laylua coroutine patch");

            lua_pushvalue(L, resumeIndex);
            lua_pushvalue(L, statusIndex);
            lua_pushvalue(L, createIndex);
            PushAsyncFunction(L, IsAwaitableFunctionName);
            PushAsyncFunction(L, ErrorFunctionName);
            lua_pushstring(L, AsyncExecutionRequiredMessage);

            var callStatus = lua_pcall(L, 6, 2, 0);
            if (callStatus.IsError())
            {
                var message = ReadErrorMessage(L);
                LuaThread.ThrowLuaException($"Failed to call the Laylua coroutine patch: {message}");
            }

            lua_pushvalue(L, -2);
            lua_setfield(L, coroutineIndex, CoroutineResumeFunctionName);
            lua_pushvalue(L, -2);
            lua_setfield(L, LuaRegistry.Index, CoroutineResumeRegistryKey);

            lua_pushvalue(L, -1);
            lua_setfield(L, coroutineIndex, CoroutineWrapFunctionName);
            lua_pushvalue(L, -1);
            lua_setfield(L, LuaRegistry.Index, CoroutineWrapRegistryKey);
        }
        finally
        {
            lua_settop(L, stackTop);
        }
    }

    internal static void PushLibrary(lua_State* L)
    {
        if (lua_getfield(L, LuaRegistry.Index, LibraryRegistryKey) != LuaType.Table)
        {
            lua_pop(L);
            LuaThread.ThrowLuaException("The Laylua async helper library is not available.");
        }
    }

    private static void PushAsyncFunction(lua_State* L, string name)
    {
        PushLibrary(L);

        if (lua_getfield(L, -1, name) != LuaType.Function)
        {
            lua_pop(L, 2);
            LuaThread.ThrowLuaException($"The Laylua async helper function '{name}' is not available.");
        }

        lua_remove(L, -2);
    }

    private static bool IsSameAsRegistryFunction(lua_State* L, int functionIndex, string registryKey)
    {
        if (lua_getfield(L, LuaRegistry.Index, registryKey) != LuaType.Function)
        {
            lua_pop(L);
            return false;
        }

        var matches = lua_rawequal(L, functionIndex, -1);
        lua_pop(L);
        return matches;
    }

    private static string ReadErrorMessage(lua_State* L)
    {
        var message = lua_tostring(L, -1).ToString() ?? "unknown Lua error";
        lua_pop(L);
        return message;
    }

    public static void WrapAsyncDelegate(Lua lua, lua_State* L)
    {
        EnsureInitialized(lua);

        if (lua_getfield(L, LuaRegistry.Index, FactoryRegistryKey) != LuaType.Function)
        {
            lua_pop(L);
            LuaThread.ThrowLuaException("Failed to retrieve the Laylua async wrapper factory.");
        }

        lua_insert(L, -2);

        var status = lua_pcall(L, 1, 1, 0);
        if (status.IsError())
        {
            LuaThread.ThrowLuaException(LuaThread.FromExtraSpace(L), status);
        }
    }

    public static int YieldAwaitable(lua_State* L)
    {
        EnsureInitialized(Lua.FromExtraSpace(L));

        if (!AwaitableUserData.IsAwaitable(L, -1))
        {
            return luaL_error(L, "awaitable expected");
        }

        if (!lua_isyieldable(L))
        {
            return RaiseAsyncExecutionRequired(L);
        }

        if (lua_gettop(L) != 1)
        {
            lua_replace(L, 1);
        }

        lua_settop(L, 1);

        var laylua = LayluaState.FromExtraSpace(L);
        if (laylua.Panic != null)
        {
            var panic = laylua.PopPanic();
            if (panic != null)
            {
                laylua.ResetPanic(panic);
            }
        }

        return lua_yield(L, 1);
    }

    private static void LoadChunkOrThrow(lua_State* L, ReadOnlySpan<byte> source, string chunkName, string errorPrefix)
    {
        fixed (byte* sourcePtr = source)
        {
            var loadStatus = luaL_loadbuffer(L, sourcePtr, (nuint) source.Length, chunkName);
            if (loadStatus.IsError())
            {
                var message = ReadErrorMessage(L);
                LuaThread.ThrowLuaException($"{errorPrefix}: {message}");
            }
        }
    }

    private static int RaiseAsyncExecutionRequired(lua_State* L)
    {
        return LuaException.RaiseErrorInfo(L, AsyncExecutionRequiredMessage, exception: null);
    }
}
