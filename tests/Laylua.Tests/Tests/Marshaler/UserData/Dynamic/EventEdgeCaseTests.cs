using System.Reflection;
using Laylua.Marshaling;
using Laylua.Moon;

namespace Laylua.Tests;

public class DisposableDynamicEventTarget : IDisposable
{
    public event Action? TestEvent;

    public static event Action? StaticEvent;

    public bool IsDisposed { get; private set; }

    public static int StaticFireCount { get; private set; }

    public void Fire()
    {
        TestEvent?.Invoke();
    }

    public static void FireStatic()
    {
        StaticEvent?.Invoke();
    }

    public static void ResetStatic()
    {
        StaticEvent = null;
        StaticFireCount = 0;
    }

    public bool HasSubscribers => TestEvent != null;

    public static bool HasStaticSubscribers => StaticEvent != null;

    public void Dispose()
    {
        IsDisposed = true;
    }
}

public readonly ref struct RefStructEventArgs
{
    public ReadOnlySpan<char> Message { get; }

    public RefStructEventArgs(ReadOnlySpan<char> message)
    {
        Message = message;
    }
}

public delegate void RefStructEventHandler(object? sender, RefStructEventArgs e);

public delegate void RefIntEventHandler(ref int value);

public delegate ReadOnlySpan<char> SpanReturnEventHandler();

public class RefStructEventTarget
{
    public event RefStructEventHandler? RefStructEvent;

    public event Action? NormalEvent;

    public bool HasNormalSubscribers => NormalEvent != null;

    public void FireNormal()
    {
        NormalEvent?.Invoke();
    }
}

public class RefStructEventIndexerTarget
{
    public event RefStructEventHandler? RefStructEvent;

    public object? this[string key]
    {
        get
        {
            throw new KeyNotFoundException();
        }
    }
}

public class ThrowingStringIndexerTarget
{
    public object? this[string key]
    {
        get
        {
            throw new InvalidOperationException("Getter failure.");
        }
        set
        {
            throw new InvalidOperationException("Setter failure.");
        }
    }
}

public class ThrowingEventCleanupTarget : IDisposable
{
    private Action? _testEvent;

    public event Action? TestEvent
    {
        add
        {
            _testEvent += value;
        }
        remove
        {
            throw new InvalidOperationException("Event cleanup failure.");
        }
    }

    public bool WasDisposed { get; private set; }

    public void Dispose()
    {
        WasDisposed = true;
    }
}

public class RetryRemoveEventTarget
{
    private Action? _testEvent;
    private bool _failNextRemove = true;

    public event Action? TestEvent
    {
        add
        {
            _testEvent += value;
        }
        remove
        {
            if (_failNextRemove)
            {
                _failNextRemove = false;
                throw new InvalidOperationException("Remove failure.");
            }

            _testEvent -= value;
        }
    }

    public bool HasSubscribers => _testEvent != null;

    public void Fire()
    {
        _testEvent?.Invoke();
    }
}

public class RetryRemoveDisposableEventTarget : IDisposable
{
    private Action? _testEvent;
    private bool _failNextRemove = true;

    public event Action? TestEvent
    {
        add
        {
            _testEvent += value;
        }
        remove
        {
            if (_failNextRemove)
            {
                _failNextRemove = false;
                throw new InvalidOperationException("Remove failure.");
            }

            _testEvent -= value;
        }
    }

    public bool HasSubscribers => _testEvent != null;

    public void Dispose()
    { }
}

public class RefParameterEventTarget
{
    public event RefIntEventHandler? UnsupportedEvent;
}

public class UnsupportedReturnEventTarget
{
    public event SpanReturnEventHandler? UnsupportedEvent;
}

