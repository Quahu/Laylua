using NUnit.Framework;

namespace Laylua.Tests
{
    public unsafe class ExceptionTests : LuaTestBase
    {
        [Test]
        public void Execute_IndexingNilGlobal_ThrowsLuaException()
        {
            var ex = Assert.Throws<LuaException>(() => lua.Execute("return nothing.something"))!;
            StringAssert.EndsWith("(global 'nothing')", ex.Message);
        }

        [Test]
        public void Execute_IndexingNilField_ThrowsLuaException()
        {
            var ex = Assert.Throws<LuaException>(() => lua.Execute("something = {} return something.nothing.something"))!;
            StringAssert.EndsWith("(field 'nothing')", ex.Message);
        }

        [Test]
        public void Execute_IndexingNilLocal_ThrowsLuaException()
        {
            var ex = Assert.Throws<LuaException>(() => lua.Execute("local nothing = nil return nothing.something"))!;
            StringAssert.EndsWith("(local 'nothing')", ex.Message);
        }
    }
}
