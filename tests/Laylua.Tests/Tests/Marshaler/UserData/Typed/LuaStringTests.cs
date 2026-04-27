namespace Laylua.Tests;

public class LuaStringTests : LuaTestBase
{
    [Test]
    public void LuaStringParam_ReadOnlySpanByte_ParsesFromLuaString()
    {
        // Arrange
        var obj = new TestLuaStringParamUtf8();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<int>("return obj:ParseUtf8('42')");

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void LuaStringParam_ReadOnlySpanByte_EchoRoundTrips()
    {
        // Arrange
        var obj = new TestLuaStringParamUtf8();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:Echo('hello world')");

        // Assert
        Assert.That(result, Is.EqualTo("hello world"));
    }

    [Test]
    public void LuaStringProperty_ReadOnlySpanByte_PushedAsString()
    {
        // Arrange
        var obj = new TestLuaStringPropertyUtf8();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj.Utf8Name");

        // Assert
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void LuaStringParam_ReadOnlySpanChar_ParsesFromLuaString()
    {
        // Arrange
        var obj = new TestLuaStringParamUtf16();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<int>("return obj:ParseUtf16('42')");

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void LuaStringParam_ReadOnlySpanChar_EchoRoundTrips()
    {
        // Arrange
        var obj = new TestLuaStringParamUtf16();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:Echo('hello world')");

        // Assert
        Assert.That(result, Is.EqualTo("hello world"));
    }

    [Test]
    public void LuaStringProperty_ReadOnlySpanChar_PushedAsString()
    {
        // Arrange
        var obj = new TestLuaStringPropertyUtf16();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj.Utf16Name");

        // Assert
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void LuaStringProperty_RegularStringStillWorks()
    {
        // Arrange
        var obj = new TestLuaStringPropertyUtf8();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj.Name");

        // Assert
        Assert.That(result, Is.EqualTo("world"));
    }

    [Test]
    public void LuaStringConstructor_ReadOnlySpanChar_ConstructsFromLuaString()
    {
        // Arrange
        Lua["TestLuaStringConstructorUtf16"] = typeof(TestLuaStringConstructorUtf16);

        // Act
        var result = Lua.Evaluate<TestLuaStringConstructorUtf16>("return TestLuaStringConstructorUtf16('hello world')");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Text, Is.EqualTo("hello world"));
    }
}
