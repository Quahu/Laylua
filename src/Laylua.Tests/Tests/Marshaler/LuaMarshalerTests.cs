using System.Collections;
using System.Numerics;
using System.Reflection;
using Qommon;

namespace Laylua.Tests;

public class LuaMarshalerTests : LuaTestBase
{
    private static IEnumerable<TestCaseData> SameValues()
    {
        var types = new[]
        {
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
        };

        var createNumberMethod = typeof(LuaMarshalerTests).GetMethod(nameof(CreateNumber), BindingFlags.NonPublic | BindingFlags.Static)!;
        var createFloatingPointMethod = typeof(LuaMarshalerTests).GetMethod(nameof(CreateFloatingPoint), BindingFlags.NonPublic | BindingFlags.Static)!;
        foreach (var type in types)
        {
            var interfaces = type.GetInterfaces();
            var isFloatingPoint = interfaces.Any(@interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IFloatingPoint<>));
            var method = isFloatingPoint
                ? createFloatingPointMethod
                : createNumberMethod;

            foreach (var testCaseData in (IEnumerable<TestCaseData>) method.MakeGenericMethod(type).Invoke(null, null)!)
            {
                yield return testCaseData;
            }
        }
    }

    private static IEnumerable<TestCaseData> CreateNumber<T>()
        where T : INumberBase<T>, IMinMaxValue<T>
    {
        yield return new TestCaseData(T.Zero)
            .SetArgDisplayNames($"{typeof(T).Name}.{nameof(T.Zero)}");

        yield return new TestCaseData(T.One)
            .SetArgDisplayNames($"{typeof(T).Name}.{nameof(T.One)}");

        yield return new TestCaseData(T.One)
            .SetArgDisplayNames($"{typeof(T).Name}.{nameof(T.One)}");

        yield return new TestCaseData(T.MinValue)
            .SetArgDisplayNames($"{typeof(T).Name}.{nameof(T.MinValue)}");

        yield return new TestCaseData(T.MaxValue)
            .SetArgDisplayNames($"{typeof(T).Name}.{nameof(T.MaxValue)}");
    }

    private static IEnumerable<TestCaseData> CreateFloatingPoint<T>()
        where T : IFloatingPoint<T>, IMinMaxValue<T>
    {
        foreach (var testCaseData in CreateNumber<T>())
        {
            yield return testCaseData;
        }

        yield return new TestCaseData(T.E)
            .SetArgDisplayNames($"{typeof(T).Name}.{nameof(T.E)}");

        yield return new TestCaseData(T.Pi)
            .SetArgDisplayNames($"{typeof(T).Name}.{nameof(T.Pi)}");

        yield return new TestCaseData(T.Tau)
            .SetArgDisplayNames($"{typeof(T).Name}.{nameof(T.Tau)}");
    }

    [Test]
    [TestCaseSource(nameof(SameValues))]
    public void PushValue_PopValue_ReturnsSameValue<TValue>(TValue value)
    {
        // Act
        Lua.Stack.Push(value);
        var result = Lua.Stack[1].GetValue<TValue>();
        Lua.Stack.Pop();

        // Assert
        Assert.That(result, Is.EqualTo(value));
    }

    private static IEnumerable<TestCaseData> CollectionValues()
    {
        yield return MakeData(new[] { 1, 2, 3 },
            static collection => table => Assert.That(table.Values.ToArray<int>(), Is.EqualTo(collection)));

        yield return MakeData(new List<int> { 1, 2, 3 },
            static collection => table => Assert.That(table.Values.ToArray<int>(), Is.EqualTo(collection)));

        yield return MakeData(new Dictionary<string, int> { ["one"] = 1, ["two"] = 2, ["three"] = 3 },
            static collection => table => Assert.That(table.ToDictionary<string, int>(), Is.EquivalentTo(collection)));

        yield return MakeData(YieldNumbers(),
            static collection => table => Assert.That(table.Values.ToArray<int>(), Is.EqualTo(collection)), nameof(YieldNumbers));

        yield return MakeData(YieldKvps(),
            static collection => table => Assert.That(table.ToDictionary<string, int>(), Is.EquivalentTo(collection)), nameof(YieldKvps));

        yield return MakeData(new ArrayList { 1, 2, 3 },
            static collection => table => Assert.That(table.Values.ToArray<int>(), Is.EqualTo(collection)));

        yield return MakeData(new Hashtable { ["one"] = 1, ["two"] = 2, ["three"] = 3 },
            static collection => table => Assert.That(table.ToDictionary<string, int>(), Is.EquivalentTo(collection.Cast<DictionaryEntry>().ToDictionary(entry => (string) entry.Key, entry => (int) entry.Value!))));

        static IEnumerable<int> YieldNumbers()
        {
            yield return 1;
            yield return 2;
            yield return 3;
        }

        static IEnumerable<KeyValuePair<string, int>> YieldKvps()
        {
            yield return KeyValuePair.Create("one", 1);
            yield return KeyValuePair.Create("two", 2);
            yield return KeyValuePair.Create("three", 3);
        }

        static TestCaseData MakeData(IEnumerable collection, Func<IEnumerable, Action<LuaTable>> getAssert, string? collectionName = null)
        {
            return new TestCaseData(collection, getAssert(collection))
                .SetArgDisplayNames(collectionName ?? collection.GetType().ToTypeString());
        }
    }

    [Test]
    [TestCaseSource(nameof(CollectionValues))]
    public void PushCollection_PopTable_ReturnsSameValues(IEnumerable collection, Action<LuaTable> assert)
    {
        // Act
        Lua.Stack.Push(collection);
        using var table = Lua.Stack[1].GetValue<LuaTable>()!;
        Lua.Stack.Pop();

        assert(table);
    }

    [Test]
    public void PushUnownedReference_Throws()
    {
        // Arrange
        using var lua1 = CreateLua();
        Lua.Stack.PushNewTable();
        using var table = Lua.Stack[1].GetValue<LuaTable>()!;
        Lua.Stack.Pop();

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => lua1.Stack.Push(table));

        // Assert
        Assert.That(ex, Has.Message.Contain("owned"));
    }

    [Test]
    public void PushUnownedStackValue_Throws()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        using var lua1 = CreateLua();
        Lua.Stack.PushNewTable();
        var table = Lua.Stack[1];

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => lua1.Stack.Push(table));

        // Assert
        Assert.That(ex, Has.Message.Contain("owned"));
    }
}
