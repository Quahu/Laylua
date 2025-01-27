using System;
using System.Runtime.InteropServices;

namespace Laylua.Moon;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct lua_Debug
{
    public LuaEvent @event;

    public byte* name; /* (n) */
    public byte* namewhat; /* (n) */
    public byte* what; /* (S) */
    public byte* source; /* (S) */
    public nuint srclen; /* (S) */
    public int currentline; /* (l) */
    public int linedefined; /* (S) */
    public int lastlinedefined; /* (S) */
    public byte nups; /* (u) number of upvalues */
    public byte nparams; /* (u) number of parameters */
    public bool isvararg; /* (u) */
    public bool istailcall; /* (t) */
    public ushort ftransfer; /* (r) index of first value transferred */
    public ushort ntransfer; /* (r) number of transferred values */
    public fixed byte short_src[LUA_IDSIZE]; /* (S) */

    private IntPtr i_ci; /* call info */

    public string? GetName()
    {
        return Marshal.PtrToStringAnsi((IntPtr) name);
    }

    public string? GetNameWhat()
    {
        return Marshal.PtrToStringAnsi((IntPtr) namewhat);
    }

    public string? GetWhat()
    {
        return Marshal.PtrToStringAnsi((IntPtr) what);
    }

    public string? GetSource()
    {
        return Marshal.PtrToStringUTF8((IntPtr) source, (int) srclen);
    }

    public string? GetShortSource()
    {
        fixed (byte* ptr = short_src)
        {
            return Marshal.PtrToStringUTF8((IntPtr) ptr);
        }
    }
}
