using System;
using System.Collections.Generic;
using Laylua.Moon;
using NUnit.Framework;

namespace Laylua.Tests
{
    public class TableTests : LuaFixture
    {
        [Test]
        public void TableIndexer()
        {
            lua.Execute("table = { 1, [true] = true, three = '3' }");

            using (var table = lua.GetGlobal<LuaTable>("table")!)
            {
                table[4] = "four";

                var first = table[1];
                var second = table[true];
                var third = table["three"];
                var four = table[4];

                var actual = new[] { first, second, third, four };

                var expected = new object[] { 1, true, "3", "four" };
                CollectionAssert.AreEqual(expected, actual);
            }
        }

        [Test]
        public unsafe void CreateTable_None_CreatesTableInRegistry()
        {
            using (var table = lua.CreateTable())
            {
                lua_rawgeti(L, LuaRegistry.Index, LuaReference.GetReference(table));
                Assert.AreEqual(LuaType.Table, lua_type(L, -1));

                lua_pop(L);
            }
        }

        [Test]
        public void SetAndGetValue_StringInt_ReturnsValidValue()
        {
            using (var table = lua.CreateTable())
            {
                table.SetValue("Value", 1);

                var actual = table.GetValue<string, int>("Value");

                Assert.AreEqual(1, actual);
            }
        }

        [Test]
        public void Count_ValidKeys_ReturnsValidCount()
        {
            lua.Execute("table = { 1, [true] = true, three = '3' }");

            using (var table = lua.GetGlobal<LuaTable>("table")!)
            {
                Assert.AreEqual(3, table.Count());
            }
        }

        [Test]
        public void ContainsKey_ValidKeys_ReturnsTrue()
        {
            lua.Execute("table = { 1, [true] = true, three = '3' }");

            using (var table = lua.GetGlobal<LuaTable>("table")!)
            {
                Assert.IsTrue(table.ContainsKey(1));
                Assert.IsTrue(table.ContainsKey(true));
                Assert.IsTrue(table.ContainsKey("three"));
            }
        }

        [Test]
        public void ContainsKey_InvalidKeys_ReturnsFalse()
        {
            lua.Execute("table = { 1, [true] = true, three = '3' }");

            using (var table = lua.GetGlobal<LuaTable>("table")!)
            {
                Assert.IsFalse(table.ContainsKey(2));
                Assert.IsFalse(table.ContainsKey(false));
                Assert.IsFalse(table.ContainsKey("four"));
            }
        }

        [Test]
        public void ContainsValue_ValidValues_ReturnsTrue()
        {
            lua.Execute("table = { 1, [true] = true, three = '3' }");

            using (var table = lua.GetGlobal<LuaTable>("table")!)
            {
                Assert.IsTrue(table.ContainsValue(1));
                Assert.IsTrue(table.ContainsValue(true));
                Assert.IsTrue(table.ContainsValue("3"));
            }
        }

        [Test]
        public void ContainsValue_InvalidValues_ReturnsFalse()
        {
            lua.Execute("table = { 1, [true] = true, three = '3' }");

            using (var table = lua.GetGlobal<LuaTable>("table")!)
            {
                Assert.IsFalse(table.ContainsValue(2));
                Assert.IsFalse(table.ContainsValue(false));
                Assert.IsFalse(table.ContainsValue("4"));
            }
        }

        [Test]
        public void SetValue_SequenceValues_ReturnsCorrectLength()
        { }

        [Test]
        public void LuaTableCopyTo_LuaTableWithElements_CopiesElementsSuccessfully()
        {
            lua.Execute("table1 = { 1, [true] = true, three = '3' }");
            lua.Execute("table2 = { }");

            using (var table1 = lua.GetGlobal<LuaTable>("table1")!)
            using (var table2 = lua.GetGlobal<LuaTable>("table2")!)
            {
                table1.CopyTo(table2);

                var first = table2[1];
                var second = table2[true];
                var third = table2["three"];

                var actual = new[] { first, second, third };
                var expected = new object[] { 1, true, "3" };
                CollectionAssert.AreEqual(expected, actual);
            }
        }

