using Laylua.Marshaling;

namespace Laylua.Tests;

public class DynamicEventTarget
{
    public event Action? SimpleEvent;

    public event EventHandler? TypedEvent;

    public event EventHandler<string>? GenericEvent;

    public event Action<int, string>? MultiArgEvent;

    [LuaIgnore]
    public event Action? IgnoredEvent;

    [LuaName("renamed")]
    public event Action? OriginalEvent;

    public int SimpleFireCount { get; private set; }

    public string? LastGenericArg { get; private set; }

    public int LastMultiArgInt { get; private set; }

    public string? LastMultiArgString { get; private set; }

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
        LastGenericArg = arg;
    }

    public void FireMultiArg(int i, string s)
    {
        MultiArgEvent?.Invoke(i, s);
        LastMultiArgInt = i;
        LastMultiArgString = s;
    }

    public void FireIgnored()
    {
        IgnoredEvent?.Invoke();
    }

    public void FireOriginal()
    {
        OriginalEvent?.Invoke();
    }

    public bool HasSimpleSubscribers => SimpleEvent != null;

    public bool HasTypedSubscribers => TypedEvent != null;
}

public class DynamicStaticEventTarget
{
    public static event Action? StaticEvent;

    public static int StaticFireCount { get; private set; }

    public static void FireStatic()
    {
        StaticEvent?.Invoke();
    }

    public static void ResetStatic()
    {
        StaticEvent = null;
        StaticFireCount = 0;
    }

    public static bool HasStaticSubscribers => StaticEvent != null;
}

public class DynamicEventNoEventsTarget
{
    public event Action? SimpleEvent;

    public void FireSimple()
    {
        SimpleEvent?.Invoke();
    }
}

public class DynamicIncludeEventTarget
{
    [LuaInclude]
    internal event Action? InternalEvent;

    public bool InternalFired { get; private set; }

    public void FireInternal()
    {
        InternalEvent?.Invoke();
    }
}

public class DynamicFilteredIncludeEventTarget
{
    [LuaInclude]
    internal event Action? IgnoredByOptionsEvent;

    [LuaInclude]
    [LuaIgnore]
    internal event Action? IgnoredByAttributeEvent;

    [LuaInclude]
    [Obsolete("Hidden from Lua for testing.")]
    internal event Action? ObsoleteEvent;
}

public class DynamicFilteredIncludeStaticEventTarget
{
    [LuaInclude]
    internal static event Action? IgnoredByOptionsEvent;

    [LuaInclude]
    [LuaIgnore]
    internal static event Action? IgnoredByAttributeEvent;

    [LuaInclude]
    [Obsolete("Hidden from Lua for testing.")]
    internal static event Action? ObsoleteEvent;
}

