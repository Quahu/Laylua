namespace Laylua.Tests;

public class TypedUserDataMethodTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
    }

    [Test]
    public void MethodCall_FromLua_ExecutesOnClrObject()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        Lua["player"] = player;

        // Act
        Lua.Execute("player:TakeDamage(30)");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(player.Health, Is.EqualTo(70));
            Assert.That(player.IsAlive, Is.True);
        });
    }

    [Test]
    public void MethodWithLuaName_CalledByLuaName_Works()
    {
        // Arrange
        var player = new TestPlayer
        {
            Health = 100
        };

        Lua["player"] = player;

        // Act
        Lua.Execute("player:heal(25)");

        // Assert
        Assert.That(player.Health, Is.EqualTo(125));
    }

    [Test]
    public void MethodWithReturnValue_FromLua_ReturnsResult()
    {
        // Arrange
        var player = new TestPlayer
        {
            Health = 100
        };

        Lua["player"] = player;

        // Act & Assert
        Lua.Execute("assert(player:GetHealth() == 100)");
    }

    [Test]
    public void Method_ThrowsException_BecomesLuaError()
    {
        // Arrange
        var obj = new TestThrowingType
        {
            Value = 42
        };

        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:GetValueOrThrow(true)"));
    }

    [Test]
    public void Method_DoesNotThrow_ReturnsNormally()
    {
        // Arrange
        var obj = new TestThrowingType
        {
            Value = 42
        };

        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj:GetValueOrThrow(false) == 42)");
    }

    [Test]
    public void OverloadedMethod_SingleParam_Works()
    {
        // Arrange
        var obj = new TestOverloaded();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj:Compute(5) == 10)");
    }

    [Test]
    public void OverloadedMethod_TwoParams_Works()
    {
        // Arrange
        var obj = new TestOverloaded();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj:Compute(5, 3) == 15)");
    }

    [Test]
    public void OverloadedMethod_TypeDispatch_String()
    {
        // Arrange
        var obj = new TestOverloaded();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj:Describe('hello') == 'text:hello')");
    }

    [Test]
    public void OverloadedMethod_TypeDispatch_Int()
    {
        // Arrange
        var obj = new TestOverloaded();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj:Describe(42) == 'number:42')");
    }

    [Test]
    public void OverloadedMethod_TypeDispatch_Bool()
    {
        // Arrange
        var obj = new TestOverloaded();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj:Describe(true) == 'bool:true')");
    }

    [Test]
    public void OverloadedMethod_TypeDispatch_DoubleVsString()
    {
        // Arrange
        var obj = new TestOverloaded();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute(@"
            assert(obj:Typed(3.14) == 'double')
            assert(obj:Typed('hi') == 'string')
        ");
    }

    [Test]
    public void OptionalParameter_WithValue_UsesProvidedValue()
    {
        // Arrange
        var obj = new TestWithOptionalParams();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj:Calculate(5, 3) == 15)");
    }

    [Test]
    public void OptionalParameter_WithoutValue_UsesDefault()
    {
        // Arrange
        var obj = new TestWithOptionalParams();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj:Calculate(5) == 10)");
    }

    [Test]
    public void Params_Sum_CollectsAllArgs()
    {
        // Arrange
        var obj = new TestWithParams();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute(@"
            assert(obj:Sum(1, 2, 3) == 6)
            assert(obj:Sum(10) == 10)
            assert(obj:Sum() == 0)
        ");
    }

    [Test]
    public void Params_Join_FixedAndVariableArgs()
    {
        // Arrange
        var obj = new TestWithParams();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute(@"
            assert(obj:Join('-', 'a', 'b', 'c') == 'a-b-c')
            assert(obj:Join(', ', 'hello') == 'hello')
        ");
    }

    [Test]
    public void Params_OverloadedWithFixed_DispatchesCorrectly()
    {
        // Arrange
        var obj = new TestWithParams();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute(@"
            assert(obj:Format(1) == 'single:1')
            assert(obj:Format(1, 2, 3) == 'multi:1+2,3')
        ");
    }

    [Test]
    public void Methods_SameClrNameDifferentLuaNames_AreAccessibleIndependently()
    {
        // Arrange
        var obj = new TestMethodAliasCallbacks();
        Lua["obj"] = obj;

        // Act
        var numberResult = Lua.Evaluate<string>("return obj:from_number(7)");
        var textResult = Lua.Evaluate<string>("return obj:from_text('lua')");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(numberResult, Is.EqualTo("int:7"));
            Assert.That(textResult, Is.EqualTo("string:lua"));
        });
    }

    [Test]
    public void RefOut_TryParse_ReturnsSuccessAndValue()
    {
        // Arrange
        var obj = new TestWithRefOut();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute(@"
            local success, value = obj:TryParse('42')
            assert(success == true)
            assert(value == 42)
        ");

        Lua.Execute(@"
            local success, value = obj:TryParse('abc')
            assert(success == false)
            assert(value == -1)
        ");
    }

    [Test]
    public void RefOut_Swap_ReturnsSwappedValues()
    {
        // Arrange
        var obj = new TestWithRefOut();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute(@"
            local a, b = obj:Swap(10, 20)
            assert(a == 20, 'expected a=20, got ' .. tostring(a))
            assert(b == 10, 'expected b=10, got ' .. tostring(b))
        ");
    }

    [Test]
    public void RefOut_AddAndGetOriginal_ReturnsResultAndRefValue()
    {
        // Arrange
        var obj = new TestWithRefOut();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute(@"
            local original, newValue = obj:AddAndGetOriginal(5, 3)
            assert(original == 5, 'expected original=5, got ' .. tostring(original))
            assert(newValue == 8, 'expected newValue=8, got ' .. tostring(newValue))
        ");
    }

    [Test]
    public void CallingThread_PushMultipleValues_ReturnsMultipleResults()
    {
        // Arrange
        var obj = new TestWithCallingThread
        {
            Name = "test"
        };

        Lua["obj"] = obj;

        // Act
        Lua.Execute("local a, b, c = obj:PushMultipleValues(3); assert(a == 'test_1'); assert(b == 'test_2'); assert(c == 'test_3')");
    }

    [Test]
    public void CallingThread_SetNameViaThread_ModifiesObject()
    {
        // Arrange
        var obj = new TestWithCallingThread
        {
            Name = "original"
        };

        Lua["obj"] = obj;

        // Act
        Lua.Execute("obj:SetNameViaThread('updated')");

        // Assert
        Assert.That(obj.Name, Is.EqualTo("updated"));
    }

    [Test]
    public void CallingThread_IntegerReturn_WithoutReturnCountAttribute_ReturnsSingleValue()
    {
        // Arrange
        var obj = new TestWithCallingThread();
        Lua["obj"] = obj;

        // Act
        Lua.Execute("local value = obj:MultiplyByTwo(21); assert(value == 42)");
    }

    [Test]
    public void OverloadedMethod_WithCallingThreadAndOut_DispatchesCorrectly()
    {
        // Arrange
        var obj = new TestCallingThreadOutOverloadTarget();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute(@"
            assert(obj:Resolve(42) == 'int:42')

            local text, length = obj:Resolve('Steve')
            assert(text == 'string:Steve')
            assert(length == 5)
        ");
    }

    [Test]
    public void LuaReturnCount_WithOutParameters_ReturnsRawCountAndOutValues()
    {
        // Arrange
        var overloaded = new TestWithReturnCountAndOut();
        Lua["overloaded"] = overloaded;

        var single = new TestWithSingleReturnCountAndOut();
        Lua["single"] = single;

        // Act & Assert
        Lua.Execute(@"
            local upper, length = single:PushValueAndLength('Steve')
            assert(upper == 'STEVE')
            assert(length == 5)

            local text, textLength = overloaded:PushValueAndLength('Alex')
            assert(text == 'ALEX')
            assert(textLength == 4)

            local number, doubled = overloaded:PushValueAndLength(21)
            assert(number == 21)
            assert(doubled == 42)
        ");
    }

    [Test]
    public void ArgValidation_TooFewArgs_RaisesLuaError()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        Lua["player"] = player;

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("player:TakeDamage()"));
        Assert.That(ex!.Message, Does.Contain("expected at least 1 argument"));
    }

    [Test]
    public void ArgValidation_IntTypeCheck_InvalidType_RaisesLuaError()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        Lua["player"] = player;

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("player:TakeDamage('not a number')"));
        Assert.That(ex!.Message, Does.Contain("number"));
    }

    [Test]
    public void ArgValidation_StringTypeCheck_NilArg_RaisesLuaError()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        Lua["player"] = player;

        // Act
        // TakeDamage expects int; pass a table instead

        // Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("player:TakeDamage({})"));
        Assert.That(ex!.Message, Does.Contain("number"));
    }

    [Test]
    public void ArgValidation_Constructor_TooFewArgs_RaisesLuaError()
    {
        // Arrange
        Lua["TestFactoryCustomName"] = typeof(TestFactoryCustomName);

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() =>
            Lua.Evaluate("return TestFactoryCustomName.create(nil)"));

        Assert.That(ex!.Message, Does.Contain("string"));
    }
}
