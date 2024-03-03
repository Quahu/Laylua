using System;
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
    public abstract unsafe class LuaTestBase
    {
        protected TestContext Context = null!;
        protected ILogger Logger = null!;

        protected Lua lua = null!;
        protected lua_State* L;

        protected WeakReference? _weakReference;

        protected static readonly ILoggerFactory LoggerFactory;

        static LuaTestBase()
        {
            var serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            LoggerFactory = new SerilogLoggerFactory(serilogLogger);
        }

        protected LuaTestBase()
        { }

        [SetUp]
        public virtual void Setup()
        {
            Context = TestContext.CurrentContext;
            Logger = LoggerFactory.CreateLogger(Context.Test.Name);

            var allocator = CreateLuaAllocator();
            lua = CreateLua(allocator);
            L = lua.GetStatePointer();
        }

        [TearDown]
        public virtual void Teardown()
        {
            if (_weakReference != null)
            {
                Logger.LogInformation("Weak reference: {0}", _weakReference.IsAlive ? "alive" : "dead");
            }

            var leakedReferences = 0;
            lua.Marshaler.ReferenceLeaked += (sender, e) =>
            {
                leakedReferences++;
            };

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

            Assert.That(leakedReferences, Is.EqualTo(0), $"There were {leakedReferences} leaked references.");

            if (lua.State.Allocator is NativeMemoryLuaAllocator allocator)
            {
                Assert.That((int) allocator.CurrentlyAllocatedBytes, Is.EqualTo(0), "Lua did not free all its memory.");
                Logger.LogInformation("Lua allocated {0} times for a total of {1}KiB", allocator.TimesAllocated, Math.Round(allocator.TotalAllocatedBytes / 1024.0, 2));
            }
        }

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
            return new Lua(allocator)
            {
                Id = Context.Test.Name,
                FormatProvider = CultureInfo.InvariantCulture
            };
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
                lua.DumpStack(Context.Test.Name);
                throw;
            }
        }
    }
}
