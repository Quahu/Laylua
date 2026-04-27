namespace Laylua.Tests;

public class StructNullabilityTests : LuaTestBase
{
    [Test]
    public void StructParam_Strict_NilThrowsError()
    {
        // Arrange
        var obj = new TestStructNullability
        {
            Value = 10
        };

        Lua["obj"] = obj;

        // Act
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:Add(nil)"));

        // Assert
        Assert.That(ex!.Message, Does.Contain("cannot be nil"));
    }

    [Test]
    public void StructParam_Strict_ValueWorks()
    {
        // Arrange
        var obj = new TestStructNullability
        {
            Value = 10
        };

        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<int>("return obj:Add(5)");

        // Assert
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void StructParam_None_NilUsesDefault()
    {
        // Arrange
        var obj = new TestStructNullabilityNone
        {
            Value = 10
        };

        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<int>("return obj:Add(nil)");

        // Assert
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void StructParam_None_ValueWorks()
    {
        // Arrange
        var obj = new TestStructNullabilityNone
        {
            Value = 10
        };

        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<int>("return obj:Add(5)");

        // Assert
        Assert.That(result, Is.EqualTo(15));
    }
}
