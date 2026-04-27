namespace Laylua.Tests;

public class TypedUserDataArgumentValidationTests : LuaTestBase
{
    [Test]
    public void Strict_NonNullableParam_RejectsNil()
    {
        // Arrange
        var obj = new TestStrictValidation();
        Lua["obj"] = obj;

        // Act
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:Greet(nil)"));

        // Assert
        Assert.That(ex!.Message, Does.Contain("cannot be nil"));
    }

    [Test]
    public void Strict_NonNullableParam_AcceptsValue()
    {
        // Arrange
        var obj = new TestStrictValidation();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:Greet('Steve')");

        // Assert
        Assert.That(result, Is.EqualTo("Hello, Steve!"));
    }

    [Test]
    public void Strict_NullableParam_AcceptsNil()
    {
        // Arrange
        var obj = new TestStrictValidation();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:GreetOptional(nil)");

        // Assert
        Assert.That(result, Is.EqualTo("Hello, World!"));
    }

    [Test]
    public void Strict_NullableParam_AcceptsValue()
    {
        // Arrange
        var obj = new TestStrictValidation();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:GreetOptional('Alex')");

        // Assert
        Assert.That(result, Is.EqualTo("Hello, Alex!"));
    }

    [Test]
    public void None_NonNullableParam_StillFailsOnNil()
    {
        // Arrange
        var obj = new TestNoValidation();
        Lua["obj"] = obj;

        // Act
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:Greet(nil)"));

        // Assert
        Assert.That(ex!.Message, Does.Contain("string expected"));
        Assert.That(ex.Message, Does.Not.Contain("cannot be nil"));
    }

    [Test]
    public void None_NonNullableParam_AcceptsValue()
    {
        // Arrange
        var obj = new TestNoValidation();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:Greet('Steve')");

        // Assert
        Assert.That(result, Is.EqualTo("Hello, Steve!"));
    }

    [Test]
    public void NullabilityOnly_NonNullableParam_RejectsNil()
    {
        // Arrange
        var obj = new TestNullabilityOnlyValidation();
        Lua["obj"] = obj;

        // Act
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:Greet(nil)"));

        // Assert
        Assert.That(ex!.Message, Does.Contain("cannot be nil"));
    }

    [Test]
    public void NullabilityOnly_NullableParam_AcceptsNil()
    {
        // Arrange
        var obj = new TestNullabilityOnlyValidation();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:GreetOptional(nil)");

        // Assert
        Assert.That(result, Is.EqualTo("Hello, World!"));
    }

    [Test]
    public void NullabilityOnly_NonNullableParam_AcceptsValue()
    {
        // Arrange
        var obj = new TestNullabilityOnlyValidation();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:Greet('Steve')");

        // Assert
        Assert.That(result, Is.EqualTo("Hello, Steve!"));
    }
}
