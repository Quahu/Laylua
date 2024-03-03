using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Laylua.Moon;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Laylua
{
    internal unsafe class Program
    {
        public delegate void TestDelegate(lua_State* state);

        private static ILogger Logger = null!;

        private static void Main(string[] args)
        {
            var serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            using var serilogLoggerFactory = new SerilogLoggerFactory(serilogLogger, true);
            Logger = serilogLoggerFactory.CreateLogger<Program>();

            try
            {
                Code();
            }
            catch (Exception ex)
            {
                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Exception: {ex}");
                Console.ForegroundColor = oldColor;
            }
        }

        private static void Code()
        {
            var allocator = new NativeMemoryLuaAllocator(1024 * 1024);

            // NativeMemoryLuaAllocatorLogging.Hook(allocator, LoggerFactory.CreateLogger<NativeMemoryLuaAllocator>());

            Logger.LogInformation("Creating Lua...");
            using (var lua = new Lua(new LuaState(allocator))
            {
                FormatProvider = CultureInfo.InvariantCulture
            })
            {
                lua.State.Panicked += (sender, e) =>
                {
                    Logger.LogInformation(e.Exception, "Panicked!");
                };

                var L = lua.GetStatePointer();

                try
                {
                    static int del(lua_State* L)
                    {
                        return luaL_error(L, "haha yes");
                    }

                    static int deln(lua_State* L)
                    {
                        lua_pushcfunction(L, del);
                        lua_call(L, 0, 0);
                        return 0;
                    }

                    lua_pushcfunction(L, deln);
                    lua_call(L, 0, 0);
                }
                catch (Exception ex)
                {
                    Logger.LogInformation(ex, "Panicked at luacall.");
                }

                var timeout = TimeSpan.FromSeconds(5);
                using (var cts = new CancellationTokenSource(timeout))
                {
                    lua.State.Hook = new CancellationTokenLuaHook(cts.Token);
                    try
                    {
                        var result = lua.Evaluate<int>("return 42");
                    }
                    finally
                    {
                        lua.State.Hook = null;
                    }
                }

                try
                {
                    lua.Stack.EnsureFreeCapacity(3);

                    lua.Stack.Push(42);
                    lua.Stack.Push("Hello, world!");
                    lua.Stack.Push(new[] { 1, 2, 3 });

                    foreach (var value in lua.Stack)
                    {
                        Logger.LogInformation("Stack value {0}", value);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Exception");
                }

                try
                {
                    lua["print"] = (Action<int>) Console.WriteLine;
                    lua["func"] = (string a) =>
                    {
                        lua.Execute(a);
                    };

                    // lua.Execute("print('abc')");

                    lua.Execute("print(func('print(\\'abc\\')'))");

                    Logger.LogError("Didn't catch exception from argument error...");
                }
                catch (Exception ex)
                {
                    Logger.LogInformation(ex, "Caught exception from argument error!");
                }

                try
                {
                    using (lua.Stack.SnapshotCount())
                    {
                        var counter = 0;
                        while (true)
                        {
                            var top = lua_gettop(L);
                            lua.Stack.EnsureFreeCapacity(1);
                            lua_pushstring(L, new string('a', 4096 + counter));
                            Logger.LogInformation("Pushed {0} strings ({1} -> {2})", counter++, top, lua_gettop(L));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogInformation(ex, $"Panicked at {nameof(lua_pushstring)}");
                }

                lua.OpenLibrary(LuaLibraries.Standard.IO);
                using (var stdout = lua.Evaluate<LuaTable>("return io.stdout")!)
                {
                    if (stdout.TryGetMetatable(out var fileMetatable))
                    {
                        using (fileMetatable)
                        {
                            var metaDictionary = fileMetatable.ToDictionary<object, object>();
                            Logger.LogInformation("File metatable: {0}", metaDictionary);
                            LuaReference.Dispose(metaDictionary);
                        }
                    }

                    if (stdout.TryGetValue<string, LuaFunction>("write", out var writeFunction))
                    {
                        using (writeFunction)
                        {
                            writeFunction.Call(stdout, "hello world");
                        }
                    }
                }

                Logger.LogInformation($"L: 0x{(IntPtr) lua.GetStatePointer():X}");
                Logger.LogInformation("Press enter to run lua table code.");
                Console.ReadLine();

                using (var lent = lua.CreateTable())
                {
                    LuaReference.PushValue(lent);
                    lua_createtable(L, 0, 1);
                    lua_pushstring(L, LuaMetatableKeys.__len);
                    LuaCFunction func = L =>
                    {
                        lua_pushinteger(L, 12345678987654321);

                        var top = lua_gettop(L);
                        var sb = new StringBuilder($"Stack ({top} values):\n");
                        for (var i = 1; i <= top; i++)
                        {
                            sb.Append($"@{i} => <{luaL_typename(L, i)}> = '{luaL_tostring(L, i).ToString()}'\n");
                            lua_pop(L);
                        }

                        sb.Append($"{new string('=', 20)}\n");

                        Console.WriteLine(sb.ToString());
                        return 1;
                    };

                    lua_pushcfunction(L, func);

                    lua_rawset(L, -3);

                    lua_pushvalue(L, -1);
                    lua_setfield(L, LuaRegistry.Index, "pluspy");

                    lua.DumpStack();
                    lua_setmetatable(L, -2);

                    Logger.LogInformation($"Press enter to run {nameof(luaL_len)} code.");
                    Console.ReadLine();

                    var length = luaL_len(L, -1);
                    if (length != 12345678987654321)
                        Logger.LogError("Value is messed up ({0} != 12345678987654321)...", length);
                    else
                        Logger.LogInformation("Value is NOT messed up...");

                    lua_pop(L);
                }

                Logger.LogInformation("Press enter to run new LuaTable(lua, int.MaxValue) code.");
                Console.ReadLine();
                try
                {
                    var t = lua.CreateTable(int.MaxValue);
                }
                catch (Exception ex)
                {
                    Logger.LogInformation(ex, "Panicked at max table");
                }

                // return;
                lua_pushglobaltable(L);
                luaL_setfuncs(L, new luaL_Reg[]
                {
                    new("print", L =>
                    {
                        var value = luaL_tostring(L, 1);
                        Console.WriteLine(value);
                        return 0;
                    }),
                    new("sum", L =>
                    {
                        var sum = 0d;
                        var top = lua_gettop(L);
                        for (var i = 1; i <= top; i++)
                        {
                            var number = lua_tonumberx(L, i, out var isNumber);
                            if (!isNumber)
                            {
                                luaL_error(L, $"Argument {i} is not a number.");
                                return 0;
                            }

                            sum += number;
                        }

                        lua_pushnumber(L, sum);
                        return 1;
                    }),
                    luaL_Reg.Null
                }, 0);

                lua_pop(L);

                try
                {
                    try
                    {
                        luaL_error(L, "This must be caught!");
                    }
                    catch (LuaPanicException ex)
                    {
                        Logger.LogInformation(ex, "");
                        lua.DumpStack("panic");
                        Console.ReadLine();
                    }

                    // lua.State.Hook = new MaxInstructionsLuaHook(5000);

                    var stringWithNulls = "testing null char\0 here and here\0";
                    lua_pushstring(L, stringWithNulls);
                    var stringFromLua = lua_tostring(L, -1);
                    Debug.Assert(stringWithNulls == stringFromLua.ToString());

                    lua.Execute("utf8 = '👺🤳'");
                    var utf8 = (string) lua["utf8"]!;
                    Debug.Assert("👺🤳" == utf8);

                    lua.Execute("mixed = { 1, test = 'hello', 2, 3, another = 'hello' }\n"
                        + "mixed[42.5] = 'hello'\n"
                        + "mixed['yep'] = true\n"
                        + "mixed[10] = 20\n"
                        + "mixed[41] = 'hello'"
                        + "mixed[42.5] = 'overwriting 42.5'\n"
                        + "mixed[1] = 'overwriting first'\n"
                        + "mixed[true] = 'ok'");

                    lua.Execute("print(#mixed)");

                    var mixedTable = lua.GetGlobal<LuaTable>("mixed")!;
                    foreach (var kvp in mixedTable)
                    {
                        Console.WriteLine("[{0}] = {1}", kvp.Key, kvp.Value);
                    }

                    lua["array"] = new object[] { true, 42, "Hello, World!" };
                    using (var table = lua["array"] as LuaTable)
                    {
                        var array = table!.Values.ToArray<object>();
                        Logger.LogInformation("Array contents: {0}", (object) array.Select(x => (x!.GetType().Name, x)).ToArray());
                    }

                    lua_getglobal(L, "array");
                    lua_pushnil(L);
                    while (lua_next(L, -2))
                    {
                        Console.WriteLine($"({luaL_typename(L, -1)}): {luaL_tolstring(L, -1, out _)}");
                        lua_pop(L, 2);
                    }

                    lua_pop(L);

                    lua.Execute("print(test.nested)");
                    lua.Execute("print(test.nested)");
                    lua.Execute("print(test.nested)");

                    lua.Execute(@"
test.x = 0.1234
print(test.x .. ' ' .. test.y)
print(test.nested.x .. ' ' .. test.nested.y)
test.hello = 'abc'
-- test.print()
-- test:test()");

                    lua.Execute("x = 183319356489465856");
                    var value = lua["x"];
                    if (value is long)
                    {
                        // is actually integer
                        var ulongValue = (ulong) (long) value;

                        Debug.Assert(ulongValue == ulong.MaxValue);
                    }

                    lua.Execute("print(x)");
                    lua.Execute("print(sum(10, 42, '69'))");

                    Logger.LogInformation("Globals:\n");
                    lua_pushglobaltable(L);
                    lua_pushnil(L);
                    while (lua_next(L, -2))
                    {
                        Logger.LogInformation($"'{lua_tostring(L, -2)}' ({luaL_typename(L, -1)})");
                        lua_pop(L);
                    }

                    lua_pop(L);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Logger.LogInformation(e, "");
                    Console.ResetColor();
                }
            }

            Logger.LogInformation("survived!");
        }
    }
}
