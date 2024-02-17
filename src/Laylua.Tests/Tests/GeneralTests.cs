using System.Threading;
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

        [Test]
        [Timeout(500)]
        public void ExecutionCancellation()
        {
            var spinWait = new SpinWait();
            lua["spin"] = () =>
            {
                spinWait.SpinOnce();
            };

            lua.Execute("""
                function brick()
                    while (true) do
                        spin()
                    end
                end
                """);

            Assert.Throws<LuaException>(() =>
            {
                using (var cts = new CancellationTokenSource(50))
                {
                    lua.State.Hook = new CancellationTokenLuaHook(cts.Token);
                    lua.Execute("brick()");
                }
            });
        }
    }
}
