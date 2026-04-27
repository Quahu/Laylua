namespace Laylua.Tests;

public unsafe class TypedUserDataSelfValidationTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
    }

    [Test]
    public void SourceGen_CrossTypeMethodCall_Errors()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        var entity = new TestEntity
        {
            Tag = "NPC",
            Id = 42
        };

        TestPlayer.LuaUserDataHandler.Push(L, player);
        lua_setglobal(L, "player");
        TestEntity.LuaUserDataHandler.Push(L, entity);
        lua_setglobal(L, "entity");

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute(@"
            local takeDamage = player.TakeDamage
            takeDamage(entity, 10)
        "));

        Assert.That(ex!.Message, Does.Contain("TestPlayer"));
    }

    [Test]
    public void SourceGen_CrossTypePropertyRead_Errors()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        var entity = new TestEntity
        {
            Tag = "NPC",
            Id = 42
        };

        TestPlayer.LuaUserDataHandler.Push(L, player);
        lua_setglobal(L, "player");
        TestEntity.LuaUserDataHandler.Push(L, entity);
        lua_setglobal(L, "entity");

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute(@"
            local mt = debug.getmetatable(player)
            local index = rawget(mt, '__index')
            index(entity, 'Name')
        "));

        Assert.That(ex!.Message, Does.Contain("TestPlayer"));
    }

    [Test]
    public void SourceGen_CrossTypePropertyWrite_Errors()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        var entity = new TestEntity
        {
            Tag = "NPC",
            Id = 42
        };

        TestPlayer.LuaUserDataHandler.Push(L, player);
        lua_setglobal(L, "player");
        TestEntity.LuaUserDataHandler.Push(L, entity);
        lua_setglobal(L, "entity");

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute(@"
            local mt = debug.getmetatable(player)
            local newindex = rawget(mt, '__newindex')
            newindex(entity, 'Name', 'hacked')
        "));

        Assert.That(ex!.Message, Does.Contain("TestPlayer"));
    }

    [Test]
    public void SourceGen_CorrectType_Works()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        TestPlayer.LuaUserDataHandler.Push(L, player);
        lua_setglobal(L, "player");

        // Act
        Lua.Execute(@"
            local takeDamage = player.TakeDamage
            takeDamage(player, 10)
        ");

        // Assert
        Assert.That(player.Health, Is.EqualTo(90));
    }

    [Test]
    public void SourceGen_NilSelf_Errors()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        TestPlayer.LuaUserDataHandler.Push(L, player);
        lua_setglobal(L, "player");

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute(@"
            local takeDamage = player.TakeDamage
            takeDamage(nil, 10)
        "));

        Assert.That(ex!.Message, Does.Contain("not a valid userdata"));
    }

    [Test]
    public void SourceGen_StringSelf_Errors()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100
        };

        TestPlayer.LuaUserDataHandler.Push(L, player);
        lua_setglobal(L, "player");

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute(@"
            local takeDamage = player.TakeDamage
            takeDamage('not a userdata', 10)
        "));

        Assert.That(ex!.Message, Does.Contain("not a valid userdata"));
    }
}
