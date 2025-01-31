namespace Laylua.Marshaling;

public class OverloadedMethodUserDataDescriptor : CallUserDataDescriptor
{
    /// <inheritdoc/>
    public override string MetatableName => "overloaded_method";

    public OverloadedMethodUserDataDescriptor()
    { }

    /// <summary>
    ///     By default, calls the method.
    /// </summary>
    /// <param name="thread"> The Lua thread. </param>
    /// <param name="userData"> The user data. </param>
    /// <param name="arguments"> The function arguments. </param>
    /// <returns>
    ///     The amount of values pushed onto the stack.
    /// </returns>
    public override int Call(LuaThread thread, LuaStackValue userData, LuaStackValueRange arguments)
    {
        return thread.RaiseError("Calling overloaded methods is currently not supported.");
    }
}
