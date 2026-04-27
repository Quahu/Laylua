namespace Laylua.Tests;

public class TypedUserDataObsoleteMemberTests : LuaTestBase
{
    [Test]
    [Obsolete("Tests obsolete member handling")]
    public void SourceGen_Ignore_HidesObsoleteProperty()
    {
        // Arrange
        var obj = new TestObsoleteIgnored
        {
            Active = 10,
            Legacy = 20
        };

        Lua["obj"] = obj;

        // Act & Assert
        Assert.That(Lua.Evaluate<int>("return obj.Active"), Is.EqualTo(10));
        Assert.That(Lua.Evaluate<object?>("return obj.Legacy"), Is.Null);
    }

    [Test]
    [Obsolete("Tests obsolete member handling")]
    public void SourceGen_Ignore_HidesObsoleteMethod()
    {
        // Arrange
        var obj = new TestObsoleteIgnored
        {
            Active = 10,
            Legacy = 20
        };

        Lua["obj"] = obj;

        // Act & Assert
        Assert.That(Lua.Evaluate<int>("return obj:GetActive()"), Is.EqualTo(10));
        Assert.That(Lua.Evaluate<object?>("return obj.GetLegacy"), Is.Null);
    }

    [Test]
    [Obsolete("Tests obsolete member handling")]
    public void SourceGen_Include_ExposesObsoleteProperty()
    {
        // Arrange
        var obj = new TestObsoleteIncluded
        {
            Active = 10,
            Legacy = 20
        };

        Lua["obj"] = obj;

        // Act
        var active = Lua.Evaluate<int>("return obj.Active");
        var legacy = Lua.Evaluate<int>("return obj.Legacy");

        // Assert
        Assert.That(active, Is.EqualTo(10));
        Assert.That(legacy, Is.EqualTo(20));
    }

    [Test]
    [Obsolete("Tests obsolete member handling")]
    public void SourceGen_Include_ExposesObsoleteMethod()
    {
        // Arrange
        var obj = new TestObsoleteIncluded
        {
            Active = 10,
            Legacy = 20
        };

        Lua["obj"] = obj;

        // Act
        var active = Lua.Evaluate<int>("return obj:GetActive()");
        var legacy = Lua.Evaluate<int>("return obj:GetLegacy()");

        // Assert
        Assert.That(active, Is.EqualTo(10));
        Assert.That(legacy, Is.EqualTo(20));
    }
}
