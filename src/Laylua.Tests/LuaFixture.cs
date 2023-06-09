using System;
using System.Collections;
using System.Globalization;
using Laylua.Moon;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Laylua.Tests
{
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    [SingleThreaded]
    public abstract unsafe class LuaFixture
    {
        protected TestContext Context = null!;
        protected ILogger Logger = null!;

        protected Lua lua = null!;
        protected lua_State* L;

        protected TestComparer Comparer = null!;

        protected WeakReference? _wo;

        protected static readonly ILoggerFactory LoggerFactory;

        static LuaFixture()
        {
            var serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            LoggerFactory = new SerilogLoggerFactory(serilogLogger);
        }

        protected LuaFixture()
        { }

        protected virtual LuaAllocator CreateLuaAllocator()
        {
            var allocator = new NativeMemoryLuaAllocator();

#if DEBUG
            NativeMemoryLuaAllocatorLogging.Hook(allocator, LoggerFactory.CreateLogger("Alloc"));
#endif
            return allocator;
        }

        protected virtual Lua CreateLua(LuaAllocator allocator)
        {
            return new Lua(CultureInfo.InvariantCulture, allocator)
            {
                Id = Context.Test.Name
            };
        }

        [SetUp]
        public virtual void Setup()
        {
            Context = TestContext.CurrentContext;
            Logger = LoggerFactory.CreateLogger(Context.Test.Name);

            var allocator = CreateLuaAllocator();
            lua = CreateLua(allocator);
            L = lua.GetStatePointer();

            Comparer = new TestComparer(lua.Comparer);
        }

        protected virtual void AssertStackCount(int expected)
        {
            var top = lua_gettop(L);
            try
            {
                Assert.AreEqual(expected, top, "Stack count mismatch.");
            }
            catch
            {
                lua.DumpStack(Context.Test.Name);
                throw;
            }
        }

        [TearDown]
        public virtual void Teardown()
        {
            if (_wo != null)
            {
                Logger.LogInformation("Weak reference: {0}", _wo.IsAlive ? "alive" : "dead");
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

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

            lua.Dispose();

            if (lua.State.Allocator is NativeMemoryLuaAllocator allocator)
            {
                Assert.AreEqual(0, (int) allocator.CurrentlyAllocatedBytes, "Lua did not free all its memory.");
                Logger.LogInformation("Lua allocated {0} times for a total of {1}KiB", allocator.TimesAllocated, Math.Round(allocator.TotalAllocatedBytes / 1024.0, 2));
            }
        }

        protected class TestComparer : IComparer
        {
            private readonly LuaComparer _comparer;

            public TestComparer(LuaComparer comparer)
            {
                _comparer = comparer;
            }

            public int Compare(object? x, object? y)
            {
                return _comparer.Equals(x, y) ? 0 : -1;
            }
        }
    }
}
