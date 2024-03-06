using System.Collections.Generic;
using System.Linq;
using Laylua.Moon;

namespace Laylua;

public unsafe partial class LuaLibraries
{
    /// <summary>
    ///     Defines the Lua standard libraries.
    ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#6">Lua manual</a>. </para>
    /// </summary>
    /// <remarks>
    ///     These libraries provide a plethora of functionality out of the box.
    ///     However, a significant portion of the feature set is unsafe for use
    ///     with external code that you cannot verify the safety of.
    ///     <para/>
    ///     Load all libraries freely (using <see cref="EnumerateAll"/> or <see cref="LuaExtensions.OpenStandardLibraries"/>)
    ///     if the Lua code being executed is solely your own and known to be safe.
    ///     Otherwise, exercise caution by ensuring the standard libraries being loaded are safe.
    ///     <para/>
    ///     List of standard libraries that are safe (or nearly safe) to use with unverified code:
    ///     <list type="bullet">
    ///         <item>
    ///             <term> <see cref="Coroutine"/> </term>
    ///             <description> provides coroutine management. </description>
    ///         </item>
    ///         <item>
    ///             <term> <see cref="Table"/> </term>
    ///             <description> provides table management. </description>
    ///         </item>
    ///         <item>
    ///             <term> <see cref="String"/> </term>
    ///             <description>
    ///                 provides string management.
    ///                 <para> Contains a potentially unsafe <a href="https://www.lua.org/manual/5.4/manual.html#pdf-string.dump"><c>string.dump()</c></a> function,
    ///                 which allows viewing the binary representation of a function. </para>
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term> <see cref="UTF8"/> </term>
    ///             <description> provides UTF8 character management. </description>
    ///         </item>
    ///         <item>
    ///             <term> <see cref="Math"/> </term>
    ///             <description> provides mathematical functions. </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static class Standard
    {
        /// <summary>
        ///     Gets a library that opens the basic Lua standard library.
        ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#6.1">Lua manual</a>. </para>
        /// </summary>
        public static LuaLibrary Base { get; } = new BaseLibrary("basic", "assert", "collectgarbage", "dofile", "error", "getmetatable", "ipairs", "loadfile", "load",
            "next", "pairs", "pcall", "print", "warn", "rawequal", "rawlen", "rawget", "rawset", "select", "setmetatable", "tonumber", "tostring", "type", "xpcall",
            "_G", "_VERSION");

        /// <summary>
        ///     Gets a library that opens the coroutine Lua standard library.
        ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#6.2">Lua manual</a>. </para>
        /// </summary>
        public static LuaLibrary Coroutine { get; } = new RequireLibrary(LUA_COLIBNAME, luaopen_coroutine);

        /// <summary>
        ///     Gets a library that opens the coroutine Lua standard library.
        ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#6.6">Lua manual</a>. </para>
        /// </summary>
        public static LuaLibrary Table { get; } = new RequireLibrary(LUA_TABLIBNAME, luaopen_table);

        /// <summary>
        ///     Gets a library that opens the IO Lua standard library.
        ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#6.8">Lua manual</a>. </para>
        /// </summary>
        public static LuaLibrary IO { get; } = new RequireLibrary(LUA_IOLIBNAME, luaopen_io);

        /// <summary>
        ///     Gets a library that opens the OS Lua standard library.
        ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#6.9">Lua manual</a>. </para>
        /// </summary>
        public static LuaLibrary OS { get; } = new RequireLibrary(LUA_OSLIBNAME, luaopen_os);

        /// <summary>
        ///     Gets a library that opens the string Lua standard library.
        ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#6.4">Lua manual</a>. </para>
        /// </summary>
        public static LuaLibrary String { get; } = new RequireLibrary(LUA_STRLIBNAME, luaopen_string);

        /// <summary>
        ///     Gets a library that opens the UTF-8 Lua standard library.
        ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#6.5">Lua manual</a>. </para>
        /// </summary>
        public static LuaLibrary UTF8 { get; } = new RequireLibrary(LUA_UTF8LIBNAME, luaopen_utf8);

        /// <summary>
        ///     Gets a library that opens the math Lua standard library.
        ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#6.7">Lua manual</a>. </para>
        /// </summary>
        public static LuaLibrary Math { get; } = new RequireLibrary(LUA_MATHLIBNAME, luaopen_math);

        /// <summary>
        ///     Gets a library that opens the debug Lua standard library.
        ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#6.10">Lua manual</a>. </para>
        /// </summary>
        public static LuaLibrary Debug { get; } = new RequireLibrary(LUA_DBLIBNAME, luaopen_debug);

        /// <summary>
        ///     Gets a library that opens the package Lua standard library.
        ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#6.3">Lua manual</a>. </para>
        /// </summary>
        public static LuaLibrary Package { get; } = new RequireLibrary(LUA_LOADLIBNAME, luaopen_package, "require");

        /// <summary>
        ///     Enumerates all standard libraries defined within this type.
        /// </summary>
        /// <returns>
        ///     An enumerable yielding all standard libraries.
        /// </returns>
        public static IEnumerable<LuaLibrary> EnumerateAll()
        {
            yield return Base;
            yield return Coroutine;
            yield return Table;
            yield return IO;
            yield return OS;
            yield return String;
            yield return UTF8;
            yield return Math;
            yield return Debug;
            yield return Package;
        }

        private class BaseLibrary : LuaLibrary
        {
            public override string Name { get; }

            public override IReadOnlyList<string> Globals => _globals;

            private readonly string[] _globals;

            public BaseLibrary(string name, params string[] globals)
            {
                Name = name;
                _globals = globals;
            }

            protected internal override void Open(Lua lua, bool leaveOnStack)
            {
                lua.Stack.EnsureFreeCapacity(1);

                var L = lua.GetStatePointer();
                luaopen_base(L);

                if (!leaveOnStack)
                    lua_pop(L);
            }

            protected internal override void Close(Lua lua)
            {
                lua.Stack.EnsureFreeCapacity(2);

                var L = lua.GetStatePointer();
                using (lua.Stack.SnapshotCount())
                {
                    lua_pushglobaltable(L);
                    foreach (var global in _globals)
                    {
                        lua_pushnil(L);
                        lua_setfield(L, -2, global);
                    }
                }
            }
        }

        private sealed class RequireLibrary : BaseLibrary
        {
            private readonly string _global;
            private readonly LuaCFunction _openFunction;

            public RequireLibrary(string global, LuaCFunction openFunction, params string[] extraGlobals)
                : base(global, extraGlobals.Append(global).ToArray())
            {
                _global = global;
                _openFunction = openFunction;
            }

            protected internal override void Open(Lua lua, bool leaveOnStack)
            {
                lua.Stack.EnsureFreeCapacity(1);

                var L = lua.GetStatePointer();
                luaL_requiref(L, _global, _openFunction, true);

                if (!leaveOnStack)
                    lua_pop(L);
            }

            protected internal override void Close(Lua lua)
            {
                base.Close(lua);

                var L = lua.GetStatePointer();
                using (lua.Stack.SnapshotCount())
                {
                    luaL_getsubtable(L, LuaRegistry.Index, LUA_LOADED_TABLE);
                    lua_pushnil(L);
                    lua_setfield(L, -2, Name);
                }
            }
        }
    }
}
