using System;
using System.Linq;
using Laylua.Moon;
using NUnit.Framework;

namespace Laylua.Tests
{
    public unsafe class FunctionTests : LuaTestBase
    {
        [Test]
        public void CallLuaFunctionWithLongJmp()
        {
            var x = 42;
            lua_register(lua.GetStatePointer(), "error", L =>
            {
                luaL_error(L, "Error!");
                x = 43;
                return 0;
            });

            Assert.Throws<LuaException>(() => lua.Execute("error()"));
            Assert.AreEqual(42, x);
        }

        [Test]
        public void CallErrorLuaFunction()
        {
            lua.OpenLibrary(LuaLibraries.Standard.Base);

            const string code = """
                                return function()
                                    error('problem?')
                                end
                                """;

            using (var function = lua.Evaluate<LuaFunction>(code)!)
            {
                Assert.Throws<LuaException>(() => function.Call());
            }
        }

        [Test]
        public void CallVariadicActionFromLua()
        {
            var count = -1;
            lua["func"] = (LuaStackValueRange args) =>
            {
                count = args.Count;
            };

            lua.Execute("func(1, 2, 3)");

            Assert.AreEqual(3, count);
        }

        [Test]
        public void CallParamAndVariadicActionFromLua()
        {
            var count = 0;
            lua["func"] = (int arg1, LuaStackValueRange args) =>
            {
                count++;
                count += args.Count;
            };

            lua.Execute("func(1, 2, 3)");

            Assert.AreEqual(3, count);
        }

        // TODO: LuaStackValueRange return?
        // [Test]
        // public void CallVariadicFuncFromLua()
        // {
        //     lua["func"] = (LuaStackValueRange args) =>
        //     {
        //         return args;
        //     };
        //
        //     lua.Execute("result = { func(1, 2, 3) }");
        //
        //     using (var result = lua.GetGlobal<LuaTable>("result")!)
        //     using (result.AsSequenceArray(out var actual))
        //     {
        //         var expected = new object[] { 1, 2, 3 };
        //         CollectionAssert.AreEqual(expected, actual);
        //     }
        // }

        // Can't be local, because params doesn't work in local methods.
        private static double Sum(params double[] numbers)
        {
            return numbers.Sum();
        }

        [Test]
        public void CallParamsFuncFromLua()
        {
#pragma warning disable CS8974
            lua["func"] = Sum;
#pragma warning restore CS8974

            var result = lua.Evaluate<double>("return func(1, 2, 3)");

            Assert.AreEqual(6, result);

            result = lua.Evaluate<double>("return func()");
            Assert.AreEqual(0, result);
        }

        [Test]
        public void CallConsoleWriteLineFromLua()
        {
            lua["print"] = (Action<string>) Console.WriteLine;
            lua.Execute("print('Hello, World!')");
        }

        [Test]
        public void CallDelegateReturningString()
        {
            lua["print"] = (Func<string>) (() => "Hello, World!");
            var result = lua.Evaluate<string>("return print()");
            Assert.AreEqual("Hello, World!", result);
        }

        [Test]
        public void CallVariadicConsoleWriteLineFromLua()
        {
            lua["print"] = (Action<string, object[]>) Console.WriteLine;
            lua.Execute("print('{0} + {1} = {2}', 10, 42, 10 + 42)");
        }

        [Test]
        public void CallConsoleWriteLineFromLuaWithWrongArgumentType()
        {
            lua["print"] = (Action<int>) Console.WriteLine;

            var ex = Assert.Throws<LuaException>(() => lua.Execute("print('Hello, World!')"))!;
            Assert.IsNotNull(ex.Status);
            Assert.AreEqual(LuaStatus.RuntimeError, ex.Status);
        }

        [Test]
        public void DelegateCalledWithLuaReferenceParameterDisposesLuaReference()
        {
            LuaReference? reference = null;
            lua["func"] = (LuaTable table) =>
            {
                reference = table;
            };

            lua.Execute("func({ 1, 2, 3 })");

            Assert.IsNotNull(reference);
            Assert.IsFalse(LuaReference.IsAlive(reference!));
        }

        private LuaReference[]? _references = null;

        // Can't be local, because params doesn't work in local methods.
        private void ParamsObjectMethod(params object[] args)
        {
            _references = args.OfType<LuaReference>().ToArray();
        }

        [Test]
        public void DelegateCalledWithObjectArrayParameterDisposesLuaReferences()
        {
#pragma warning disable CS8974
            lua["func"] = ParamsObjectMethod;
#pragma warning restore CS8974

            lua.Execute("func({ 1 }, { 2 }, { 3 })");

            Assert.IsNotNull(_references);
            foreach (var reference in _references!)
            {
                Assert.IsFalse(LuaReference.IsAlive(reference));
            }
        }

        [Test]
        public void DelegateCalledReturnsValue()
        {
            lua["func"] = () =>
            {
                return 42;
            };

            var result = lua.Evaluate<int>("return func()");

            Assert.AreEqual(42, result);
        }

        [Test]
        public void CallLuaFunctionWithNull()
        {
            lua.Execute("""
                        function func(x)
                            return x
                        end
                        """);

            using (var function = lua.GetGlobal<LuaFunction>("func")!)
            using (var results = function.Call())
            {
                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(null, results.First.Value);
            }
        }

        [Test]
        public void CallMultiReturnLuaFunction()
        {
            lua.Execute("""
                        function func(x, y, z)
                            return z, y, x
                        end
                        """);

            using (var function = lua.GetGlobal<LuaFunction>("func")!)
            using (var results = function.Call(3, 2, 1))
            {
                var expectedValues = new[] { 1, 2, 3 };
                Assert.AreEqual(expectedValues.Length, results.Count);

                var i = 0;
                foreach (var result in results)
                {
                    var expected = expectedValues[i++];
                    var actual = result.GetValue<int>();
                    Assert.AreEqual(expected, actual);
                }
            }
        }

        [Test]
        public void CallVariadicLuaFunction()
        {
            lua.Execute("""
                        function func(x, ...)
                            local temp = { ... }
                            return x, #temp
                        end
                        """);

            using (var function = lua.GetGlobal<LuaFunction>("func")!)
            using (var results = function.Call(42, 1, 2, 3))
            {
                var expectedValues = new[] { 42, 3 };

                Assert.AreEqual(expectedValues.Length, results.Count);

                var i = 0;
                foreach (var result in results)
                {
                    var expected = expectedValues[i++];
                    var actual = result.GetValue<int>();
                    Assert.AreEqual(expected, actual);
                }
            }
        }

        [Test]
        public void CallTableLuaFunction()
        {
            lua.Execute("""
                        function func(table)
                            return table
                        end
                        """);

            using (var function = lua.GetGlobal<LuaFunction>("func")!)
            using (var results = function.Call(new[] { 1, 2, 3 }))
            using (var table = results.First.GetValue<LuaTable>()!)
            {
                var actual = table.Values.ToArray<int>();
                try
                {
                    var expected = new[] { 1, 2, 3 };
                    CollectionAssert.AreEqual(expected, actual);
                }
                finally
                {
                    LuaReference.Dispose(actual);
                }
            }
        }
    }
}
