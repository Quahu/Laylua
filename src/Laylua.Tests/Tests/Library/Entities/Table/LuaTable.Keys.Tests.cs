namespace Laylua.Tests;

public class LuaTableKeysTests : LuaTestBase
{
    [Test]
    public void GetEnumerator_YieldsCorrectKeys()
    {
        // Arrange
        using var table = Lua.Evaluate<LuaTable>("return { a = 1, b = 2, c = 3 }")!;
        var keys = new List<string>();

        // Act
        foreach (var key in table.Keys)
        {
            keys.Add(key.GetValue<string>()!);
        }

        // Assert
        Assert.That(keys, Is.EquivalentTo(new[] { "a", "b", "c" }));
    }

    [Test]
    public void ToArray_YieldsCorrectKeys()
    {
        // Arrange
        using var table = Lua.Evaluate<LuaTable>("return { a = 1, b = 2, c = 3 }")!;

        // Act
        var keys = table.Keys.ToArray<string>();

        // Assert
        Assert.That(keys, Is.EquivalentTo(new[] { "a", "b", "c" }));
    }

    [Test]
    public void ToEnumerable_YieldsCorrectKeys()
    {
        // Arrange
        using var table = Lua.Evaluate<LuaTable>("return { a = 1, b = 2, c = 3 }")!;

        // Act
        var keys = table.Keys.ToEnumerable<string>();

        // Assert
        Assert.That(keys, Is.EquivalentTo(new[] { "a", "b", "c" }));
    }
}
