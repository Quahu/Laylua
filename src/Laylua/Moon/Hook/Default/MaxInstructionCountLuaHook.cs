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
    protected internal override void Execute(LuaThread lua, ref LuaDebug debug)
    {
        lua_getinfo(lua.State.L, "Sn", debug.ActivationRecord);

        char[]? rentedArray = null;
        Exception exception;
        try
        {
            scoped Span<char> nameSpan;
            if (debug.ActivationRecord->name != null)
            {
                var utf8NameSpan = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(debug.ActivationRecord->name);
                var charCount = Encoding.UTF8.GetCharCount(utf8NameSpan);
                nameSpan = charCount > 256
                    ? (rentedArray = ArrayPool<char>.Shared.Rent(charCount)).AsSpan(0, charCount)
                    : stackalloc char[charCount];

                Encoding.UTF8.GetChars(utf8NameSpan, nameSpan);
            }
            else
            {
                nameSpan = Span<char>.Empty;
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

        lua.RaiseError(exception);
    }
}
