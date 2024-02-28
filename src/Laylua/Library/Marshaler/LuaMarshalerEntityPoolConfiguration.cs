namespace Laylua.Marshaling;

public class LuaMarshalerEntityPoolConfiguration
{
    /// <summary>
    ///     Gets or sets the table pool capacity. Defaults to <c>128</c>.
    /// </summary>
    public int TablePoolCapacity { get; set; } = 128;

    /// <summary>
    ///     Gets or sets the function pool capacity. Defaults to <c>64</c>.
    /// </summary>
    public int FunctionPoolCapacity { get; set; } = 64;

    /// <summary>
    ///     Gets or sets the user data pool capacity. Defaults to <c>16</c>.
    /// </summary>
    public int UserDataPoolCapacity { get; set; } = 16;

    /// <summary>
    ///     Gets or sets the thread pool capacity. Defaults to <c>16</c>.
    /// </summary>
    public int ThreadPoolCapacity { get; set; } = 16;
}
