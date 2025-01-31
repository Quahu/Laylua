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
        var descriptorProvider = new DefaultUserDataDescriptorProvider();
        descriptorProvider.SetInstanceDescriptor<StrictUserData1>(TypeMemberProvider.Strict);
        using var lua1 = new Lua(new DefaultLuaMarshaler(descriptorProvider));

        var ud = new StrictUserData1();
        lua1.SetGlobal("ud", ud);

        // Act
        var udAsObject = lua1.GetGlobal<object>("ud");

        // Assert
        Assert.That(udAsObject, Is.EqualTo(ud));
    }

    [Test]
    public void MainThreadUserData_PushedToChildThreadStack_IsPushedToChildStack()
    {
        // Arrange
        var descriptorProvider = new DefaultUserDataDescriptorProvider();
        descriptorProvider.SetInstanceDescriptor<MainThreadUserData>(TypeMemberProvider.Strict);
        using var lua1 = new Lua(new DefaultLuaMarshaler(descriptorProvider));

        var userData = new MainThreadUserData();
        lua1.Stack.Push(userData);
        using var thread = lua1.CreateThread();

        // Act
        thread.Stack.Push(userData);

        // Assert
        Assert.That(lua1.Stack.Count, Is.EqualTo(1));
        Assert.That(thread.Stack.Count, Is.EqualTo(1));
        Assert.That(lua1.Stack[-1].GetValue<MainThreadUserData>(), Is.EqualTo(thread.Stack[-1].GetValue<MainThreadUserData>()));
    }

    private sealed class MainThreadUserData;
}
