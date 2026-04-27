using Laylua.Marshaling;

namespace Laylua.Tests;

public class DynamicSelfValidationEntity
{
    public string Tag { get; set; } = "";

    public int Id { get; set; }
}

public unsafe class DynamicSelfValidationTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
        LuaTypeRegistry.Unregister<ReflectionTarget>();
        LuaTypeRegistry.Unregister<DynamicSelfValidationEntity>();
    }

    [Test]
    public void Dynamic_CrossTypeMethodCall_Errors()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ReflectionTarget>();

        LuaTypeRegistry.ConfigureDynamic<DynamicSelfValidationEntity>();

        var target = new ReflectionTarget
        {
            Name = "target",
            Value = 42
        };

        var entity = new DynamicSelfValidationEntity
        {
            Tag = "NPC",
            Id = 1
        };

        Marshaler.PushValue(Lua.MainThread, target);
        lua_setglobal(L, "target");
        Marshaler.PushValue(Lua.MainThread, entity);
        lua_setglobal(L, "entity");

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute(@"
            local add = target.Add
            add(entity, 1, 2)
        "));

        Assert.That(ex!.Message, Does.Contain("ReflectionTarget"));
    }

    [Test]
    public void Dynamic_CrossTypePropertyRead_Errors()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ReflectionTarget>();

        LuaTypeRegistry.ConfigureDynamic<DynamicSelfValidationEntity>();

        var target = new ReflectionTarget
        {
            Name = "target",
            Value = 42
        };

        var entity = new DynamicSelfValidationEntity
        {
            Tag = "NPC",
            Id = 1
        };

        Marshaler.PushValue(Lua.MainThread, target);
        lua_setglobal(L, "target");
        Marshaler.PushValue(Lua.MainThread, entity);
        lua_setglobal(L, "entity");

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute(@"
            local mt = debug.getmetatable(target)
            local index = rawget(mt, '__index')
            index(entity, 'Name')
        "));

        Assert.That(ex!.Message, Does.Contain("ReflectionTarget"));
    }

    [Test]
    public void Dynamic_CrossTypePropertyWrite_Errors()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ReflectionTarget>();

        LuaTypeRegistry.ConfigureDynamic<DynamicSelfValidationEntity>();

        var target = new ReflectionTarget
        {
            Name = "target",
            Value = 42
        };

        var entity = new DynamicSelfValidationEntity
        {
            Tag = "NPC",
            Id = 1
        };

        Marshaler.PushValue(Lua.MainThread, target);
        lua_setglobal(L, "target");
        Marshaler.PushValue(Lua.MainThread, entity);
        lua_setglobal(L, "entity");

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute(@"
            local mt = debug.getmetatable(target)
            local newindex = rawget(mt, '__newindex')
            newindex(entity, 'Name', 'hacked')
        "));

        Assert.That(ex!.Message, Does.Contain("ReflectionTarget"));
    }

    [Test]
    public void Dynamic_CorrectType_Works()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ReflectionTarget>();

        var target = new ReflectionTarget
        {
            Name = "target",
            Value = 42
        };

        Marshaler.PushValue(Lua.MainThread, target);
        lua_setglobal(L, "target");

        // Act
        var result = Lua.Evaluate<int>(@"
            local add = target.Add
            return add(target, 1, 2)
        ");

        // Assert
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Dynamic_NilSelf_Errors()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ReflectionTarget>();

        var target = new ReflectionTarget
        {
            Name = "target",
            Value = 42
        };

        Marshaler.PushValue(Lua.MainThread, target);
        lua_setglobal(L, "target");

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute(@"
            local add = target.Add
            add(nil, 1, 2)
        "));

        Assert.That(ex!.Message, Does.Contain("not a valid userdata"));
    }

    [Test]
    public void Dynamic_StringSelf_Errors()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ReflectionTarget>();

        var target = new ReflectionTarget
        {
            Name = "target",
            Value = 42
        };

        Marshaler.PushValue(Lua.MainThread, target);
        lua_setglobal(L, "target");

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute(@"
            local add = target.Add
            add('not a userdata', 1, 2)
        "));

        Assert.That(ex!.Message, Does.Contain("not a valid userdata"));
    }
}
