using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Laylua.Moon;

namespace Laylua;

public static unsafe class LuaExtensions
{
    /// <summary>
    ///     Opens all standard libraries using <see cref="LuaLibraries.Standard.EnumerateAll"/>.
    /// </summary>
    /// <param name="lua"> The Lua instance. </param>
    public static void OpenStandardLibraries(this Lua lua)
    {
        foreach (var library in LuaLibraries.Standard.EnumerateAll())
        {
            lua.OpenLibrary(library);
        }
    }

    /// <summary>
    ///     Closes all standard libraries using <see cref="LuaLibraries.Standard.EnumerateAll"/>.
    /// </summary>
    /// <param name="lua"> The Lua instance. </param>
    public static void CloseStandardLibraries(this Lua lua)
    {
        foreach (var library in LuaLibraries.Standard.EnumerateAll())
        {
            lua.CloseLibrary(library.Name);
        }
    }

    public static bool CloseLibrary(this Lua lua, ILuaLibrary library)
    {
        return lua.CloseLibrary(library.Name);
    }

    [Conditional("DEBUG")]
    public static void DumpStack(this Lua lua, string? location = null)
    {
        lua.DumpStack(Console.WriteLine, location);
    }

    [Conditional("DEBUG")]
    public static void DumpStack(this Lua lua, Action<string, object[]> writer, string? location = null)
    {
        var L = lua.GetStatePointer();
        var top = lua_gettop(L);
        var sb = new StringBuilder();
        for (var i = 1; i <= top; i++)
        {
            sb.Append($"@{i} => <{luaL_typename(L, i)}> = {luaL_tostring(L, i).SingleQuoted().ToString()}\n");
            lua_pop(L);
        }

        sb.Append(new string('=', 20));
        writer("Stack ({0} values){1} -->\n{2}", new object[] { top, location != null ? $" @{location}" : "", sb });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static lua_State* GetStatePointer(this Lua lua)
    {
        if (lua.IsDisposed)
            return null;

        return lua.State.L;
    }
}