        [Test]
        public void TableEnumerator()
        {
            lua.Execute("table = { 1, [true] = true, three = '3' }");

            using (var table = lua.GetGlobal<LuaTable>("table")!)
            {
                var actual = new object?[3];
                using (var enumerator = table.EnumeratePairs())
                {
                    for (var i = 0; i < 3; i++)
                    {
                        Assert.IsTrue(enumerator.MoveNext(), "The enumerator yielded too few key/value pairs.");

                        actual[i] = enumerator.Current.Value.GetValue<object>();
                    }

                    Assert.IsFalse(enumerator.MoveNext(), "The enumerator yielded too many key/value pairs.");

                    var expected = new object[] { 1, true, "3" };
                    CollectionAssert.AreEquivalent(expected, actual);
                }
            }
        }

        [Test]
        public void TableToArray()
        {
            lua.Execute("table = { 1, 2, 3, four = 4 }");

            using (var table = lua.GetGlobal<LuaTable>("table")!)
            {
                Assert.Throws<InvalidOperationException>(() => table.ToArray<int>(throwOnNonIntegerKeys: true));

                var actual = table.ToArray<int>();
                var expected = new object[] { 1, 2, 3 };
                CollectionAssert.AreEqual(expected, actual);
            }
        }

        [Test]
        public void TableToMixedDictionary()
        {
            lua.Execute("table = { 1, 2, 3, four = 4 }");

            using (var table = lua.GetGlobal<LuaTable>("table")!)
            {
                var actual = table.ToDictionary<object, object>();
                var expected = new Dictionary<object, object>
                {
                    [1] = 1,
                    [2] = 2,
                    [3] = 3,
                    ["four"] = 4
                };

                CollectionAssert.AreEquivalent(expected, actual);
            }
        }

        [Test]
        public void TableToRecordDictionary()
        {
            lua.Execute("table = { 1, 2, 3, four = 4 }");

            using (var table = lua.GetGlobal<LuaTable>("table")!)
            {
                Assert.Throws<InvalidOperationException>(() => table.ToRecordDictionary<object>(throwOnNonStringKeys: true));

                var actual = table.ToRecordDictionary<object>();
                var expected = new Dictionary<string, object>
                {
                    ["four"] = 4
                };

                CollectionAssert.AreEquivalent(expected, actual);
            }
        }

        [Test]
        public void TableToIntStringDictionary()
        {
            lua.Execute("table = { '1', '2', '3', {}, ['4'] = '4' }");

            using (var table = lua.GetGlobal<LuaTable>("table")!)
            {
                Assert.Throws<InvalidOperationException>(() => table.ToDictionary<int, string>(throwOnNonConvertible: true));

                var actual = table.ToDictionary<int, string>(throwOnNonConvertible: false);
                var expected = new Dictionary<int, string>
                {
                    [1] = "1",
                    [2] = "2",
                    [3] = "3",
                    [4] = "4"
                };

                CollectionAssert.AreEquivalent(expected, actual);
            }
        }

        [Test]
        public void TableToStringIntDictionary()
        {
            lua.Execute("table = { ['1'] = 1, ['2'] = 2, ['3'] = 3, {}, ['4'] = 4 }");

            using (var table = lua.GetGlobal<LuaTable>("table")!)
            {
                Assert.Throws<InvalidOperationException>(() => table.ToDictionary<string, int>(throwOnNonConvertible: true));

                var actual = table.ToDictionary<string, int>(throwOnNonConvertible: false);
                var expected = new Dictionary<string, int>
                {
                    ["1"] = 1,
                    ["2"] = 2,
                    ["3"] = 3,
                    ["4"] = 4
                };

                CollectionAssert.AreEquivalent(expected, actual);
            }
        }
    }
}
