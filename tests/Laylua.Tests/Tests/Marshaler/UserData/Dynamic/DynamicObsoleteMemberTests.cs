using Laylua.Marshaling;

namespace Laylua.Tests;

public class DynamicObsoleteMemberTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        LuaTypeRegistry.Unregister<DynamicObsoleteTarget>();
    }

    [Test]
    [Obsolete("Tests obsolete member handling")]
    public void Dynamic_DefaultInclude_ExposesObsoleteProperty()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicObsoleteTarget>();

        var obj = new DynamicObsoleteTarget
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
    public void Dynamic_DefaultInclude_ExposesObsoleteMethod()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicObsoleteTarget>();

        var obj = new DynamicObsoleteTarget
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

    [Test]
    [Obsolete("Tests obsolete member handling")]
    public void Dynamic_Ignore_HidesObsoleteProperty()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicObsoleteTarget>(opts =>
        {
            opts.ObsoleteMembers = LuaObsoleteMemberHandling.Ignore;
        });

        var obj = new DynamicObsoleteTarget
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
    public void Dynamic_Ignore_HidesObsoleteMethod()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicObsoleteTarget>(opts =>
        {
            opts.ObsoleteMembers = LuaObsoleteMemberHandling.Ignore;
        });

        var obj = new DynamicObsoleteTarget
        {
            Active = 10,
            Legacy = 20
        };

        Lua["obj"] = obj;

        // Act & Assert
        Assert.That(Lua.Evaluate<int>("return obj:GetActive()"), Is.EqualTo(10));
        Assert.That(Lua.Evaluate<object?>("return obj.GetLegacy"), Is.Null);
    }
}
