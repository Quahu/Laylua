using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Laylua.Marshaling;

namespace Laylua.Tests;

public unsafe class TypedUserDataBasicTests : LuaTestBase
{
    private sealed class PrimitiveOptimizationProbeMarshaler : DefaultLuaMarshaler
    {
        public int MagicIntPushValue { get; } = 777;

        public int MagicIntReadValue { get; } = 999;

        public bool RewriteIntPushes { get; set; }

        public bool RewriteIntReads { get; set; }

        public int IntPushCount { get; private set; }

        public int IntReadCount { get; private set; }

        public override void PushValue<T>(LuaThread thread, T obj)
        {
            if (RewriteIntPushes && typeof(T) == typeof(int))
            {
                IntPushCount++;
                lua_pushinteger(thread.State.L, MagicIntPushValue);
                return;
            }

            base.PushValue(thread, obj);
        }

        public override bool TryGetValue<T>(LuaThread thread, int stackIndex, out T? obj)
            where T : default
        {
            if (RewriteIntReads && typeof(T) == typeof(int) && lua_type(thread.State.L, stackIndex) == LuaType.Number)
            {
                IntReadCount++;
                obj = (T) (object) MagicIntReadValue;
                return true;
            }

            return base.TryGetValue(thread, stackIndex, out obj);
        }

