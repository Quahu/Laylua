using System.Reflection;

namespace Laylua.Tests;

public unsafe class TypedUserDataMarshalingTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
    }

    [Test]
    public void Marshaler_PushViaIndexer_WorksAutomatically()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        // Act
        Lua["player"] = player;

        // Assert
        Lua.Execute(@"
            assert(player.Name == 'Steve')
            assert(player.Health == 100)
        ");
    }

    [Test]
    public void Marshaler_PushViaSetGlobal_WorksAutomatically()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        // Act
        Lua.MainThread.SetGlobal("player", player);

        // Assert
        Lua.Execute(@"
            assert(player.Name == 'Steve')
            assert(player.Health == 100)
        ");
    }

    [Test]
    public void Marshaler_GetViaIndexer_ReturnsSameInstance()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        Lua["player"] = player;

        // Act
        var retrieved = Lua["player"];

        // Assert
        Assert.That(retrieved, Is.SameAs(player));
    }

    [Test]
    public void DerivedType_InheritsBaseProperties()
    {
        // Arrange
        var entity = new TestDerivedEntity
        {
            Tag = "enemy",
            Id = 5,
            Level = 3
        };

        Lua["entity"] = entity;

        // Act & Assert
        Lua.Execute(@"
            assert(entity.Tag == 'enemy')
            assert(entity.Id == 5)
            assert(entity.Level == 3)
        ");
    }

    [Test]
    public void DerivedType_InheritsBaseMethods()
    {
        // Arrange
        var entity = new TestDerivedEntity
        {
            Tag = "npc",
            Id = 7
        };

        Lua["entity"] = entity;

        // Act & Assert
        Lua.Execute("assert(entity:Describe() == 'npc#7')");
    }

    [Test]
    public void DerivedType_CanWriteInheritedProperties()
    {
        // Arrange
        var entity = new TestDerivedEntity
        {
            Tag = "old",
            Id = 0
        };

        Lua["entity"] = entity;

        // Act
        Lua.Execute("entity.Tag = 'new'");
        Lua.Execute("entity.Id = 99");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(entity.Tag, Is.EqualTo("new"));
            Assert.That(entity.Id, Is.EqualTo(99));
        });
    }

    [Test]
    public void DerivedType_RetrievedAsBaseType()
    {
        // Arrange
        var derived = new TestDerivedEntity
        {
            Tag = "boss",
            Id = 1,
            Level = 10
        };

        Lua["entity"] = derived;

        // Act
        var asBase = Lua.Evaluate<TestEntity>("return entity");

        // Assert
        Assert.That(asBase, Is.SameAs(derived));
        Assert.That(asBase!.Tag, Is.EqualTo("boss"));
    }

    [Test]
    public void Inheritance_TryGet_BaseTypeMatchesDerived()
    {
        // Arrange
        var derived = new TestDerivedEntity
        {
            Tag = "npc",
            Id = 42,
            Level = 5
        };

        TestDerivedEntity.LuaUserDataHandler.Push(L, derived);

        // Act
        var found = TestEntity.LuaUserDataHandler.TryGet(L, -1, out var entity);
        lua_pop(L);

        // Assert
        Assert.That(found, Is.True);
        Assert.That(entity, Is.Not.Null);
        Assert.That(entity!.Tag, Is.EqualTo("npc"));
        Assert.That(entity.Id, Is.EqualTo(42));
        Assert.That(entity, Is.SameAs(derived));
    }

    [Test]
    public void InterfacePush_ConcreteType_StillAccessible()
    {
        // Arrange
        var dog = new TestDog
        {
            Name = "Barker",
            Age = 5
        };

        Lua.MainThread.SetGlobal<ITestAnimal>("animal", dog);

        // Act & Assert
        Lua.Execute(@"
            assert(animal.Name == 'Barker')
            assert(animal:Speak() == 'Woof!')
        ");
    }

    [Test]
    public void InterfacePush_DifferentImplementations_BothWork()
    {
        // Arrange
        var dog = new TestDog
        {
            Name = "Barker"
        };

        var cat = new TestCat
        {
            Name = "Churu"
        };

        Lua.MainThread.SetGlobal<ITestAnimal>("dog", dog);
        Lua.MainThread.SetGlobal<ITestAnimal>("cat", cat);

        // Act & Assert
        Lua.Execute(@"
            assert(dog:Speak() == 'Woof!')
            assert(cat:Speak() == 'Meow!')
        ");
    }

    [Test]
    public void TestDog_AgeProperty_AccessibleFromLua()
    {
        // Arrange
        var dog = new TestDog
        {
            Name = "Barker",
            Age = 5
        };

        Lua["dog"] = dog;

        // Act
        Lua.Execute("assert(dog.Age == 5)");
        Lua.Execute("dog.Age = 10");

        // Assert
        Assert.That(dog.Age, Is.EqualTo(10));
    }

    [Test]
    public void CrossThread_UserdataAccessible()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        Lua["player"] = player;

        // Act
        Lua.Execute(@"
            co = coroutine.create(function()
                assert(player.Name == 'Steve')
                assert(player.Health == 100)
                player.Health = 50
            end)
            coroutine.resume(co)
        ");

        // Assert
        Assert.That(player.Health, Is.EqualTo(50));
    }

    [Test]
    public void UnregisteredType_ThrowsWhenDynamicDisabled()
    {
        // Arrange
        var target = new NeverRegisteredType();

        // Act & Assert
        Assert.That(() => Lua["obj"] = target,
            Throws.TypeOf<InvalidOperationException>()
                .With.Message.Contains("No Lua type handler is registered"));
    }

    [Test]
    public void FunctionToDelegate_Action()
    {
        // Arrange
        Lua.Execute("function noop() end");

        // Act
        var action = Lua.Evaluate<Action>("return noop");

        // Assert
        Assert.That(action, Is.Not.Null);
        Assert.DoesNotThrow(() => action!());
    }

    [Test]
    public void FunctionToDelegate_Func()
    {
        // Arrange
        Lua.Execute("function add(a, b) return a + b end");

        // Act
        var add = Lua.Evaluate<Func<int, int, int>>("return add");

        // Assert
        Assert.That(add, Is.Not.Null);
        Assert.That(add!(3, 7), Is.EqualTo(10));
    }

    [Test]
    public void FunctionToDelegate_LuaFunction()
    {
        // Arrange
        Lua.Execute("function add(a, b) return a + b end");

        // Act
        var add = Lua.Evaluate<Func<int, int, int>>("return add");

        // Assert
        Assert.That(add, Is.Not.Null);
        Assert.That(add!(3, 7), Is.EqualTo(10));
    }

    [Test]
    public void FunctionToDelegate_RepeatedConversions_ReturnDistinctWorkingDelegates()
    {
        // Arrange
        Lua.Execute("function add(a, b) return a + b end");

        // Act
        var add1 = Lua.Evaluate<Func<int, int, int>>("return add");
        var add2 = Lua.Evaluate<Func<int, int, int>>("return add");

        // Assert
        Assert.That(add1, Is.Not.Null);
        Assert.That(add2, Is.Not.Null);
        Assert.That(add1, Is.Not.SameAs(add2));
        Assert.That(add1!(3, 7), Is.EqualTo(10));
        Assert.That(add2!(5, 9), Is.EqualTo(14));
    }

    [Test]
    public void FunctionToMethodInfo_ReturnsClrMethodInfo()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        Lua.Globals["player"] = player;

        // Act
        var methodInfo = Lua.Evaluate<MethodInfo>("return player.heal");

        Assert.That(methodInfo, Is.Not.Null);
        Assert.That(methodInfo!.Name, Is.EqualTo("RestoreHealth"));
        Assert.That(methodInfo.DeclaringType, Is.EqualTo(typeof(TestPlayer)));

        // Assert
        methodInfo.Invoke(player, [25]);
        Assert.That(player.Health, Is.EqualTo(125));
    }

    [Test]
    public void FunctionToMethodInfo_StaticMethod()
    {
        // Arrange
        TestWithStatics.LuaUserDataHandler.PushDefinition(L);
        lua_setglobal(L, "TestWithStatics");

        // Act
        var methodInfo = Lua.Evaluate<MethodInfo>("return TestWithStatics.Add");

        // Assert
        Assert.That(methodInfo, Is.Not.Null);
        Assert.That(methodInfo!.Name, Is.EqualTo("Add"));
        Assert.That(methodInfo.IsStatic, Is.True);
        Assert.That(methodInfo.Invoke(null, [3, 7]), Is.EqualTo(10));
    }

    [Test]
    public void FunctionToMethodInfo_NoLuaStateDependency()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        Lua.Globals["player"] = player;

        // Act
        var methodInfo = Lua.Evaluate<MethodInfo>("return player.heal");
        Lua.Execute("player = nil; collectgarbage()");

        // Assert
        Assert.That(methodInfo, Is.Not.Null);
        methodInfo!.Invoke(player, [10]);
        Assert.That(player.Health, Is.EqualTo(110));
    }

    [Test]
    public void FunctionToMethodInfo_RepeatedConversions_ReturnWorkingMethodInfos()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        Lua.Globals["player"] = player;

        // Act
        var methodInfo1 = Lua.Evaluate<MethodInfo>("return player.heal");
        var methodInfo2 = Lua.Evaluate<MethodInfo>("return player.heal");

        // Assert
        Assert.That(methodInfo1, Is.Not.Null);
        Assert.That(methodInfo2, Is.Not.Null);
        Assert.That(methodInfo1!.Name, Is.EqualTo("RestoreHealth"));
        Assert.That(methodInfo2!.Name, Is.EqualTo("RestoreHealth"));
        methodInfo1.Invoke(player, [10]);
        methodInfo2.Invoke(player, [15]);
        Assert.That(player.Health, Is.EqualTo(125));
    }

    [Test]
    public void FunctionToOpenDelegate_Action()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        Lua.Globals["player"] = player;

        // Act
        var heal = Lua.Evaluate<Action<TestPlayer, int>>("return player.heal");

        // Assert
        Assert.That(heal, Is.Not.Null);
        heal!(player, 30);
        Assert.That(player.Health, Is.EqualTo(130));
    }

    [Test]
    public void FunctionToOpenDelegate_Func()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        Lua.Globals["player"] = player;

        // Act
        var getHealth = Lua.Evaluate<Func<TestPlayer, int>>("return player.GetHealth");

        // Assert
        Assert.That(getHealth, Is.Not.Null);
        Assert.That(getHealth!(player), Is.EqualTo(100));
    }

    [Test]
    public void FunctionToOpenDelegate_NoLuaStateDependency()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        Lua.Globals["player"] = player;

        // Act
        var heal = Lua.Evaluate<Action<TestPlayer, int>>("return player.heal");
        Lua.Execute("player = nil; collectgarbage()");

        // Assert
        Assert.That(heal, Is.Not.Null);
        heal!(player, 20);
        Assert.That(player.Health, Is.EqualTo(120));
    }

    [Test]
    public void FunctionToOpenDelegate_RepeatedConversions_ReturnDistinctWorkingDelegates()
    {
        // Arrange
        var steve = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        var alex = new TestPlayer
        {
            Name = "Alex",
            Health = 80
        };

        Lua.Globals["player"] = steve;

        // Act
        var heal1 = Lua.Evaluate<Action<TestPlayer, int>>("return player.heal");
        var heal2 = Lua.Evaluate<Action<TestPlayer, int>>("return player.heal");

        // Assert
        Assert.That(heal1, Is.Not.Null);
        Assert.That(heal2, Is.Not.Null);
        Assert.That(heal1, Is.Not.SameAs(heal2));
        heal1!(steve, 10);
        heal2!(alex, 20);
        Assert.That(steve.Health, Is.EqualTo(110));
        Assert.That(alex.Health, Is.EqualTo(100));
    }

    [Test]
    public void FunctionToOpenDelegate_StaticFunc()
    {
        // Arrange
        TestWithStatics.LuaUserDataHandler.PushDefinition(L);
        lua_setglobal(L, "TestWithStatics");

        // Act
        var add = Lua.Evaluate<Func<int, int, int>>("return TestWithStatics.Add");

        // Assert
        Assert.That(add, Is.Not.Null);
        Assert.That(add!(5, 12), Is.EqualTo(17));
    }

    [Test]
    public void FunctionToMethodInfo_PlainLuaFunction_Throws()
    {
        // Arrange
        Lua.Execute("function luaAdd(a, b) return a + b end");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => Lua.Evaluate<MethodInfo>("return luaAdd"));
    }

    [Test]
    public void FunctionToMethodInfo_OverloadedMethod_ThrowsInvalidOperation()
    {
        // Arrange
        var obj = new TestOverloaded();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            Lua.Evaluate<MethodInfo>("return obj.Compute"));
    }

    [Test]
    public void FunctionToOpenDelegate_OverloadedMethod_PicksBySignature()
    {
        // Arrange
        var obj = new TestOverloaded();
        Lua["obj"] = obj;

        // Act
        var del = Lua.Evaluate<Func<TestOverloaded, int, int>>("return obj.Compute");

        // Assert
        Assert.That(del, Is.Not.Null);
        Assert.That(del!(obj, 5), Is.EqualTo(10));
    }
}
