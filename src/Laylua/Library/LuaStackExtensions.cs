using System;

namespace Laylua;

public static class LuaStackExtensions
{
    /// <summary>
    ///     Snapshots the current stack count and returns a disposable instance
    ///     that will restore it upon disposal.
    /// </summary>
    /// <remarks>
    ///     If the amount of values on the stack is lower than the initial amount,
    ///     then when the snapshot is disposed the missing values are filled with nil.
    /// </remarks>
    /// <example>
    ///     The purpose of this is to easily prevent
    ///     leaving garbage values on the Lua stack when
    ///     performing multiple stack operations where any of them might fail.
    ///     <code language="csharp">
    ///     using (/* count snapshot */)
    ///     {
    ///         // push x
    ///         // push y
    ///         // logic
    ///     }
    ///     </code>
    ///     In the above example, if <c>push y</c> or <c>logic</c> fails, i.e. throws an exception,
    ///     the stack will be restored to the count prior to the code within the using block.
    /// </example>
    /// <param name="stack"> The Lua stack. </param>
    /// <returns>
    ///     A <see cref="LuaStackCountSnapshot"/>.
    /// </returns>
    public static LuaStackCountSnapshot SnapshotCount(this LuaStack stack)
    {
        return new LuaStackCountSnapshot(stack);
    }
}

/// <summary>
///     Represents a snapshot of <see cref="LuaStack.Count"/> which
///     will be reset when this type is disposed.
/// </summary>
public struct LuaStackCountSnapshot : IDisposable
{
    /// <summary>
    ///     Gets the count of this snapshot.
    /// </summary>
    public readonly int Count => _count;

    private LuaStack? _stack;
    private readonly int _count;

    /// <summary>
    ///     Instantiates a new <see cref="LuaStackCountSnapshot"/>.
    /// </summary>
    /// <param name="stack"> The stack. </param>
    public LuaStackCountSnapshot(LuaStack stack)
    {
        _stack = stack;
        _count = stack.Count;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        var stack = _stack;
        if (stack == null)
            return;

        stack.Count = _count;
        _stack = null;
    }
}
