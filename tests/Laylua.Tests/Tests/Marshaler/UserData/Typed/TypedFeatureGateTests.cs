namespace Laylua.Tests;

public unsafe class TypedFeatureGateTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
    }

    [Test]
    public void TypedGate_NoProperties_PropertyNotAccessible()
    {
        // Arrange
        var obj = new TestGateNoProperties();
        TestGateNoProperties.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act & Assert
        Lua.Execute("assert(obj.Value == nil)");
    }

    [Test]
    public void TypedGate_NoProperties_MethodStillWorks()
    {
        // Arrange
        var obj = new TestGateNoProperties();
        TestGateNoProperties.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act
        var value = Lua.Evaluate<int>("return obj:GetValue()");

        // Assert
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void TypedGate_NoMethods_MethodNotAccessible()
    {
        // Arrange
        var obj = new TestGateNoMethods();
        TestGateNoMethods.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act & Assert
        Lua.Execute("assert(obj.GetValue == nil)");
    }

    [Test]
    public void TypedGate_NoMethods_PropertyStillWorks()
    {
        // Arrange
        var obj = new TestGateNoMethods();
        TestGateNoMethods.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act
        var value = Lua.Evaluate<int>("return obj.Value");

        // Assert
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void TypedGate_NoFields_FieldNotAccessible()
    {
        // Arrange
        var obj = new TestGateNoFields();
        TestGateNoFields.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act & Assert
        Lua.Execute("assert(obj.Field == nil)");
    }

    [Test]
    public void TypedGate_NoFields_PropertyStillWorks()
    {
        // Arrange
        var obj = new TestGateNoFields();
        TestGateNoFields.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act
        var value = Lua.Evaluate<int>("return obj.Value");

        // Assert
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void TypedGate_NoEvents_EventNotAccessible()
    {
        // Arrange
        var obj = new TestEventNoEventsFeature();
        TestEventNoEventsFeature.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act & Assert
        Lua.Execute("assert(obj.SimpleEvent == nil)");
    }

    [Test]
    public void TypedGate_NoEvents_PropertyStillWorks()
    {
        // Arrange
        var obj = new TestEventNoEventsFeature
        {
            Value = 42
        };

        TestEventNoEventsFeature.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act
        var value = Lua.Evaluate<int>("return obj.Value");

        // Assert
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void TypedGate_PropertiesOnly_OnlyPropertiesAccessible()
    {
        // Arrange
        var obj = new TestGatePropertiesOnly();
        TestGatePropertiesOnly.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act
        var value = Lua.Evaluate<int>("return obj.Value");

        // Assert
        Assert.That(value, Is.EqualTo(42));
        Lua.Execute("assert(obj.GetValue == nil)");
        Lua.Execute("assert(obj.TestEvent == nil)");
    }

    [Test]
    public void TypedGate_MethodsOnly_OnlyMethodsAccessible()
    {
        // Arrange
        var obj = new TestGateMethodsOnly();
        TestGateMethodsOnly.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act
        Lua.Execute("assert(obj.Value == nil)");
        var value = Lua.Evaluate<int>("return obj:GetValue()");

        // Assert
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void TypedGate_NoFeatures_NothingAccessible()
    {
        // Arrange
        var obj = new TestNoFeatures();
        TestNoFeatures.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act & Assert
        Lua.Execute("assert(obj.Value == nil)");
    }

    [Test]
    public void TypedGate_NoConstants_ConstantNotAccessible()
    {
        // Arrange
        Lua["TestGateNoConstants"] = typeof(TestGateNoConstants);

        // Act & Assert
        Lua.Execute("assert(TestGateNoConstants.ConstantValue == nil)");
    }

    [Test]
    public void TypedGate_NoConstants_StaticPropertyStillWorks()
    {
        // Arrange
        Lua["TestGateNoConstants"] = typeof(TestGateNoConstants);

        // Act
        var value = Lua.Evaluate<int>("return TestGateNoConstants.StaticValue");

        // Assert
        Assert.That(value, Is.EqualTo(7));
    }
}
