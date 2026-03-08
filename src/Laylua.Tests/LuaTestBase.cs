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

    private Lua _lua = null!;

    protected Lua Lua
    {
        get => _lua;
        set
        {
            _lua = value;
            _lua.WarningEmitted += (_, e) =>
            {
                if (_tracksWarnings)
                {
                    Warnings.Add(e.Message.ToString());
                }
            };
        }
    }

    protected lua_State* L => Lua.State.L;

    protected static readonly ILoggerFactory LoggerFactory;

    protected Dictionary<LuaReference, string> References { get; } = new(ReferenceEqualityComparer.Instance);

    protected bool IsTornDown { get; private set; }

    protected List<string> Warnings { get; } = [];

    private bool _tracksWarnings = true;

    static LuaTestBase()
    {
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        LoggerFactory = new SerilogLoggerFactory(serilogLogger, dispose: true);

        LuaMarshaler.Default.FormatProvider = CultureInfo.InvariantCulture;
    }

    protected LuaTestBase()
    { }

    [SetUp]
    public virtual void SetUp()
    {
        Context = TestContext.CurrentContext;
        Logger = LoggerFactory.CreateLogger(Context.Test.Name);

        var allocator = CreateLuaAllocator();
        Lua = CreateLua(allocator, new TestLuaMarshaler(UserDataDescriptorProvider.Default, this));
    }

    [TearDown]
    public virtual void TearDown()
    {
        IsTornDown = true;

        Assert.That(Warnings, Is.Empty);

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
            else
            {
                DumpStack(Lua, nameof(TearDown));
            }
        }

        Assert.That(Warnings, Is.Empty);

        Assert.That(References.Values, Is.Empty, getExceptionMessage: () => $"Leaked Lua references:\n{string.Join('\n', References.Values)}");

        if (allocator is NativeMemoryLuaAllocator nativeMemoryLuaAllocator)
        {
            Assert.That((int) nativeMemoryLuaAllocator.CurrentlyAllocatedBytes, Is.EqualTo(0), "Lua failed to free memory.");

#if TRACE_ALLOCS
            Logger.LogInformation("Lua allocated {0} times for a total of {1}KiB", nativeMemoryLuaAllocator.TimesAllocated, Math.Round(nativeMemoryLuaAllocator.TotalAllocatedBytes / 1024.0, 2));
#endif
        }
    }

    [OneTimeTearDown]
    public static void OneTimeTearDown()
    {
        LoggerFactory.Dispose();
    }

    protected virtual LuaAllocator CreateLuaAllocator(nuint maxBytes = 0)
    {
        var allocator = new NativeMemoryLuaAllocator(maxBytes);

#if TRACE_ALLOCS
        NativeMemoryLuaAllocatorLogging.Hook(allocator, LoggerFactory.CreateLogger("Alloc"));
#endif
        return allocator;
    }

    protected virtual Lua CreateLua(LuaAllocator? allocator = null, LuaMarshaler? marshaler = null)
    {
        return new Lua(allocator ?? CreateLuaAllocator(), marshaler ?? LuaMarshaler.Default);
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
            DumpStack(Lua, Context.Test.Name);
            throw;
        }
    }

    protected void DisableWarningTracking()
    {
        _tracksWarnings = false;
    }

    public static void DumpStack(LuaThread thread, string? location = null)
    {
        DumpStack(thread, TestContext.WriteLine, location);
    }

    public static void DumpStack(LuaThread thread, Action<string, object[]> writer, string? location = null)
    {
        var L = thread.State.L;
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

    protected class TestLuaMarshaler : DefaultLuaMarshaler
    {
        private readonly LuaTestBase _test;

        public TestLuaMarshaler(UserDataDescriptorProvider userDataDescriptorProvider, LuaTestBase test)
            : base(userDataDescriptorProvider)
        {
            _test = test;
        }

        public override bool TryGetValue<T>(LuaThread thread, int stackIndex, out T? obj)
            where T : default
        {
            var result = base.TryGetValue(thread, stackIndex, out obj);
            if (obj is LuaReference reference)
            {
                if (!_test.IsTornDown)
                {
                    _test.References[reference] = reference.ToString();
                    thread.State.GC.RegisterCallback(reference, () => _test.References.Remove(reference));
                }
            }

            return result;
        }
    }
}
