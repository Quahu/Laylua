namespace Laylua.Tests;

public class LuaTableValuesTests : LuaTestBase
{
    [Test]
    public void GetEnumerator_YieldsCorrectValues()
    {
        // Arrange
        using var table = Lua.Evaluate<LuaTable>("return { a = 1, b = 2, c = 3 }")!;
        var values = new List<int>();

        // Act
        foreach (var value in table.Values)
        {
            values.Add(value.GetValue<int>()!);
        }

        // Assert
        Assert.That(values, Is.EquivalentTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void ToArray_YieldsCorrectValues()
    {
        // Arrange
        using var table = Lua.Evaluate<LuaTable>("return { a = 1, b = 2, c = 3 }")!;

        // Act
        var values = table.Values.ToArray<int>();

        // Assert
        Assert.That(values, Is.EquivalentTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void ToEnumerable_YieldsCorrectValues()
    {
        // Arrange
        using var table = Lua.Evaluate<LuaTable>("return { a = 1, b = 2, c = 3 }")!;

        // Act
        var values = table.Values.AsEnumerable<int>();

        // Assert
        Assert.That(values, Is.EquivalentTo(new[] { 1, 2, 3 }));
    }
}
