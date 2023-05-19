namespace Laylua.Marshalling;

public abstract class UserDataDescriptor
{
    public virtual string? Name => null;

    public abstract string MetatableName { get; }

    protected UserDataDescriptor()
    { }

    public abstract void OnMetatableCreated(Lua lua, LuaStackValue metatable);
}
