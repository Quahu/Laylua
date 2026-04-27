namespace Laylua.Marshaling;

/// <summary>
///     Controls the dispatch priority order for <c>__index</c> and <c>__newindex</c>.
/// </summary>
public enum LuaIndexPriority
{
    /// <summary>
    ///     Try named members before falling back to the C# indexer.
    /// </summary>
    MembersFirst = 1,

    /// <summary>
    ///     Try the C# indexer before falling back to named members.
    /// </summary>
    IndexerFirst = 2,
}
