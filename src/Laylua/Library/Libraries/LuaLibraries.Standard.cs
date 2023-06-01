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
    ///     These libraries provide a ton of functionality out of the box,
    ///     however a large part of the feature set is unsafe for use with external
    ///     code that you cannot verify the safety of.
    ///     <para/>
    ///     Freely load all libraries (by using <see cref="EnumerateAll"/> or <see cref="LuaExtensions.OpenStandardLibraries"/>) if the Lua code you are executing
    ///     is exclusively your own code and you know it is safe.
    ///     Otherwise ensure the standard libraries you are loading are safe.
    ///     <para/>
    ///     List of standard libraries that are safe (or almost safe) by themselves:
    ///     <list type="bullet">
    ///         <item>
    ///             <term> Coroutine </term>
    ///             <description> provides coroutine management. </description>
    ///         </item>
    ///         <item>
    ///             <term> Table </term>
    ///             <description> provides table management. </description>
    ///         </item>
    ///         <item>
    ///             <term> String </term>
    ///             <description>
    ///                 provides string management.
    ///                 <para> Contains a possibly unsafe <a href="https://www.lua.org/manual/5.4/manual.html#pdf-string.dump"><c>string.dump()</c></a>
    ///                 that allows viewing the binary representation of a function. </para>
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term> Utf8 </term>
    ///             <description> provides UTF8 character management. </description>
    ///         </item>
    ///         <item>
    ///             <term> Math </term>
    ///             <description> provides mathematical functions. </description>
    ///         </item>
    ///     </list>
    ///     The rest of the standard libraries range from possibly unsafe to simply dangerous.
    ///     <para/>
    ///     Remember that Lua code can overwrite the global variable(s) a library uses
    ///     or even overwrite its functionality, possibly creating a security risk.
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
