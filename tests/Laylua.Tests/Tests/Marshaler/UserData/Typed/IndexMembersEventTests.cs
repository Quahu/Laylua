namespace Laylua.Tests;

public unsafe class IndexMembersEventTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
    }

    [Test]
    public void IndexMembers_NoEvents_EventNotAccessible()
    {
        // Arrange
        var obj = new TestIndexMembersNoEvents();
        TestIndexMembersNoEvents.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act & Assert
        Lua.Execute("assert(obj.SimpleEvent == nil)");
    }

    [Test]
    public void IndexMembers_NoEvents_PropertiesStillWork()
    {
        // Arrange
        var obj = new TestIndexMembersNoEvents();
        TestIndexMembersNoEvents.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act
        var value = Lua.Evaluate<int>("return obj.Value");

        // Assert
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void IndexMembers_NoEvents_MethodsStillWork()
    {
        // Arrange
        var obj = new TestIndexMembersNoEvents();
        TestIndexMembersNoEvents.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act
        var value = Lua.Evaluate<int>("return obj:GetValue()");

        // Assert
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void IndexMembers_EventsOnly_EventAccessible()
    {
        // Arrange
        var obj = new TestIndexMembersEventsOnly();
        TestIndexMembersEventsOnly.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act
        Lua.Execute(@"
            fired = false
            obj.SimpleEvent:add(function() fired = true end)
        ");

        obj.FireSimple();

        // Assert
        Lua.Execute("assert(fired == true)");
    }

    [Test]
    public void IndexMembers_EventsOnly_PropertiesNotAccessible()
    {
        // Arrange
        var obj = new TestIndexMembersEventsOnly();
        TestIndexMembersEventsOnly.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act & Assert
        Lua.Execute("assert(obj.Value == nil)");
    }

    [Test]
    public void IndexMembers_EventsOnly_MethodsNotAccessible()
    {
        // Arrange
        var obj = new TestIndexMembersEventsOnly();
        TestIndexMembersEventsOnly.LuaUserDataHandler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Act & Assert
        Lua.Execute("assert(obj.GetValue == nil)");
    }
}
