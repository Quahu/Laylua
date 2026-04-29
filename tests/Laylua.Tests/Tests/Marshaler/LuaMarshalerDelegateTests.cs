using System.Globalization;
using System.Reflection;

namespace Laylua.Tests;

public class LuaMarshalerDelegateTests : LuaTestBase
{
    [Test]
    public void Delegate_LuaStackValueRange_ReturnsInt32ArgCount()
    {
        // Arrange
        Lua.SetGlobal("func", static (LuaStackValueRange args) => args.Count);

        // Act
        var result = Lua.Evaluate<int>("return func(1, 2, 3)");

        // Assert
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Delegate_String_ReceivesValidValue()
    {
        // Arrange
        string? result = null;
        Lua.SetGlobal("func", (string arg1) =>
        {
            result = arg1;
        });

        // Act
        Lua.Execute("func('value')");

        // Assert
        Assert.That(result, Is.EqualTo("value"));
    }

    [Test]
    public void Delegate_ReturnsValidStringValue()
    {
        // Arrange
        Lua.SetGlobal("func", static () => "value");

        // Act
        var result = Lua.Evaluate<string>("return func()");

        // Assert
        Assert.That(result, Is.EqualTo("value"));
    }

    [Test]
    public void Delegate_Int32_Int32_Int32_ReceivesValidValues()
    {
        // Arrange
        var (result1, result2, result3) = (0, 0, 0);
        Lua.SetGlobal("func", (int arg1, int arg2, int arg3) =>
        {
            result1 = arg1;
            result2 = arg2;
            result3 = arg3;
        });

        // Act
        Lua.Execute("func(1, 2, 3)");

        // Assert
        Assert.That(result1, Is.EqualTo(1));
        Assert.That(result2, Is.EqualTo(2));
        Assert.That(result3, Is.EqualTo(3));
    }

    [Test]
    public void Delegate_LuaStackValueRange_ReturnsVariadicArguments()
    {
        // Arrange
        Lua.SetGlobal("func", (LuaStackValueRange args) =>
        {
            return args;
        });

        // Act
        using var result = Lua.Evaluate("return func(1, 2, 3)");
        var actual = new List<int>();
        foreach (var value in result)
        {
            actual.Add(value.GetValue<int>());
        }

        // Assert
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(actual, Is.EqualTo(new[]
        {
            1, 2, 3
        }));
    }

    [Test]
    public void Delegate_ReturningLuaFunctionResults_ReturnsVariadicArguments()
    {
        // Arrange
        Lua.SetGlobal("func", () =>
        {
            return Lua.Evaluate("return 1, 2, 3");
        });

        // Act
        using var result = Lua.Evaluate("return func()");
        var actual = new List<int>();
        foreach (var value in result)
        {
            actual.Add(value.GetValue<int>());
        }

        // Assert
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(actual, Is.EqualTo(new[]
        {
            1, 2, 3
        }));
    }

    [Test]
    [TestCase(new[]
    {
        1, 2, 3, 4
    }, 1, 2, new[]
    {
        1, 2
    })]
    [TestCase(new[]
    {
        1, 2, 3, 4
    }, 2, 2, new[]
    {
        2, 3
    })]
    [TestCase(new[]
    {
        1, 2, 3, 4
    }, 1, 4, new[]
    {
        1, 2, 3, 4
    })]
    [TestCase(new[]
    {
        1, 2, 3, 4
    }, 3, 2, new[]
    {
        3, 4
    })]
    public void Delegate_WithExtraStackElements_ReturningSlice_ReturnsCorrectVariadicArguments(int[] stackValues, int rangeStart, int rangeCount, int[] expected)
    {
        // Arrange
        Lua.SetGlobal("func", () =>
        {
            foreach (var stackValue in stackValues)
            {
                Lua.Stack.Push(stackValue);
            }

            return Lua.Stack.GetRange(rangeStart, rangeCount);
        });

        // Act
        using var result = Lua.Evaluate("return func()");
        var actual = new List<int>();
        foreach (var value in result)
        {
            actual.Add(value.GetValue<int>());
        }

        // Assert
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Delegate_ParamsDouble_ReturnsValidDoubleValues()
    {
        // Arrange
        Lua.SetGlobal("func", ParamsDoubleMethod);

        // Act
        using var results = Lua.Evaluate<LuaTable>("return func(1, 2, 3)")!;

        // Assert
        Assert.That(results.Values.ToArray<double>(), Is.EquivalentTo(new[]
        {
            1, 2, 3
        }));
    }

    // Can't be local, because params doesn't work in local methods.
    private static double[] ParamsDoubleMethod(params double[] numbers)
    {
        return numbers;
    }

    [Test]
    public void Delegate_String_ParamsObject_ReturnsFormattedString()
    {
        // Arrange
        Lua.SetGlobal("func", FormatMethod);

        // Act
        var result = Lua.Evaluate<string>("return func('{0} + {1} + {2} = {3}', 1, 2, 3, 6)");

        // Assert
        Assert.That(result, Is.EqualTo("1 + 2 + 3 = 6"));
    }

    // Can't be local, because params doesn't work in local methods.
    private static string FormatMethod(string format, params object[] arguments)
    {
        return string.Format(CultureInfo.InvariantCulture, format, arguments);
    }

    [Test]
    public void Delegate_Int32_CalledWithBadArgumentThrows()
    {
        // Arrange
        Lua.SetGlobal("func", static (int _) =>
            { });

        // Act
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("func('invalid value')"))!;

        // Assert
        Assert.That(ex.Status, Is.EqualTo(LuaStatus.RuntimeError));
    }

    [Test]
    public void Delegate_LuaReference_DisposesLuaReference()
    {
        // Arrange
        LuaReference? reference = null;
        Lua.SetGlobal("func", (LuaTable table) =>
        {
            reference = table;
        });

        // Act
        Lua.Execute("func({ 1, 2, 3 })");

        // Assert
        Assert.That(reference, Is.Not.Null);
        Assert.That(IsReferenceAlive(reference), Is.False);
    }

    [Test]
    public void Delegate_ParamsObject_DisposesLuaReferences()
    {
        // Arrange
        Lua.SetGlobal("func", ParamsObjectMethod);

        // Act
        Lua.Execute("func({ 1, 2, 3 }, 0, { 1, 2, 3 }, 'value', { 1, 2, 3 })");

        // Assert
        Assert.That(_references, Is.Not.Null);
        Assert.That(_references!.Select(IsReferenceAlive), Is.All.False);
    }

    // Can't be local, because params doesn't work in local methods.
    private LuaReference[]? _references;

    private void ParamsObjectMethod(params object[] args)
    {
        _references = args.OfType<LuaReference>().ToArray();
    }

    private static bool IsReferenceAlive(LuaReference? reference)
    {
        return (bool) typeof(LuaReference).GetMethod("IsAlive", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [reference, null])!;
    }

    [Test]
    public void Delegate_MulticastAction_InvokesAllHandlers()
    {
        // Arrange
        var count = 0;
        Action<int> handler1 = _ => count += 1;
        Action<int> handler2 = _ => count += 10;
        var combined = handler1 + handler2;
        Lua.SetGlobal("func", combined);

        // Act
        Lua.Execute("func(0)");

        // Assert
        Assert.That(count, Is.EqualTo(11));
    }

    [Test]
    public void Delegate_MulticastFunc_InvokesAllHandlers()
    {
        // Arrange
        var sideEffects = new List<int>();
        Func<int, int> handler1 = x =>
        {
            sideEffects.Add(1);
            return x + 1;
        };

        Func<int, int> handler2 = x =>
        {
            sideEffects.Add(2);
            return x + 10;
        };

        var combined = handler1 + handler2;
        Lua.SetGlobal("func", combined);

        // Act
        var result = Lua.Evaluate<int>("return func(5)");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(sideEffects, Is.EqualTo(new[]
            {
                1, 2
            }));

            Assert.That(result, Is.EqualTo(15));
        });
    }

    [Test]
    public void Delegate_WithRefParameter_ThrowsOnInvoke()
    {
        // Arrange
        var method = typeof(LuaMarshalerDelegateTests).GetMethod(nameof(RefParamMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
        var del = Delegate.CreateDelegate(typeof(RefParamDelegate), method);
        Lua.SetGlobal("func", del);

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("func(1)"));
        Assert.That(ex!.InnerException, Is.TypeOf<NotSupportedException>());
    }

    private delegate void RefParamDelegate(ref int value);

    private static void RefParamMethod(ref int value)
    {
        value++;
    }

    [Test]
    public void Delegate_WithOutParameter_ThrowsOnInvoke()
    {
        // Arrange
        var method = typeof(LuaMarshalerDelegateTests).GetMethod(nameof(OutParamMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
        var del = Delegate.CreateDelegate(typeof(OutParamDelegate), method);
        Lua.SetGlobal("func", del);

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("func('hello', 0)"));
        Assert.That(ex!.InnerException, Is.TypeOf<NotSupportedException>());
    }

    private delegate void OutParamDelegate(string input, out int result);

    private static void OutParamMethod(string input, out int result)
    {
        result = input.Length;
    }

    [Test]
    public void Delegate_WithByRefReturn_ThrowsOnInvoke()
    {
        // Arrange
        var method = typeof(LuaMarshalerDelegateTests).GetMethod(nameof(ByRefReturnMethod), BindingFlags.NonPublic | BindingFlags.Static)!;
        var del = Delegate.CreateDelegate(typeof(ByRefReturnDelegate), method);
        Lua.SetGlobal("func", del);

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("func()"));
        Assert.That(ex!.InnerException, Is.TypeOf<NotSupportedException>());
    }

    private delegate ref int ByRefReturnDelegate();

    private static int s_byRefReturnValue = 42;

    private static ref int ByRefReturnMethod()
    {
        return ref s_byRefReturnValue;
    }

    [Test]
    public void Delegate_5Parameters_ManagedRoundTrip_ReturnsOriginalDelegate()
    {
        // Arrange
        Func<int, int, int, int, int, int> originalFunction = static (first, second, third, fourth, fifth) =>
        {
            return first + second + third + fourth + fifth;
        };

        Lua.SetGlobal("bigFunc", originalFunction);

        // Act
        var func = Lua.Evaluate<Func<int, int, int, int, int, int>>("return bigFunc")!;
        var result = func(1, 2, 3, 4, 5);

        // Assert
        Assert.That(func, Is.SameAs(originalFunction));
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void Delegate_16Parameters_ManagedRoundTrip_ReturnsOriginalDelegate()
    {
        // Arrange
        Func<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int> originalFunction =
            static (first, second, third, fourth, fifth, sixth, seventh, eighth, ninth, tenth, eleventh, twelfth, thirteenth, fourteenth, fifteenth, sixteenth) =>
            {
                return first + second + third + fourth + fifth + sixth + seventh + eighth + ninth + tenth + eleventh + twelfth + thirteenth + fourteenth + fifteenth + sixteenth;
            };

        Lua.SetGlobal("func16", originalFunction);

        // Act
        var func = Lua.Evaluate<Func<int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int, int>>("return func16")!;
        var result = func(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);

        // Assert
        Assert.That(func, Is.SameAs(originalFunction));
        Assert.That(result, Is.EqualTo(136));
    }

    private delegate int TooManyParamsDelegate(int a, int b, int c, int d, int e, int f, int g, int h,
        int i, int j, int k, int l, int m, int n, int o, int p, int q);

    [Test]
    public void Delegate_MoreThan16Parameters_PlainLuaFunction_ThrowsInvalidOperation()
    {
        // Arrange
        Lua.Execute("function hugeFunc() return 0 end");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            Lua.Evaluate<TooManyParamsDelegate>("return hugeFunc");
        });
    }
}
