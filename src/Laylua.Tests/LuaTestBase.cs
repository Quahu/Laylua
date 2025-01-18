using System.Globalization;
using System.Text;
using Laylua.Marshaling;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Laylua.Tests;

[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[SingleThreaded]
public abstract unsafe class LuaTestBase
{
    protected TestContext Context = null!;
    protected ILogger Logger = null!;

    protected Lua Lua = null!;
    protected lua_State* L;

    protected static readonly ILoggerFactory LoggerFactory;

    static LuaTestBase()
    {
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        LoggerFactory = new SerilogLoggerFactory(serilogLogger);

        LuaMarshaler.Default.FormatProvider = CultureInfo.InvariantCulture;
    }

    protected LuaTestBase()
    { }

    [SetUp]
    public virtual void Setup()
    {
        Context = TestContext.CurrentContext;
        Logger = LoggerFactory.CreateLogger(Context.Test.Name);

        var allocator = CreateLuaAllocator();
        Lua = CreateLua(allocator);
        L = Lua.State.L;
    }

    [TearDown]
    public virtual void Teardown()
    {
        var allocator = Lua.State.Allocator;
        using (Lua)
        {
            if (Context.Result.FailCount == 0)
            {
                try
                {
                    AssertStackCount(0);
                }
                catch (AssertionException ex)
                {
                    throw new AssertionException("Garbage on the Lua stack.", ex);
                }
            }
        }

        if (allocator is NativeMemoryLuaAllocator nativeMemoryLuaAllocator)
        {
            Assert.That((int) nativeMemoryLuaAllocator.CurrentlyAllocatedBytes, Is.EqualTo(0), "Lua failed to free memory.");

#if TRACE_ALLOCS
            Logger.LogInformation("Lua allocated {0} times for a total of {1}KiB", nativeMemoryLuaAllocator.TimesAllocated, Math.Round(nativeMemoryLuaAllocator.TotalAllocatedBytes / 1024.0, 2));
#endif
        }
    }

    protected virtual LuaAllocator CreateLuaAllocator(nuint maxBytes = 0)
    {
        var allocator = new NativeMemoryLuaAllocator(maxBytes);

#if TRACE_ALLOCS
        NativeMemoryLuaAllocatorLogging.Hook(allocator, LoggerFactory.CreateLogger("Alloc"));
#endif
        return allocator;
    }

    protected virtual Lua CreateLua(LuaAllocator? allocator = null)
    {
        return new Lua(allocator ?? CreateLuaAllocator());
    }

    protected virtual void AssertStackCount(int expected)
    {
        var top = lua_gettop(L);
        try
        {
            Assert.That(top, Is.EqualTo(expected), "Stack count mismatch.");
        }
        catch
        {
            DumpStack(Context.Test.Name);
            throw;
        }
    }

    public void DumpStack(string? location = null)
    {
        DumpStack(TestContext.WriteLine, location);
    }

    public void DumpStack(Action<string, object[]> writer, string? location = null)
    {
        var top = lua_gettop(L);
        var sb = new StringBuilder();
        for (var i = 1; i <= top; i++)
        {
            sb.Append($"@{i} => <{luaL_typename(L, i)}> = '{luaL_tostring(L, i).ToString()}'\n");
            lua_pop(L);
        }

        sb.Append(new string('=', 20));
        writer("Stack ({0} values){1} -->\n{2}", [top, location != null ? $" @{location}" : "", sb]);
    }
}
