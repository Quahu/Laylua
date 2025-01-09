namespace Laylua.Tests;

public class LuaTableTests : LuaTestBase
{
    [Test]
    public void IsEmpty_EmptyTable_ReturnsTrue()
    {
        // Arrange
        using var table = Lua.Evaluate<LuaTable>("return { }")!;

        // Act
        var isEmpty = table.IsEmpty;

        // Assert
        Assert.That(isEmpty, Is.True);
    }

    [Test]
    public void IsEmpty_NonEmptyTable_ReturnsFalse()
    {
        // Arrange
        using var table = Lua.Evaluate<LuaTable>("return { 'one', 'two', 'three' }")!;

        // Act
        var isEmpty = table.IsEmpty;

        // Assert
        Assert.That(isEmpty, Is.False);
    }

    [Test]
    public void ToEnumerable_YieldsValidKeyValuePairs()
    {
        // Arrange
        using var table = Lua.Evaluate<LuaTable>("return { 'one', 'two', 'three' }")!;

        // Act
        var kvps = table.AsEnumerable<int, string>();

        // Assert
        Assert.That(kvps, Is.EquivalentTo(new Dictionary<int, string>
        {
            [1] = "one",
            [2] = "two",
            [3] = "three"
        }));
    }

    [Test]
    public void ToRecordDictionary_ReturnsValidKeyValuePairsDictionary()
    {
        // Arrange
        using var table = Lua.Evaluate<LuaTable>("return { one = 1, two = 2, three = 3 }")!;

        // Act
        var dictionary = table.ToRecordDictionary<int>();

        // Assert
        Assert.That(dictionary, Is.EquivalentTo(new Dictionary<string, int>
        {
            ["one"] = 1,
            ["two"] = 2,
            ["three"] = 3
        }));
    }

    [Test]
    public void ToDictionary_ReturnsValidKeyValuePairsDictionary()
    {
        // Arrange
        using var table = Lua.Evaluate<LuaTable>("return { 'one', 'two', 'three' }")!;

        // Act
        var dictionary = table.ToDictionary<int, string>();

        // Assert
        Assert.That(dictionary, Is.EquivalentTo(new Dictionary<int, string>
        {
            [1] = "one",
            [2] = "two",
            [3] = "three"
        }));
    }

