using System.Runtime.CompilerServices;
using Laylua.Moon;

namespace Laylua;

internal static unsafe class LuaExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static lua_State* GetStatePointer(this LuaThread lua)
    {
        if (lua.IsDisposed)
            return null;

        return lua.State.L;
    }
}
