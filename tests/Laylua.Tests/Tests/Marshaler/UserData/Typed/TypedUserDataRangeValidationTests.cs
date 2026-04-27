namespace Laylua.Tests;

public class TypedUserDataRangeValidationTests : LuaTestBase
{
    [Test]
    public void Strict_ByteParam_AcceptsValidValue()
    {
        // Arrange
        var obj = new TestRangeValidation();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<int>("return obj:ToByte(200)");

        // Assert
        Assert.That(result, Is.EqualTo(200));
    }

    [Test]
    public void Strict_ByteParam_RejectsNegative()
    {
        // Arrange
        var obj = new TestRangeValidation();
        Lua["obj"] = obj;

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:ToByte(-1)"));
        Assert.That(ex!.Message, Does.Contain("out of range"));
    }

    [Test]
    public void Strict_ByteParam_RejectsOverflow()
    {
        // Arrange
        var obj = new TestRangeValidation();
        Lua["obj"] = obj;

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:ToByte(256)"));
        Assert.That(ex!.Message, Does.Contain("out of range"));
    }

    [Test]
    public void Strict_ByteParam_AcceptsBoundaryValues()
    {
        // Arrange
        var obj = new TestRangeValidation();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.That(Lua.Evaluate<int>("return obj:ToByte(0)"), Is.EqualTo(0));
        Assert.That(Lua.Evaluate<int>("return obj:ToByte(255)"), Is.EqualTo(255));
    }

    [Test]
    public void Strict_SByteParam_RejectsOverflow()
    {
        // Arrange
        var obj = new TestRangeValidation();
        Lua["obj"] = obj;

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:ToSByte(128)"));
        Assert.That(ex!.Message, Does.Contain("out of range"));
    }

    [Test]
    public void Strict_SByteParam_RejectsUnderflow()
    {
        // Arrange
        var obj = new TestRangeValidation();
        Lua["obj"] = obj;

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:ToSByte(-129)"));
        Assert.That(ex!.Message, Does.Contain("out of range"));
    }

    [Test]
    public void Strict_ShortParam_RejectsOverflow()
    {
        // Arrange
        var obj = new TestRangeValidation();
        Lua["obj"] = obj;

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:ToShort(32768)"));
        Assert.That(ex!.Message, Does.Contain("out of range"));
    }

    [Test]
    public void Strict_UShortParam_RejectsNegative()
    {
        // Arrange
        var obj = new TestRangeValidation();
        Lua["obj"] = obj;

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:ToUShort(-1)"));
        Assert.That(ex!.Message, Does.Contain("out of range"));
    }

    [Test]
    public void Strict_IntParam_AcceptsNormalValue()
    {
        // Arrange
        var obj = new TestRangeValidation();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<int>("return obj:ToInt(42)");

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Strict_IntParam_RejectsOverflow()
    {
        // Arrange
        var obj = new TestRangeValidation();
        Lua["obj"] = obj;

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:ToInt(2147483648)"));
        Assert.That(ex!.Message, Does.Contain("out of range"));
    }

    [Test]
    public void Strict_UIntParam_RejectsNegative()
    {
        // Arrange
        var obj = new TestRangeValidation();
        Lua["obj"] = obj;

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:ToUInt(-1)"));
        Assert.That(ex!.Message, Does.Contain("out of range"));
    }

    [Test]
    public void Strict_LongParam_AcceptsLargeValue()
    {
        // Arrange
        var obj = new TestRangeValidation();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<long>("return obj:ToLong(9007199254740992)");

        // Assert
        Assert.That(result, Is.EqualTo(9007199254740992L));
    }

    [Test]
    public void Strict_ULongParam_RejectsNegative()
    {
        // Arrange
        var obj = new TestRangeValidation();
        Lua["obj"] = obj;

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:ToULong(-1)"));
        Assert.That(ex!.Message, Does.Contain("out of range"));
    }

    [Test]
    public void Strict_MultipleByteParams_BothValidated()
    {
        // Arrange
        var obj = new TestRangeValidation();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.That(Lua.Evaluate<int>("return obj:AddBytes(100, 50)"), Is.EqualTo(150));

        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:AddBytes(100, 300)"));
        Assert.That(ex!.Message, Does.Contain("out of range"));
    }

    [Test]
    public void Strict_OptionalIntParam_RejectsOverflow()
    {
        // Arrange
        var obj = new TestRangeValidation();
        Lua["obj"] = obj;

                // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute($"obj:OptionalInt(2147483648)"));
        Assert.That(ex!.Message, Does.Contain("out of range"));
    }

    [Test]
    public void Strict_OptionalIntParam_UsesDefaultWhenOmitted()
    {
        // Arrange
        var obj = new TestRangeValidation();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<int>("return obj:OptionalInt()");

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void NoRange_ByteParam_SilentlyTruncates()
    {
        // Arrange
        var obj = new TestNoRangeValidation();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<int>("return obj:ToByte(256)");

        // Assert
        Assert.That(result, Is.EqualTo(0)); // truncates to 0
    }

    [Test]
    public void NoRange_IntParam_SilentlyTruncates()
    {
        // Arrange
        var obj = new TestNoRangeValidation();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<long>("return obj:ToInt(2147483648)");

        // Assert
        Assert.That(result, Is.EqualTo(int.MinValue)); // overflows to int.MinValue
    }
}
