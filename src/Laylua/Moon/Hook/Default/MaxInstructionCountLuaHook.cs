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
    protected internal override void Execute(lua_State* L, lua_Debug* ar)
    {
        lua_getinfo(L, "Sn", ar);

        scoped Span<char> nameSpan;
        char[]? rentedArray = null;
        if (ar->name != null)
        {
            var asciiNameSpan = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(ar->name);
            var charCount = Encoding.Unicode.GetCharCount(asciiNameSpan);
            nameSpan = charCount > 256
                ? (rentedArray = ArrayPool<char>.Shared.Rent(charCount)).AsSpan(0, charCount)
                : stackalloc char[charCount];
        }
        else
        {
            nameSpan = Span<char>.Empty;
        }

        string message;
        try
        {
            message = $"The maximum instruction count of {InstructionCount} was exceeded by {(nameSpan.IsEmpty || MemoryExtensions.IsWhiteSpace(nameSpan) ? "main code" : $"'{nameSpan}'")}.";
        }
        finally
        {
            if (rentedArray != null)
            {
                ArrayPool<char>.Shared.Return(rentedArray);
            }
        }

        luaL_error(L, message);
    }
}
