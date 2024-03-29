using Qommon;

namespace Laylua.Marshaling;

public abstract partial class LuaMarshaler
{
    protected internal abstract void RemoveUserDataHandle(UserDataHandle handle);

    /// <summary>
    ///     Returns a <see cref="LuaReference"/> to the entity pool of this marshaler.
    /// </summary>
    /// <param name="reference"> The Lua reference to return. </param>
    internal void ReturnReference(LuaReference reference)
    {
#if DEBUG
        if (LuaReference.IsAlive(reference))
        {
            Throw.ArgumentException($"The given {reference.GetType().Name.SingleQuoted()} is alive and cannot be returned to the pool.", nameof(reference));
        }
#endif

        _entityPool?.Return(reference);
    }
}