public class DynamicEventTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
        Marshaler.AllowsDynamicMarshaling = true;
        LuaTypeRegistry.Unregister<DynamicIncludeEventTarget>();
        LuaTypeRegistry.Unregister<DynamicFilteredIncludeEventTarget>();
        LuaTypeRegistry.Unregister<DynamicFilteredIncludeStaticEventTarget>();
    }

    public override void TearDown()
    {
        Marshaler.AllowsDynamicMarshaling = false;
        base.TearDown();
    }

    [Test]
    public void DynamicEvent_AccessorReturned_FromIndex()
    {
        // Arrange
        var obj = new DynamicEventTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute("local acc = obj.SimpleEvent; assert(acc ~= nil)");
    }

    [Test]
    public void DynamicEvent_AccessorHasAddAndRemove()
    {
        // Arrange
        var obj = new DynamicEventTarget();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute(@"
            local acc = obj.SimpleEvent
            assert(type(acc.add) == 'function')
            assert(type(acc.remove) == 'function')
        ");
    }

    [Test]
    public void DynamicEvent_Subscribe_SimpleAction()
    {
        // Arrange
        var obj = new DynamicEventTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            fired = false
            obj.SimpleEvent:add(function() fired = true end)
        ");

        obj.FireSimple();

        // Assert
        Lua.Execute("assert(fired == true)");
    }

    [Test]
    public void DynamicEvent_Subscribe_MultipleHandlers()
    {
        // Arrange
        var obj = new DynamicEventTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            count = 0
            obj.SimpleEvent:add(function() count = count + 1 end)
            obj.SimpleEvent:add(function() count = count + 10 end)
        ");

        obj.FireSimple();

        // Assert
        var count = Lua.Evaluate<int>("return count");
        Assert.That(count, Is.EqualTo(11));
    }

    [Test]
    public void DynamicEvent_Unsubscribe_ByIdentity()
    {
        // Arrange
        var obj = new DynamicEventTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            count = 0
            local function handler() count = count + 1 end
            obj.SimpleEvent:add(handler)
            obj.SimpleEvent:remove(handler)
        ");

        obj.FireSimple();

        // Assert
        var count = Lua.Evaluate<int>("return count");
        Assert.That(count, Is.EqualTo(0));
        Assert.That(obj.HasSimpleSubscribers, Is.False);
    }

    [Test]
    public void DynamicEvent_Unsubscribe_NonExistent_NoError()
    {
        // Arrange
        var obj = new DynamicEventTarget();
        Lua["obj"] = obj;

        // Act
        TestDelegate action = () =>
        {
            Lua.Execute(@"
                obj.SimpleEvent:remove(function() end)
            ");
        };

        // Assert
        Assert.DoesNotThrow(action);
    }

    [Test]
    public void DynamicEvent_EventHandler_ReceivesArgs()
    {
        // Arrange
        var obj = new DynamicEventTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            received_i = nil
            received_s = nil
            obj.MultiArgEvent:add(function(i, s)
                received_i = i
                received_s = s
            end)
        ");

        obj.FireMultiArg(42, "hello");

        // Assert
        var i = Lua.Evaluate<int>("return received_i");
        var s = Lua.Evaluate<string>("return received_s");
        Assert.That(i, Is.EqualTo(42));
        Assert.That(s, Is.EqualTo("hello"));
    }

    [Test]
    public void DynamicEvent_EventHandlerGeneric_ReceivesSenderAndArg()
    {
        // Arrange
        var obj = new DynamicEventTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            received_arg = nil
            obj.GenericEvent:add(function(sender, arg)
                received_arg = arg
            end)
        ");

        obj.FireGeneric("world");

        // Assert
        var arg = Lua.Evaluate<string>("return received_arg");
        Assert.That(arg, Is.EqualTo("world"));
    }

    [Test]
    public void DynamicEvent_LuaIgnore_NotAccessible()
    {
        // Arrange
        var obj = new DynamicEventTarget();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj.IgnoredEvent == nil)");
    }

    [Test]
    public void DynamicEvent_LuaName_AccessibleByCustomName()
    {
        // Arrange
        var obj = new DynamicEventTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            fired = false
            obj.renamed:add(function() fired = true end)
        ");

        obj.FireOriginal();

        // Assert
        Lua.Execute("assert(fired == true)");
    }

    [Test]
    public void DynamicEvent_StaticEvent_RemoveAcrossDefinitionPushes_Works()
    {
        // Arrange
        DynamicStaticEventTarget.ResetStatic();
        LuaTypeRegistry.ConfigureDynamic<DynamicStaticEventTarget>(static opts =>
        {
            opts.Features = LuaTypeFeatures.All;
        });

        Lua["first"] = typeof(DynamicStaticEventTarget);
        Lua["second"] = typeof(DynamicStaticEventTarget);

        // Act
        Lua.Execute(@"
            count = 0
            local function handler() count = count + 1 end
            first.StaticEvent:add(handler)
            second.StaticEvent:remove(handler)
        ");

        DynamicStaticEventTarget.FireStatic();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(Lua.Evaluate<int>("return count"), Is.EqualTo(0));
            Assert.That(DynamicStaticEventTarget.HasStaticSubscribers, Is.False);
        });
    }

    [Test]
    public void DynamicEvent_LuaInclude_NonPublicAccessible()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicIncludeEventTarget>(opts =>
        {
            opts.Features = LuaTypeFeatures.All;
        });

        // Act
        var obj = new DynamicIncludeEventTarget();
        Lua["obj"] = obj;

        TestDelegate action = () =>
        {
            Lua.Execute(@"
                obj.InternalEvent:add(function() end)
            ");
        };

        // Assert
        Assert.DoesNotThrow(action);
    }

    [Test]
    public void DynamicEvent_NonPublicLuaInclude_RespectsIgnoreFilters()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicFilteredIncludeEventTarget>(opts =>
        {
            opts.Features = LuaTypeFeatures.All;
            opts.Ignore("IgnoredByOptionsEvent");
            opts.ObsoleteMembers = LuaObsoleteMemberHandling.Ignore;
        });

        Lua["obj"] = new DynamicFilteredIncludeEventTarget();

        // Act
        var ignoredByOptionsIsNil = Lua.Evaluate<bool>("return obj.IgnoredByOptionsEvent == nil");
        var ignoredByAttributeIsNil = Lua.Evaluate<bool>("return obj.IgnoredByAttributeEvent == nil");
        var obsoleteIsNil = Lua.Evaluate<bool>("return obj.ObsoleteEvent == nil");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ignoredByOptionsIsNil, Is.True);
            Assert.That(ignoredByAttributeIsNil, Is.True);
            Assert.That(obsoleteIsNil, Is.True);
        });
    }

    [Test]
    public void DynamicEvent_StaticNonPublicLuaInclude_RespectsIgnoreFilters()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicFilteredIncludeStaticEventTarget>(opts =>
        {
            opts.Features = LuaTypeFeatures.All;
            opts.Ignore("IgnoredByOptionsEvent");
            opts.ObsoleteMembers = LuaObsoleteMemberHandling.Ignore;
        });

        Lua["DynamicFilteredIncludeStaticEventTarget"] = typeof(DynamicFilteredIncludeStaticEventTarget);

        // Act
        var ignoredByOptionsIsNil = Lua.Evaluate<bool>("return DynamicFilteredIncludeStaticEventTarget.IgnoredByOptionsEvent == nil");
        var ignoredByAttributeIsNil = Lua.Evaluate<bool>("return DynamicFilteredIncludeStaticEventTarget.IgnoredByAttributeEvent == nil");
        var obsoleteIsNil = Lua.Evaluate<bool>("return DynamicFilteredIncludeStaticEventTarget.ObsoleteEvent == nil");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(ignoredByOptionsIsNil, Is.True);
            Assert.That(ignoredByAttributeIsNil, Is.True);
            Assert.That(obsoleteIsNil, Is.True);
        });
    }

    [Test]
    public void DynamicEvent_FeaturesDisabled_NotAccessible()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicEventNoEventsTarget>(opts =>
        {
            opts.Members = LuaTypeMembers.All & ~LuaTypeMembers.Events;
        });

        // Act
        var obj = new DynamicEventNoEventsTarget();
        Lua["obj"] = obj;

        // Assert
        Lua.Execute("assert(obj.SimpleEvent == nil)");
    }

    [Test]
    public void DynamicEvent_GcCleansUpSubscriptions()
    {
        // Arrange
        var obj = new DynamicEventTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            obj.SimpleEvent:add(function() end)
        ");

        Assert.That(obj.HasSimpleSubscribers, Is.True);

        Lua.Execute("obj = nil");
        Lua.Execute("collectgarbage('collect')");
        Lua.Execute("collectgarbage('collect')");

        // Assert
        Assert.That(obj.HasSimpleSubscribers, Is.False);
    }

    [Test]
    public void DynamicEvent_StaticEvent_Subscribe()
    {
        // Arrange
        DynamicStaticEventTarget.ResetStatic();

        // Act
        LuaTypeRegistry.ConfigureDynamic<DynamicStaticEventTarget>(opts =>
        {
            opts.Features = LuaTypeFeatures.All;
        });

        Lua["DynamicStaticEventTarget"] = typeof(DynamicStaticEventTarget);

        Lua.Execute(@"
            static_fired = false
            DynamicStaticEventTarget.StaticEvent:add(function() static_fired = true end)
        ");

        DynamicStaticEventTarget.FireStatic();

        // Assert
        Lua.Execute("assert(static_fired == true)");

        DynamicStaticEventTarget.ResetStatic();
    }

    [Test]
    public void DynamicEvent_AddReturnsNil()
    {
        // Arrange
        var obj = new DynamicEventTarget();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute(@"
            local result = obj.SimpleEvent:add(function() end)
            assert(result == nil)
        ");
    }

    [Test]
    public void DynamicEvent_Subscribe_UnsubscribeOne_OtherStillFires()
    {
        // Arrange
        var obj = new DynamicEventTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            count = 0
            local function h1() count = count + 1 end
            local function h2() count = count + 10 end
            obj.SimpleEvent:add(h1)
            obj.SimpleEvent:add(h2)
            obj.SimpleEvent:remove(h1)
        ");

        obj.FireSimple();

        // Assert
        var count = Lua.Evaluate<int>("return count");
        Assert.That(count, Is.EqualTo(10));
    }
}
