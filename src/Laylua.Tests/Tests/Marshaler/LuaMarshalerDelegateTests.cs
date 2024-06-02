using System.Globalization;

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
        var result = Lua.Evaluate<string>("return func('value')");

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
        Assert.That(actual, Is.EquivalentTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void Delegate_LuaStackValueRange_ReturningLuaFunctionResults_ReturnsVariadicArguments()
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
        Assert.That(actual, Is.EquivalentTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void Delegate_ParamsDouble_ReturnsValidDoubleValues()
    {
        // Arrange
        Lua.SetGlobal("func", ParamsDoubleMethod);

        // Act
        using var results = Lua.Evaluate<LuaTable>("return func(1, 2, 3)")!;

        // Assert
        Assert.That(results.Values.ToArray<double>(), Is.EquivalentTo(new[] { 1, 2, 3 }));
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
        Lua.SetGlobal("func", static (int _) => { });

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
        Assert.That(LuaReference.IsAlive(reference!), Is.False);
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
        Assert.That(_references!.Select(LuaReference.IsAlive), Is.All.False);
    }

    // Can't be local, because params doesn't work in local methods.
    private LuaReference[]? _references;
    private void ParamsObjectMethod(params object[] args)
    {
        _references = args.OfType<LuaReference>().ToArray();
    }
}