        public override bool TryGetValue<T>(LuaThread thread, int stackIndex, LuaAllowedValueConversions allowedConversions, out T? obj)
            where T : default
        {
            if (RewriteIntReads && typeof(T) == typeof(int) && lua_type(thread.State.L, stackIndex) == LuaType.Number)
            {
                IntReadCount++;
                obj = (T) (object) MagicIntReadValue;
                return true;
            }

            return base.TryGetValue(thread, stackIndex, allowedConversions, out obj);
        }
    }

    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
    }

    [Test]
    public void Push_ThenTryGet_ReturnsSameInstance()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100,
            Speed = 5.5
        };

        // Act
        TestPlayer.LuaUserDataHandler.Push(L, player);
        var found = TestPlayer.LuaUserDataHandler.TryGet(L, -1, out var retrieved);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True);
            Assert.That(retrieved, Is.SameAs(player));
        });

        lua_pop(L);
    }

    [Test]
    public void TryGet_WithWrongType_ReturnsFalse()
    {
        // Arrange
        lua_pushinteger(L, 42);

        // Act
        var found = TestPlayer.LuaUserDataHandler.TryGet(L, -1, out _);

        // Assert
        Assert.That(found, Is.False);

        lua_pop(L);
    }

    [Test]
    public void TryGet_ForgedUserDataWithMatchingMetatableName_ReturnsFalse()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        var forgedTarget = new TestPlayer
        {
            Name = "forged",
            Health = 1,
            Speed = 2
        };

        TestPlayer.LuaUserDataHandler.Push(L, new TestPlayer());
        Assert.That(lua_getmetatable(L, -1), Is.True);

        var handle = GCHandle.Alloc(forgedTarget);
        try
        {
            var forgedUserData = (nint*) lua_newuserdatauv(L, (nuint) (sizeof(nint) * 2), 0);
            forgedUserData[0] = GCHandle.ToIntPtr(handle);
            forgedUserData[1] = 0;

            lua_pushvalue(L, -2);
            lua_setmetatable(L, -2);

            // Act
            var found = TestPlayer.LuaUserDataHandler.TryGet(L, -1, out var value);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(found, Is.False);
                Assert.That(value, Is.Null);
            });
        }
        finally
        {
            handle.Free();
        }
    }

    [Test]
    public void MultipleInstances_ShareMetatable()
    {
        // Arrange
        var p1 = new TestPlayer
        {
            Name = "Steve"
        };

        var p2 = new TestPlayer
        {
            Name = "Alex"
        };

        Lua["p1"] = p1;
        Lua["p2"] = p2;

        // Act & Assert
        Lua.Execute("assert(p1.Name == 'Steve')");
        Lua.Execute("assert(p2.Name == 'Alex')");
    }

    [Test]
    public void PropertyRead_FromLua_ReturnsCorrectValues()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100,
            Speed = 3.0,
            IsAlive = true
        };

        Lua["player"] = player;

        // Act & Assert
        Lua.Execute("assert(player.Name == 'Steve')");
        Lua.Execute("assert(player.Health == 100)");
        Lua.Execute("assert(player.Speed == 3.0)");
        Lua.Execute("assert(player.IsAlive == true)");
    }

    [Test]
    public void PropertyWrite_FromLua_UpdatesClrObject()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        Lua["player"] = player;

        // Act
        Lua.Execute("player.Name = 'Alex'");
        Lua.Execute("player.Health = 200");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(player.Name, Is.EqualTo("Alex"));
            Assert.That(player.Health, Is.EqualTo(200));
        });
    }

    [Test]
    public void IgnoredProperty_FromLua_ReturnsNil()
    {
        // Arrange
        var player = new TestPlayer
        {
            InternalId = 42
        };

        Lua["player"] = player;

        // Act & Assert
        Lua.Execute("assert(player.InternalId == nil)");
    }

    [Test]
    public void UnknownProperty_FromLua_ReturnsNil()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve"
        };

        Lua["player"] = player;

        // Act & Assert
        Lua.Execute("assert(player.nonexistent == nil)");
    }

    [Test]
    public void FieldReadWrite_FromLua_WorksCorrectly()
    {
        // Arrange
        var player = new TestPlayer
        {
            Field = 7
        };

        Lua["player"] = player;

        // Act
        Lua.Execute("assert(player.Field == 7)");
        Lua.Execute("player.Field = 99");

        // Assert
        Assert.That(player.Field, Is.EqualTo(99));
    }

    [Test]
    public void ToString_FromLua_UsesClrToString()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve"
        };

        Lua["player"] = player;

        // Act & Assert
        Lua.Execute("assert(tostring(player) == 'Player(Steve)')");
    }

    [Test]
    public void VectorToString_FromLua_UsesClrToString()
    {
        // Arrange
        var vector = new TestVector
        {
            X = 1.5,
            Y = 2.5
        };

        Lua["v"] = vector;

        // Act & Assert
        Lua.Execute("assert(tostring(v) == '(1.5, 2.5)')");
    }

    [Test]
    public void GarbageCollection_FreesGcHandle()
    {
        // Arrange
        var weakRef = PushAndReleasePlayer();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Act
        lua_gc(L, LuaGCOperation.Collect);
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Assert
        Assert.That(weakRef.IsAlive, Is.False);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private WeakReference PushAndReleasePlayer()
    {
        var player = new TestPlayer
        {
            Name = "Steve"
        };

        var weakRef = new WeakReference(player);
        Lua["player"] = player;
        lua_pushnil(L);
        lua_setglobal(L, "player");
        return weakRef;
    }

    [Test]
    public void GarbageCollection_CallsDispose_WhenTypeIsDisposable()
    {
        // Arrange
        var disposable = new TestDisposable
        {
            Value = 42
        };

        Lua["obj"] = disposable;

        // Act
        lua_pushnil(L);
        lua_setglobal(L, "obj");
        lua_gc(L, LuaGCOperation.Collect);

        // Assert
        Assert.That(disposable.WasDisposed, Is.True);
    }

    [Test]
    public void ObjectIdentity_SameInstancePushedTwice_RawEqual()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve"
        };

        Lua["a"] = player;
        Lua["b"] = player;

        // Act & Assert
        Lua.Execute("assert(rawequal(a, b))");
    }

    [Test]
    public void GetGlobal_AsObject_ReturnsClrInstance()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        Lua["player"] = player;

        // Act
        var retrieved = Lua.GetGlobal<object>("player");

        // Assert
        Assert.That(retrieved, Is.SameAs(player));
    }

    [Test]
    public void GetGlobal_AsConcreteType_ReturnsTypedInstance()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        Lua["player"] = player;

        // Act
        var retrieved = Lua.GetGlobal<TestPlayer>("player");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(retrieved, Is.SameAs(player));
            Assert.That(retrieved.Name, Is.EqualTo("Steve"));
        });
    }

    [Test]
    public void ReadOnlyProperty_GetFromLua_ReturnsValue()
    {
        // Arrange
        var obj = new TestReadOnly(50);
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj.Health == 50)");
        Lua.Execute("assert(obj.DoubleHealth == 100)");
    }

    [Test]
    public void ReadOnlyProperty_SetFromLua_RaisesError()
    {
        // Arrange
        var obj = new TestReadOnly(50);
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj.Health = 999"));
    }

    [Test]
    public void NullProperty_ReadFromLua_ReturnsNil()
    {
        // Arrange
        var obj = new TestNullable
        {
            Label = null,
            OptionalValue = null,
            Position = null
        };

        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj.Label == nil)");
        Lua.Execute("assert(obj.OptionalValue == nil)");
        Lua.Execute("assert(obj.Position == nil)");
    }

    [Test]
    public void NullProperty_WriteNilFromLua_SetsToNull()
    {
        // Arrange
        var obj = new TestNullable
        {
            Label = "hello",
            OptionalValue = 42
        };

        Lua["obj"] = obj;

        // Act
        Lua.Execute("obj.Label = nil");
        Lua.Execute("obj.OptionalValue = nil");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(obj.Label, Is.Null);
            Assert.That(obj.OptionalValue, Is.Null);
        });
    }

    [Test]
    public void EnumProperty_ReadFromLua_ReturnsIntegerValue()
    {
        // Arrange
        var obj = new TestWithEnum
        {
            Color = TestColor.Green
        };

        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj.Color == 1)");
    }

    [Test]
    public void EnumProperty_WriteFromLua_SetsEnumValue()
    {
        // Arrange
        var obj = new TestWithEnum
        {
            Color = TestColor.Red
        };

        Lua["obj"] = obj;

        // Act
        Lua.Execute("obj.Color = 2");

        // Assert
        Assert.That(obj.Color, Is.EqualTo(TestColor.Blue));
    }

    [Test]
    public void Struct_PushAndGet_Roundtrips()
    {
        // Arrange
        var point = new TestPoint
        {
            X = 3.5,
            Y = 7.2
        };

        Lua["point"] = point;

        // Act & Assert
        Lua.Execute("assert(point.X == 3.5)");
        Lua.Execute("assert(point.Y == 7.2)");
    }

    [Test]
    public void Struct_ModifyFromLua_IsValueSemantics()
    {
        // Arrange
        var point = new TestPoint
        {
            X = 1,
            Y = 2
        };

        Lua["point"] = point;

        // Act
        Lua.Execute("point.X = 99");

        // Assert
        Assert.That(point.X, Is.EqualTo(1));
    }

    [Test]
    public void Struct_PropertyMutation_PersistsViaNewIndex()
    {
        // Arrange
        var vec = new TestMutableStruct
        {
            X = 1.0f,
            Y = 2.0f
        };

        Lua["vec"] = vec;

        // Act
        Lua.Execute("vec.X = 10");
        Lua.Execute("vec.Y = 20");

        // Assert
        Lua.Execute("assert(vec.X == 10)");
        Lua.Execute("assert(vec.Y == 20)");
    }

    [Test]
    public void Struct_MethodMutation_Persists()
    {
        // Arrange
        var vec = new TestMutableStruct
        {
            X = 3.0f,
            Y = 4.0f
        };

        Lua["vec"] = vec;

        // Act
        Lua.Execute("vec:Scale(2)");

        // Assert
        Lua.Execute("assert(vec.X == 6)");
        Lua.Execute("assert(vec.Y == 8)");
    }

    [Test]
    public void Close_ToBeClosedVariable_CallsDispose()
    {
        // Arrange
        var closeable = new TestCloseable
        {
            Value = 42
        };

        Lua["closeable"] = closeable;

        // Act
        Lua.Execute(@"
            do
                local obj <close> = closeable
            end
        ");

        // Assert
        Assert.That(closeable.WasClosed, Is.True);
    }

    [Test]
    public void LuaInclude_ExposesNonPublicMember()
    {
        // Arrange
        var obj = new TestWithInclude
        {
            Secret = 42,
            Visible = 10
        };

        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj.Secret == 42)");
        Lua.Execute("assert(obj.Visible == 10)");
    }

    [Test]
    public void TestWithStatics_InstanceValue_AccessibleFromLua()
    {
        // Arrange
        var obj = new TestWithStatics
        {
            Value = 77
        };

        Lua["obj"] = obj;

        // Act
        Lua.Execute("assert(obj.Value == 77)");
        Lua.Execute("obj.Value = 123");

        // Assert
        Assert.That(obj.Value, Is.EqualTo(123));
    }

    [Test]
    public void TestDisposable_Value_AccessibleFromLua()
    {
        // Arrange
        var obj = new TestDisposable
        {
            Value = 42
        };

        Lua["obj"] = obj;

        // Act
        Lua.Execute("assert(obj.Value == 42)");
        Lua.Execute("obj.Value = 100");

        // Assert
        Assert.That(obj.Value, Is.EqualTo(100));
    }

    [Test]
    public void NoFeatures_PropertiesNotAccessible()
    {
        // Arrange
        var obj = new TestNoFeatures
        {
            Value = 99
        };

        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj.Value == nil)");
    }

    [Test]
    public void MarshaledProperty_ReadFromLua_PushesNestedUserdata()
    {
        // Arrange
        var position = new TestVector
        {
            X = 10,
            Y = 20
        };

        var obj = new TestWithNestedType
        {
            Label = "origin",
            Position = position
        };

        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj.Label == 'origin')");
        Lua.Execute("assert(obj.Position.X == 10)");
        Lua.Execute("assert(obj.Position.Y == 20)");
    }

    [Test]
    public void MarshaledProperty_WriteFromLua_UpdatesReference()
    {
        // Arrange
        var obj = new TestWithNestedType
        {
            Label = "test"
        };

        var newPosition = new TestVector
        {
            X = 5,
            Y = 15
        };

        Lua["obj"] = obj;
        Lua["newPos"] = newPosition;

        // Act
        Lua.Execute("obj.Position = newPos");

        // Assert
        Assert.That(obj.Position, Is.SameAs(newPosition));
    }

    [Test]
    public void PrimitiveReadOptimizationsDisabled_PropertyRead_UsesMarshalerPushPath()
    {
        // Arrange
        var marshaler = new PrimitiveOptimizationProbeMarshaler
        {
            RewriteIntPushes = true
        };

        using var lua = CreateLua(marshaler: marshaler);
        var obj = new TestNoPrimitiveOptimization
        {
            Value = 5
        };

        lua["obj"] = obj;

        // Act
        var result = lua.Evaluate<int>("return obj.Value");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(marshaler.MagicIntPushValue));
            Assert.That(marshaler.IntPushCount, Is.GreaterThan(0));
        });
    }

    [Test]
    public void PrimitiveReadOptimizationsEnabled_PropertyRead_DoesNotUseMarshalerPushPath()
    {
        // Arrange
        var marshaler = new PrimitiveOptimizationProbeMarshaler
        {
            RewriteIntPushes = true
        };

        using var lua = CreateLua(marshaler: marshaler);
        var obj = new TestPlayer
        {
            Health = 100
        };

        lua["obj"] = obj;

        // Act
        var result = lua.Evaluate<int>("return obj.Health");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(obj.Health));
            Assert.That(result, Is.Not.EqualTo(marshaler.MagicIntPushValue));
            Assert.That(marshaler.IntPushCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void PrimitiveReadOptimizationsDisabled_PropertyWrite_UsesMarshalerReadPath()
    {
        // Arrange
        var marshaler = new PrimitiveOptimizationProbeMarshaler
        {
            RewriteIntReads = true
        };

        using var lua = CreateLua(marshaler: marshaler);
        var obj = new TestNoPrimitiveOptimization();
        lua["obj"] = obj;

        // Act
        lua.Execute("obj.Value = 1");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(obj.Value, Is.EqualTo(marshaler.MagicIntReadValue));
            Assert.That(marshaler.IntReadCount, Is.GreaterThan(0));
        });
    }

    [Test]
    public void PrimitiveMarshalerBypass_PropertyWrite_RespectsBindingConversions()
    {
        // Arrange
        var obj = new TestNoImplicitConversionWithOptimization();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.That(
            () => Lua.Execute("obj.Value = '42'"),
            Throws.TypeOf<LuaException>());
    }

    [Test]
    public void PrimitiveMarshalerBypass_EnumPropertyWrite_NumberToEnumDisabled_RaisesLuaError()
    {
        // Arrange
        var obj = new TestEnumPropertyNoNumberConversion();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.That(
            () => Lua.Execute("obj.Value = 2"),
            Throws.TypeOf<LuaException>());
    }

    [Test]
    public void PrimitiveMarshalerBypass_EnumPropertyWrite_UndefinedNumericValue_RaisesLuaError()
    {
        // Arrange
        var obj = new TestEnumPropertyValidation();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.That(
            () => Lua.Execute("obj.Value = 3"),
            Throws.TypeOf<LuaException>());
    }

    [Test]
    public void TryGetFromTable_WithMatchingFields_ReturnsPopulatedInstance()
    {
        // Arrange
        Lua.Execute(@"
            player = {
                Name = 'Steve',
                Health = 100,
                Speed = 2.5,
                IsAlive = true,
                Field = 10
            }
        ");

        lua_getglobal(L, "player");

        // Act
        var found = TestPlayer.LuaUserDataHandler.TryGetFromTable(L, -1, out var player);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True);
            Assert.That(player!.Name, Is.EqualTo("Steve"));
            Assert.That(player.Health, Is.EqualTo(100));
            Assert.That(player.Speed, Is.EqualTo(2.5));
            Assert.That(player.IsAlive, Is.True);
            Assert.That(player.Field, Is.EqualTo(10));
        });

        lua_pop(L);
    }

    [Test]
    public void TryGetFromTable_WithNonTable_ReturnsFalse()
    {
        // Arrange
        lua_pushinteger(L, 42);

        // Act
        var found = TestPlayer.LuaUserDataHandler.TryGetFromTable(L, -1, out _);

        // Assert
        Assert.That(found, Is.False);

        lua_pop(L);
    }

    [Test]
    public void Property_GetterThrows_BecomesLuaError()
    {
        // Arrange
        var obj = new TestThrowingType();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("local x = obj.ThrowingProperty"));
    }

    [Test]
    public void TestWithList_PushAndReadScores()
    {
        // Arrange
        var obj = new TestWithList
        {
            Name = "Steve",
            Scores =
            [
                10,
                20,
                30
            ]
        };

        Lua["obj"] = obj;

        // Act
        var name = Lua.Evaluate<string>("return obj.Name");
        var scoresCount = Lua.Evaluate<int>("return #obj.Scores");
        var score1 = Lua.Evaluate<int>("return obj.Scores[1]");
        var score3 = Lua.Evaluate<int>("return obj.Scores[3]");

        // Assert
        Assert.That(name, Is.EqualTo("Steve"));
        Assert.That(scoresCount, Is.EqualTo(3));
        Assert.That(score1, Is.EqualTo(10));
        Assert.That(score3, Is.EqualTo(30));
    }

    [Test]
    public void NullableStringProperty_SetToNil_SetsNull()
    {
        // Arrange
        var obj = new TestNullable
        {
            Label = "test"
        };

        Lua["obj"] = obj;

        // Act
        Lua.Execute("obj.Label = nil");

        // Assert
        Assert.That(obj.Label, Is.Null);
    }

    [Test]
    public void UnknownProperty_Read_ReturnsNilForPermissiveType()
    {
        // Arrange
        var obj = new TestPlayer
        {
            Name = "Steve"
        };

        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj.nonExistentProperty == nil)");
    }

    [Test]
    public void UnknownProperty_Read_ErrorsForStrictType()
    {
        // Arrange
        var obj = new TestStrictType
        {
            Value = 1
        };

        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("local x = obj.nonExistent"));
    }

    [Test]
    public void Metatable_GetMetatable_ReturnsFalse()
    {
        // Arrange
        var obj = new TestEntity
        {
            Id = 1,
            Tag = "test"
        };

        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<bool>("return getmetatable(obj)");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Metatable_SetMetatable_Errors()
    {
        // Arrange
        var obj = new TestEntity
        {
            Id = 1,
            Tag = "test"
        };

        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("setmetatable(obj, {})"));
    }

    [Test]
    public void InitOnlyProperty_ReadFromLua_ReturnsValue()
    {
        // Arrange
        var player = new InitOnlyPropertyPlayer
        {
            Name = "Steve",
            Health = 100
        };

        Lua["player"] = player;

        // Act & Assert
        Lua.Execute("assert(player.Name == 'Steve')");
    }

    [Test]
    public void InitOnlyProperty_SetFromLua_RaisesError()
    {
        // Arrange
        var player = new InitOnlyPropertyPlayer
        {
            Name = "Steve",
            Health = 100
        };

        Lua["player"] = player;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("player.Name = 'Alex'"));
    }

    [Test]
    public void InitOnlyProperty_RegularSetterStillWritable()
    {
        // Arrange
        var player = new InitOnlyPropertyPlayer
        {
            Name = "Steve",
            Health = 100
        };

        Lua["player"] = player;

        // Act
        Lua.Execute("player.Health = 200");

        // Assert
        Assert.That(player.Health, Is.EqualTo(200));
    }
}