    [Test]
    public void IndexerGet_YieldsValidValues()
    {
        // Arrange
        using var table = Lua.Evaluate<LuaTable>("return { 1, [true] = true, three = 'three' }")!;

        // Act
        var value1 = table[1];
        var value2 = table[true];
        var value3 = table["three"];

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(value1, Is.EqualTo(1));
            Assert.That(value2, Is.EqualTo(true));
            Assert.That(value3, Is.EqualTo("three"));
        });
    }

    [Test]
    public void SetAndGetValue_StringInt_ReturnsValidValue()
    {
        // Arrange
        using var table = Lua.CreateTable();

        // Act
        table.SetValue("Value", 1);
        var value = table.GetValue<string, int>("Value");

        // Assert
        Assert.That(value, Is.EqualTo(1));
    }

    [Test]
    public void GetValueOrDefault_MissingNullableInt_ReturnsNull()
    {
        // Arrange
        using var table = Lua.CreateTable();

        // Act
        table.SetValue("MissingValue", (int?) null);
        var missingValue = table.GetValueOrDefault<string, int?>("MissingValue");

        // Assert
        Assert.That(missingValue, Is.Null);
    }

    [Test]
    public void GetValueOrDefault_MissingNullableInt_DefaultValue_ReturnsDefaultValue()
    {
        // Arrange
        using var table = Lua.CreateTable();

        // Act
        table.SetValue("MissingValue", (int?) null);
        var missingValue = table.GetValueOrDefault<string, int?>("MissingValue", 42);

        // Assert
        Assert.That(missingValue, Is.EqualTo(42));
    }

    [Test]
    public void Count_ReturnsValidCount()
    {
        // Arrange
        using var table = Lua.Evaluate<LuaTable>("return { 1, [true] = true, three = 'three' }")!;

        // Act
        var count = table.Count();

        // Assert
        Assert.That(count, Is.EqualTo(3));
    }

    [Test]
    public void ContainsKey_ReturnsTrueForValidKeys()
    {
        // Arrange
        using var table = Lua.Evaluate<LuaTable>("return { 1, [true] = true, three = 'three' }")!;

        // Act
        var hasKey1 = table.ContainsKey(1);
        var hasKey2 = table.ContainsKey(true);
        var hasKey3 = table.ContainsKey("three");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(hasKey1, Is.True);
            Assert.That(hasKey2, Is.True);
            Assert.That(hasKey3, Is.True);
        });
    }

    [Test]
    public void ContainsKey_ReturnsFalseForInvalidKeys()
    {
        // Arrange
        using var table = Lua.Evaluate<LuaTable>("return { 1, [true] = true, three = 'three' }")!;

        // Act
        var hasKey1 = table.ContainsKey(2);
        var hasKey2 = table.ContainsKey(false);
        var hasKey3 = table.ContainsKey("four");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(hasKey1, Is.False);
            Assert.That(hasKey2, Is.False);
            Assert.That(hasKey3, Is.False);
        });
    }

    [Test]
    public void ContainsValue_ReturnsTrueForValidValues()
    {
        // Arrange
        using var table = Lua.Evaluate<LuaTable>("return { 1, [true] = true, three = 'three' }")!;

        // Act
        var hasValue1 = table.ContainsValue(1);
        var hasValue2 = table.ContainsValue(true);
        var hasValue3 = table.ContainsValue("three");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(hasValue1, Is.True);
            Assert.That(hasValue2, Is.True);
            Assert.That(hasValue3, Is.True);
        });
    }

    [Test]
    public void ContainsValue_ReturnsFalseForInvalidValues()
    {
        // Arrange
        using var table = Lua.Evaluate<LuaTable>("return { 1, [true] = true, three = 'three' }")!;

        // Act
        var hasValue1 = table.ContainsValue(2);
        var hasValue2 = table.ContainsValue(false);
        var hasValue3 = table.ContainsValue("four");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(hasValue1, Is.False);
            Assert.That(hasValue2, Is.False);
            Assert.That(hasValue3, Is.False);
        });
    }

    [Test]
    public void CopyTo_CopiesElementsSuccessfully()
    {
        // Arrange
        Lua.Execute("""
                    source = { 1, [true] = true, three = 'three' }
                    target = { }
                    """);

        using var source = Lua.GetGlobal<LuaTable>("source");
        using var target = Lua.GetGlobal<LuaTable>("target");

        // Act
        source.CopyTo(target);
        var value1 = target.GetValue<int, int>(1);
        var value2 = target.GetValue<bool, bool>(true);
        var value3 = target.GetValue<string, string>("three");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(value1, Is.EqualTo(1));
            Assert.That(value2, Is.EqualTo(true));
            Assert.That(value3, Is.EqualTo("three"));
        });
    }

    [Test]
    public void EnumeratePairs_YieldsValidKeyValuePairs()
    {
        // Arrange
        using var table = Lua.Evaluate<LuaTable>("return { 'one', 'two', 'three' }")!;
        var keys = new List<object>();
        var values = new List<object>();

        // Act
        using (var enumerator = table.GetEnumerator())
        {
            for (var i = 0; i < 3; i++)
            {
                Assert.That(enumerator.MoveNext(), $"{typeof(LuaTable.Enumerator)} yielded too few key/value pairs.");

                var current = enumerator.Current;
                keys.Add(current.Key.GetValue<object>()!);
                values.Add(current.Value.GetValue<object>()!);
            }

            Assert.That(!enumerator.MoveNext(), $"{typeof(LuaTable.Enumerator)} yielded too many key/value pairs.");
        }

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(keys, Is.EquivalentTo(new[] { 1, 2, 3 }));
            Assert.That(values, Is.EquivalentTo(new[] { "one", "two", "three" }));
        });
    }
}
