using Laylua.Marshaling;

namespace Laylua.Tests;

public class UserDataTests : LuaTestBase
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
        Lua["ud"] = DescribedUserData.Instance(new StrictUserData1(), TypeMemberProvider.Strict);

        // Act
        var propertyResult = Lua.Evaluate<int>("ud.Property = 42 return ud.Property");
        var fieldResult = Lua.Evaluate<int>("ud.Field = 42 return ud.Field");
        var methodResult = Lua.Evaluate<int>("return ud:Method(42)");

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
        Lua.SetGlobal("ud", DescribedUserData.Instance(new StrictUserData1(), TypeMemberProvider.Strict));

        // Act
        var ud = Lua.GetGlobal<object>("ud");

        // Assert
        Assert.That(ud, Is.InstanceOf<StrictUserData1>());
    }

    [Test]
    public void UserDataWithDescriptor_MarshaledAsObjectType_MarshalsCorrectly()
    {
        // Arrange
        Lua.Marshaler.UserDataDescriptorProvider.SetInstanceDescriptor<StrictUserData1>(TypeMemberProvider.Strict);

        var ud = new StrictUserData1();
        Lua.SetGlobal("ud", ud);

        // Act
        var udAsObject = Lua.GetGlobal<object>("ud");

        // Assert
        Assert.That(udAsObject, Is.EqualTo(ud));
    }
}
