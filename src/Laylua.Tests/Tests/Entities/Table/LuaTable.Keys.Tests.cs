using System.Collections.Generic;
using NUnit.Framework;

namespace Laylua.Tests
{
    public class LuaTableKeysTests : LuaFixture
    {
        [Test]
        public void GetEnumerator_YieldsCorrectKeys()
        {
            // Arrange
            lua.Execute("table = { a = 1, b = 2, c = 3 }");
            using var table = lua.GetGlobal<LuaTable>("table");
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
            lua.Execute("table = { a = 1, b = 2, c = 3 }");
            using var table = lua.GetGlobal<LuaTable>("table");

            // Act
            var keys = table.Keys.ToArray<string>();

            // Assert
            Assert.That(keys, Is.EquivalentTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void ToEnumerable_YieldsCorrectKeys()
        {
            // Arrange
            lua.Execute("table = { a = 1, b = 2, c = 3 }");
            using var table = lua.GetGlobal<LuaTable>("table");

            // Act
            var keys = table.Keys.ToEnumerable<string>();

            // Assert
            Assert.That(keys, Is.EquivalentTo(new[] { "a", "b", "c" }));
        }
    }
}
