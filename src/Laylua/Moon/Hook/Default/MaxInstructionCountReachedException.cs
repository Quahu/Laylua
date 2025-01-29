using System;

namespace Laylua.Moon;

/// <summary>
///     Thrown by <see cref="MaxInstructionCountLuaHook"/> when the maximum instruction count is reached.
/// </summary>
public sealed class MaxInstructionCountReachedException(int instructionCount, ReadOnlySpan<char> nameSpan)
    : Exception($"The maximum instruction count of {instructionCount} was reached by {(nameSpan.IsEmpty || nameSpan.IsWhiteSpace() ? "main code" : $"'{nameSpan}'")}.")
{
    /// <summary>
    ///     Gets the reached maximum instruction count.
    /// </summary>
    public int InstructionCount { get; } = instructionCount;
}
