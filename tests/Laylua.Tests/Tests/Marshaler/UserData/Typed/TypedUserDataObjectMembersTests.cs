namespace Laylua.Tests;

public class TypedUserDataObjectMembersTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
    }

    [Test]
    public void ObjectMembersAll_ToString_ExposedAsMethod()
    {
        // Arrange
        var obj = new TestObjectMembersAll
        {
            Value = 42
        };

        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:ToString()");

        // Assert
        Assert.That(result, Is.EqualTo("ObjAll(42)"));
    }

    [Test]
    public void ObjectMembersAll_Equals_ExposedAsMethod()
    {
        // Arrange
        var obj1 = new TestObjectMembersAll
        {
            Value = 42
        };

        var obj2 = new TestObjectMembersAll
        {
            Value = 42
        };

        Lua["a"] = obj1;
        Lua["b"] = obj2;

        // Act
        var result = Lua.Evaluate<bool>("return a:Equals(b)");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ObjectMembersAll_GetHashCode_ExposedAsMethod()
    {
        // Arrange
        var obj = new TestObjectMembersAll
        {
            Value = 42
        };

        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<int>("return obj:GetHashCode()");

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void ObjectMembersToStringOnly_ToString_Exposed()
    {
        // Arrange
        var obj = new TestObjectMembersToStringOnly
        {
            Value = 7
        };

        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj:ToString()");

        // Assert
        Assert.That(result, Is.EqualTo("TS(7)"));
    }

    [Test]
    public void ObjectMembersToStringOnly_Equals_NotExposed()
    {
        // Arrange
        var obj = new TestObjectMembersToStringOnly
        {
            Value = 7
        };

        Lua["obj"] = obj;

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:Equals(obj)"));
        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public void ObjectMembersToStringOnly_GetHashCode_NotExposed()
    {
        // Arrange
        var obj = new TestObjectMembersToStringOnly
        {
            Value = 7
        };

        Lua["obj"] = obj;

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:GetHashCode()"));
        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public void ObjectMembersNone_ToString_NotExposedAsMethod()
    {
        // Arrange
        var obj = new TestObjectMembersNone
        {
            Value = 5
        };

        Lua["obj"] = obj;

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:ToString()"));
        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public void ObjectMembersNone_ToStringMetamethod_StillWorks()
    {
        // Arrange
        var obj = new TestObjectMembersNone
        {
            Value = 5
        };

        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return tostring(obj)");

        // Assert
        Assert.That(result, Is.EqualTo("NoObj(5)"));
    }

    [Test]
    public void ObjectMembersNone_Equals_NotExposed()
    {
        // Arrange
        var obj = new TestObjectMembersNone
        {
            Value = 5
        };

        Lua["obj"] = obj;

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:Equals(obj)"));
        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public void ObjectMembersNone_GetHashCode_NotExposed()
    {
        // Arrange
        var obj = new TestObjectMembersNone
        {
            Value = 5
        };

        Lua["obj"] = obj;

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:GetHashCode()"));
        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public void ObjectMembersAll_ToStringMethod_CoexistsWithMetamethod()
    {
        // Arrange
        var obj = new TestObjectMembersAll
        {
            Value = 99
        };

        Lua["obj"] = obj;

        // Act
        var method = Lua.Evaluate<string>("return obj:ToString()");
        var metamethod = Lua.Evaluate<string>("return tostring(obj)");

        // Assert
        Assert.That(method, Is.EqualTo("ObjAll(99)"));
        Assert.That(metamethod, Is.EqualTo("ObjAll(99)"));
    }

    [Test]
    public void Default_ObjectMembers_NoMethodExposed()
    {
        // Arrange
        var obj = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        Lua["obj"] = obj;

        // Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:ToString()"));
        Assert.That(ex, Is.Not.Null);

        // Act
        var result = Lua.Evaluate<string>("return tostring(obj)");

        // Assert
        Assert.That(result, Is.EqualTo("Player(Steve)"));
    }
}
