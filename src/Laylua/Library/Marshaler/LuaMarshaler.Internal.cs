namespace Laylua.Marshaling;

public abstract partial class LuaMarshaler
{
    protected internal abstract void RemoveUserDataHandle(UserDataHandle handle);

    /// <summary>
    ///     Returns a <see cref="LuaReference"/> to the entity pool of this marshaler.
    /// </summary>
    /// <param name="reference"> The Lua reference to return. </param>
    internal bool ReturnReference(LuaReference reference)
    {
        return _entityPool?.Return(reference) ?? false;
    }
}
