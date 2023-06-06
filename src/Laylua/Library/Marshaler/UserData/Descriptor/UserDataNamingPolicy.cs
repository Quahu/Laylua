namespace Laylua.Marshaling;

/// <summary>
///     Represents the type responsible for converting type member names.
/// </summary>
public abstract class UserDataNamingPolicy
{
    /// <summary>
    ///     Gets a naming policy that converts names to lower-case characters.
    /// </summary>
    /// <returns>
    ///     The lower case naming policy.
    /// </returns>
    public static UserDataNamingPolicy LowerCase { get; } = new LowerCaseUserDataNamingPolicy();

    /// <summary>
    ///     Gets a naming policy that does not convert any characters.
    /// </summary>
    /// <returns>
    ///     The original naming policy.
    /// </returns>
    public static UserDataNamingPolicy Original { get; } = new OriginalUserDataNamingPolicy();

    /// <summary>
    ///     Converts the specified name according to this naming policy.
    /// </summary>
    /// <param name="name"> The name to convert. </param>
    /// <returns>
    ///     The converted name.
    /// </returns>
    public abstract string ConvertName(string name);

    private sealed class LowerCaseUserDataNamingPolicy : UserDataNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return name.ToLowerInvariant();
        }
    }

    private sealed class OriginalUserDataNamingPolicy : UserDataNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return name;
        }
    }
}
