using System.Collections.Generic;
using NUnit.Framework;

namespace Laylua.Tests
{
    public class LuaTableValuesTests : LuaFixture
    {
        [Test]
        public void GetEnumerator_YieldsCorrectValues()
        {
            // Arrange
            lua.Execute("table = { a = 1, b = 2, c = 3 }");
            using var table = lua.GetGlobal<LuaTable>("table");
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
            lua.Execute("table = { a = 1, b = 2, c = 3 }");
            using var table = lua.GetGlobal<LuaTable>("table");

            // Act
            var values = table.Values.ToArray<int>();

            // Assert
            Assert.That(values, Is.EquivalentTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void ToEnumerable_YieldsCorrectValues()
        {
            // Arrange
            lua.Execute("table = { a = 1, b = 2, c = 3 }");
            using var table = lua.GetGlobal<LuaTable>("table");

            // Act
            var values = table.Values.ToEnumerable<int>();

            // Assert
            Assert.That(values, Is.EquivalentTo(new[] { 1, 2, 3 }));
        }
    }
}
