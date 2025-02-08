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
        var values = table.Sequence.AsEnumerable<string>();

        // Assert
        Assert.That(values, Is.EqualTo(new[] { "one", "two", "three" }));
    }

    [Test]
    public void Add_EmptyTable_AddsValue()
    {
        // Arrange
        var table = Lua.CreateTable();

        // Act
        table.Sequence.Add(42);

        // Assert
        Assert.That(table.Sequence.ToArray<int>(), Is.EqualTo(new[] { 42 }));
    }

    [Test]
    public void Add_ThreeElementSequence_AddsValue()
    {
        // Arrange
        var table = Lua.CreateTable();
        table.SetValue(1, 1);
        table.SetValue(2, 2);
        table.SetValue(3, 3);

        // Act
        table.Sequence.Add(42);

        // Assert
        Assert.That(table.Sequence.ToArray<int>(), Is.EqualTo(new[] { 1, 2, 3, 42 }));
    }

    [Test]
    public void Insert_EmptyTable_InsertsAtTheEnd()
    {
        // Arrange
        var table = Lua.CreateTable();

        // Act
        table.Sequence.Insert(1, 42);

        // Assert
        Assert.That(table.Sequence.ToArray<int>(), Is.EqualTo(new[] { 42 }));
    }

    [Test]
    public void Insert_ThreeElementSequence_InsertsAtTheEnd()
    {
        // Arrange
        var table = Lua.CreateTable();
        table.SetValue(1, 1);
        table.SetValue(2, 2);
        table.SetValue(3, 3);

        // Act
        table.Sequence.Insert(4, 42);

        // Assert
        Assert.That(table.Sequence.ToArray<int>(), Is.EqualTo(new[] { 1, 2, 3, 42 }));
    }

    [Test]
    public void Insert_IndexOneOfThreeElementSequence_InsertsAtIndexOne()
    {
        // Arrange
        var table = Lua.CreateTable();
        table.SetValue(1, 1);
        table.SetValue(2, 2);
        table.SetValue(3, 3);

        // Act
        table.Sequence.Insert(1, 42);

        // Assert
        Assert.That(table.Sequence.ToArray<int>(), Is.EqualTo(new[] { 42, 1, 2, 3 }));
    }

    [Test]
    public void Insert_IndexTwoOfThreeElementSequence_InsertsAtIndexTwo()
    {
        // Arrange
        var table = Lua.CreateTable();
        table.SetValue(1, 1);
        table.SetValue(2, 2);
        table.SetValue(3, 3);

        // Act
        table.Sequence.Insert(2, 42);

        // Assert
        Assert.That(table.Sequence.ToArray<int>(), Is.EqualTo(new[] { 1, 42, 2, 3 }));
    }

    [Test]
    public void Remove_IndexOneOfThreeElementSequence_RemovesAndShifts()
    {
        // Arrange
        var table = Lua.CreateTable();
        table.SetValue(1, 1);
        table.SetValue(2, 2);
        table.SetValue(3, 3);

        // Act
        table.Sequence.RemoveAt(1);

        // Assert
        Assert.That(table.Sequence.ToArray<int>(), Is.EqualTo(new[] { 2, 3 }));
    }

    [Test]
    public void Remove_IndexTwoOfThreeElementSequence_RemovesAndShifts()
    {
        // Arrange
        var table = Lua.CreateTable();
        table.SetValue(1, 1);
        table.SetValue(2, 2);
        table.SetValue(3, 3);

        // Act
        table.Sequence.RemoveAt(2);

        // Assert
        Assert.That(table.Sequence.ToArray<int>(), Is.EqualTo(new[] { 1, 3 }));
    }

    [Test]
    public void RemoveAt_IndexThreeOfThreeElementSequence_RemovesAndShifts()
    {
        // Arrange
        var table = Lua.CreateTable();
        table.SetValue(1, 1);
        table.SetValue(2, 2);
        table.SetValue(3, 3);

        // Act
        table.Sequence.RemoveAt(3);

        // Assert
        Assert.That(table.Sequence.ToArray<int>(), Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void Clear_ThreeElementSequence_ClearsTable()
    {
        // Arrange
        var table = Lua.CreateTable();
        table.SetValue(1, 1);
        table.SetValue(2, 2);
        table.SetValue(3, 3);

        // Act
        table.Sequence.Clear();

        // Assert
        Assert.That(table.Sequence.ToArray<int>(), Is.Empty);
    }

    [Test]
    public void Clear_ThreeElementSequenceAndThreeOtherKeys_ClearsSequenceOnly()
    {
        // Arrange
        var table = Lua.CreateTable();
        table.SetValue(1, 1);
        table.SetValue(2, 2);
        table.SetValue(3, 3);

        table.SetValue("a", 1);
        table.SetValue("b", 2);
        table.SetValue("c", 3);

        // Act
        table.Sequence.Clear();

        // Assert
        Assert.That(table.Sequence.ToArray<int>(), Is.Empty);
        Assert.That(table.ToDictionary<string, int>(), Is.EquivalentTo(new Dictionary<string, int>()
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3
        }));
    }
}
