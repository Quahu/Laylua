namespace Laylua.Tests;

public class FunctionTests : LuaTestBase
{
    [Test]
    public void CallLuaFunctionWithNull()
    {
        Lua.Execute("""
                    function func(x)
                        return x
                    end
                    """);

        using (var function = Lua.GetGlobal<LuaFunction>("func")!)
        using (var results = function.Call())
        {
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results.First.Value, Is.Null);
        }
    }

    [Test]
    public void CallMultiReturnLuaFunction()
    {
        Lua.Execute("""
                    function func(x, y, z)
                        return z, y, x
                    end
                    """);

        using (var function = Lua.GetGlobal<LuaFunction>("func")!)
        using (var results = function.Call(3, 2, 1))
        {
            var expectedValues = new[] { 1, 2, 3 };
            Assert.That(results.Count, Is.EqualTo(expectedValues.Length));

            var i = 0;
            foreach (var result in results)
            {
                var expected = expectedValues[i++];
                var actual = result.GetValue<int>();
                Assert.That(actual, Is.EqualTo(expected));
            }
        }
    }

    [Test]
    public void CallVariadicLuaFunction()
    {
        Lua.Execute("""
                    function func(x, ...)
                        local temp = { ... }
                        return x, #temp
                    end
                    """);

        using (var function = Lua.GetGlobal<LuaFunction>("func")!)
        using (var results = function.Call(42, 1, 2, 3))
        {
            var expectedValues = new[] { 42, 3 };

            Assert.That(results.Count, Is.EqualTo(expectedValues.Length));

            var i = 0;
            foreach (var result in results)
            {
                var expected = expectedValues[i++];
                var actual = result.GetValue<int>();
                Assert.That(actual, Is.EqualTo(expected));
            }
        }
    }

    [Test]
    public void CallTableLuaFunction()
    {
        Lua.Execute("""
                    function func(table)
                        return table
                    end
                    """);

        using (var function = Lua.GetGlobal<LuaFunction>("func")!)
        using (var results = function.Call(new[] { 1, 2, 3 }))
        using (var table = results.First.GetValue<LuaTable>()!)
        {
            var actual = table.Values.ToArray<int>();
            try
            {
                var expected = new[] { 1, 2, 3 };
                Assert.That(actual, Is.EquivalentTo(expected));
            }
            finally
            {
                LuaReference.Dispose(actual);
            }
        }
    }
}
