using System.Collections.Generic;

namespace Laylua;

public unsafe partial class LuaLibraries
{
    /// <summary>
    ///     Defines Laylua-provided Lua libraries.
    /// </summary>
    public static class Laylua
    {
        /// <summary>
        ///     Gets the Laylua async helper library.
        ///     Opening this library exposes the <c>async</c> global to Lua code.
        ///     Async-capable execution uses the same helpers internally without exposing that global implicitly.
        /// </summary>
        public static LuaLibrary Async { get; } = new AsyncLibrary();

        private sealed class AsyncLibrary : LuaLibrary
        {
            private const string LibraryName = "laylua.async";
            private const string GlobalName = "async";

            private static readonly IReadOnlyList<string> LibraryGlobals =
            [
                GlobalName
            ];

            public override string Name => LibraryName;

            public override IReadOnlyList<string> Globals => LibraryGlobals;

            protected internal override void Open(Lua lua, bool leaveOnStack)
            {
                LuaAsyncLibrary.EnsureInitialized(lua);

                var L = lua.State.L;
                LuaAsyncLibrary.PushLibrary(L);
                lua_pushvalue(L, -1);
                lua_setglobal(L, GlobalName);

                if (!leaveOnStack)
                {
                    lua_pop(L);
                }
            }

            protected internal override void Close(Lua lua)
            {
                var L = lua.State.L;
                lua_pushnil(L);
                lua_setglobal(L, GlobalName);
            }
        }
    }
}
