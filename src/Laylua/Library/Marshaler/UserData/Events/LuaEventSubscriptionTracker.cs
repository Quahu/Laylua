using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Laylua.Moon;

namespace Laylua.Marshaling;

public sealed unsafe class LuaEventSubscriptionTracker
{
    private readonly struct Subscription(LuaFunction function, Delegate handler, EventInfo eventInfo, object? target, object cleanupKey)
    {
        public readonly LuaFunction Function = function;
        public readonly Delegate Handler = handler;
        public readonly EventInfo EventInfo = eventInfo;
        public readonly object? Target = target;
        public readonly object CleanupKey = cleanupKey;
    }

    private readonly ConcurrentDictionary<object, Dictionary<string, List<Subscription>>> _subscriptionsByLookupKey = new();

    public bool Subscribe(lua_State* L, object lookupKey, object cleanupKey, string eventName, EventInfo eventInfo, object? target, int functionStackIndex)
    {
        var lua = Lua.FromExtraSpace(L);

        if (!LuaReference.TryCreate(L, functionStackIndex, out var reference))
        {
            return false;
        }

        LuaFunction function;
        try
        {
            function = new LuaFunction();
            function.Lua = lua;
            function.Reference = reference;
        }
        catch
        {
            luaL_unref(L, LuaRegistry.Index, reference);
            throw;
        }

        var delegateType = eventInfo.EventHandlerType!;
        var handler = DefaultLuaMarshaler.CreateDelegateFromFunction(function, delegateType);
        if (handler == null)
        {
            function.Dispose();
            return false;
        }

        var addMethod = eventInfo.AddMethod ?? eventInfo.GetAddMethod(nonPublic: true);
        if (addMethod == null)
        {
            function.Dispose();
            return false;
        }

        var wasAdded = false;
        try
        {
            addMethod.Invoke(target, [handler]);
            wasAdded = true;

            var subscriptionsForLookupKey = _subscriptionsByLookupKey.GetOrAdd(lookupKey, static _ => new Dictionary<string, List<Subscription>>());
            lock (subscriptionsForLookupKey)
            {
                if (!subscriptionsForLookupKey.TryGetValue(eventName, out var list))
                {
                    list = [];
                    subscriptionsForLookupKey[eventName] = list;
                }

                list.Add(new Subscription(function, handler, eventInfo, target, cleanupKey));
            }

            return true;
        }
        catch (Exception ex)
        {
            var cleanupException = ex;
            var canDisposeFunction = true;

            if (wasAdded)
            {
                var removeMethod = eventInfo.RemoveMethod ?? eventInfo.GetRemoveMethod(nonPublic: true);
                if (removeMethod != null)
                {
                    try
                    {
                        removeMethod.Invoke(target, [handler]);
                    }
                    catch (Exception rollbackEx)
                    {
                        cleanupException = LuaTypeServices.CombineCleanupException(UnwrapInvocationException(cleanupException), UnwrapInvocationException(rollbackEx));
                        canDisposeFunction = false;
                    }
                }
                else
                {
                    canDisposeFunction = false;
                }
            }

            if (canDisposeFunction)
            {
                try
                {
                    function.Dispose();
                }
                catch (Exception disposeEx)
                {
                    cleanupException = LuaTypeServices.CombineCleanupException(cleanupException, disposeEx);
                }
            }

            LuaTypeServices.ThrowCleanupException(UnwrapInvocationException(cleanupException));
            return false;
        }
    }

    public bool Unsubscribe(lua_State* L, object lookupKey, string eventName, EventInfo eventInfo, object? target, int functionStackIndex)
    {
        if (!_subscriptionsByLookupKey.TryGetValue(lookupKey, out var subscriptionsForLookupKey))
        {
            return false;
        }

        lock (subscriptionsForLookupKey)
        {
            if (!subscriptionsForLookupKey.TryGetValue(eventName, out var list) || list.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < list.Count; i++)
            {
                var sub = list[i];
                lua_rawgeti(L, LuaRegistry.Index, LuaReference.GetReference(sub.Function));
                var equal = lua_rawequal(L, -1, functionStackIndex < 0 ? functionStackIndex - 1 : functionStackIndex);
                lua_pop(L, 1);

                if (equal)
                {
                    var removeMethod = sub.EventInfo.RemoveMethod ?? sub.EventInfo.GetRemoveMethod(nonPublic: true);
                    if (removeMethod == null)
                    {
                        return false;
                    }

                    try
                    {
                        removeMethod.Invoke(sub.Target, [sub.Handler]);
                    }
                    catch (Exception ex)
                    {
                        ExceptionDispatchInfo.Capture(UnwrapInvocationException(ex)).Throw();
                        return false;
                    }

                    list.RemoveAt(i);
                    sub.Function.Dispose();

                    if (list.Count == 0)
                    {
                        subscriptionsForLookupKey.Remove(eventName);
                    }

                    if (subscriptionsForLookupKey.Count == 0)
                    {
                        _subscriptionsByLookupKey.TryRemove(lookupKey, out _);
                    }

                    return true;
                }
            }
        }

        return false;
    }

    public void CleanupAll(lua_State* L, object cleanupKey)
    {
        Exception? cleanupException = null;
        foreach (var kvp in _subscriptionsByLookupKey)
        {
            var subscriptionsForLookupKey = kvp.Value;
            lock (subscriptionsForLookupKey)
            {
                foreach (var eventName in new List<string>(subscriptionsForLookupKey.Keys))
                {
                    var list = subscriptionsForLookupKey[eventName];
                    for (var i = list.Count - 1; i >= 0; i--)
                    {
                        var sub = list[i];
                        if (!Equals(sub.CleanupKey, cleanupKey))
                        {
                            continue;
                        }

                        var removeMethod = sub.EventInfo.RemoveMethod ?? sub.EventInfo.GetRemoveMethod(nonPublic: true);
                        if (removeMethod == null)
                        {
                            cleanupException = LuaTypeServices.CombineCleanupException(cleanupException,
                                new InvalidOperationException($"Event '{sub.EventInfo.Name}' does not have a remove accessor."));

                            continue;
                        }

                        try
                        {
                            removeMethod.Invoke(sub.Target, [sub.Handler]);
                        }
                        catch (Exception ex)
                        {
                            cleanupException = LuaTypeServices.CombineCleanupException(cleanupException, UnwrapInvocationException(ex));
                            continue;
                        }

                        list.RemoveAt(i);

                        try
                        {
                            sub.Function.Dispose();
                        }
                        catch (Exception ex)
                        {
                            cleanupException = LuaTypeServices.CombineCleanupException(cleanupException, ex);
                        }
                    }

                    if (list.Count == 0)
                    {
                        subscriptionsForLookupKey.Remove(eventName);
                    }
                }

                if (subscriptionsForLookupKey.Count == 0)
                {
                    _subscriptionsByLookupKey.TryRemove(kvp.Key, out _);
                }
            }
        }

        LuaTypeServices.ThrowCleanupException(cleanupException);
    }

    private static Exception UnwrapInvocationException(Exception exception)
    {
        if (exception is TargetInvocationException { InnerException: not null } invocationException)
        {
            return invocationException.InnerException!;
        }

        return exception;
    }
}