public class EventEdgeCaseTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
        Marshaler.AllowsDynamicMarshaling = true;
    }

    public override void TearDown()
    {
        Marshaler.AllowsDynamicMarshaling = false;
        base.TearDown();
    }

    [Test]
    public void Dynamic_DisposableDefinition_GcDoesNotThrow()
    {
        // Arrange
        DisposableDynamicEventTarget.ResetStatic();

        LuaTypeRegistry.ConfigureDynamic<DisposableDynamicEventTarget>(opts =>
        {
            opts.Features = LuaTypeFeatures.All;
        });

        Lua["DefType"] = typeof(DisposableDynamicEventTarget);

        // Act
        TestDelegate action = () =>
        {
            Lua.Execute(@"
                DefType = nil
                collectgarbage('collect')
                collectgarbage('collect')
            ");
        };

        // Assert
        Assert.DoesNotThrow(action);

        DisposableDynamicEventTarget.ResetStatic();
    }

    [Test]
    public void Dynamic_DisposableDefinition_StaticEventCleanup()
    {
        // Arrange
        DisposableDynamicEventTarget.ResetStatic();

        // Act
        LuaTypeRegistry.ConfigureDynamic<DisposableDynamicEventTarget>(opts =>
        {
            opts.Features = LuaTypeFeatures.All;
        });

        Lua["DefType"] = typeof(DisposableDynamicEventTarget);

        Lua.Execute(@"
            DefType.StaticEvent:add(function() end)
        ");

        Assert.That(DisposableDynamicEventTarget.HasStaticSubscribers, Is.True);

        Lua.Execute(@"
            DefType = nil
            collectgarbage('collect')
            collectgarbage('collect')
        ");

        // Assert
        Assert.That(DisposableDynamicEventTarget.HasStaticSubscribers, Is.False);
        DisposableDynamicEventTarget.ResetStatic();
    }

    [Test]
    public void Dynamic_DefinitionIndex_NumericKey_ReturnsNil()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicStaticEventTarget>(opts =>
        {
            opts.Features = LuaTypeFeatures.All;
        });

        // Act
        Lua["DefType"] = typeof(DynamicStaticEventTarget);

        // Assert
        Lua.Execute(@"
            local val = DefType[42]
            assert(val == nil)
        ");
    }

    [Test]
    public void Dynamic_DefinitionNewIndex_NumericKey_NoError()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicStaticEventTarget>(opts =>
        {
            opts.Features = LuaTypeFeatures.All;
        });

        // Act
        Lua["DefType"] = typeof(DynamicStaticEventTarget);

        TestDelegate action = () =>
        {
            Lua.Execute(@"
                DefType[42] = 'test'
            ");
        };

        // Assert
        Assert.DoesNotThrow(action);
    }

    [Test]
    public void Dynamic_DefinitionIndex_TableKey_ReturnsNil()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicStaticEventTarget>(opts =>
        {
            opts.Features = LuaTypeFeatures.All;
        });

        // Act
        Lua["DefType"] = typeof(DynamicStaticEventTarget);

        // Assert
        Lua.Execute(@"
            local val = DefType[{}]
            assert(val == nil)
        ");
    }

    [Test]
    public void Dynamic_DefinitionIndex_BooleanKey_ReturnsNil()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicStaticEventTarget>(opts =>
        {
            opts.Features = LuaTypeFeatures.All;
        });

        // Act
        Lua["DefType"] = typeof(DynamicStaticEventTarget);

        // Assert
        Lua.Execute(@"
            local val = DefType[true]
            assert(val == nil)
        ");
    }

    [Test]
    public void Dynamic_UnsubscribeAll_ThenResubscribe_Works()
    {
        // Arrange
        var obj = new EventLifecycleTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            count = 0
            local function h1() count = count + 1 end
            obj.TestEvent:add(h1)
            obj.TestEvent:remove(h1)
        ");

        Assert.That(obj.HasSubscribers, Is.False);

        Lua.Execute(@"
            local function h2() count = count + 10 end
            obj.TestEvent:add(h2)
        ");

        obj.Fire();

        // Assert
        var count = Lua.Evaluate<int>("return count");
        Assert.That(count, Is.EqualTo(10));
    }

    [Test]
    public void Dynamic_UnsubscribeLastFromOneEvent_OtherEventUnaffected()
    {
        // Arrange
        var obj = new DynamicEventTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            simple_count = 0
            typed_count = 0
            local function sh() simple_count = simple_count + 1 end
            local function th() typed_count = typed_count + 1 end
            obj.SimpleEvent:add(sh)
            obj.TypedEvent:add(th)
            obj.SimpleEvent:remove(sh)
        ");

        Assert.That(obj.HasSimpleSubscribers, Is.False);
        Assert.That(obj.HasTypedSubscribers, Is.True);

        obj.FireTyped();

        // Assert
        var typedCount = Lua.Evaluate<int>("return typed_count");
        Assert.That(typedCount, Is.EqualTo(1));
    }

    [Test]
    public void Dynamic_SubscribeToNonAddable_DoesNotLeakFunction()
    {
        // Arrange
        var obj = new EventLifecycleTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            obj.TestEvent:add(function() end)
        ");

        // Assert
        Assert.That(obj.HasSubscribers, Is.True);
    }

    [Test]
    public void Dynamic_EventAccessor_AddAndRemove_Work()
    {
        // Arrange
        var obj = new EventLifecycleTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            count = 0
            h = function() count = count + 1 end
            obj.TestEvent:add(h)
        ");

        obj.Fire();
        var count = Lua.Evaluate<int>("return count");
        Assert.That(count, Is.EqualTo(1));

        Lua.Execute(@"
            obj.TestEvent:remove(h)
        ");

        // Assert
        Assert.That(obj.HasSubscribers, Is.False);
    }

    [Test]
    public void Dynamic_CloseCallback_CleansUpEvents()
    {
        // Arrange
        var obj = new DisposableDynamicEventTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            obj.TestEvent:add(function() end)
        ");

        Assert.That(obj.HasSubscribers, Is.True);

        Lua.Execute(@"
            obj = nil
            collectgarbage('collect')
            collectgarbage('collect')
        ");

        // Assert
        Assert.That(obj.HasSubscribers, Is.False);
    }

    [Test]
    public void Dynamic_CloseCallback_EventCleanupFailure_StillDisposesAndSurfacesError()
    {
        // Arrange
        var obj = new ThrowingEventCleanupTarget();
        Lua["obj"] = obj;

        // Act
        var ex = Assert.Throws<LuaException>(() =>
        {
            Lua.Execute(@"
                do
                    local closeable <close> = obj
                    closeable.TestEvent:add(function() end)
                end
            ");
        });

        // Assert
        Assert.That(obj.WasDisposed, Is.True);
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(ex.InnerException!.Message, Is.EqualTo("Event cleanup failure."));
    }

    [Test]
    public void Dynamic_StaleAccessor_DoesNotLeakSubscriptionAfterAccessorCollected()
    {
        // Arrange
        var obj = new DynamicEventTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            acc = obj.SimpleEvent
            obj = nil
            collectgarbage('collect')
            collectgarbage('collect')
            acc:add(function() end)
            acc = nil
            collectgarbage('collect')
            collectgarbage('collect')
        ");

        // Assert
        Assert.That(obj.HasSimpleSubscribers, Is.False);
    }

    [Test]
    public void Dynamic_RemoveFailure_KeepsSubscriptionUsableAndRetriable()
    {
        // Arrange
        var obj = new RetryRemoveEventTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            count = 0
            h = function() count = count + 1 end
            obj.TestEvent:add(h)
        ");

        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj.TestEvent:remove(h)"));
        Assert.That(ex!.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(ex.InnerException!.Message, Is.EqualTo("Remove failure."));

        obj.Fire();
        var count = Lua.Evaluate<int>("return count");
        Assert.That(count, Is.EqualTo(1));

        Lua.Execute("obj.TestEvent:remove(h)");
        obj.Fire();

        // Assert
        count = Lua.Evaluate<int>("return count");
        Assert.That(count, Is.EqualTo(1));
        Assert.That(obj.HasSubscribers, Is.False);
    }

    [Test]
    public unsafe void Dynamic_CleanupFailure_KeepsSubscriptionTrackedForRetry()
    {
        // Arrange
        var obj = new RetryRemoveDisposableEventTarget();
        LuaTypeRegistry.ConfigureDynamic<RetryRemoveDisposableEventTarget>();
        Lua["obj"] = obj;

        Lua.Execute(@"
            h = function() end
            obj.TestEvent:add(h)
        ");

        var handler = (DynamicUserDataHandler) LuaTypeServices.GetHandler(typeof(RetryRemoveDisposableEventTarget))!;
        var trackerField = typeof(DynamicUserDataHandler).GetField("_eventTracker", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var tracker = (LuaEventSubscriptionTracker) trackerField.GetValue(handler)!;

        // Act
        using var _ = Lua.Stack.SnapshotCount();
        lua_getglobal(L, "obj");
        var userDataKey = (nint) lua_touserdata(L, -1);
        var ex = Assert.Throws<InvalidOperationException>(() => tracker.CleanupAll(L, userDataKey));

        Lua.Execute("obj.TestEvent:remove(h)");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex!.Message, Is.EqualTo("Remove failure."));
            Assert.That(obj.HasSubscribers, Is.False);
        });
    }

    [Test]
    public void Dynamic_ByRefParameterEvent_NotVisible()
    {
        // Arrange
        var obj = new RefParameterEventTarget();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj.UnsupportedEvent == nil)");
    }

    [Test]
    public void Dynamic_UnsupportedReturnEvent_NotVisible()
    {
        // Arrange
        var obj = new UnsupportedReturnEventTarget();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj.UnsupportedEvent == nil)");
    }

    [Test]
    public void Dynamic_ForeignUserdata_DoesNotCrashAccessor()
    {
        // Arrange
        var obj = new EventLifecycleTarget();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute(@"
            local acc = obj.TestEvent
            assert(acc ~= nil)
            assert(type(acc.add) == 'function')
        ");
    }

    [Test]
    public void Dynamic_RepeatedGcCycles_NoCorruption()
    {
        // Arrange
        for (var i = 0; i < 5; i++)
        {
            var obj = new EventLifecycleTarget();
            Lua["obj"] = obj;

            // Act
            Lua.Execute(@"
                obj.TestEvent:add(function() end)
                obj = nil
                collectgarbage('collect')
                collectgarbage('collect')
            ");

            // Assert
            Assert.That(obj.HasSubscribers, Is.False);
        }
    }

    [Test]
    public void Dynamic_RapidSubscribeUnsubscribe_NoLeak()
    {
        // Arrange
        var obj = new EventLifecycleTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            for i = 1, 100 do
                local h = function() end
                obj.TestEvent:add(h)
                obj.TestEvent:remove(h)
            end
        ");

        // Assert
        Assert.That(obj.HasSubscribers, Is.False);
    }

    [Test]
    public void Dynamic_RefStructEventArgs_EventNotVisible()
    {
        // Arrange
        var obj = new RefStructEventTarget();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute(@"
            local acc = obj.RefStructEvent
            assert(acc == nil)
        ");
    }

    [Test]
    public void Dynamic_RefStructEventArgs_WithMissingKeyIndexer_PropagatesException()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<RefStructEventIndexerTarget>(opts =>
        {
            opts.Features = LuaTypeFeatures.All;
        });

        // Act
        var obj = new RefStructEventIndexerTarget();
        Lua["obj"] = obj;

        var ex = Assert.Throws<LuaException>(() =>
        {
            Lua.Execute(@"
                local acc = obj.RefStructEvent
            ");
        });

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("An exception occurred while executing managed code."));
        Assert.That(ex.InnerException, Is.TypeOf<KeyNotFoundException>());
    }

    [Test]
    public void Dynamic_ThrowingIndexerGetter_PropagatesOriginalException()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ThrowingStringIndexerTarget>(opts =>
        {
            opts.Features = LuaTypeFeatures.All;
        });

        // Act
        var obj = new ThrowingStringIndexerTarget();
        Lua["obj"] = obj;

        var ex = Assert.Throws<LuaException>(() =>
        {
            Lua.Execute(@"
                return obj.Missing
            ");
        });

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("An exception occurred while executing managed code."));
        Assert.That(ex.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(ex.InnerException!.Message, Is.EqualTo("Getter failure."));
    }

    [Test]
    public void Dynamic_ThrowingIndexerSetter_PropagatesOriginalException()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ThrowingStringIndexerTarget>(opts =>
        {
            opts.Features = LuaTypeFeatures.All;
        });

        // Act
        var obj = new ThrowingStringIndexerTarget();
        Lua["obj"] = obj;

        var ex = Assert.Throws<LuaException>(() =>
        {
            Lua.Execute(@"
                obj.Missing = 42
            ");
        });

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("An exception occurred while executing managed code."));
        Assert.That(ex.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(ex.InnerException!.Message, Is.EqualTo("Setter failure."));
    }

    [Test]
    public void Dynamic_RefStructEventArgs_NormalEventStillWorks()
    {
        // Arrange
        var obj = new RefStructEventTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            count = 0
            obj.NormalEvent:add(function() count = count + 1 end)
        ");

        obj.FireNormal();

        // Assert
        var count = Lua.Evaluate<int>("return count");
        Assert.That(count, Is.EqualTo(1));
    }
}
