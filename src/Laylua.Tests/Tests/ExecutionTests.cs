using NUnit.Framework;

namespace Laylua.Tests
{
    [Order(-100)]
    public unsafe class ExecutionTests : LuaFixture
    {
        [Test]
        public void ExecuteCode()
        {
            var L = lua.GetStatePointer();
            var oldTop = lua_gettop(L);
            lua.Execute("return 'Hello, World!'");
            var newTop = lua_gettop(L);
            Assert.AreEqual(oldTop, newTop);
        }

        [Test]
        public void EvaluateCode()
        {
            var result = lua.Evaluate<string>("return 'Hello,\0 World!'");
            Assert.AreEqual("Hello,\0 World!", result);
        }

        [Test]
        public void CompileCode()
        {
            using (var function = lua.Compile("return 'Hello, World!'"))
            {
                using (var results = function.Call())
                {
                    var result = results.First.GetValue<string>();
                    Assert.AreEqual("Hello, World!", result);
                }

                using (var results = function.Call())
                {
                    var result = results.First.GetValue<string>();
                    Assert.AreEqual("Hello, World!", result);
                }
            }
        }
    }
}
