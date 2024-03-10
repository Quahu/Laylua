namespace Laylua.Tests;

public class LuaTableSequenceTests : LuaTestBase
{
    [Test]
    public void GetEnumerator_YieldsCorrectValues()
    {
        // Arrange
        using var table = Lua.Evaluate<LuaTable>("return { 'one', 'two', 'three', four = 'four' }")!;
        var keys = new List<long>();
        var values = new List<object>();

        // Act
        using (var enumerator = table.Sequence.GetEnumerator())
        {
            for (var i = 0; i < 3; i++)
            {
                Assert.That(enumerator.MoveNext(), $"{typeof(LuaTable.SequenceCollection.Enumerator)} yielded too few key/value pairs.");

                var current = enumerator.Current;
                keys.Add(current.Key);
                values.Add(current.Value.GetValue<object>()!);
            }

            Assert.That(!enumerator.MoveNext(), $"{typeof(LuaTable.SequenceCollection.Enumerator)} yielded too many key/value pairs.");
        }

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(keys, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(values, Is.EqualTo(new[] { "one", "two", "three" }));
        });
    }

    [Test]
    public void ToArray_YieldsCorrectValues()
    {
        // Arrange
        using var table = Lua.Evaluate<LuaTable>("return { 'one', 'two', 'three', four = 'four' }")!;

        // Act
        var values = table.Sequence.ToArray<string>();

        // Assert
        Assert.That(values, Is.EqualTo(new[] { "one", "two", "three" }));
    }

    [Test]
    public void ToEnumerable_YieldsCorrectValues()
    {
        // Arrange
        using var table = Lua.Evaluate<LuaTable>("return { 'one', 'two', 'three', four = 'four' }")!;

        // Act
        var values = table.Sequence.ToEnumerable<string>();

        // Assert
        Assert.That(values, Is.EqualTo(new[] { "one", "two", "three" }));
    }
}
