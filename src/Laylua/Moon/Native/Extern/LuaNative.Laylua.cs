using System;
using System.Runtime.CompilerServices;
using System.Text;
using Qommon.Pooling;

namespace Laylua.Moon;

public static unsafe partial class LuaNative
{
    /*
     *
     *  Laylua API extras
     *
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void lua_pop(lua_State* L)
    {
        lua_pop(L, 1);
    }

    public static LuaType lua_rawgetglobal(lua_State* L, ReadOnlySpan<char> name)
    {
        luaL_checkstack(L, 2, null);

        lua_pushglobaltable(L);
        lua_pushstring(L, name);
        var type = lua_rawget(L, -2);
        lua_remove(L, -2);
        return type;
    }

    public static LuaType lua_rawgetglobal(lua_State* L, LuaString name)
    {
        luaL_checkstack(L, 2, null);

        lua_pushglobaltable(L);
        lua_pushstring(L, name);
        var type = lua_rawget(L, -2);
        lua_remove(L, -2);
        return type;
    }

    public static void lua_rawsetglobal(lua_State* L, ReadOnlySpan<char> name)
    {
        luaL_checkstack(L, 2, null);

        lua_pushglobaltable(L);
        lua_pushstring(L, name);
        lua_rotate(L, -3, 2);
        lua_rawset(L, -3);
        lua_pop(L);
    }

    public static void lua_rawsetglobal(lua_State* L, LuaString name)
    {
        luaL_checkstack(L, 2, null);

        lua_pushglobaltable(L);
        lua_pushstring(L, name);
        lua_rotate(L, -3, 2);
        lua_rawset(L, -3);
        lua_pop(L);
    }

    /*
     *
     *  String marshaling
     *
     */
    private static string? FromStringPtr(byte* ptr, nuint len)
    {
        if (ptr == null)
            return null;

        return Encoding.UTF8.GetString(ptr, (int) len);
    }

    private static StringBytes ToCString(ReadOnlySpan<char> s)
    {
        if (s.Length == 0)
            return default;

        var needsTerminator = s[^1] != 0;
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(s.Length);

        if (needsTerminator)
            maxByteCount++;

        var bytes = RentedArray<byte>.Rent(maxByteCount);
        var len = Encoding.UTF8.GetBytes(s, bytes);

        if (needsTerminator)
        {
            bytes[len] = 0;
            len++;
        }

        return new StringBytes(bytes, len);
    }

    private static StringBytes ToLString(ReadOnlySpan<char> s, out nuint len)
    {
        if (s.Length == 0)
        {
            len = 0;
            return default;
        }

        var maxByteCount = Encoding.UTF8.GetMaxByteCount(s.Length);
        var bytes = RentedArray<byte>.Rent(maxByteCount);
        len = (nuint) Encoding.UTF8.GetBytes(s, bytes);
        return new StringBytes(bytes, (int) len);
    }

    private readonly ref struct StringBytes
    {
        private readonly RentedArray<byte> _array;
        private readonly Span<byte> _span;

        public StringBytes(RentedArray<byte> array, int len)
        {
            _array = array;
            _span = array.AsSpan(0, len);
        }

        public ref byte GetPinnableReference()
        {
            return ref _span.GetPinnableReference();
        }

        public void Dispose()
        {
            _array.Dispose();
        }
    }
}
