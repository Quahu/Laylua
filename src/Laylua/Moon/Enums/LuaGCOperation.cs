using System;

namespace Laylua.Moon;

/// <summary>
///     Defines the operation to be performed on the Lua garbage collector.
/// </summary>
public enum LuaGCOperation
{
    /// <summary>
    ///     Stops the garbage collector.
    /// </summary>
    Stop = 0,

    /// <summary>
    ///     Restarts the garbage collector.
    /// </summary>
    Restart = 1,

    /// <summary>
    ///     Forces an immediate garbage collection.
    /// </summary>
    Collect = 2,

    /// <summary>
    ///     Returns the current amount of memory in kilobytes in use by Lua.
    /// </summary>
    Count = 3,

    /// <summary>
    ///     Returns the remainder of dividing the current amount of bytes of memory in use by Lua by <c>1024</c>.
    /// </summary>
    CountRemainder = 4,

    Step = 5,

    [Obsolete($"Use {nameof(Incremental)} instead.")]
    SetPause = 6,

    [Obsolete($"Use {nameof(Incremental)} instead.")]
    SetStepMultiplier = 7,

    IsRunning = 9,

    Generational = 10,

    Incremental = 11
}
