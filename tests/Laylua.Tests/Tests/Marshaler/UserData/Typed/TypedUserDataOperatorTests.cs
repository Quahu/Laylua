namespace Laylua.Tests;

public unsafe class TypedUserDataOperatorTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
    }

    [Test]
    public void OperatorAdd_FromLua_ReturnsNewInstance()
    {
        // Arrange
        var a = new TestVector
        {
            X = 1,
            Y = 2
        };

        var b = new TestVector
        {
            X = 3,
            Y = 4
        };

        Lua["a"] = a;
        Lua["b"] = b;

        // Act
        Lua.Execute("result = a + b");

        // Assert
        Lua.Execute(@"
            assert(result.X == 4)
            assert(result.Y == 6)
        ");
    }

    [Test]
    public void OperatorSubtract_FromLua_ReturnsNewInstance()
    {
        // Arrange
        var a = new TestVector
        {
            X = 5,
            Y = 8
        };

        var b = new TestVector
        {
            X = 2,
            Y = 3
        };

        Lua["a"] = a;
        Lua["b"] = b;

        // Act
        Lua.Execute("result = a - b");

        // Assert
        Lua.Execute(@"
            assert(result.X == 3)
            assert(result.Y == 5)
        ");
    }

    [Test]
    public void OperatorUnaryMinus_FromLua_NegatesValues()
    {
        // Arrange
        var vector = new TestVector
        {
            X = 3,
            Y = -7
        };

        Lua["v"] = vector;

        // Act
        Lua.Execute("result = -v");

        // Assert
        Lua.Execute(@"
            assert(result.X == -3)
            assert(result.Y == 7)
        ");
    }

    [Test]
    public void OperatorEquality_FromLua_ComparesValues()
    {
        // Arrange
        var a = new TestVector
        {
            X = 1,
            Y = 2
        };

        var b = new TestVector
        {
            X = 1,
            Y = 2
        };

        var c = new TestVector
        {
            X = 3,
            Y = 4
        };

        Lua["a"] = a;
        Lua["b"] = b;
        Lua["c"] = c;

        // Act & Assert
        Lua.Execute(@"
            assert(a == b)
            assert(a ~= c)
        ");
    }

    [Test]
    public void SelectiveOperators_OnlyEmitsEnabled()
    {
        // Arrange
        var left = new TestSelectiveOps
        {
            Value = 10
        };

        var right = new TestSelectiveOps
        {
            Value = 5
        };

        Lua["a"] = left;
        Lua["b"] = right;

        // Act
        Lua.Execute("result = a + b");
        lua_getglobal(L, "result");
        TestSelectiveOps.LuaUserDataHandler.TryGet(L, -1, out var result);
        lua_pop(L);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.EqualTo(15));
    }

    [Test]
    public void SelectiveOperators_DisabledOperator_Errors()
    {
        // Arrange
        var left = new TestSelectiveOps
        {
            Value = 10
        };

        var right = new TestSelectiveOps
        {
            Value = 5
        };

        Lua["a"] = left;
        Lua["b"] = right;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("result = a - b"));
    }

    [Test]
    public void NoOperators_OperatorErrors()
    {
        // Arrange
        var left = new TestNoOps
        {
            Value = 10
        };

        var right = new TestNoOps
        {
            Value = 5
        };

        Lua["a"] = left;
        Lua["b"] = right;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("result = a + b"));
    }

    [Test]
    public void NoOperators_ToStringStillWorks()
    {
        // Arrange
        var obj = new TestNoOps
        {
            Value = 42
        };

        Lua["obj"] = obj;

        // Act
        Lua.Execute("result = tostring(obj)");
        lua_getglobal(L, "result");
        var result = lua_tostring(L, -1).ToString()!;
        lua_pop(L);

        // Assert
        Assert.That(result, Is.EqualTo("NoOps(42)"));
    }

    [Test]
    public void OperatorMultiply_FromLua_Works()
    {
        // Arrange
        var a = new TestAllOperators
        {
            Value = 6
        };

        var b = new TestAllOperators
        {
            Value = 7
        };

        Lua["a"] = a;
        Lua["b"] = b;

        // Act
        Lua.Execute("result = a * b");

        // Assert
        lua_getglobal(L, "result");
        TestAllOperators.LuaUserDataHandler.TryGet(L, -1, out var result);
        lua_pop(L);
        Assert.That(result!.Value, Is.EqualTo(42));
    }

    [Test]
    public void OperatorDivide_FromLua_Works()
    {
        // Arrange
        var a = new TestAllOperators
        {
            Value = 42
        };

        var b = new TestAllOperators
        {
            Value = 7
        };

        Lua["a"] = a;
        Lua["b"] = b;

        // Act
        Lua.Execute("result = a / b");

        // Assert
        lua_getglobal(L, "result");
        TestAllOperators.LuaUserDataHandler.TryGet(L, -1, out var result);
        lua_pop(L);
        Assert.That(result!.Value, Is.EqualTo(6));
    }

    [Test]
    public void OperatorModulus_FromLua_Works()
    {
        // Arrange
        var a = new TestAllOperators
        {
            Value = 17
        };

        var b = new TestAllOperators
        {
            Value = 5
        };

        Lua["a"] = a;
        Lua["b"] = b;

        // Act
        Lua.Execute("result = a % b");

        // Assert
        lua_getglobal(L, "result");
        TestAllOperators.LuaUserDataHandler.TryGet(L, -1, out var result);
        lua_pop(L);
        Assert.That(result!.Value, Is.EqualTo(2));
    }

    [Test]
    public void OperatorLessThan_FromLua_Works()
    {
        // Arrange
        var a = new TestAllOperators
        {
            Value = 3
        };

        var b = new TestAllOperators
        {
            Value = 7
        };

        Lua["a"] = a;
        Lua["b"] = b;

        // Act & Assert
        Lua.Execute(@"
            assert(a < b)
            assert(not (b < a))
            assert(not (a < a))
        ");
    }

    [Test]
    public void OperatorLessThanOrEqual_FromLua_Works()
    {
        // Arrange
        var a = new TestAllOperators
        {
            Value = 3
        };

        var b = new TestAllOperators
        {
            Value = 7
        };

        var c = new TestAllOperators
        {
            Value = 3
        };

        Lua["a"] = a;
        Lua["b"] = b;
        Lua["c"] = c;

        // Act & Assert
        Lua.Execute(@"
            assert(a <= b)
            assert(a <= c)
            assert(not (b <= a))
        ");
    }

    [Test]
    public void OperatorGreaterThan_FromLua_DerivedFromLt()
    {
        // Arrange
        var a = new TestAllOperators
        {
            Value = 10
        };

        var b = new TestAllOperators
        {
            Value = 3
        };

        Lua["a"] = a;
        Lua["b"] = b;

        // Act & Assert
        Lua.Execute(@"
            assert(a > b)
            assert(not (b > a))
        ");
    }

    [Test]
    public void OperatorGreaterThanOrEqual_FromLua_DerivedFromLe()
    {
        // Arrange
        var a = new TestAllOperators
        {
            Value = 10
        };

        var b = new TestAllOperators
        {
            Value = 10
        };

        var c = new TestAllOperators
        {
            Value = 3
        };

        Lua["a"] = a;
        Lua["b"] = b;
        Lua["c"] = c;

        // Act & Assert
        Lua.Execute(@"
            assert(a >= b)
            assert(a >= c)
            assert(not (c >= a))
        ");
    }

    [Test]
    public void OperatorBitwiseAnd_FromLua_Works()
    {
        // Arrange
        var a = new TestAllOperators
        {
            Value = 0b1100
        };

        var b = new TestAllOperators
        {
            Value = 0b1010
        };

        Lua["a"] = a;
        Lua["b"] = b;

        // Act
        Lua.Execute("result = a & b");

        // Assert
        lua_getglobal(L, "result");
        TestAllOperators.LuaUserDataHandler.TryGet(L, -1, out var result);
        lua_pop(L);
        Assert.That(result!.Value, Is.EqualTo(0b1000));
    }

    [Test]
    public void OperatorBitwiseOr_FromLua_Works()
    {
        // Arrange
        var a = new TestAllOperators
        {
            Value = 0b1100
        };

        var b = new TestAllOperators
        {
            Value = 0b1010
        };

        Lua["a"] = a;
        Lua["b"] = b;

        // Act
        Lua.Execute("result = a | b");

        // Assert
        lua_getglobal(L, "result");
        TestAllOperators.LuaUserDataHandler.TryGet(L, -1, out var result);
        lua_pop(L);
        Assert.That(result!.Value, Is.EqualTo(0b1110));
    }

    [Test]
    public void OperatorBitwiseXor_FromLua_Works()
    {
        // Arrange
        var a = new TestAllOperators
        {
            Value = 0b1100
        };

        var b = new TestAllOperators
        {
            Value = 0b1010
        };

        Lua["a"] = a;
        Lua["b"] = b;

        // Act
        Lua.Execute("result = a ~ b");

        // Assert
        lua_getglobal(L, "result");
        TestAllOperators.LuaUserDataHandler.TryGet(L, -1, out var result);
        lua_pop(L);
        Assert.That(result!.Value, Is.EqualTo(0b0110));
    }

    [Test]
    public void OperatorBitwiseNot_FromLua_Works()
    {
        // Arrange
        var a = new TestAllOperators
        {
            Value = 0
        };

        Lua["a"] = a;

        // Act
        Lua.Execute("result = ~a");

        // Assert
        lua_getglobal(L, "result");
        TestAllOperators.LuaUserDataHandler.TryGet(L, -1, out var result);
        lua_pop(L);
        Assert.That(result!.Value, Is.EqualTo(~0));
    }

    [Test]
    public void OperatorLeftShift_FromLua_Works()
    {
        // Arrange
        var a = new TestAllOperators
        {
            Value = 1
        };

        Lua["a"] = a;

        // Act
        Lua.Execute("result = a << 4");

        // Assert
        lua_getglobal(L, "result");
        TestAllOperators.LuaUserDataHandler.TryGet(L, -1, out var result);
        lua_pop(L);
        Assert.That(result!.Value, Is.EqualTo(16));
    }

    [Test]
    public void OperatorRightShift_FromLua_Works()
    {
        // Arrange
        var a = new TestAllOperators
        {
            Value = 64
        };

        Lua["a"] = a;

        // Act
        Lua.Execute("result = a >> 3");

        // Assert
        lua_getglobal(L, "result");
        TestAllOperators.LuaUserDataHandler.TryGet(L, -1, out var result);
        lua_pop(L);
        Assert.That(result!.Value, Is.EqualTo(8));
    }

    [Test]
    public void MixedOperator_ObjTimesScalar_Works()
    {
        // Arrange
        var a = new TestMixedOperators
        {
            Value = 5.0
        };

        Lua["a"] = a;

        // Act
        Lua.Execute("result = a * 3.0");

        // Assert
        lua_getglobal(L, "result");
        TestMixedOperators.LuaUserDataHandler.TryGet(L, -1, out var result);
        lua_pop(L);
        Assert.That(result!.Value, Is.EqualTo(15.0));
    }

    [Test]
    public void MixedOperator_ScalarTimesObj_Works()
    {
        // Arrange
        var a = new TestMixedOperators
        {
            Value = 4.0
        };

        Lua["a"] = a;

        // Act
        Lua.Execute("result = 2.0 * a");

        // Assert
        lua_getglobal(L, "result");
        TestMixedOperators.LuaUserDataHandler.TryGet(L, -1, out var result);
        lua_pop(L);
        Assert.That(result!.Value, Is.EqualTo(8.0));
    }

    [Test]
    public void MixedOperator_ObjTimesObj_Works()
    {
        // Arrange
        var a = new TestMixedOperators
        {
            Value = 3.0
        };

        var b = new TestMixedOperators
        {
            Value = 7.0
        };

        Lua["a"] = a;
        Lua["b"] = b;

        // Act
        Lua.Execute("result = a * b");

        // Assert
        lua_getglobal(L, "result");
        TestMixedOperators.LuaUserDataHandler.TryGet(L, -1, out var result);
        lua_pop(L);
        Assert.That(result!.Value, Is.EqualTo(21.0));
    }
}
