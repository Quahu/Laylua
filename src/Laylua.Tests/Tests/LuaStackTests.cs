using NUnit.Framework;

namespace Laylua.Tests;

public class LuaStackTests : LuaTestBase
{
    [Test]
    public void LuaStackWith3Integers_Insert1Integer_CorrectStack()
    {
        using var _ = lua.Stack.SnapshotCount();

        // Arrange
        lua.Stack.Push(1);
        lua.Stack.Push(2);
        lua.Stack.Push(3);

        // Act
        lua.Stack.Insert(1, 4);

        // Assert
        Assert.That(lua.Stack.Count, Is.EqualTo(4));

        var expectedValues = new[] { 4, 1, 2, 3 };
        for (var i = 1; i <= lua.Stack.Count; i++)
        {
            var actualValue = lua.Stack[i].GetValue<int>();
            var expectedValue = expectedValues[i - 1];

            Assert.That(actualValue, Is.EqualTo(expectedValue));
        }
    }

    [Test]
    public void LuaStackWith3Integers_GetRange_CorrectRange()
    {
        using var _ = lua.Stack.SnapshotCount();

        // Arrange
        lua.Stack.Push(1);
        lua.Stack.Push(2);
        lua.Stack.Push(3);

        // Act
        var range = lua.Stack.GetRange(1);

        // Assert
        Assert.That(lua.Stack.Count, Is.EqualTo(3));
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
