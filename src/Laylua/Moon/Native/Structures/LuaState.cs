using System.Runtime.InteropServices;

namespace Laylua.Moon;

/// <summary>
///     Represents the state of a Lua thread.
/// </summary>
/// <remarks>
///     A pointer to this structure is required by every Lua API method,
///     except for the methods that create a new standalone state.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct lua_State
{ }
