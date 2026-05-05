using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Laylua.Moon;

namespace Laylua.Marshaling;

public unsafe partial class DefaultLuaMarshaler : LuaMarshaler
{
    /// <summary>
    ///     Gets or sets the format provider of this marshaler.
    /// </summary>
    /// <remarks>
    ///     This is used to determine how conversion and comparison of values is performed.
    ///     <br/>
    ///     Defaults to <see cref="CultureInfo.CurrentCulture"/>.
    /// </remarks>
    public IFormatProvider FormatProvider { get; set; } = CultureInfo.CurrentCulture;

    /// <summary>
    ///     Gets or sets whether unregistered types are automatically exposed via dynamic (reflection-based) marshaling.
    ///     Defaults to <c>false</c> for sandboxing purposes.
    ///     When <c>false</c>, pushing an unregistered type throws <see cref="InvalidOperationException"/>.
    ///     For pre-defined types, prefer annotating with <see cref="LuaTypeAttribute"/> for source-generated, high-performance marshaling.
    /// </summary>
    public bool AllowsDynamicMarshaling { get; set; }

    /// <summary>
    ///     Gets or sets how awaitable CLR delegate and method results are returned to Lua.
    /// </summary>
    public LuaAwaitableReturnHandling AwaitableReturnHandling { get; set; } = LuaAwaitableReturnHandling.ImplicitAwait;

    private static readonly ConditionalWeakTable<Lua, DelegateHandleSet> _delegateHandles = new();
    private static readonly ConditionalWeakTable<Lua, IdentityCache> _identityCaches = new();
    private static long _nextIdentityCacheKey;

    private sealed class DelegateHandleSet
    {
        public readonly HashSet<nint> Handles = [];
    }

    /// <summary>
    ///     Identity cache for userdata. Maps .NET objects to their Lua userdata handles.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     Uses a <see cref="ConditionalWeakTable{TKey,TValue}"/> on the .NET side (objects are weakly referenced)
    ///     paired with a weak-valued Lua table (<c>__mode="v"</c>) on the Lua side.
    ///     When either side collects, the mapping is cleaned up on the next access.
    ///     </para>
    ///     The marshaler's <c>PushValue</c> path uses identity caching so that pushing the same
    ///     .NET object yields the same Lua userdata.
    ///     The generated <c>LuaUserDataHandler.Push</c> is the low-level path and does NOT
    ///     use identity caching - each call creates a new GCHandle and userdata.
    /// </remarks>
    private sealed class IdentityCache
    {
        public readonly ConditionalWeakTable<object, IdentityCacheEntry> Refs = new();

        public int TableRef;
    }

    private sealed class IdentityCacheEntry
    {
        public readonly nint Key = unchecked((nint) Interlocked.Increment(ref _nextIdentityCacheKey));
    }

    /// <summary>
    ///     Attempts to push a previously cached userdata for the given object.
    ///     Returns <c>true</c> if the cached userdata was pushed onto the stack.
    /// </summary>
    internal static bool TryPushCachedIdentity(lua_State* L, Lua lua, object obj)
    {
        if (!_identityCaches.TryGetValue(lua, out var cache))
        {
            return false;
        }

        if (!cache.Refs.TryGetValue(obj, out var entry))
        {
            return false;
        }

        if (cache.TableRef == 0)
        {
            return false;
        }

        lua_rawgeti(L, LuaRegistry.Index, cache.TableRef);
        if (lua_rawgetp(L, -1, (void*) entry.Key).IsNoneOrNil())
        {
            lua_pop(L, 2);
            return false;
        }

        lua_remove(L, -2);
        return true;
    }

    /// <summary>
    ///     Caches the userdata currently on top of the stack for identity-based reuse.
    ///     The userdata must contain a GCHandle pointer as its first field.
    /// </summary>
    internal static void CacheIdentity(lua_State* L, Lua lua, object obj)
    {
        var cache = _identityCaches.GetValue(lua, static _ => new IdentityCache());

        if (cache.TableRef == 0)
        {
            lua_createtable(L, 0, 0);
            lua_createtable(L, 0, 1);
            lua_pushstring(L, "__mode"u8);
            lua_pushstring(L, "v"u8);
            lua_rawset(L, -3);
            lua_setmetatable(L, -2);
            cache.TableRef = luaL_ref(L, LuaRegistry.Index);
        }

        var entry = cache.Refs.GetValue(obj, static _ => new IdentityCacheEntry());

        lua_rawgeti(L, LuaRegistry.Index, cache.TableRef);
        lua_pushvalue(L, -2);
        lua_rawsetp(L, -2, (void*) entry.Key);
        lua_pop(L, 1);
    }

    internal static void TrackDelegateHandle(Lua lua, nint handlePtr)
    {
        var handleSet = _delegateHandles.GetValue(lua, static _ => new DelegateHandleSet());
        lock (handleSet.Handles)
        {
            handleSet.Handles.Add(handlePtr);
        }
    }

    internal static bool TryUntrackDelegateHandle(Lua lua, nint handlePtr)
    {
        if (_delegateHandles.TryGetValue(lua, out var handleSet))
        {
            lock (handleSet.Handles)
            {
                var removed = handleSet.Handles.Remove(handlePtr);
                if (removed && handleSet.Handles.Count == 0)
                {
                    _delegateHandles.Remove(lua);
                }

                return removed;
            }
        }

        return false;
    }

    internal static bool IsDelegateHandleTracked(Lua lua, nint handlePtr)
    {
        if (_delegateHandles.TryGetValue(lua, out var handleSet))
        {
            lock (handleSet.Handles)
            {
                return handleSet.Handles.Contains(handlePtr);
            }
        }

        return false;
    }

    /// <inheritdoc/>
    protected internal override void OnLuaDisposing(Lua lua)
    {
        if (_identityCaches.TryGetValue(lua, out var cache) && cache.TableRef != 0)
        {
            luaL_unref(lua.State.L, LuaRegistry.Index, cache.TableRef);
            cache.TableRef = 0;
        }

        base.OnLuaDisposing(lua);
    }

    /// <inheritdoc/>
    protected internal override void OnLuaDisposed(Lua lua)
    {
        _identityCaches.Remove(lua);

        if (_delegateHandles.TryGetValue(lua, out var handleSet))
        {
            lock (handleSet.Handles)
            {
                foreach (var ptr in handleSet.Handles)
                {
                    var handle = GCHandle.FromIntPtr(ptr);
                    if (handle.IsAllocated)
                    {
                        handle.Free();
                    }
                }
            }

            _delegateHandles.Remove(lua);
        }

        base.OnLuaDisposed(lua);
    }
}
