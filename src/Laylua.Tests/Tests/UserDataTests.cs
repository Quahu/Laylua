using Laylua.Marshaling;
using NUnit.Framework;

namespace Laylua.Tests;

public class UserDataTests : LuaFixture
{
    [LuaUserData]
    private class StrictUserData1
    {
        public int Property { get; set; }

        public int Field;

        public int Method(int value)
        {
            return value;
        }
    }

    [Test]
    public void DescribedUserData_Instance_PropertyFieldMethod_AccessibleFromLua()
    {
        // Arrange
        lua["ud"] = DescribedUserData.Instance(new StrictUserData1(), TypeMemberProvider.Strict);

        // Act
        var propertyResult = lua.Evaluate<int>("ud.Property = 42 return ud.Property");
        var fieldResult = lua.Evaluate<int>("ud.Field = 42 return ud.Field");
        var methodResult = lua.Evaluate<int>("return ud:Method(42)");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(propertyResult, Is.EqualTo(42));
            Assert.That(fieldResult, Is.EqualTo(42));
            Assert.That(methodResult, Is.EqualTo(42));
        });
    }

    [Test]
    public void DescribedUserData_Instance_GetGlobalAsObjectReturnsUserData()
    {
        // Arrange
        lua["ud"] = DescribedUserData.Instance(new StrictUserData1(), TypeMemberProvider.Strict);

        // Act
        var ud = lua["ud"];

        // Assert
        Assert.That(ud, Is.InstanceOf<StrictUserData1>());
    }

    [Test]
    public void UserDataWithDescriptor_MarshaledAsObjectType_MarshalsCorrectly()
    {
        // Arrange
        lua.Marshaler.UserDataDescriptorProvider.SetDescriptor<StrictUserData1>(
            new InstanceTypeUserDataDescriptor(typeof(StrictUserData1), TypeMemberProvider.Strict));

        var ud = new StrictUserData1();
        lua["ud"] = ud;

        // Act
        var udAsObject = lua["ud"];

        // Assert
        Assert.That(udAsObject, Is.EqualTo(ud));
    }
}
