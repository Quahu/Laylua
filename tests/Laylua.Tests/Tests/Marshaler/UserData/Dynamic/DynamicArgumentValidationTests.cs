using Laylua.Marshaling;

namespace Laylua.Tests;

public class DynamicArgumentValidationTarget
{
    public string Greet(string name)
    {
        return $"Hello, {name}!";
    }

    public string GreetOptional(string? name)
    {
        return $"Hello, {name ?? "World"}!";
    }

    public int TakeDamage(int damage)
    {
        return damage;
    }

    public int DescribeOptionalValue(int? value)
    {
        return value ?? -1;
    }

    public int CountOptionalValues(params int?[] values)
    {
        return values.Length;
    }
}

public class DynamicArgumentValidationConstructable
{
    public string Name { get; }

    public DynamicArgumentValidationConstructable(string name)
    {
        Name = name;
    }
}

public class DynamicArgumentValidationTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
        Marshaler.AllowsDynamicMarshaling = true;
        LuaTypeRegistry.Unregister<DynamicArgumentValidationTarget>();
        LuaTypeRegistry.Unregister<DynamicArgumentValidationConstructable>();
    }

    public override void TearDown()
    {
        Marshaler.AllowsDynamicMarshaling = false;
        base.TearDown();
    }

    private void ConfigureTarget(LuaArgumentValidation validation)
    {
        LuaTypeRegistry.ConfigureDynamic<DynamicArgumentValidationTarget>(opts =>
        {
            opts.ArgumentValidation = validation;
        });

        Lua["obj"] = new DynamicArgumentValidationTarget();
    }

    private void ConfigureConstructable(LuaArgumentValidation validation)
    {
        LuaTypeRegistry.ConfigureDynamic<DynamicArgumentValidationConstructable>(opts =>
        {
            opts.ArgumentValidation = validation;
        });

        Lua["Target"] = typeof(DynamicArgumentValidationConstructable);
    }

    [Test]
    public void Strict_NonNullableParam_RejectsNil()
    {
        // Arrange
        ConfigureTarget(LuaArgumentValidation.Strict);

        // Act
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:Greet(nil)"));

        // Assert
        Assert.That(ex!.Message, Does.Contain("cannot be nil"));
    }

    [Test]
    public void Strict_NonNullableParam_AcceptsValue()
    {
        // Arrange
        ConfigureTarget(LuaArgumentValidation.Strict);

        // Act
        var result = Lua.Evaluate<string>("return obj:Greet('Steve')");

        // Assert
        Assert.That(result, Is.EqualTo("Hello, Steve!"));
    }

    [Test]
    public void Strict_NullableParam_AcceptsNil()
    {
        // Arrange
        ConfigureTarget(LuaArgumentValidation.Strict);

        // Act
        var result = Lua.Evaluate<string>("return obj:GreetOptional(nil)");

        // Assert
        Assert.That(result, Is.EqualTo("Hello, World!"));
    }

    [Test]
    public void Strict_NullableParam_AcceptsValue()
    {
        // Arrange
        ConfigureTarget(LuaArgumentValidation.Strict);

        // Act
        var result = Lua.Evaluate<string>("return obj:GreetOptional('Alex')");

        // Assert
        Assert.That(result, Is.EqualTo("Hello, Alex!"));
    }

    [Test]
    public void None_NonNullableParam_StillFailsOnNil()
    {
        // Arrange
        ConfigureTarget(LuaArgumentValidation.None);

        // Act
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:Greet(nil)"));

        // Assert
        Assert.That(ex!.Message, Does.Contain("string expected"));
        Assert.That(ex.Message, Does.Not.Contain("cannot be nil"));
    }

    [Test]
    public void None_NonNullableParam_AcceptsValue()
    {
        // Arrange
        ConfigureTarget(LuaArgumentValidation.None);

        // Act
        var result = Lua.Evaluate<string>("return obj:Greet('Steve')");

        // Assert
        Assert.That(result, Is.EqualTo("Hello, Steve!"));
    }

    [Test]
    public void NullabilityOnly_NonNullableParam_RejectsNil()
    {
        // Arrange
        ConfigureTarget(LuaArgumentValidation.Nullability);

        // Act
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:Greet(nil)"));

        // Assert
        Assert.That(ex!.Message, Does.Contain("cannot be nil"));
    }

    [Test]
    public void NullabilityOnly_NullableParam_AcceptsNil()
    {
        // Arrange
        ConfigureTarget(LuaArgumentValidation.Nullability);

        // Act
        var result = Lua.Evaluate<string>("return obj:GreetOptional(nil)");

        // Assert
        Assert.That(result, Is.EqualTo("Hello, World!"));
    }

    [Test]
    public void NullabilityOnly_NonNullableParam_AcceptsValue()
    {
        // Arrange
        ConfigureTarget(LuaArgumentValidation.Nullability);

        // Act
        var result = Lua.Evaluate<string>("return obj:Greet('Steve')");

        // Assert
        Assert.That(result, Is.EqualTo("Hello, Steve!"));
    }

    [Test]
    public void Strict_NullableValueTypeParam_AcceptsNil()
    {
        // Arrange
        ConfigureTarget(LuaArgumentValidation.Strict);

        // Act
        var value = Lua.Evaluate<int>("return obj:DescribeOptionalValue(nil)");

        // Assert
        Assert.That(value, Is.EqualTo(-1));
    }

    [Test]
    public void Strict_ParamsNullableValueTypeElements_AcceptsNil()
    {
        // Arrange
        ConfigureTarget(LuaArgumentValidation.Strict);

        // Act
        var value = Lua.Evaluate<int>("return obj:CountOptionalValues(1, nil, 3)");

        // Assert
        Assert.That(value, Is.EqualTo(3));
    }

    [Test]
    public void Method_TooFewArgs_RaisesLuaError()
    {
        // Arrange
        ConfigureTarget(LuaArgumentValidation.Strict);

        // Act
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:TakeDamage()"));

        // Assert
        Assert.That(ex!.Message, Does.Contain("expected at least 1 argument"));
    }

    [Test]
    public void Method_InvalidNumericType_RaisesLuaError()
    {
        // Arrange
        ConfigureTarget(LuaArgumentValidation.Strict);

        // Act
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:TakeDamage('not a number')"));

        // Assert
        Assert.That(ex!.Message, Does.Contain("number"));
    }

    [Test]
    public void Constructor_Strict_NonNullableParam_RejectsNil()
    {
        // Arrange
        ConfigureConstructable(LuaArgumentValidation.Strict);

        // Act
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("Target(nil)"));

        // Assert
        Assert.That(ex!.Message, Does.Contain("cannot be nil"));
    }

    [Test]
    public void Constructor_None_NonNullableParam_StillFailsOnNil()
    {
        // Arrange
        ConfigureConstructable(LuaArgumentValidation.None);

        // Act
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("Target(nil)"));

        // Assert
        Assert.That(ex!.Message, Does.Contain("string"));
        Assert.That(ex.Message, Does.Not.Contain("cannot be nil"));
    }
}
