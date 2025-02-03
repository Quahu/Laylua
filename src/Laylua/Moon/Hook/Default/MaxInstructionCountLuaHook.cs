using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace Laylua.Moon;

/// <summary>
///     Represents a Lua hook that raises an error
///     when the specified instruction count is reached.
/// </summary>
public sealed unsafe class MaxInstructionCountLuaHook : LuaHook
{
    /// <inheritdoc/>
    protected internal override int InstructionCount { get; }

    /// <inheritdoc/>
    protected internal override LuaEventMask EventMask => LuaEventMask.Count;

    /// <summary>
    ///     Instantiates a new <see cref="MaxInstructionCountLuaHook"/>.
    /// </summary>
    /// <param name="maxInstructionCount"> The instruction count to trigger on. </param>
    public MaxInstructionCountLuaHook(int maxInstructionCount)
    {
        InstructionCount = maxInstructionCount;
    }

    /// <inheritdoc/>
    protected internal override void Execute(LuaThread thread, LuaEvent @event, ref LuaDebug debug)
    {
        char[]? rentedArray = null;
        Exception exception;
        try
        {
            scoped Span<char> nameSpan;
            var functionName = debug.FunctionName;
            if (functionName.Pointer != null)
            {
                var charCount = functionName.CharLength;
                nameSpan = charCount > 256
                    ? (rentedArray = ArrayPool<char>.Shared.Rent(charCount)).AsSpan(0, charCount)
                    : stackalloc char[charCount];

                functionName.GetChars(nameSpan);
            }
            else
            {
                nameSpan = [];
            }

            exception = new MaxInstructionCountReachedException(InstructionCount, nameSpan);
        }
        finally
        {
            if (rentedArray != null)
            {
                ArrayPool<char>.Shared.Return(rentedArray);
            }
        }

        thread.RaiseError(exception);
    }
}
