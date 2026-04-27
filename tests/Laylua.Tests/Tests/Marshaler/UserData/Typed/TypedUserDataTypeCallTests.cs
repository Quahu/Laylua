namespace Laylua.Tests;

public class TypedUserDataTypeCallTests : LuaTestBase
{
    [Test]
    public void TypeCall_IntegerOverload_ReturnsComputedValue()
    {
        // Arrange
        var obj = new TestCallable
        {
            BaseValue = 10
        };

        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<int>("return obj(5)");

        // Assert
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void TypeCall_TwoIntegerArgs_ReturnsSum()
    {
        // Arrange
        var obj = new TestCallable
        {
            BaseValue = 10
        };

        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<int>("return obj(3, 7)");

        // Assert
        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void TypeCall_StringOverload_ReturnsFormattedString()
    {
        // Arrange
        var obj = new TestCallable
        {
            BaseValue = 42
        };

        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj('Value: ')");

        // Assert
        Assert.That(result, Is.EqualTo("Value: 42"));
    }

    [Test]
    public void TypeCall_MethodsNotExposedAsRegular()
    {
        // Arrange
        var obj = new TestCallable
        {
            BaseValue = 10
        };

        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:Compute(5)"));
    }

    [Test]
    public void TypeCall_WrongArgCount_Errors()
    {
        // Arrange
        var obj = new TestCallable
        {
            BaseValue = 10
        };

        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj()"));
    }
}
