using System.Runtime.InteropServices;

namespace Laylua.Moon;

/// <summary>
///     Represents a Lua continuation function.
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void LuaKFunction(lua_State* L, LuaStatus status, void* ctx);

/// <summary>
///     Represents a Lua C function.
/// </summary>
/// <param name="L"> The Lua state. </param>
/// <returns>
///     The amount of values put on the stack, i.e. how many values the function returned.
/// </returns>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate int LuaCFunction(lua_State* L);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate byte* LuaReaderFunction(lua_State* L, void* ud, out nuint sz);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate int LuaWriterFunction(lua_State* L, void* p, nuint sz, void* ud);

/// <summary>
///     Represents a Lua warn function.
/// </summary>
/// <param name="ud"> The userdata. </param>
/// <param name="msg"> The message. </param>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void LuaWarnFunction(void* ud, [MarshalAs(UnmanagedType.LPStr)] string msg, int tocont);

/// <summary>
///     Represents a Lua allocation function.
/// </summary>
/// <param name="ud"> The pointer to userdata. </param>
/// <param name="ptr"> The allocation pointer. </param>
/// <param name="osize"> The old allocation size or the type of the object allocated. </param>
/// <param name="nsize"> The new allocation size. </param>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void* LuaAllocFunction(void* ud, void* ptr, nuint osize, nuint nsize);

/// <summary>
///     Represents a Lua hook function.
/// </summary>
/// <param name="L"> The Lua state. </param>
/// <param name="ar"> The pointer to the debug information. </param>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void LuaHookFunction(lua_State* L, lua_Debug* ar);
