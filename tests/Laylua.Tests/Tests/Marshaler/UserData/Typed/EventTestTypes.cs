using Laylua.Marshaling;

namespace Laylua.Tests;

[LuaType]
public partial class TestEventSimple
{
    public event Action? SimpleEvent;

    public event EventHandler? TypedEvent;

    public event EventHandler<string>? GenericEvent;

    public event Action<int, string>? MultiArgEvent;

    public bool HasSimpleSubscribers => SimpleEvent != null;

    public bool HasTypedSubscribers => TypedEvent != null;

    public void FireSimple()
    {
        SimpleEvent?.Invoke();
    }

    public void FireTyped()
    {
        TypedEvent?.Invoke(this, EventArgs.Empty);
    }

    public void FireGeneric(string arg)
    {
        GenericEvent?.Invoke(this, arg);
    }

    public void FireMultiArg(int i, string s)
    {
        MultiArgEvent?.Invoke(i, s);
    }
}

[LuaType]
public partial class TestEventIgnored
{
    public event Action? VisibleEvent;

    [LuaIgnore]
    public event Action? IgnoredEvent;

    public void FireVisible()
    {
        VisibleEvent?.Invoke();
    }

    public void FireIgnored()
    {
        IgnoredEvent?.Invoke();
    }
}

[LuaType]
public partial class TestEventRenamed
{
    [LuaName("renamed")]
    public event Action? OriginalEvent;

    public void FireOriginal()
    {
        OriginalEvent?.Invoke();
    }
}

[LuaType]
public partial class TestEventInclude
{
    [LuaInclude]
    internal event Action? InternalEvent;

    public void FireInternal()
    {
        InternalEvent?.Invoke();
    }
}

[LuaType]
public partial class TestEventStatic
{
    public static event Action? StaticEvent;

    public static bool HasStaticSubscribers => StaticEvent != null;

    public static int StaticFireCount { get; private set; }

    public static void FireStatic()
    {
        StaticFireCount++;
        StaticEvent?.Invoke();
    }

    public static void ResetStatic()
    {
        StaticEvent = null;
        StaticFireCount = 0;
    }
}

[LuaType(Members = LuaTypeMembers.All & ~LuaTypeMembers.Events)]
public partial class TestEventNoEventsFeature
{
    public event Action? SimpleEvent;

    public int Value { get; set; }

    public void FireSimple()
    {
        SimpleEvent?.Invoke();
    }
}

[LuaType(IndexMembers = LuaIndexMembers.All & ~LuaIndexMembers.Events)]
public partial class TestIndexMembersNoEvents
{
    public event Action? SimpleEvent;

    public int Value { get; set; } = 42;

    public int GetValue()
    {
        return Value;
    }

    public void FireSimple()
    {
        SimpleEvent?.Invoke();
    }
}

[LuaType(IndexMembers = LuaIndexMembers.Events)]
public partial class TestIndexMembersEventsOnly
{
    public event Action? SimpleEvent;

    public int Value { get; set; } = 42;

    public int GetValue()
    {
        return Value;
    }

    public void FireSimple()
    {
        SimpleEvent?.Invoke();
    }
}

[LuaType]
public partial class TestEventDisposable : IDisposable
{
    public event Action? TestEvent;

    public static event Action? StaticEvent;

    public bool IsDisposed { get; private set; }

    public bool HasSubscribers => TestEvent != null;

    public static bool HasStaticSubscribers => StaticEvent != null;

    public static int StaticValue { get; set; }

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
        StaticValue = 0;
    }

    public void Dispose()
    {
        IsDisposed = true;
    }
}

[LuaType]
public partial class TestEventCleanupFailure : IDisposable
{
    private Action? _testEvent;

    public event Action? TestEvent
    {
        add => _testEvent += value;
        remove => throw new InvalidOperationException("Event cleanup failure.");
    }

    public bool WasDisposed { get; private set; }

    public void Dispose()
    {
        WasDisposed = true;
    }
}

[LuaType]
public partial class TestEventRemoveFailure
{
    private Action? _testEvent;
    private bool _failNextRemove = true;

    public event Action? TestEvent
    {
        add => _testEvent += value;
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

[LuaType]
public partial class TestEventWithStaticProperty
{
    public static int Counter { get; set; }

    public event Action? TestEvent;

    public bool HasSubscribers => TestEvent != null;

    public void Fire()
    {
        TestEvent?.Invoke();
    }
}

[LuaType]
public partial class TestEventWithRefStructHandler
{
#pragma warning disable LAYLUA102
    public event RefStructEventHandler? UnsupportedEvent;
#pragma warning restore LAYLUA102
    public event Action? NormalEvent;

    public bool HasNormalSubscribers => NormalEvent != null;

    public void FireNormal()
    {
        NormalEvent?.Invoke();
    }
}

[LuaType]
public partial class TestEventWithRefStructHandlerIndexer
{
#pragma warning disable LAYLUA102
    public event RefStructEventHandler? UnsupportedEvent;
#pragma warning restore LAYLUA102

    public object? this[string key] => throw new KeyNotFoundException();
}

[LuaType]
public partial class TestThrowingTypedStringIndexer
{
    public object? this[string key]
    {
        get => throw new InvalidOperationException("Getter failure.");
        set => throw new InvalidOperationException("Setter failure.");
    }
}

[LuaType]
public partial class TestEventWithRefParameter
{
#pragma warning disable LAYLUA102
    public event RefIntEventHandler? UnsupportedEvent;
#pragma warning restore LAYLUA102
}

[LuaType]
public partial class TestEventWithUnsupportedReturn
{
#pragma warning disable LAYLUA102
    public event SpanReturnEventHandler? UnsupportedEvent;
#pragma warning restore LAYLUA102
}
