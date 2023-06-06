using System;
using System.Reflection;
using Laylua.Marshaling;
using Laylua.Moon;
using NUnit.Framework;

namespace Laylua.Tests
{
    [Order(-1)]
    public class LibraryTests : LuaFixture
    {
        [Test]
        public void OpenCloseMathLibrary()
        {
            var result = lua.OpenLibrary(LuaLibraries.Standard.Math);
            Assert.IsTrue(result);
            var pi = lua.Evaluate<double>("return math.pi");
            Assert.AreEqual(Math.PI, pi);

            result = lua.CloseLibrary(LuaLibraries.Standard.Math);
            Assert.IsTrue(result);
            Assert.IsFalse(lua.Globals.ContainsKey("math"));
        }

        [Test]
        public void OpenCloseStandardLibraries()
        {
            lua.State.Hook = new MaxInstructionCountLuaHook(5000);

            lua.OpenStandardLibraries();

            {
                // Base
                using (var globals = lua.GetGlobal<LuaTable>("_G"))
                {
                    Assert.IsNotNull(globals);
                }
            }

            {
                // String
                const string expected = "rats live on no evil star";
                var result = lua.Evaluate<string>($"return string.reverse('{expected}')");
                Assert.AreEqual(expected, result);
            }

            lua.CloseStandardLibraries();
            Assert.IsFalse(lua.Globals.ContainsKey("_G"));
        }

        [Test]
        public unsafe void ValidateLibraryGlobals()
        {
            foreach (var field in typeof(LuaLibraries.Standard).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.Name == "All")
                    continue;

                var library = (field.GetValue(null) as LuaLibrary)!;
                lua.OpenLibrary(library!);

                lua.Stack.EnsureFreeCapacity(1);

                lua_pushglobaltable(L);
                using (var globals = lua.Marshaler.PopValue<LuaTable>()!)
                {
                    foreach (var (key, _) in globals.EnumeratePairs())
                    {
                        CollectionAssert.Contains(library.Globals, key.GetValue<string>());
                    }

                    lua.CloseLibrary(library);
                }
            }
        }

        [Test]
        public void FileLibraryReturn()
        {
            lua.OpenLibrary(LuaLibraries.Standard.IO);

            const string code = "return io.stdout";

            using (var file = lua.Evaluate<LuaUserData>(code)!)
            {
                var actual = file.Size;
                Assert.AreEqual(16, actual);
            }
        }
    }
}
