using NUnit.Framework;

namespace Laylua.Tests
{
    public unsafe class BufferTests : LuaFixture
    {
        [Test]
        public void Buffer()
        {
            fixed (byte* ptr = new byte[2])
            {
                var L = lua.GetStatePointer();

                luaL_buffinit(L, out var B);

                void Inner(luaL_Buffer* B)
                {
                    luaL_addchar(B, (byte) 'a');
                    luaL_addchar(B, (byte) 'b');
                    luaL_addchar(B, (byte) 'c');
                    luaL_addstring(B, " def");
                    var xs = new string('x', 1017);
                    luaL_addstring(B, xs);

                    Assert.AreEqual(1024, (int) B->n);

                    luaL_addchar(B, (byte) 'X');

                    Assert.AreEqual(1025, (int) B->n);

                    luaL_pushresult(B);

                    var str = lua_tostring(L, -1).ToString();
                    Assert.AreEqual($"abc def{xs}X", str);

                    lua_pop(L);
                }

                Inner(&B);
            }
        }
    }
}
