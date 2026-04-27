namespace Laylua.Tests;

public unsafe class TypedUserDataEventTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
    }

    [Test]
    public void TypedEvent_AccessorReturned_FromIndex()
    {
        // Arrange
        var obj = new TestEventSimple();
        TestEventSimple.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act
        Lua.Execute("local acc = obj.SimpleEvent; assert(acc ~= nil)");
    }

    [Test]
    public void TypedEvent_AccessorHasAddAndRemove()
    {
        // Arrange
        var obj = new TestEventSimple();
        TestEventSimple.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act & Assert
        Lua.Execute(@"
            local acc = obj.SimpleEvent
            assert(type(acc.add) == 'function')
            assert(type(acc.remove) == 'function')
        ");
    }

    [Test]
    public void TypedEvent_Subscribe_SimpleAction()
    {
        // Arrange
        var obj = new TestEventSimple();
        TestEventSimple.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

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
    public void TypedEvent_Subscribe_MultipleHandlers()
    {
        // Arrange
        var obj = new TestEventSimple();
        TestEventSimple.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

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
    public void TypedEvent_Unsubscribe_ByIdentity()
    {
        // Arrange
        var obj = new TestEventSimple();
        TestEventSimple.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

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
    public void TypedEvent_MultiArgEvent_ReceivesArgs()
    {
        // Arrange
        var obj = new TestEventSimple();
        TestEventSimple.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

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
    public void TypedEvent_GenericEventHandler_ReceivesSenderAndArg()
    {
        // Arrange
        var obj = new TestEventSimple();
        TestEventSimple.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

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
    public void TypedEvent_LuaIgnore_NotAccessible()
    {
        // Arrange
        var obj = new TestEventIgnored();
        TestEventIgnored.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act & Assert
        Lua.Execute("assert(obj.IgnoredEvent == nil)");
    }

    [Test]
    public void TypedEvent_LuaName_AccessibleByCustomName()
    {
        // Arrange
        var obj = new TestEventRenamed();
        TestEventRenamed.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

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
    public void TypedEvent_LuaInclude_NonPublicAccessible()
    {
        // Arrange
        var obj = new TestEventInclude();
        TestEventInclude.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act
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
    public void TypedEvent_FeaturesDisabled_NotAccessible()
    {
        // Arrange
        var obj = new TestEventNoEventsFeature
        {
            Value = 42
        };

        TestEventNoEventsFeature.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act
        Lua.Execute("assert(obj.SimpleEvent == nil)");
        var value = Lua.Evaluate<int>("return obj.Value");

        // Assert
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void TypedEvent_GcCleansUpSubscriptions()
    {
        // Arrange
        var obj = new TestEventSimple();
        TestEventSimple.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

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
    public void TypedEvent_StaticEvent_Subscribe()
    {
        // Arrange
        TestEventStatic.ResetStatic();

        // Act
        TestEventStatic.LuaUserDataHandler.PushDefinition(L);
        lua_setglobal(L, "TestEventStatic");

        Lua.Execute(@"
            static_fired = false
            TestEventStatic.StaticEvent:add(function() static_fired = true end)
        ");

        TestEventStatic.FireStatic();

        // Assert
        Lua.Execute("assert(static_fired == true)");

        TestEventStatic.ResetStatic();
    }

    [Test]
    public void TypedEvent_StaticEvent_RemoveAcrossDefinitionPushes_Works()
    {
        // Arrange
        TestEventStatic.ResetStatic();
        Lua["first"] = typeof(TestEventStatic);
        Lua["second"] = typeof(TestEventStatic);

        // Act
        Lua.Execute(@"
            count = 0
            local function handler() count = count + 1 end
            first.StaticEvent:add(handler)
            second.StaticEvent:remove(handler)
        ");

        TestEventStatic.FireStatic();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(Lua.Evaluate<int>("return count"), Is.EqualTo(0));
            Assert.That(TestEventStatic.HasStaticSubscribers, Is.False);
        });
    }

    [Test]
    public void TypedEvent_Subscribe_UnsubscribeOne_OtherStillFires()
    {
        // Arrange
        var obj = new TestEventSimple();
        TestEventSimple.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

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

    [Test]
    public void TypedEvent_AddReturnsNil()
    {
        // Arrange
        var obj = new TestEventSimple();
        TestEventSimple.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act & Assert
        Lua.Execute(@"
            local result = obj.SimpleEvent:add(function() end)
            assert(result == nil)
        ");
    }
}
