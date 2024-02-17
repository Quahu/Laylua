using System.Collections.Generic;
using NUnit.Framework;

namespace Laylua.Tests
{
    public class LuaTableTests : LuaTestBase
    {
        [Test]
        public void IsEmpty_EmptyTable_ReturnsTrue()
        {
            // Arrange
            lua.Execute("table = { }");
            using var table = lua.GetGlobal<LuaTable>("table");

            // Act
            var isEmpty = table.IsEmpty;

            // Assert
            Assert.That(isEmpty, Is.True);
        }

        [Test]
        public void IsEmpty_NonEmptyTable_ReturnsFalse()
        {
            // Arrange
            lua.Execute("table = { 'one', 'two', 'three' }");
            using var table = lua.GetGlobal<LuaTable>("table");

            // Act
            var isEmpty = table.IsEmpty;

            // Assert
            Assert.That(isEmpty, Is.False);
        }

        [Test]
        public void ToEnumerable_YieldsValidKeyValuePairs()
        {
            // Arrange
            lua.Execute("table = { 'one', 'two', 'three' }");
            using var table = lua.GetGlobal<LuaTable>("table");

            // Act
            var kvps = table.ToEnumerable<int, string>();

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
            lua.Execute("table = { one = 1, two = 2, three = 3 }");
            using var table = lua.GetGlobal<LuaTable>("table");

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
            lua.Execute("table = { 'one', 'two', 'three' }");
            using var table = lua.GetGlobal<LuaTable>("table");

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
            lua.Execute("table = { 1, [true] = true, three = 'three' }");
            using var table = lua.GetGlobal<LuaTable>("table");

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
            using var table = lua.CreateTable();

            // Act
            table.SetValue("Value", 1);
            var value = table.GetValue<string, int>("Value");

            // Assert
            Assert.That(value, Is.EqualTo(1));
        }

        [Test]
        public void Count_ReturnsValidCount()
        {
            // Arrange
            lua.Execute("table = { 1, [true] = true, three = 'three' }");
            using var table = lua.GetGlobal<LuaTable>("table");

            // Act
            var count = table.Count();

            // Assert
            Assert.That(count, Is.EqualTo(3));
        }

        [Test]
        public void ContainsKey_ReturnsTrueForValidKeys()
        {
            // Arrange
            lua.Execute("table = { 1, [true] = true, three = 'three' }");
            using var table = lua.GetGlobal<LuaTable>("table");

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
            lua.Execute("table = { 1, [true] = true, three = 'three' }");
            using var table = lua.GetGlobal<LuaTable>("table");

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
            lua.Execute("table = { 1, [true] = true, three = 'three' }");
            using var table = lua.GetGlobal<LuaTable>("table");

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
            lua.Execute("table = { 1, [true] = true, three = 'three' }");
            using var table = lua.GetGlobal<LuaTable>("table");

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
            lua.Execute("""
                source = { 1, [true] = true, three = 'three' }
                target = { }
                """);

            using var source = lua.GetGlobal<LuaTable>("source");
            using var target = lua.GetGlobal<LuaTable>("target");

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
            lua.Execute("table = { 'one', 'two', 'three' }");
            using var table = lua.GetGlobal<LuaTable>("table");
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
}
