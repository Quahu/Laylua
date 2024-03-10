namespace Laylua.Tests;

public class ExceptionTests : LuaTestBase
{
    [Test]
    public void Execute_IndexingNilGlobal_ThrowsLuaException()
    {
        // Act
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("return nothing.something"))!;

        // Assert
        Assert.That(ex.Message, Does.EndWith("(global 'nothing')"));
    }

    [Test]
    public void Execute_IndexingNilField_ThrowsLuaException()
    {
        // Act
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("something = {} return something.nothing.something"))!;

        // Assert
        Assert.That(ex.Message, Does.EndWith("(field 'nothing')"));
    }

    [Test]
    public void Execute_IndexingNilLocal_ThrowsLuaException()
    {
        // Act
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("local nothing = nil return nothing.something"))!;

        // Assert
        Assert.That(ex.Message, Does.EndWith("(local 'nothing')"));
    }
}
