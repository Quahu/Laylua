using Laylua.Marshaling;

namespace Laylua.Tests;

public class EventLifecycleTarget
{
    public event Action? TestEvent;

    public int HandlerCallCount { get; private set; }

    public void Fire()
    {
        TestEvent?.Invoke();
    }

    public bool HasSubscribers => TestEvent != null;

    public void ResetCallCount()
    {
        HandlerCallCount = 0;
    }
}

public unsafe class EventLifecycleTests : LuaTestBase
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
    public void Subscribe_SameHandlerTwice_CalledTwice()
    {
        // Arrange
        var obj = new EventLifecycleTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            count = 0
            local function handler() count = count + 1 end
            obj.TestEvent:add(handler)
            obj.TestEvent:add(handler)
        ");

        obj.Fire();

        // Assert
        var count = Lua.Evaluate<int>("return count");
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public void Unsubscribe_OneOfDuplicate_CalledOnce()
    {
        // Arrange
        var obj = new EventLifecycleTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            count = 0
            local function handler() count = count + 1 end
            obj.TestEvent:add(handler)
            obj.TestEvent:add(handler)
            obj.TestEvent:remove(handler)
        ");

        obj.Fire();

        // Assert
        var count = Lua.Evaluate<int>("return count");
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void ManyHandlers_AllFire()
    {
        // Arrange
        var obj = new EventLifecycleTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            total = 0
            for i = 1, 50 do
                obj.TestEvent:add(function() total = total + 1 end)
            end
        ");

        obj.Fire();

        // Assert
        var total = Lua.Evaluate<int>("return total");
        Assert.That(total, Is.EqualTo(50));
    }

    [Test]
    public void SubscribeUnsubscribeInterleaved_ConsistentState()
    {
        // Arrange
        var obj = new EventLifecycleTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            count = 0
            local function h1() count = count + 1 end
            local function h2() count = count + 10 end
            local function h3() count = count + 100 end

            obj.TestEvent:add(h1)
            obj.TestEvent:add(h2)
            obj.TestEvent:remove(h1)
            obj.TestEvent:add(h3)
            obj.TestEvent:remove(h2)
        ");

        obj.Fire();

        // Assert
        var count = Lua.Evaluate<int>("return count");
        Assert.That(count, Is.EqualTo(100));
    }

    [Test]
    public void UnsubscribeAll_NoHandlersCalled()
    {
        // Arrange
        var obj = new EventLifecycleTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            count = 0
            local function h1() count = count + 1 end
            local function h2() count = count + 10 end
            obj.TestEvent:add(h1)
            obj.TestEvent:add(h2)
            obj.TestEvent:remove(h1)
            obj.TestEvent:remove(h2)
        ");

        obj.Fire();

        // Assert
        var count = Lua.Evaluate<int>("return count");
        Assert.That(count, Is.EqualTo(0));
        Assert.That(obj.HasSubscribers, Is.False);
    }

    [Test]
    public void RemoveNonExistent_NoError()
    {
        // Arrange
        var obj = new EventLifecycleTarget();
        Lua["obj"] = obj;

        // Act
        TestDelegate action = () =>
        {
            Lua.Execute(@"
                local function handler() end
                obj.TestEvent:remove(handler)
            ");
        };

        // Assert
        Assert.DoesNotThrow(action);
    }

    [Test]
    public void SubscribeFromCoroutine_Works()
    {
        // Arrange
        var obj = new EventLifecycleTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            fired = false
            local co = coroutine.create(function()
                obj.TestEvent:add(function() fired = true end)
            end)
            coroutine.resume(co)
        ");

        obj.Fire();

        // Assert
        Lua.Execute("assert(fired == true)");
    }

    [Test]
    public void Subscribe_SameTargetDifferentWrappers_GcOfOneWrapper_KeepsOtherSubscriptions()
    {
        // Arrange
        var obj = new EventLifecycleTarget();
        var handler = new DynamicUserDataHandler(typeof(EventLifecycleTarget));
        LuaTypeRegistry.Register<EventLifecycleTarget>(handler);

        try
        {
            handler.Push(L, obj);
            lua_setglobal(L, "first");

            handler.Push(L, obj);
            lua_setglobal(L, "second");

            // Act
            Lua.Execute(@"
                firstCount = 0
                secondCount = 0

                local firstHandler = function() firstCount = firstCount + 1 end
                local secondHandler = function() secondCount = secondCount + 1 end

                first.TestEvent:add(firstHandler)
                second.TestEvent:add(secondHandler)

                first = nil
                collectgarbage('collect')
                collectgarbage('collect')
            ");

            obj.Fire();

            Lua.Execute(@"
                second = nil
                collectgarbage('collect')
                collectgarbage('collect')
            ");

            // Assert
            var firstCount = Lua.Evaluate<int>("return firstCount");
            var secondCount = Lua.Evaluate<int>("return secondCount");
            Assert.That(firstCount, Is.EqualTo(0));
            Assert.That(secondCount, Is.EqualTo(1));
            Assert.That(obj.HasSubscribers, Is.False);
        }
        finally
        {
            LuaTypeRegistry.Unregister<EventLifecycleTarget>();
        }
    }
}
