using System.Reflection;

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

    [Test]
    public void UnwrapException_LuaExceptionWithoutNestedException_ReturnsLuaException()
    {
        // Arrange
        var ex = MakeLuaException("root", innerException: null);

        // Act
        var unwrapped = ex.UnwrapException();

        // Assert
        Assert.That(unwrapped, Is.EqualTo(ex));
    }

    [Test]
    public void UnwrapException_LuaExceptionWithNestedException_ReturnsNestedException()
    {
        // Arrange
        var ex = MakeLuaException("root", new Exception("nested"));

        // Act
        var unwrapped = ex.UnwrapException();

        // Assert
        Assert.That(unwrapped, Is.TypeOf<Exception>().And.Message.EqualTo("nested"));
    }

    [Test]
    public void UnwrapException_LuaPanicExceptionWithNestedLuaExceptionWithNestedException_ReturnsNestedException()
    {
        // Arrange
        var ex = MakeLuaException("root", MakeLuaException("nested", new Exception("nested"), isPanic: true));

        // Act
        var unwrapped = ex.UnwrapException();

        // Assert
        Assert.That(unwrapped, Is.TypeOf<Exception>().And.Message.EqualTo("nested"));
    }

    private static LuaException MakeLuaException(string message, Exception? innerException, bool isPanic = false)
    {
        return (LuaException) Activator.CreateInstance(isPanic ? typeof(LuaPanicException) : typeof(LuaException), BindingFlags.Instance | BindingFlags.NonPublic, binder: null, args: [message, innerException], culture: null)!;
    }
}
