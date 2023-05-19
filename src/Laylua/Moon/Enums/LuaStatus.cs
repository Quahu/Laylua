namespace Laylua.Moon;

/// <summary>
///     Defines Lua operation statuses.
/// </summary>
public enum LuaStatus
{
    /// <summary>
    ///     Represents <c>OK</c>, i.e. no errors.
    /// </summary>
    Ok = 0,

    /// <summary>
    ///     Represents <c>YIELD</c>, i.e. the coroutine yielded.
    /// </summary>
    Yield = 1,

    /// <summary>
    ///     Represents <c>ERRRUN</c>, i.e. a runtime error.
    /// </summary>
    RuntimeError = 2,

    /// <summary>
    ///     Represents <c>ERRSYN</c>, i.e. a syntax error.
    /// </summary>
    SyntaxError = 3,

    /// <summary>
    ///     Represents <c>ERRMEM</c>, i.e. a memory allocation error.
    /// </summary>
    MemoryError = 4,

    /// <summary>
    ///     Represents <c>ERRERR</c>, i.e. a message handler error.
    /// </summary>
    HandlerError = 5,

    /// <summary>
    ///     Represents <c>ERRFILE</c>, i.e. a file loading error.
    /// </summary>
    FileError = 6
}