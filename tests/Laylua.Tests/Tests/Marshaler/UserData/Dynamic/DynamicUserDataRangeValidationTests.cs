using Laylua.Marshaling;

namespace Laylua.Tests;

public class DynamicUserDataRangeValidationTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        LuaTypeRegistry.Unregister<RangeTestType>();
    }

    public class RangeTestType
    {
        public byte ToByte(byte value)
        {
            return value;
        }

        public sbyte ToSByte(sbyte value)
        {
            return value;
        }

        public short ToShort(short value)
        {
            return value;
        }

        public int ToInt(int value)
        {
            return value;
        }

        public uint ToUInt(uint value)
        {
            return value;
        }

        public ulong ToULong(ulong value)
        {
            return value;
        }

        public byte AddBytes(byte a, byte b)
        {
            return (byte) (a + b);
        }
    }

    [Test]
    public void Strict_ByteParam_RejectsOverflow()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<RangeTestType>(o => o.ArgumentValidation = LuaArgumentValidation.Strict);
        Lua["obj"] = new RangeTestType();

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:ToByte(256)"));
        Assert.That(ex!.Message, Does.Contain("out of range"));
    }

    [Test]
    public void Strict_ByteParam_AcceptsValidValue()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<RangeTestType>(o => o.ArgumentValidation = LuaArgumentValidation.Strict);
        Lua["obj"] = new RangeTestType();

        // Act
        var result = Lua.Evaluate<int>("return obj:ToByte(200)");

        // Assert
        Assert.That(result, Is.EqualTo(200));
    }

    [Test]
    public void Strict_IntParam_RejectsOverflow()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<RangeTestType>(o => o.ArgumentValidation = LuaArgumentValidation.Strict);
        Lua["obj"] = new RangeTestType();

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:ToInt(2147483648)"));
        Assert.That(ex!.Message, Does.Contain("out of range"));
    }

    [Test]
    public void Strict_UIntParam_RejectsNegative()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<RangeTestType>(o => o.ArgumentValidation = LuaArgumentValidation.Strict);
        Lua["obj"] = new RangeTestType();

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:ToUInt(-1)"));
        Assert.That(ex!.Message, Does.Contain("out of range"));
    }

    [Test]
    public void Strict_ULongParam_RejectsNegative()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<RangeTestType>(o => o.ArgumentValidation = LuaArgumentValidation.Strict);
        Lua["obj"] = new RangeTestType();

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:ToULong(-1)"));
        Assert.That(ex!.Message, Does.Contain("out of range"));
    }

    [Test]
    public void Strict_MultipleByteParams_BothValidated()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<RangeTestType>(o => o.ArgumentValidation = LuaArgumentValidation.Strict);
        Lua["obj"] = new RangeTestType();

        // Act & Assert
        Assert.That(Lua.Evaluate<int>("return obj:AddBytes(100, 50)"), Is.EqualTo(150));

        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:AddBytes(100, 300)"));
        Assert.That(ex!.Message, Does.Contain("out of range"));
    }

    [Test]
    public void NoRange_ByteParam_SilentlyTruncates()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<RangeTestType>(o => o.ArgumentValidation = LuaArgumentValidation.None);
        Lua["obj"] = new RangeTestType();

        // Act
        var result = Lua.Evaluate<int>("return obj:ToByte(256)");

        // Assert
        Assert.That(result, Is.EqualTo(0)); // 256 truncates to 0
    }
}
