namespace Laylua.Tests;

public unsafe class TypedEventEdgeCaseTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
    }

    [Test]
    public void TypedDisposable_DefinitionGc_DoesNotThrow()
    {
        // Arrange
        TestEventDisposable.ResetStatic();

        TestEventDisposable.LuaUserDataHandler.PushDefinition(L);
        lua_setglobal(L, "TestDef");

        // Act
        TestDelegate action = () =>
        {
            Lua.Execute(@"
                TestDef = nil
                collectgarbage('collect')
                collectgarbage('collect')
            ");
        };

        // Assert
        Assert.DoesNotThrow(action);

        TestEventDisposable.ResetStatic();
    }

    [Test]
    public void TypedDisposable_DefinitionGc_CleansUpStaticEvents()
    {
        // Arrange
        TestEventDisposable.ResetStatic();

        // Act
        TestEventDisposable.LuaUserDataHandler.PushDefinition(L);
        lua_setglobal(L, "TestDef");

        Lua.Execute(@"
            TestDef.StaticEvent:add(function() end)
        ");

        Assert.That(TestEventDisposable.HasStaticSubscribers, Is.True);

        Lua.Execute(@"
            TestDef = nil
            collectgarbage('collect')
            collectgarbage('collect')
        ");

        // Assert
        Assert.That(TestEventDisposable.HasStaticSubscribers, Is.False);
        TestEventDisposable.ResetStatic();
    }

    [Test]
    public void TypedNonDisposable_DefinitionGc_Works()
    {
        // Arrange
        TestEventWithStaticProperty.Counter = 0;

        // Act
        TestEventWithStaticProperty.LuaUserDataHandler.PushDefinition(L);
        lua_setglobal(L, "TestDef");

        Lua.Execute(@"
            TestDef.Counter = 42
        ");

        // Assert
        Assert.That(TestEventWithStaticProperty.Counter, Is.EqualTo(42));

        Lua.Execute(@"
            TestDef = nil
            collectgarbage('collect')
            collectgarbage('collect')
        ");

        TestEventWithStaticProperty.Counter = 0;
    }

    [Test]
    public void TypedDefinition_NumericKey_ReturnsNil()
    {
        // Arrange
        TestEventStatic.ResetStatic();

        // Act
        TestEventStatic.LuaUserDataHandler.PushDefinition(L);
        lua_setglobal(L, "TestDef");

        // Assert
        Lua.Execute(@"
            local val = TestDef[42]
            assert(val == nil)
        ");

        TestEventStatic.ResetStatic();
    }

    [Test]
    public void TypedDefinition_BooleanKey_ReturnsNil()
    {
        // Arrange
        TestEventStatic.ResetStatic();

        // Act
        TestEventStatic.LuaUserDataHandler.PushDefinition(L);
        lua_setglobal(L, "TestDef");

        // Assert
        Lua.Execute(@"
            local val = TestDef[true]
            assert(val == nil)
        ");

        TestEventStatic.ResetStatic();
    }

    [Test]
    public void TypedDefinition_TableKey_ReturnsNil()
    {
        // Arrange
        TestEventStatic.ResetStatic();

        // Act
        TestEventStatic.LuaUserDataHandler.PushDefinition(L);
        lua_setglobal(L, "TestDef");

        // Assert
        Lua.Execute(@"
            local val = TestDef[{}]
            assert(val == nil)
        ");

        TestEventStatic.ResetStatic();
    }

    [Test]
    public void TypedDefinition_NumericNewIndex_NoError()
    {
        // Arrange
        TestEventDisposable.ResetStatic();

        TestEventDisposable.LuaUserDataHandler.PushDefinition(L);
        lua_setglobal(L, "TestDef");

        // Act
        TestDelegate action = () =>
        {
            Lua.Execute(@"
                TestDef[42] = 'test'
            ");
        };

        // Assert
        Assert.DoesNotThrow(action);

        TestEventDisposable.ResetStatic();
    }

    [Test]
    public void TypedDisposable_Close_CleansUpEvents()
    {
        // Arrange
        var obj = new TestEventDisposable();
        TestEventDisposable.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

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
        Assert.That(obj.IsDisposed, Is.True);
    }

    [Test]
    public void TypedDisposable_Close_EventCleanupFailure_StillDisposesAndSurfacesError()
    {
        // Arrange
        var obj = new TestEventCleanupFailure();
        TestEventCleanupFailure.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

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
    public void TypedEvent_StaleAccessor_DoesNotLeakSubscriptionAfterAccessorCollected()
    {
        // Arrange
        var obj = new TestEventSimple();
        TestEventSimple.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

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
    public void TypedEvent_RemoveFailure_KeepsSubscriptionUsableAndRetriable()
    {
        // Arrange
        var obj = new TestEventRemoveFailure();
        TestEventRemoveFailure.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

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
    public void TypedEvent_RapidSubscribeUnsubscribe_NoLeak()
    {
        // Arrange
        var obj = new TestEventSimple();
        TestEventSimple.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act
        Lua.Execute(@"
            for i = 1, 100 do
                local h = function() end
                obj.SimpleEvent:add(h)
                obj.SimpleEvent:remove(h)
            end
        ");

        // Assert
        Assert.That(obj.HasSimpleSubscribers, Is.False);
    }

    [Test]
    public void TypedEvent_UnsubscribeAll_ThenResubscribe()
    {
        // Arrange
        var obj = new TestEventSimple();
        TestEventSimple.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act
        Lua.Execute(@"
            count = 0
            local function h() count = count + 1 end
            obj.SimpleEvent:add(h)
            obj.SimpleEvent:remove(h)
        ");

        Assert.That(obj.HasSimpleSubscribers, Is.False);

        Lua.Execute(@"
            local function h2() count = count + 10 end
            obj.SimpleEvent:add(h2)
        ");

        obj.FireSimple();

        // Assert
        var count = Lua.Evaluate<int>("return count");
        Assert.That(count, Is.EqualTo(10));
    }

    [Test]
    public void TypedEvent_RepeatedGcCycles_NoCorruption()
    {
        // Arrange
        for (var i = 0; i < 5; i++)
        {
            var obj = new TestEventSimple();
            TestEventSimple.LuaUserDataHandler.Push(L, obj);
            lua_setglobal(L, "obj");

            // Act
            Lua.Execute(@"
                obj.SimpleEvent:add(function() end)
                obj = nil
                collectgarbage('collect')
                collectgarbage('collect')
            ");

            // Assert
            Assert.That(obj.HasSimpleSubscribers, Is.False);
        }
    }

    [Test]
    public void TypedStatic_MultipleDefinitionPushes_EventsWork()
    {
        // Arrange
        TestEventStatic.ResetStatic();

        // Act
        TestEventStatic.LuaUserDataHandler.PushDefinition(L);
        lua_setglobal(L, "TestDef");

        Lua.Execute(@"
            fired = false
            TestDef.StaticEvent:add(function() fired = true end)
        ");

        TestEventStatic.FireStatic();

        // Assert
        Lua.Execute("assert(fired == true)");

        TestEventStatic.ResetStatic();
    }

    [Test]
    public void TypedEvent_SameTargetDifferentWrappers_GcOfOneWrapper_KeepsOtherSubscriptions()
    {
        // Arrange
        var obj = new TestEventSimple();
        TestEventSimple.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "first");

        TestEventSimple.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "second");

        // Act
        Lua.Execute(@"
            firstCount = 0
            secondCount = 0

            local firstHandler = function() firstCount = firstCount + 1 end
            local secondHandler = function() secondCount = secondCount + 1 end

            first.SimpleEvent:add(firstHandler)
            second.SimpleEvent:add(secondHandler)

            first = nil
            collectgarbage('collect')
            collectgarbage('collect')
        ");

        obj.FireSimple();

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
        Assert.That(obj.HasSimpleSubscribers, Is.False);
    }

    [Test]
    public void TypedDisposable_InstanceEvent_FiresBeforeDispose()
    {
        // Arrange
        var obj = new TestEventDisposable();
        TestEventDisposable.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act
        Lua.Execute(@"
            count = 0
            obj.TestEvent:add(function() count = count + 1 end)
        ");

        obj.Fire();

        // Assert
        var count = Lua.Evaluate<int>("return count");
        Assert.That(count, Is.EqualTo(1));
        Assert.That(obj.IsDisposed, Is.False);
    }

    [Test]
    public void TypedEvent_RefStructHandler_EventNotVisible()
    {
        // Arrange
        var obj = new TestEventWithRefStructHandler();
        TestEventWithRefStructHandler.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act & Assert
        Lua.Execute(@"
            local acc = obj.UnsupportedEvent
            assert(acc == nil)
        ");
    }

    [Test]
    public void TypedEvent_RefStructHandler_WithMissingKeyIndexer_PropagatesException()
    {
        // Arrange
        var obj = new TestEventWithRefStructHandlerIndexer();
        TestEventWithRefStructHandlerIndexer.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act
        var ex = Assert.Throws<LuaException>(() =>
        {
            Lua.Execute(@"
                local acc = obj.UnsupportedEvent
            ");
        });

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("An exception occurred while executing managed code."));
        Assert.That(ex.InnerException, Is.TypeOf<KeyNotFoundException>());
    }

    [Test]
    public void TypedEvent_ThrowingIndexerGetter_PropagatesOriginalException()
    {
        // Arrange
        var obj = new TestThrowingTypedStringIndexer();
        TestThrowingTypedStringIndexer.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act
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
    public void TypedEvent_ThrowingIndexerSetter_PropagatesOriginalException()
    {
        // Arrange
        var obj = new TestThrowingTypedStringIndexer();
        TestThrowingTypedStringIndexer.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act
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
    public void TypedEvent_RefStructHandler_NormalEventStillWorks()
    {
        // Arrange
        var obj = new TestEventWithRefStructHandler();
        TestEventWithRefStructHandler.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

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

    [Test]
    public void TypedEvent_ByRefParameter_NotVisible()
    {
        // Arrange
        var obj = new TestEventWithRefParameter();
        TestEventWithRefParameter.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act & Assert
        Lua.Execute("assert(obj.UnsupportedEvent == nil)");
    }

    [Test]
    public void TypedEvent_UnsupportedReturn_NotVisible()
    {
        // Arrange
        var obj = new TestEventWithUnsupportedReturn();
        TestEventWithUnsupportedReturn.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act & Assert
        Lua.Execute("assert(obj.UnsupportedEvent == nil)");
    }
}
