namespace Laylua.Tests;

public class LuaStackTests : LuaTestBase
{
    [Test]
    public void LuaStackWith3Integers_Insert1Integer_CorrectStack()
    {
        using var _ = Lua.Stack.SnapshotCount();

        // Arrange
        Lua.Stack.Push(1);
        Lua.Stack.Push(2);
        Lua.Stack.Push(3);

        // Act
        Lua.Stack.Insert(1, 4);

        // Assert
        Assert.That(Lua.Stack.Count, Is.EqualTo(4));

        var expectedValues = new[] { 4, 1, 2, 3 };
        for (var i = 1; i <= Lua.Stack.Count; i++)
        {
            var actualValue = Lua.Stack[i].GetValue<int>();
            var expectedValue = expectedValues[i - 1];

            Assert.That(actualValue, Is.EqualTo(expectedValue));
        }
    }

    [Test]
    public void LuaStackWith3Integers_GetRange_CorrectRange()
    {
        using var _ = Lua.Stack.SnapshotCount();

        // Arrange
        Lua.Stack.Push(1);
        Lua.Stack.Push(2);
        Lua.Stack.Push(3);

        // Act
        var range = Lua.Stack.GetRange(1);

        // Assert
        Assert.That(Lua.Stack.Count, Is.EqualTo(3));
        Assert.That(range.IsEmpty, Is.False);
        Assert.That(range.Count, Is.EqualTo(3));

        for (var i = 1; i <= range.Count; i++)
        {
            var actualValue = range[i].GetValue<int>();
            var expectedValue = i;

            Assert.That(actualValue, Is.EqualTo(expectedValue));
        }
    }
}
