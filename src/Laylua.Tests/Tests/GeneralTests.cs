using Laylua.Moon;
using NUnit.Framework;

namespace Laylua.Tests
{
    public class GeneralTests : LuaTestBase
    {
        [Test]
        public void LuaError_LongJmp_GetsCaughtWithLuaPanicException()
        {
            Assert.Throws<LuaPanicException>(() => lua.RaiseError("This must be caught!"));
        }

        [Test]
        public void LuaError_ProtectedLongJmp_GetsCaughtWithLuaException()
        {
            lua.OpenLibrary(LuaLibraries.Standard.Base);

            Assert.Throws<LuaException>(() => lua.Execute("error('This must be caught!')"));
        }
    }
}
