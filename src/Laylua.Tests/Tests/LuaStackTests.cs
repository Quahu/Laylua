using NUnit.Framework;

namespace Laylua.Tests;

public class LuaStackTests : LuaFixture
{
    [Test]
    public void LuaStack_Insert_LeavesNoGarbage()
    {
        lua.Stack.Push(42);
        lua.Stack.Insert(1, 43);
        AssertStackCount(2);
        lua.Stack.Count = 0;
    }
}
