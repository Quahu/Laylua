using System.Collections;
using System.Collections.Concurrent;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Laylua.Marshaling;

namespace Laylua.Tests;

public class ReflectionTarget
{
    public static int InstanceCount { get; set; }

    public string Name { get; set; } = "";

    public int Value { get; set; }

    [LuaIgnore]
    public int Secret { get; set; }

    [LuaName("alias")]
    public string OriginalName { get; set; } = "";

    [LuaInclude]
    internal int Hidden { get; set; }

    public ReflectionTarget()
    { }

    public ReflectionTarget(string name)
    {
        Name = name;
    }

    public ReflectionTarget(string name, int value)
    {
        Name = name;
        Value = value;
    }

    public int Add(int a, int b)
    {
        return a + b;
    }

    public static int StaticAdd(int a, int b)
    {
        return a + b;
    }

    public override string ToString()
    {
        return $"ReflectionTarget({Name})";
    }
}

public class ReflectionDisposable : IDisposable
{
    public bool WasDisposed { get; private set; }

    public int Value { get; set; }

    public void Dispose()
    {
        WasDisposed = true;
    }
}

public class NeverRegisteredType
{
    public int Value { get; set; }
}

public class CallbackHolder
{
    public LuaFunction? Captured { get; private set; }

    public void Capture(LuaFunction callback)
    {
        Captured = callback.CloneReference();
    }
}

public class CamelCaseTarget
{
    public string MyProperty { get; set; } = "";

    public int AnotherValue { get; set; }
}

public class SnakeCaseTarget
{
    public string IsAlive { get; set; } = "true";

    public int MaxHealth { get; set; }
}

public class IgnoredMemberTarget
{
    public string Name { get; set; } = "";

    public int Secret { get; set; }

    public string Visible { get; set; } = "";
}

public class DynamicFactoryTarget
{
    public string Name { get; set; } = "";

    public int Value { get; set; }

    public DynamicFactoryTarget()
    { }

    public DynamicFactoryTarget(string name)
    {
        Name = name;
    }

    public DynamicFactoryTarget(string name, int value)
    {
        Name = name;
        Value = value;
    }
}

public class DisabledOpsTarget
{
    public int Value { get; set; }

    public static DisabledOpsTarget operator +(DisabledOpsTarget a, DisabledOpsTarget b)
    {
        return new DisabledOpsTarget
        {
            Value = a.Value + b.Value
        };
    }
}

public class DynamicAllOperators
{
    public int Value { get; set; }

    public static DynamicAllOperators operator +(DynamicAllOperators a, DynamicAllOperators b)
    {
        return new DynamicAllOperators
        {
            Value = a.Value + b.Value
        };
    }

    public static DynamicAllOperators operator -(DynamicAllOperators a, DynamicAllOperators b)
    {
        return new DynamicAllOperators
        {
            Value = a.Value - b.Value
        };
    }

    public static DynamicAllOperators operator *(DynamicAllOperators a, DynamicAllOperators b)
    {
        return new DynamicAllOperators
        {
            Value = a.Value * b.Value
        };
    }

    public static DynamicAllOperators operator /(DynamicAllOperators a, DynamicAllOperators b)
    {
        return new DynamicAllOperators
        {
            Value = a.Value / b.Value
        };
    }

    public static DynamicAllOperators operator %(DynamicAllOperators a, DynamicAllOperators b)
    {
        return new DynamicAllOperators
        {
            Value = a.Value % b.Value
        };
    }

    public static DynamicAllOperators operator -(DynamicAllOperators a)
    {
        return new DynamicAllOperators
        {
            Value = -a.Value
        };
    }

    public static bool operator ==(DynamicAllOperators a, DynamicAllOperators b)
    {
        return a.Value == b.Value;
    }

    public static bool operator !=(DynamicAllOperators a, DynamicAllOperators b)
    {
        return a.Value != b.Value;
    }

    public static bool operator <(DynamicAllOperators a, DynamicAllOperators b)
    {
        return a.Value < b.Value;
    }

    public static bool operator <=(DynamicAllOperators a, DynamicAllOperators b)
    {
        return a.Value <= b.Value;
    }

    public static bool operator >(DynamicAllOperators a, DynamicAllOperators b)
    {
        return a.Value > b.Value;
    }

    public static bool operator >=(DynamicAllOperators a, DynamicAllOperators b)
    {
        return a.Value >= b.Value;
    }

    public static DynamicAllOperators operator &(DynamicAllOperators a, DynamicAllOperators b)
    {
        return new DynamicAllOperators
        {
            Value = a.Value & b.Value
        };
    }

    public static DynamicAllOperators operator |(DynamicAllOperators a, DynamicAllOperators b)
    {
        return new DynamicAllOperators
        {
            Value = a.Value | b.Value
        };
    }

    public static DynamicAllOperators operator ^(DynamicAllOperators a, DynamicAllOperators b)
    {
        return new DynamicAllOperators
        {
            Value = a.Value ^ b.Value
        };
    }

    public static DynamicAllOperators operator ~(DynamicAllOperators a)
    {
        return new DynamicAllOperators
        {
            Value = ~a.Value
        };
    }

    public static DynamicAllOperators operator <<(DynamicAllOperators a, int b)
    {
        return new DynamicAllOperators
        {
            Value = a.Value << b
        };
    }

    public static DynamicAllOperators operator >> (DynamicAllOperators a, int b)
    {
        return new DynamicAllOperators
        {
            Value = a.Value >> b
        };
    }

    public override bool Equals(object? obj)
    {
        return obj is DynamicAllOperators other && Value == other.Value;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

public class DynamicBase
{
    public string BaseName { get; set; } = "base";

    public int BaseValue { get; set; }

    public static int StaticBaseMethod(int a, int b)
    {
        return a + b;
    }

    public static string StaticBaseProp
    {
        get
        {
            return "from_base";
        }
    }

    public int GetBaseValue()
    {
        return BaseValue;
    }

    public override string ToString()
    {
        return $"DynamicBase({BaseName})";
    }
}

public class DynamicDerived : DynamicBase
{
    public int Level { get; set; }

    public int GetLevel()
    {
        return Level;
    }
}

public struct DynamicPoint
{
    public double X { get; set; }

    public double Y { get; set; }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}

public class DynamicOverloaded
{
    public int Compute(int a)
    {
        return a * 2;
    }

    public int Compute(int a, int b)
    {
        return a + b;
    }

    public string Describe()
    {
        return "none";
    }

    public string Describe(string prefix)
    {
        return $"{prefix}: item";
    }
}

public class DynamicReadOnlyTarget
{
    public string Name { get; } = "readonly";

    public int Value { get; set; }
}

public class DynamicWithCount
{
    public string Label { get; set; } = "";

    public int Count { get; set; }
}

public class DynamicWithLength
{
    public int Length { get; set; }
}

public class DynamicCollection : List<int>
{
    public string Label { get; set; } = "";
}

public interface IDynamicAnimal
{
    string Species { get; }
}

public class DynamicDog : IDynamicAnimal
{
    public string Species
    {
        get
        {
            return "Dog";
        }
    }

    public string Name { get; set; } = "";
}

public class DynamicCat : IDynamicAnimal
{
    public string Species
    {
        get
        {
            return "Cat";
        }
    }

    public string Name { get; set; } = "";
}

public class DynamicTypedOverloaded
{
    public string Process(int value)
    {
        return $"int:{value}";
    }

    public string Process(string value)
    {
        return $"str:{value}";
    }
}

public class DynamicOverloadResolutionTarget
{
    public string Resolve(int value)
    {
        return $"int:{value}";
    }

    public string Resolve(double value)
    {
        return $"double:{value:0.##}";
    }

    public string Resolve(string value)
    {
        return $"string:{value}";
    }

    public static string ResolveStatic(int value)
    {
        return $"int:{value}";
    }

    public static string ResolveStatic(double value)
    {
        return $"double:{value:0.##}";
    }

    public static string ResolveStatic(string value)
    {
        return $"string:{value}";
    }
}

public class DynamicOverloadResolutionBestMatchReversedTarget
{
    public string Resolve(double value)
    {
        return $"double:{value:0.##}";
    }

    public string Resolve(int value)
    {
        return $"int:{value}";
    }
}

public class DynamicSByteOverloadResolutionTarget
{
    public string Resolve(sbyte value)
    {
        return $"sbyte:{value}";
    }

    public string Resolve(string value)
    {
        return $"string:{value}";
    }
}

public class DynamicUShortOverloadResolutionTarget
{
    public string Resolve(ushort value)
    {
        return $"ushort:{value}";
    }

    public string Resolve(string value)
    {
        return $"string:{value}";
    }
}

public class DynamicNilCompatibilityOverloadTarget
{
    public string Resolve(DynamicDog dog)
    {
        return $"dog:{dog.Name}";
    }

    public string Resolve(object? value)
    {
        return value == null ? "nil" : "value";
    }
}

public class DynamicTableCompatibilityOverloadTarget
{
    public string Resolve(LuaFunction function)
    {
        return "function";
    }

    public string Resolve(LuaTable table)
    {
        return "table";
    }
}

public class DynamicFunctionCompatibilityOverloadTarget
{
    public string Resolve(DynamicDog dog)
    {
        return $"dog:{dog.Name}";
    }

    public int Resolve(LuaFunction function)
    {
        return function != null ? 42 : 0;
    }
}

public class DynamicDelegateCompatibilityOverloadTarget
{
    public string Resolve(LuaTable table)
    {
        return "table";
    }

    public int Resolve(Func<int, int> callback)
    {
        return callback(21);
    }
}

public class DynamicDecimalTarget
{
    public decimal Price { get; set; }

    public decimal Calculate(decimal a, decimal b)
    {
        return a + b;
    }
}

public class DynamicNumericTableTarget
{
    public uint UIntValue { get; set; }

    public ulong ULongValue { get; set; }

    public ushort UShortValue { get; set; }

    public sbyte SByteValue { get; set; }

    public decimal DecimalValue { get; set; }
}

public class DynamicCtorIntOnlyTarget
{
    public int Value { get; }

    public DynamicCtorIntOnlyTarget(int value)
    {
        Value = value;
    }

    public DynamicCtorIntOnlyTarget(bool value)
    {
        Value = value ? 1 : 0;
    }
}

public class DynamicPartialMaterializationTarget
{
    public int Number { get; set; }

    public string Text { get; set; } = "";
}

public class DynamicLuaReferenceAssignmentTarget
{
    public LuaTable? Table { get; set; }
}

public class DynamicDefaultsCloneTarget
{
    public string Name { get; set; } = "";
}

public class DynamicStringIndexer
{
    private readonly Dictionary<string, string> _data = new();

    public string this[string key]
    {
        get
        {
            return _data.TryGetValue(key, out var value) ? value : "";
        }
        set
        {
            _data[key] = value;
        }
    }

    public string Name { get; set; } = "test";
}

public class DynamicIntIndexer
{
    private readonly List<string> _items = ["zero", "one", "two"];

    public string this[int index]
    {
        get
        {
            return _items[index];
        }
        set
        {
            _items[index] = value;
        }
    }
}

public class DynamicMultiIndexer
{
    private readonly Dictionary<string, string> _stringData = new();
    private readonly Dictionary<int, string> _intData = new();

    public string this[string key]
    {
        get
        {
            return _stringData.TryGetValue(key, out var value) ? value : "";
        }
        set
        {
            _stringData[key] = value;
        }
    }

    public string this[int key]
    {
        get
        {
            return _intData.TryGetValue(key, out var value) ? value : "";
        }
        set
        {
            _intData[key] = value;
        }
    }
}

public class DynamicUserDataMultiIndexer
{
    private readonly Dictionary<DynamicDog, string> _dogData = new();
    private readonly Dictionary<DynamicCat, string> _catData = new();

    public string this[DynamicDog dog]
    {
        get
        {
            return _dogData.TryGetValue(dog, out var value) ? value : "";
        }
        set
        {
            _dogData[dog] = value;
        }
    }

    public string this[DynamicCat cat]
    {
        get
        {
            return _catData.TryGetValue(cat, out var value) ? value : "";
        }
        set
        {
            _catData[cat] = value;
        }
    }
}

public class DynamicReadOnlyIndexer
{
    public string this[string key]
    {
        get
        {
            return key.ToUpperInvariant();
        }
    }
}

public class DynamicStrictStringIndexer
{
    private readonly Dictionary<string, string> _data = new();

    public string this[string key]
    {
        get
        {
            return _data.TryGetValue(key, out var value) ? value : "";
        }
        set
        {
            _data[key] = value;
        }
    }
}

public class DynamicEnumerable(params int[] items) : IEnumerable<int>
{
    private readonly List<int> _items = [..items];

    public IEnumerator<int> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public class DynamicWithOptionalParams
{
    public string Greet(string name, string greeting = "Hello")
    {
        return $"{greeting}, {name}!";
    }

    public int Sum(int a, int b = 0, int c = 0)
    {
        return a + b + c;
    }
}

public class DynamicWithParamsArray
{
    public int Sum(params int[] values)
    {
        var total = 0;
        foreach (var value in values)
        {
            total += value;
        }

        return total;
    }

    public string Format(string template, params string[] args)
    {
        var result = template;
        for (var i = 0; i < args.Length; i++)
        {
            result = result.Replace($"{{{i}}}", args[i]);
        }

        return result;
    }
}

public class DynamicWithRefOut
{
    public void Increment(ref int value)
    {
        value++;
    }

    public bool TryParse(string input, out int result)
    {
        result = int.TryParse(input, out var value) ? value : -1;
        return int.TryParse(input, out _);
    }

    public int Double(in int value)
    {
        return value * 2;
    }
}

[method: LuaIgnore]
public class CtorIgnoredTarget(int value)
{
    public int Value { get; set; } = value;

    public CtorIgnoredTarget()
        : this(-1)
    { }
}

public class CtorIncludedTarget
{
    public int Value { get; set; }

    [LuaInclude]
    internal CtorIncludedTarget(int value)
    {
        Value = value;
    }
}

public class DynamicStrictTarget
{
    public string Name { get; set; } = "test";
}

public class UnregisterReconfigureTarget
{
    public string Name { get; set; } = "";

    public int Value { get; set; }
}

public class UnregisterIgnoreTarget
{
    public string Name { get; set; } = "";

    public int Value { get; set; }
}

public class UnregisterFeaturesTarget
{
    public string Name { get; set; } = "";

    public override string ToString()
    {
        return $"UnregisterFeaturesTarget({Name})";
    }
}

public class DynamicCallableTarget
{
    public int BaseValue { get; set; }

    [LuaTypeCall]
    public int Compute(int a)
    {
        return BaseValue + a;
    }

    [LuaTypeCall]
    public int ComputeTwo(int a, int b)
    {
        return BaseValue + a + b;
    }
}

public class DynamicObjectMembersTarget
{
    public int Value { get; set; }

    public override string ToString()
    {
        return $"DynObj({Value})";
    }

    public override bool Equals(object? obj)
    {
        return obj is DynamicObjectMembersTarget other && Value == other.Value;
    }

    public override int GetHashCode()
    {
        return Value;
    }
}

public class DynamicArgValidationTarget
{
    public string Greet(string name)
    {
        return $"Hello, {name}!";
    }

    public string GreetOptional(string? name)
    {
        return $"Hello, {name ?? "World"}!";
    }
}

public class DynamicObsoleteTarget
{
    public int Active { get; set; }

    [Obsolete("Use Active instead.")]
    public int Legacy { get; set; }

    public int GetActive()
    {
        return Active;
    }

    [Obsolete("Use GetActive instead.")]
    public int GetLegacy()
    {
        return Legacy;
    }
}

public class CtorOptionalTarget(string name, int value = 10, string tag = "default")
{
    public string Name { get; set; } = name;

    public int Value { get; set; } = value;

    public string Tag { get; set; } = tag;
}

public class CtorParamsTarget(params int[] values)
{
    public int[] Values { get; set; } = values;
}

public class CtorMixedParamsTarget(string label, params int[] values)
{
    public string Label { get; set; } = label;

    public int[] Values { get; set; } = values;
}

public struct DynamicImplicitStructCtorTarget(int value)
{
    public int Value { get; set; } = value;
}

public struct DynamicExplicitParameterlessStructCtorTarget(int value)
{
    public int Value { get; set; } = value;

    public DynamicExplicitParameterlessStructCtorTarget()
        : this(123)
    { }
}

public class DynamicRefReturnMembersTarget(int value)
{
    private int _value = value;

    public ref int Value
    {
        get
        {
            return ref _value;
        }
    }

    public ref int GetValueRef()
    {
        return ref _value;
    }

    public int GetValue()
    {
        return _value;
    }
}

public unsafe class DynamicUserDataTests : LuaTestBase
{
    private interface IBlockingAssignableTarget
    { }

    private sealed class BlockingAssignableTarget : IBlockingAssignableTarget
    {
        public int Value { get; set; }
    }

    private sealed class GetOrCreateLockTarget
    {
        public int Value { get; set; }
    }

    private sealed class MarshalerProbeTarget
    {
        public int Value { get; set; }
    }

    private sealed class BlockingTypeHandler : ILuaUserDataHandler
    {
        private readonly Type _type;
        private readonly ManualResetEventSlim _typeReadStarted;
        private readonly ManualResetEventSlim _continueTypeRead;

        public Type Type
        {
            get
            {
                _typeReadStarted.Set();
                _continueTypeRead.Wait();
                return _type;
            }
        }

        public BlockingTypeHandler(Type type, ManualResetEventSlim typeReadStarted, ManualResetEventSlim continueTypeRead)
        {
            _type = type;
            _typeReadStarted = typeReadStarted;
            _continueTypeRead = continueTypeRead;
        }

        public void Push(lua_State* L, object value)
        {
            throw new NotSupportedException();
        }

        public bool TryGet(lua_State* L, int stackIndex, out object? value)
        {
            value = null;
            return false;
        }

        public bool TryGetFromTable(lua_State* L, int stackIndex, out object? value)
        {
            value = null;
            return false;
        }

        public void PushDefinition(lua_State* L)
        {
            throw new NotSupportedException();
        }

        public void CleanUpState(lua_State* L)
        { }
    }

    private sealed class MarshalerProbeMarshaler : DefaultLuaMarshaler
    {
        public bool RewriteObjectIntPushes { get; set; }

        public bool RewriteIntReads { get; set; }

        public int ObjectIntPushCount { get; private set; }

        public int IntReadCount { get; private set; }

        public override void PushValue<T>(LuaThread thread, T obj)
        {
            if (RewriteObjectIntPushes && typeof(T) == typeof(object) && obj is int)
            {
                ObjectIntPushCount++;
                lua_pushinteger(thread.State.L, 777);
                return;
            }

            base.PushValue(thread, obj);
        }

        public override bool TryGetValue<T>(LuaThread thread, int stackIndex, out T? obj)
            where T : default
        {
            if (RewriteIntReads && typeof(T) == typeof(int) && lua_type(thread.State.L, stackIndex) == LuaType.Number)
            {
                IntReadCount++;
                obj = (T) (object) 999;
                return true;
            }

            return base.TryGetValue(thread, stackIndex, out obj);
        }
    }

    private static ConcurrentDictionary<Type, (FieldInfo InstanceField, FieldInfo TypedInstanceField)> GetTypedHandlerFieldsCache()
    {
        var cacheField = typeof(LuaTypeRegistry).GetField("TypedHandlerFields", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (ConcurrentDictionary<Type, (FieldInfo InstanceField, FieldInfo TypedInstanceField)>) cacheField.GetValue(null)!;
    }

    private static object GetHandlerLock()
    {
        var lockField = typeof(LuaTypeRegistry).GetField("HandlerLock", BindingFlags.NonPublic | BindingFlags.Static)!;
        return lockField.GetValue(null)!;
    }

    private static MethodInfo GetAssignableHandlersMethod()
    {
        return typeof(LuaTypeRegistry).GetMethod("GetAssignableHandlers", BindingFlags.NonPublic | BindingFlags.Static)!;
    }

    private static ConcurrentDictionary<Type, DynamicUserDataOptions> GetPerTypeDynamicOptions()
    {
        var optionsField = typeof(LuaTypeRegistry).GetField("PerTypeDynamicOptions", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (ConcurrentDictionary<Type, DynamicUserDataOptions>) optionsField.GetValue(null)!;
    }

    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
        Marshaler.AllowsDynamicMarshaling = true;
        LuaTypeRegistry.Unregister<DynamicFactoryTarget>();
        LuaTypeRegistry.Unregister<DynamicCtorIntOnlyTarget>();
        LuaTypeRegistry.Unregister<DynamicPartialMaterializationTarget>();
        LuaTypeRegistry.Unregister<DynamicLuaReferenceAssignmentTarget>();
    }

    public override void TearDown()
    {
        LuaTypeRegistry.Unregister<DynamicFactoryTarget>();
        LuaTypeRegistry.Unregister<DynamicCtorIntOnlyTarget>();
        LuaTypeRegistry.Unregister<DynamicPartialMaterializationTarget>();
        LuaTypeRegistry.Unregister<DynamicLuaReferenceAssignmentTarget>();
        Marshaler.AllowsDynamicMarshaling = false;
        base.TearDown();
    }

    [Test]
    public void DynamicFallback_PropertyRead_UsesMarshalerPushPath()
    {
        // Arrange
        var marshaler = new MarshalerProbeMarshaler
        {
            AllowsDynamicMarshaling = true,
            RewriteObjectIntPushes = true
        };

        using var lua = CreateLua(marshaler: marshaler);
        var target = new MarshalerProbeTarget
        {
            Value = 42
        };

        lua["obj"] = target;

        try
        {
            // Act
            var result = lua.Evaluate<int>("return obj.Value");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(777));
                Assert.That(marshaler.ObjectIntPushCount, Is.GreaterThan(0));
            });
        }
        finally
        {
            LuaTypeRegistry.Unregister<MarshalerProbeTarget>();
        }
    }

    [Test]
    public void DynamicFallback_PropertyWrite_UsesMarshalerReadPath()
    {
        // Arrange
        var marshaler = new MarshalerProbeMarshaler
        {
            AllowsDynamicMarshaling = true,
            RewriteIntReads = true
        };

        using var lua = CreateLua(marshaler: marshaler);
        var target = new MarshalerProbeTarget();
        lua["obj"] = target;

        try
        {
            // Act
            lua.Execute("obj.Value = 5");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(target.Value, Is.EqualTo(999));
                Assert.That(marshaler.IntReadCount, Is.GreaterThan(0));
            });
        }
        finally
        {
            LuaTypeRegistry.Unregister<MarshalerProbeTarget>();
        }
    }

    [Test]
    public void DynamicFallback_PropertyReadWrite_Works()
    {
        // Arrange
        var target = new ReflectionTarget
        {
            Name = "Hello",
            Value = 42
        };

        Lua["obj"] = target;

        // Act
        Lua.Execute(@"
            assert(obj.Name == 'Hello')
            assert(obj.Value == 42)
        ");

        Lua.Execute("obj.Name = 'World'");

        // Assert
        Assert.That(target.Name, Is.EqualTo("World"));
    }

    [Test]
    public void DynamicFallback_MethodCall_Works()
    {
        // Arrange
        var target = new ReflectionTarget
        {
            Name = "Test"
        };

        Lua["obj"] = target;

        // Act & Assert
        Lua.Execute("assert(obj:Add(3, 4) == 7)");
    }

    [Test]
    public void DynamicFallback_LuaIgnore_Respected()
    {
        // Arrange
        var target = new ReflectionTarget
        {
            Secret = 42
        };

        Lua["obj"] = target;

        // Act & Assert
        Lua.Execute("assert(obj.Secret == nil)");
    }

    [Test]
    public void DynamicFallback_LuaName_Respected()
    {
        // Arrange
        var target = new ReflectionTarget
        {
            OriginalName = "hello"
        };

        Lua["obj"] = target;

        // Act & Assert
        Lua.Execute("assert(obj.alias == 'hello')");
    }

    [Test]
    public void DynamicFallback_LuaInclude_ExposesInternalMember()
    {
        // Arrange
        var target = new ReflectionTarget
        {
            Hidden = 99
        };

        Lua["obj"] = target;

        // Act & Assert
        Lua.Execute("assert(obj.Hidden == 99)");
    }

    [Test]
    public void DynamicFallback_ToString_Works()
    {
        // Arrange
        var target = new ReflectionTarget
        {
            Name = "Test"
        };

        Lua["obj"] = target;

        // Act & Assert
        Lua.Execute("assert(tostring(obj) == 'ReflectionTarget(Test)')");
    }

    [Test]
    public void DynamicFallback_IDisposable_CleanedUpOnGc()
    {
        // Arrange
        var target = new ReflectionDisposable
        {
            Value = 42
        };

        Lua["obj"] = target;

        // Act
        Lua.Execute("obj = nil");
        lua_gc(L, LuaGCOperation.Collect);

        // Assert
        Assert.That(target.WasDisposed, Is.True);
    }

    [Test]
    public void DynamicFallback_LuaFunctionArg_Cloned_RemainsUsableAfterMethodReturns()
    {
        // Arrange
        var holder = new CallbackHolder();
        Lua["holder"] = holder;

        // Act
        Lua.Execute("holder:Capture(function(v) return v + 10 end)");

        // Assert
        Assert.That(holder.Captured, Is.Not.Null);

        using var results = holder.Captured!.Call(5);
        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results.First.GetValue<int>(), Is.EqualTo(15));
    }

    [Test]
    public void LuaReference_Use_AfterOwningLuaDisposed_ThrowsWithLuaStateMessage()
    {
        // Arrange
        LuaFunction captured;
        var lua = CreateLua();
        try
        {
            lua["make"] = (Func<LuaFunction>) (() => lua.Evaluate<LuaFunction>("return function(v) return v + 1 end")!);
            captured = lua.Evaluate<LuaFunction>("return make()")!;
        }
        finally
        {
            lua.Dispose();
        }

        // Act
        var exception = Assert.Throws<ObjectDisposedException>(() => captured.Call());

        // Assert
        Assert.That(exception!.Message, Does.Contain("Lua").And.Contains("state").And.Contains("disposed"));
    }

    [Test]
    public void DynamicFallback_GetGlobalAsObject_ReturnsClrInstance()
    {
        // Arrange
        var target = new ReflectionTarget
        {
            Name = "Hello"
        };

        Lua["obj"] = target;

        // Act
        var retrieved = Lua.GetGlobal<object>("obj");

        // Assert
        Assert.That(retrieved, Is.SameAs(target));
    }

    [Test]
    public void DynamicPushDefinition_StaticProperty_Accessible()
    {
        // Arrange
        ReflectionTarget.InstanceCount = 42;
        try
        {
            Lua["ReflectionTarget"] = typeof(ReflectionTarget);

            // Act & Assert
            Lua.Execute("assert(ReflectionTarget.InstanceCount == 42)");
        }
        finally
        {
            ReflectionTarget.InstanceCount = 0;
        }
    }

    [Test]
    public void DynamicPushDefinition_StaticMethod_Callable()
    {
        // Arrange
        Lua["ReflectionTarget"] = typeof(ReflectionTarget);

        // Act & Assert
        Lua.Execute("assert(ReflectionTarget.StaticAdd(3, 7) == 10)");
    }

    [Test]
    public void DynamicPushDefinition_Constructor_ParameterlessViaCall()
    {
        // Arrange
        Lua["ReflectionTarget"] = typeof(ReflectionTarget);

        // Act
        Lua.Execute("obj = ReflectionTarget()");

        // Assert
        var obj = Lua.GetGlobal<object>("obj");
        Assert.That(obj, Is.TypeOf<ReflectionTarget>());
        Assert.That(((ReflectionTarget) obj).Name, Is.EqualTo(""));
    }

    [Test]
    public void DynamicPushDefinition_Constructor_WithParametersViaCall()
    {
        // Arrange
        Lua["ReflectionTarget"] = typeof(ReflectionTarget);

        // Act
        Lua.Execute("obj = ReflectionTarget('hello', 42)");

        // Assert
        var obj = Lua.GetGlobal<object>("obj");
        Assert.That(obj, Is.TypeOf<ReflectionTarget>());
        var target = (ReflectionTarget) obj;
        Assert.Multiple(() =>
        {
            Assert.That(target.Name, Is.EqualTo("hello"));
            Assert.That(target.Value, Is.EqualTo(42));
        });
    }

    [Test]
    public void DynamicPushDefinition_ConstructorOverload_ResolvesCorrectly()
    {
        // Arrange
        Lua["ReflectionTarget"] = typeof(ReflectionTarget);

        // Act
        Lua.Execute("a = ReflectionTarget()");
        Lua.Execute("b = ReflectionTarget('one')");
        Lua.Execute("c = ReflectionTarget('two', 2)");

        var a = (ReflectionTarget) Lua.GetGlobal<object>("a");
        var b = (ReflectionTarget) Lua.GetGlobal<object>("b");
        var c = (ReflectionTarget) Lua.GetGlobal<object>("c");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(a.Name, Is.EqualTo(""));
            Assert.That(b.Name, Is.EqualTo("one"));
            Assert.That(b.Value, Is.EqualTo(0));
            Assert.That(c.Name, Is.EqualTo("two"));
            Assert.That(c.Value, Is.EqualTo(2));
        });
    }

    public void DynamicPushDefinition_ConstructedInstance_SupportsPropertyAccess()
    {
        // Arrange
        Lua["ReflectionTarget"] = typeof(ReflectionTarget);

        // Act
        Lua.Execute(@"
            obj = ReflectionTarget('test', 10)
            obj.Name = 'modified'
            obj.Value = 99
        ");

        // Assert
        var obj = (ReflectionTarget) Lua.GetGlobal<object>("obj");
        Assert.Multiple(() =>
        {
            Assert.That(obj.Name, Is.EqualTo("modified"));
            Assert.That(obj.Value, Is.EqualTo(99));
        });
    }

    [Test]
    public void DynamicTableToPoco_ReflectionTarget_Materializes()
    {
        // Arrange
        Lua["dummy"] = new ReflectionTarget("dummy", 0);
        Lua.Execute("t = { Name = 'reflected', Value = 99 }");

        // Act
        using var _ = Lua.Stack.SnapshotCount();
        lua_getglobal(L, "t");
        var result = Lua.Stack[-1].GetValue<ReflectionTarget>(LuaAllowedValueConversions.TableToObject);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Name, Is.EqualTo("reflected"));
            Assert.That(result.Value, Is.EqualTo(99));
        });
    }

    [Test]
    public void DynamicTryGet_ForgedUserDataWithMatchingMetatableName_ReturnsFalse()
    {
        // Arrange
        using var _ = Lua.Stack.SnapshotCount();
        LuaTypeRegistry.ConfigureDynamic<ReflectionTarget>();
        var handler = LuaTypeServices.GetOrCreate(typeof(ReflectionTarget));
        var forgedTarget = new ReflectionTarget("forged", 1);

        handler.Push(L, new ReflectionTarget("real", 2));
        Assert.That(lua_getmetatable(L, -1), Is.True);

        var handle = GCHandle.Alloc(forgedTarget);
        try
        {
            var forgedUserData = (nint*) lua_newuserdatauv(L, (nuint) (sizeof(nint) * 2), 0);
            forgedUserData[0] = GCHandle.ToIntPtr(handle);
            forgedUserData[1] = 0;

            lua_pushvalue(L, -2);
            lua_setmetatable(L, -2);

            // Act
            var found = handler.TryGet(L, -1, out var value);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(found, Is.False);
                Assert.That(value, Is.Null);
            });
        }
        finally
        {
            handle.Free();
        }
    }

    [Test]
    public void DynamicPairs_IteratesProperties()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ReflectionTarget>(opts =>
        {
            opts.PairsBehavior = LuaPairsBehavior.Properties;
        });

        var target = new ReflectionTarget("test", 42);
        Lua["obj"] = target;

        // Act
        Lua.Execute(@"
            local keys = {}
            for k, v in pairs(obj) do
                keys[k] = v
            end
            _keys = keys
        ");

        using var keysTable = Lua.GetGlobal<LuaTable>("_keys");
        var name = keysTable.GetValue<string, string>("Name");
        var value = keysTable.GetValue<string, int>("Value");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(name, Is.EqualTo("test"));
            Assert.That(value, Is.EqualTo(42));
        });
    }

    [Test]
    public void DynamicWritableProperty_WriteFromLua_Succeeds()
    {
        // Arrange
        var target = new ReflectionTarget("test", 42);
        Lua["obj"] = target;

        // Act & Assert
        Assert.DoesNotThrow(() => Lua.Execute("obj.Name = 'modified'"));
        Assert.DoesNotThrow(() => Lua.Execute("obj.Value = 99"));
        Assert.That(target.Value, Is.EqualTo(99));
    }

    [Test]
    public void DynamicOperator_Vector2Add_Works()
    {
        // Arrange
        Lua["vec"] = Vector2.One;
        Lua["vec2"] = Vector2.One;

        // Act
        var result = Lua.Evaluate<Vector2>("return vec + vec2");

        // Assert
        Assert.That(result, Is.EqualTo(new Vector2(2, 2)));
    }

    [Test]
    public void DynamicOperator_Vector2Sub_Works()
    {
        // Arrange
        Lua["a"] = new Vector2(5, 3);
        Lua["b"] = new Vector2(2, 1);

        // Act
        var result = Lua.Evaluate<Vector2>("return a - b");

        // Assert
        Assert.That(result, Is.EqualTo(new Vector2(3, 2)));
    }

    [Test]
    public void DynamicOperator_Vector2Negate_Works()
    {
        // Arrange
        Lua["vec"] = new Vector2(3, -4);

        // Act
        var result = Lua.Evaluate<Vector2>("return -vec");

        // Assert
        Assert.That(result, Is.EqualTo(new Vector2(-3, 4)));
    }

    [Test]
    public void DynamicOperator_Vector2MulScalar_Works()
    {
        // Arrange
        Lua["vec"] = new Vector2(2, 3);

        // Act
        var result = Lua.Evaluate<Vector2>("return vec * vec");

        // Assert
        Assert.That(result, Is.EqualTo(new Vector2(4, 9)));
    }

    [Test]
    public void DynamicOperator_Vector2Equality_Works()
    {
        // Arrange
        Lua["a"] = Vector2.One;
        Lua["b"] = Vector2.One;
        Lua["c"] = Vector2.Zero;

        // Act & Assert
        Lua.Execute(@"
            assert(a == b)
            assert(a ~= c)
        ");
    }

    [Test]
    public void DynamicToString_AlwaysAvailable()
    {
        // Arrange
        Lua["vec"] = new Vector2(1, 2);

        // Act
        var result = Lua.Evaluate<string>("return tostring(vec)");

        // Assert
        Assert.That(result, Is.Not.Null.And.Not.Empty);
        Assert.That(result, Does.Contain("1"));
        Assert.That(result, Does.Contain("2"));
    }

    [Test]
    public void DynamicToString_NoOverride_ReturnsTypeName()
    {
        // Arrange
        Lua["obj"] = new ReflectionDisposable
        {
            Value = 42
        };

        // Act
        var result = Lua.Evaluate<string>("return tostring(obj)");

        // Assert
        Assert.That(result, Does.Contain("ReflectionDisposable"));
    }

    [Test]
    public void DynamicLen_ListCount_Works()
    {
        // Arrange
        var list = new List<int>
        {
            1,
            2,
            3,
            4,
            5
        };

        Lua["list"] = list;

        // Act
        var len = Lua.Evaluate<long>("return #list");

        // Assert
        Assert.That(len, Is.EqualTo(5));
    }

    [Test]
    public void DynamicLen_CountProperty_Works()
    {
        // Arrange
        Lua["obj"] = new DynamicWithCount
        {
            Label = "test",
            Count = 42
        };

        // Act
        var len = Lua.Evaluate<long>("return #obj");

        // Assert
        Assert.That(len, Is.EqualTo(42));
    }

    [Test]
    public void DynamicLen_LengthProperty_Works()
    {
        // Arrange
        Lua["obj"] = new DynamicWithLength
        {
            Length = 7
        };

        // Act
        var len = Lua.Evaluate<long>("return #obj");

        // Assert
        Assert.That(len, Is.EqualTo(7));
    }

    [Test]
    public void DynamicConfig_CamelCase_NamingPolicy()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<CamelCaseTarget>(opts =>
        {
            opts.NamingPolicy = LuaNamingPolicy.CamelCase;
        });

        Lua["obj"] = new CamelCaseTarget
        {
            MyProperty = "hello",
            AnotherValue = 42
        };

        // Act & Assert
        Lua.Execute(@"
            assert(obj.myProperty == 'hello')
            assert(obj.anotherValue == 42)
        ");
    }

    [Test]
    public void DynamicConfig_SnakeCase_NamingPolicy()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<SnakeCaseTarget>(opts =>
        {
            opts.NamingPolicy = LuaNamingPolicy.LowerSnakeCase;
        });

        Lua["obj"] = new SnakeCaseTarget
        {
            IsAlive = "yes",
            MaxHealth = 100
        };

        // Act & Assert
        Lua.Execute(@"
            assert(obj.is_alive == 'yes')
            assert(obj.max_health == 100)
        ");
    }

    [Test]
    public void DynamicConfig_IgnoredMembers_NotExposed()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<IgnoredMemberTarget>(opts =>
        {
            opts.Ignore("Secret");
        });

        Lua["obj"] = new IgnoredMemberTarget
        {
            Name = "hello",
            Secret = 42,
            Visible = "yes"
        };

        // Act & Assert
        Lua.Execute(@"
            assert(obj.Name == 'hello')
            assert(obj.Visible == 'yes')
            assert(obj.Secret == nil)
        ");
    }

    [Test]
    public void DynamicConfig_DisabledOperators_NoMetamethod()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DisabledOpsTarget>(opts =>
        {
            opts.Features = LuaTypeFeatures.All & ~LuaTypeFeatures.Operators;
        });

        var a = new DisabledOpsTarget
        {
            Value = 1
        };

        var b = new DisabledOpsTarget
        {
            Value = 2
        };

        Lua["a"] = a;
        Lua["b"] = b;

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("return a + b"));
        Assert.That(ex!.Message, Does.Contain("attempt to perform arithmetic"));
    }

    [Test]
    public void DynamicOperator_Multiply_Works()
    {
        // Arrange
        Lua["a"] = new DynamicAllOperators
        {
            Value = 6
        };

        Lua["b"] = new DynamicAllOperators
        {
            Value = 7
        };

        // Act
        Lua.Execute("result = a * b");

        // Assert
        var result = Lua.GetGlobal<object>("result") as DynamicAllOperators;
        Assert.That(result!.Value, Is.EqualTo(42));
    }

    [Test]
    public void DynamicOperator_Divide_Works()
    {
        // Arrange
        Lua["a"] = new DynamicAllOperators
        {
            Value = 42
        };

        Lua["b"] = new DynamicAllOperators
        {
            Value = 7
        };

        // Act
        Lua.Execute("result = a / b");

        // Assert
        var result = Lua.GetGlobal<object>("result") as DynamicAllOperators;
        Assert.That(result!.Value, Is.EqualTo(6));
    }

    [Test]
    public void DynamicOperator_Modulus_Works()
    {
        // Arrange
        Lua["a"] = new DynamicAllOperators
        {
            Value = 17
        };

        Lua["b"] = new DynamicAllOperators
        {
            Value = 5
        };

        // Act
        Lua.Execute("result = a % b");

        // Assert
        var result = Lua.GetGlobal<object>("result") as DynamicAllOperators;
        Assert.That(result!.Value, Is.EqualTo(2));
    }

    [Test]
    public void DynamicOperator_LessThan_Works()
    {
        // Arrange
        Lua["a"] = new DynamicAllOperators
        {
            Value = 3
        };

        Lua["b"] = new DynamicAllOperators
        {
            Value = 7
        };

        // Act & Assert
        Lua.Execute(@"
            assert(a < b)
            assert(not (b < a))
            assert(not (a < a))
        ");
    }

    [Test]
    public void DynamicOperator_LessThanOrEqual_Works()
    {
        // Arrange
        Lua["a"] = new DynamicAllOperators
        {
            Value = 3
        };

        Lua["b"] = new DynamicAllOperators
        {
            Value = 7
        };

        Lua["c"] = new DynamicAllOperators
        {
            Value = 3
        };

        // Act & Assert
        Lua.Execute(@"
            assert(a <= b)
            assert(a <= c)
            assert(not (b <= a))
        ");
    }

    [Test]
    public void DynamicOperator_GreaterThan_DerivedFromLt()
    {
        // Arrange
        Lua["a"] = new DynamicAllOperators
        {
            Value = 10
        };

        Lua["b"] = new DynamicAllOperators
        {
            Value = 3
        };

        // Act & Assert
        Lua.Execute(@"
            assert(a > b)
            assert(not (b > a))
        ");
    }

    [Test]
    public void DynamicOperator_GreaterThanOrEqual_DerivedFromLe()
    {
        // Arrange
        Lua["a"] = new DynamicAllOperators
        {
            Value = 10
        };

        Lua["b"] = new DynamicAllOperators
        {
            Value = 10
        };

        Lua["c"] = new DynamicAllOperators
        {
            Value = 3
        };

        // Act & Assert
        Lua.Execute(@"
            assert(a >= b)
            assert(a >= c)
            assert(not (c >= a))
        ");
    }

    [Test]
    public void DynamicOperator_BitwiseAnd_Works()
    {
        // Arrange
        Lua["a"] = new DynamicAllOperators
        {
            Value = 0b1100
        };

        Lua["b"] = new DynamicAllOperators
        {
            Value = 0b1010
        };

        // Act
        Lua.Execute("result = a & b");

        // Assert
        var result = Lua.GetGlobal<object>("result") as DynamicAllOperators;
        Assert.That(result!.Value, Is.EqualTo(0b1000));
    }

    [Test]
    public void DynamicOperator_BitwiseOr_Works()
    {
        // Arrange
        Lua["a"] = new DynamicAllOperators
        {
            Value = 0b1100
        };

        Lua["b"] = new DynamicAllOperators
        {
            Value = 0b1010
        };

        // Act
        Lua.Execute("result = a | b");

        // Assert
        var result = Lua.GetGlobal<object>("result") as DynamicAllOperators;
        Assert.That(result!.Value, Is.EqualTo(0b1110));
    }

    [Test]
    public void DynamicOperator_BitwiseXor_Works()
    {
        // Arrange
        Lua["a"] = new DynamicAllOperators
        {
            Value = 0b1100
        };

        Lua["b"] = new DynamicAllOperators
        {
            Value = 0b1010
        };

        // Act
        Lua.Execute("result = a ~ b");

        // Assert
        var result = Lua.GetGlobal<object>("result") as DynamicAllOperators;
        Assert.That(result!.Value, Is.EqualTo(0b0110));
    }

    public void DynamicOperator_BitwiseNot_Works()
    {
        // Arrange
        Lua["a"] = new DynamicAllOperators
        {
            Value = 0
        };

        // Act
        Lua.Execute("result = ~a");

        // Assert
        var result = Lua.GetGlobal<object>("result") as DynamicAllOperators;
        Assert.That(result!.Value, Is.EqualTo(~0));
    }

    [Test]
    public void DynamicOperator_LeftShift_Works()
    {
        // Arrange
        Lua["a"] = new DynamicAllOperators
        {
            Value = 1
        };

        // Act
        Lua.Execute("result = a << 4");

        // Assert
        var result = Lua.GetGlobal<object>("result") as DynamicAllOperators;
        Assert.That(result!.Value, Is.EqualTo(16));
    }

    [Test]
    public void DynamicOperator_RightShift_Works()
    {
        // Arrange
        Lua["a"] = new DynamicAllOperators
        {
            Value = 64
        };

        // Act
        Lua.Execute("result = a >> 3");

        // Assert
        var result = Lua.GetGlobal<object>("result") as DynamicAllOperators;
        Assert.That(result!.Value, Is.EqualTo(8));
    }

    [Test]
    public void DynamicInheritance_DerivedType_ExposesBaseMembers()
    {
        // Arrange
        Lua["obj"] = new DynamicDerived
        {
            BaseName = "hello",
            BaseValue = 10,
            Level = 5
        };

        // Act & Assert
        Lua.Execute(@"
            assert(obj.BaseName == 'hello')
            assert(obj.BaseValue == 10)
            assert(obj.Level == 5)
        ");
    }

    [Test]
    public void DynamicInheritance_DerivedType_BaseMethodCallable()
    {
        // Arrange
        Lua["obj"] = new DynamicDerived
        {
            BaseValue = 42,
            Level = 7
        };

        // Act & Assert
        Lua.Execute(@"
            assert(obj:GetBaseValue() == 42)
            assert(obj:GetLevel() == 7)
        ");
    }

    [Test]
    public void DynamicInheritance_DerivedType_ToStringFromBase()
    {
        // Arrange
        Lua["obj"] = new DynamicDerived
        {
            BaseName = "test"
        };

        // Act
        var result = Lua.Evaluate<string>("return tostring(obj)");

        // Assert
        Assert.That(result, Does.Contain("DynamicBase(test)"));
    }

    [Test]
    public void DynamicInheritance_DerivedTypeDefinition_ExposesBaseStaticMethod()
    {
        // Arrange
        Lua["DynamicDerived"] = typeof(DynamicDerived);

        // Act
        var result = Lua.Evaluate<int>("return DynamicDerived.StaticBaseMethod(3, 4)");

        // Assert
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void DynamicInheritance_DerivedTypeDefinition_ExposesBaseStaticProperty()
    {
        // Arrange
        Lua["DynamicDerived"] = typeof(DynamicDerived);

        // Act
        var result = Lua.Evaluate<string>("return DynamicDerived.StaticBaseProp");

        // Assert
        Assert.That(result, Is.EqualTo("from_base"));
    }

    [Test]
    public void DynamicStruct_PropertyReadWrite_Works()
    {
        // Act
        Lua["pt"] = new DynamicPoint
        {
            X = 1.5,
            Y = 2.5
        };

        // Assert
        Lua.Execute(@"
            assert(pt.X == 1.5)
            assert(pt.Y == 2.5)
        ");
    }

    [Test]
    public void DynamicStruct_ToString_Works()
    {
        // Arrange
        Lua["pt"] = new DynamicPoint
        {
            X = 3.0,
            Y = 4.0
        };

        // Act
        var result = Lua.Evaluate<string>("return tostring(pt)");

        // Assert
        Assert.That(result, Is.EqualTo("(3, 4)"));
    }

    [Test]
    public void DynamicOverloaded_SingleArg_DispatchesCorrectly()
    {
        // Arrange
        Lua["obj"] = new DynamicOverloaded();

        // Act & Assert
        Lua.Execute("assert(obj:Compute(5) == 10)");
    }

    [Test]
    public void DynamicOverloaded_TwoArgs_DispatchesCorrectly()
    {
        // Arrange
        Lua["obj"] = new DynamicOverloaded();

        // Act & Assert
        Lua.Execute("assert(obj:Compute(3, 7) == 10)");
    }

    [Test]
    public void DynamicOverloaded_StringMethod_DispatchesCorrectly()
    {
        // Arrange
        Lua["obj"] = new DynamicOverloaded();

        // Act & Assert
        Lua.Execute(@"
            assert(obj:Describe() == 'none')
            assert(obj:Describe('test') == 'test: item')
        ");
    }

    [Test]
    public void DynamicReadOnly_WriteAttempt_Errors()
    {
        // Arrange
        Lua["obj"] = new DynamicReadOnlyTarget
        {
            Value = 42
        };

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj.Name = 'changed'"));

        Lua.Execute("obj.Value = 99");
        var target = Lua.GetGlobal<object>("obj") as DynamicReadOnlyTarget;
        Assert.That(target!.Value, Is.EqualTo(99));
    }

    [Test]
    public void DynamicReadOnly_ReadAccess_Works()
    {
        // Arrange
        Lua["obj"] = new DynamicReadOnlyTarget();

        // Act & Assert
        Lua.Execute("assert(obj.Name == 'readonly')");
    }

    [Test]
    public void DynamicStringProperty_SetToNil_SetsNull()
    {
        // Arrange
        var target = new ReflectionTarget
        {
            Name = "hello"
        };

        Lua["obj"] = target;

        // Act
        Lua.Execute("obj.Name = nil");

        // Assert
        Assert.That(target.Name, Is.Null);
    }

    [Test]
    public void DynamicUnknownProperty_Read_ReturnsNil()
    {
        // Arrange
        Lua["obj"] = new ReflectionTarget
        {
            Name = "test"
        };

        // Act & Assert
        Lua.Execute("assert(obj.nonExistent == nil)");
    }

    [Test]
    public void DynamicUnknownProperty_Write_SilentlyIgnored()
    {
        // Arrange
        var target = new ReflectionTarget
        {
            Name = "test"
        };

        Lua["obj"] = target;

        // Act & Assert
        Assert.DoesNotThrow(() => Lua.Execute("obj.doesNotExist = 42"));
        Assert.That(target.Name, Is.EqualTo("test"));
    }

    [Test]
    public void DynamicLen_ArrayLength_AsTable()
    {
        // Arrange
        var arr = new[]
        {
            10, 20, 30
        };

        Lua["arr"] = arr;

        // Act
        var len = Lua.Evaluate<long>("return #arr");

        // Assert
        Assert.That(len, Is.EqualTo(3));
    }

    [Test]
    public void DynamicStruct_WriteProperty_PersistsInLua()
    {
        // Arrange
        Lua["pt"] = new DynamicPoint
        {
            X = 1.0,
            Y = 2.0
        };

        // Act
        Lua.Execute("pt.X = 99");

        // Assert
        Lua.Execute("assert(pt.X == 99)");
    }

    [Test]
    public void DynamicConfigured_EnumerableType_MarshaledAsUserData()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicCollection>();

        var collection = new DynamicCollection
        {
            Label = "items"
        };

        collection.Add(1);
        collection.Add(2);
        collection.Add(3);
        Lua["col"] = collection;

        // Act & Assert
        Lua.Execute(@"
            assert(col.Label == 'items')
            assert(col.Count == 3)
        ");
    }

    [Test]
    public void DynamicDefaults_MutatedAfterHandlerCreation_DoesNotAffectExisting()
    {
        // Arrange
        var originalFeatures = LuaTypeRegistry.DynamicDefaults.Features;
        LuaTypeRegistry.DynamicDefaults.Features = LuaTypeFeatures.All;

        // Push triggers GetOrCreate (not ConfigureDynamic), which uses defaults directly
        Lua["obj"] = new DynamicDefaultsCloneTarget
        {
            Name = "test"
        };

        // Act
        LuaTypeRegistry.DynamicDefaults.Features = LuaTypeFeatures.All & ~LuaTypeFeatures.ToString;

        // Assert
        Lua.Execute("assert(tostring(obj) ~= 'userdata')");

        LuaTypeRegistry.DynamicDefaults.Features = originalFeatures;
    }

    [Test]
    public void DynamicOverloaded_SameArity_IntArg_DispatchesByType()
    {
        // Arrange
        Lua["obj"] = new DynamicTypedOverloaded();

        // Act
        Lua.Execute("result = obj:Process(42)");

        // Assert
        var result = Lua.Evaluate<string>("return result");
        Assert.That(result, Is.EqualTo("int:42"));
    }

    [Test]
    public void DynamicOverloaded_SameArity_StringArg_DispatchesByType()
    {
        // Arrange
        Lua["obj"] = new DynamicTypedOverloaded();

        // Act
        Lua.Execute("result = obj:Process('hello')");

        // Assert
        var result = Lua.Evaluate<string>("return result");
        Assert.That(result, Is.EqualTo("str:hello"));
    }

    [Test]
    public void DynamicOverloadResolution_BestMatch_SelectsMostSpecificOverload()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicOverloadResolutionTarget>(options =>
            options.OverloadResolution = LuaOverloadResolution.BestMatch);

        Lua["obj"] = new DynamicOverloadResolutionTarget();

        try
        {
            // Act & Assert
            Lua.Execute(@"
                assert(obj:Resolve(11) == 'int:11')
                assert(obj:Resolve('hello') == 'string:hello')
            ");
        }
        finally
        {
            LuaTypeRegistry.Unregister<DynamicOverloadResolutionTarget>();
        }
    }

    [Test]
    public void DynamicOverloadResolution_Error_AmbiguousCallRaisesLuaError()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicOverloadResolutionTarget>(options =>
            options.OverloadResolution = LuaOverloadResolution.Error);

        Lua["obj"] = new DynamicOverloadResolutionTarget();

        try
        {
            // Act & Assert
            Assert.Throws<LuaException>(() => Lua.Execute("return obj:Resolve(11)"));
        }
        finally
        {
            LuaTypeRegistry.Unregister<DynamicOverloadResolutionTarget>();
        }
    }

    [Test]
    public void DynamicOverloadResolution_Error_NonAmbiguousCallSucceeds()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicOverloadResolutionTarget>(options =>
            options.OverloadResolution = LuaOverloadResolution.Error);

        Lua["obj"] = new DynamicOverloadResolutionTarget();

        try
        {
            // Act & Assert
            Lua.Execute("assert(obj:Resolve('ok') == 'string:ok')");
        }
        finally
        {
            LuaTypeRegistry.Unregister<DynamicOverloadResolutionTarget>();
        }
    }

    public void DynamicOverloadResolution_NilArgument_SelectsNilCompatibleOverload()
    {
        // Arrange
        Lua["obj"] = new DynamicNilCompatibilityOverloadTarget();

        try
        {
            // Act
            var result = Lua.Evaluate<string>("return obj:Resolve(nil)");

            // Assert
            Assert.That(result, Is.EqualTo("nil"));
        }
        finally
        {
            LuaTypeRegistry.Unregister<DynamicNilCompatibilityOverloadTarget>();
        }
    }

    [Test]
    public void DynamicOverloadResolution_TableArgument_SelectsTableOverload()
    {
        // Arrange
        Lua["obj"] = new DynamicTableCompatibilityOverloadTarget();

        try
        {
            // Act
            var result = Lua.Evaluate<string>("return obj:Resolve({ value = 42 })");

            // Assert
            Assert.That(result, Is.EqualTo("table"));
        }
        finally
        {
            LuaTypeRegistry.Unregister<DynamicTableCompatibilityOverloadTarget>();
        }
    }

    [Test]
    public void DynamicOverloadResolution_FunctionArgument_SelectsFunctionOverload()
    {
        // Arrange
        Lua["obj"] = new DynamicFunctionCompatibilityOverloadTarget();

        try
        {
            // Act
            var result = Lua.Evaluate<int>("return obj:Resolve(function(value) return value * 2 end)");

            // Assert
            Assert.That(result, Is.EqualTo(42));
        }
        finally
        {
            LuaTypeRegistry.Unregister<DynamicFunctionCompatibilityOverloadTarget>();
        }
    }

    [Test]
    public void DynamicOverloadResolution_DelegateFunctionArgument_SelectsDelegateOverload()
    {
        // Arrange
        Lua["obj"] = new DynamicDelegateCompatibilityOverloadTarget();

        try
        {
            // Act
            var result = Lua.Evaluate<int>("return obj:Resolve(function(value) return value * 2 end)");

            // Assert
            Assert.That(result, Is.EqualTo(42));
        }
        finally
        {
            LuaTypeRegistry.Unregister<DynamicDelegateCompatibilityOverloadTarget>();
        }
    }

    [Test]
    public void DynamicOverloadResolution_Error_StaticOverload_AmbiguousCallRaisesLuaError()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicOverloadResolutionTarget>(options =>
            options.OverloadResolution = LuaOverloadResolution.Error);

        Lua["DynamicOverloadResolutionTarget"] = typeof(DynamicOverloadResolutionTarget);

        try
        {
            // Act & Assert
            Assert.Throws<LuaException>(() => Lua.Execute("return DynamicOverloadResolutionTarget.ResolveStatic(11)"));
        }
        finally
        {
            LuaTypeRegistry.Unregister<DynamicOverloadResolutionTarget>();
        }
    }

    [Test]
    public void DynamicDecimal_PropertyReadWrite_Works()
    {
        // Arrange
        var target = new DynamicDecimalTarget
        {
            Price = 19.99m
        };

        Lua["obj"] = target;

        // Act
        Lua.Execute("assert(obj.Price == 19.99)");
        Lua.Execute("obj.Price = 42.5");

        // Assert
        Assert.That(target.Price, Is.EqualTo(42.5m));
    }

    [Test]
    public void DynamicTryGet_ViaInterface_ReturnsObject()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicDog>();

        var dog = new DynamicDog
        {
            Name = "Barker"
        };

        Lua["dog"] = dog;

        // Act
        var retrieved = Lua.GetGlobal<IDynamicAnimal>("dog");

        // Assert
        Assert.That(retrieved, Is.SameAs(dog));
    }

    [Test]
    public void Dynamic_StringIndexer_GetAndSet()
    {
        // Arrange
        var obj = new DynamicStringIndexer();
        Lua["obj"] = obj;

        // Act
        Lua.Execute("obj['hello'] = 'world'");

        // Assert
        var result = Lua.Evaluate<string>("return obj['hello']");
        Assert.That(result, Is.EqualTo("world"));
    }

    [Test]
    public void Dynamic_StringIndexer_NamedPropertyTakesPriority()
    {
        // Arrange
        var obj = new DynamicStringIndexer();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj.Name");

        // Assert
        Assert.That(result, Is.EqualTo("test"));
    }

    [Test]
    public void Dynamic_IntIndexer_GetAndSet()
    {
        // Arrange
        var obj = new DynamicIntIndexer();
        Lua["obj"] = obj;

        // Act
        var initialValue = Lua.Evaluate<string>("return obj[0]");

        Assert.That(initialValue, Is.EqualTo("zero"));

        Lua.Execute("obj[0] = 'replaced'");

        // Assert
        Assert.That(obj[0], Is.EqualTo("replaced"));
    }

    [Test]
    public void Dynamic_MultiIndexer_DispatchesByKeyType()
    {
        // Arrange
        var obj = new DynamicMultiIndexer();
        Lua["obj"] = obj;

        // Act
        Lua.Execute("obj['key'] = 'by_string'; obj[42] = 'by_int'");

        // Assert
        var byString = Lua.Evaluate<string>("return obj['key']");
        var byInt = Lua.Evaluate<string>("return obj[42]");
        Assert.That(byString, Is.EqualTo("by_string"));
        Assert.That(byInt, Is.EqualTo("by_int"));
    }

    public void Dynamic_ReadOnlyIndexer_GetWorks_SetIgnored()
    {
        // Arrange
        var obj = new DynamicReadOnlyIndexer();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj['hello']");

        // Assert
        Assert.That(result, Is.EqualTo("HELLO"));
    }

    [Test]
    public void Dynamic_StrictIndexer_UnknownNumericRead_RaisesError()
    {
        // Arrange
        try
        {
            LuaTypeRegistry.ConfigureDynamic<DynamicStrictStringIndexer>(opts =>
            {
                opts.UnknownPropertyBehavior = LuaUnknownPropertyBehavior.Error;
                opts.BindingConversions = LuaAllowedValueConversions.Default & ~LuaAllowedValueConversions.PrimitiveToString;
            });

            Lua["obj"] = new DynamicStrictStringIndexer();

            // Act & Assert
            Assert.Throws<LuaException>(() => Lua.Execute("local value = obj[1]"));
        }
        finally
        {
            LuaTypeRegistry.Unregister<DynamicStrictStringIndexer>();
        }
    }

    [Test]
    public void Dynamic_StrictIndexer_UnknownNumericWrite_RaisesError()
    {
        // Arrange
        try
        {
            LuaTypeRegistry.ConfigureDynamic<DynamicStrictStringIndexer>(opts =>
            {
                opts.UnknownPropertyBehavior = LuaUnknownPropertyBehavior.Error;
                opts.BindingConversions = LuaAllowedValueConversions.Default & ~LuaAllowedValueConversions.PrimitiveToString;
            });

            Lua["obj"] = new DynamicStrictStringIndexer();

            // Act & Assert
            Assert.Throws<LuaException>(() => Lua.Execute("obj[1] = 'value'"));
        }
        finally
        {
            LuaTypeRegistry.Unregister<DynamicStrictStringIndexer>();
        }
    }

    [Test]
    public void Dynamic_IEnumerable_PairsIteratesElements()
    {
        // Arrange
        var obj = new DynamicEnumerable(10, 20, 30);
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            result = {}
            for k, v in pairs(obj) do
                result[k] = v
            end
        ");

        // Assert
        var val1 = Lua.Evaluate<long>("return result[1]");
        var val2 = Lua.Evaluate<long>("return result[2]");
        var val3 = Lua.Evaluate<long>("return result[3]");
        Assert.That(val1, Is.EqualTo(10));
        Assert.That(val2, Is.EqualTo(20));
        Assert.That(val3, Is.EqualTo(30));
    }

    [Test]
    public void Dynamic_OptionalParams_DefaultsApplied()
    {
        // Arrange
        var obj = new DynamicWithOptionalParams();
        Lua["obj"] = obj;

        // Act
        var defaultGreeting = Lua.Evaluate<string>("return obj:Greet('World')");
        var customGreeting = Lua.Evaluate<string>("return obj:Greet('World', 'Hi')");

        // Assert
        Assert.That(defaultGreeting, Is.EqualTo("Hello, World!"));
        Assert.That(customGreeting, Is.EqualTo("Hi, World!"));
    }

    [Test]
    public void Dynamic_OptionalParams_MultipleDefaults()
    {
        // Arrange
        var obj = new DynamicWithOptionalParams();
        Lua["obj"] = obj;

        // Act
        var sumOne = Lua.Evaluate<long>("return obj:Sum(5)");
        var sumTwo = Lua.Evaluate<long>("return obj:Sum(5, 3)");
        var sumThree = Lua.Evaluate<long>("return obj:Sum(5, 3, 2)");

        // Assert
        Assert.That(sumOne, Is.EqualTo(5));
        Assert.That(sumTwo, Is.EqualTo(8));
        Assert.That(sumThree, Is.EqualTo(10));
    }

    [Test]
    public void Dynamic_ParamsArray_CollectsArguments()
    {
        // Arrange
        var obj = new DynamicWithParamsArray();
        Lua["obj"] = obj;

        // Act
        var sumNone = Lua.Evaluate<long>("return obj:Sum()");
        var sumOne = Lua.Evaluate<long>("return obj:Sum(1)");
        var sumMultiple = Lua.Evaluate<long>("return obj:Sum(1, 2, 3)");

        // Assert
        Assert.That(sumNone, Is.EqualTo(0));
        Assert.That(sumOne, Is.EqualTo(1));
        Assert.That(sumMultiple, Is.EqualTo(6));
    }

    [Test]
    public void Dynamic_ParamsArray_WithFixedArgs()
    {
        // Arrange
        var obj = new DynamicWithParamsArray();
        Lua["obj"] = obj;

        // Act
        var simpleFormat = Lua.Evaluate<string>("return obj:Format('hello')");
        var parameterizedFormat = Lua.Evaluate<string>("return obj:Format('{0} {1}', 'a', 'b')");

        // Assert
        Assert.That(simpleFormat, Is.EqualTo("hello"));
        Assert.That(parameterizedFormat, Is.EqualTo("a b"));
    }

    [Test]
    public void Dynamic_RefOut_ReturnsAdditionalValues()
    {
        // Arrange
        var obj = new DynamicWithRefOut();
        Lua["obj"] = obj;

        // Act
        var ok = Lua.Evaluate<bool>("local ok, val = obj:TryParse('42'); return ok");
        var value = Lua.Evaluate<long>("local ok, value = obj:TryParse('42'); return value");

        // Assert
        Assert.That(ok, Is.True);
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void Dynamic_RefParameter_ReturnsUpdatedValue()
    {
        // Arrange
        var obj = new DynamicWithRefOut();
        Lua["obj"] = obj;

        // Act
        var value = Lua.Evaluate<long>("return obj:Increment(41)");

        // Assert
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void Dynamic_InParameter_ReturnsSingleValue()
    {
        // Arrange
        var obj = new DynamicWithRefOut();
        Lua["obj"] = obj;

        // Act
        var value = Lua.Evaluate<long>("""
                                       local result, extra = obj:Double(21)
                                       assert(extra == nil)
                                       return result
                                       """);

        // Assert
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void Dynamic_UnknownPropertyBehavior_Error()
    {
        // Arrange
        var obj = new DynamicStrictTarget();
        LuaTypeRegistry.ConfigureDynamic<DynamicStrictTarget>(opts =>
        {
            opts.UnknownPropertyBehavior = LuaUnknownPropertyBehavior.Error;
        });

        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Evaluate<string>("return obj.NonExistent"));
    }

    [Test]
    public void Dynamic_IndexMembers_PropertiesOnly()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicStrictTarget>(opts =>
        {
            opts.IndexMembers = LuaIndexMembers.Properties;
        });

        var obj = new DynamicStrictTarget();
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<string>("return obj.Name");

        // Assert
        Assert.That(result, Is.EqualTo("test"));
    }

    [Test]
    public void Dynamic_PairsFeature_Disabled()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicEnumerable>(opts =>
        {
            opts.Features &= ~LuaTypeFeatures.Pairs;
        });

        var obj = new DynamicEnumerable(1, 2, 3);
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("for k, v in pairs(obj) do end"));
    }

    [Test]
    public void DynamicCtor_IgnoredCtor_FallsBackToParameterless()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<CtorIgnoredTarget>();

        Lua["CtorIgnoredTarget"] = typeof(CtorIgnoredTarget);

        // Act
        var result = Lua.Evaluate<int>("local obj = CtorIgnoredTarget(); return obj.Value");

        // Assert
        Assert.That(result, Is.EqualTo(-1));
    }

    [Test]
    public void DynamicCtor_IgnoredCtor_RejectsIgnoredOverload()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<CtorIgnoredTarget>();

        Lua["CtorIgnoredTarget"] = typeof(CtorIgnoredTarget);

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("CtorIgnoredTarget(42)"));
    }

    [Test]
    public void DynamicCtor_IncludedInternal_CanBeInvoked()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<CtorIncludedTarget>();

        Lua["CtorIncludedTarget"] = typeof(CtorIncludedTarget);

        // Act
        var result = Lua.Evaluate<int>("local obj = CtorIncludedTarget(99); return obj.Value");

        // Assert
        Assert.That(result, Is.EqualTo(99));
    }

    [Test]
    public void DynamicCtor_ImplicitStruct_RejectsZeroArgCallButKeepsExplicitConstructor()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicImplicitStructCtorTarget>();

        Lua["DynamicImplicitStructCtorTarget"] = typeof(DynamicImplicitStructCtorTarget);

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("DynamicImplicitStructCtorTarget()"));

        var explicitValue = Lua.Evaluate<int>("local obj = DynamicImplicitStructCtorTarget(42); return obj.Value");

        // Assert
        Assert.That(explicitValue, Is.EqualTo(42));
    }

    [Test]
    public void DynamicCtor_ExplicitParameterlessStruct_RemainsCallable()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicExplicitParameterlessStructCtorTarget>();

        Lua["DynamicExplicitParameterlessStructCtorTarget"] = typeof(DynamicExplicitParameterlessStructCtorTarget);

        // Act
        var defaultValue = Lua.Evaluate<int>("local obj = DynamicExplicitParameterlessStructCtorTarget(); return obj.Value");

        // Assert
        Assert.That(defaultValue, Is.EqualTo(123));
    }

    [Test]
    public void DynamicFallback_RefReturnMembers_AreSkippedWithoutCrashing()
    {
        // Arrange
        Lua["obj"] = new DynamicRefReturnMembersTarget(42);

        // Act
        var result = Lua.Evaluate<int>("assert(obj.Value == nil); assert(obj.GetValueRef == nil); return obj:GetValue()");

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Metatable_GetMetatable_ReturnsFalse()
    {
        // Arrange
        var obj = new ReflectionTarget
        {
            Name = "test"
        };

        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<bool>("return getmetatable(obj)");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Metatable_SetMetatable_Errors()
    {
        // Arrange
        var obj = new ReflectionTarget
        {
            Name = "test"
        };

        Lua["obj"] = obj;

        // Act & Assert
        Assert.That(() => Lua.Execute("setmetatable(obj, {})"), Throws.Exception);
    }

    [Test]
    public void PublicApi_DeclaredMethods_DoesNotExposePointerParameters()
    {
        // Arrange
        var publicMethods = typeof(LuaTypeRegistry).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        // Act
        var pointerMethods = publicMethods
            .Where(static method => method.GetParameters().Any(static parameter => parameter.ParameterType.IsPointer))
            .Select(static method => method.ToString())
            .ToArray();

        // Assert
        Assert.That(pointerMethods, Is.Empty);
    }

    [Test]
    public void Unregister_Generic_RemovesHandler()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ReflectionTarget>();

        Assert.That(LuaTypeRegistry.IsRegistered(typeof(ReflectionTarget)), Is.True);

        // Act
        var removed = LuaTypeRegistry.Unregister<ReflectionTarget>();

        // Assert
        Assert.That(removed, Is.True);
        Assert.That(LuaTypeRegistry.IsRegistered(typeof(ReflectionTarget)), Is.False);
    }

    [Test]
    public void Unregister_NonGeneric_RemovesHandler()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ReflectionTarget>();

        Assert.That(LuaTypeRegistry.IsRegistered(typeof(ReflectionTarget)), Is.True);

        // Act
        var removed = LuaTypeRegistry.Unregister(typeof(ReflectionTarget));

        // Assert
        Assert.That(removed, Is.True);
        Assert.That(LuaTypeRegistry.IsRegistered(typeof(ReflectionTarget)), Is.False);
    }

    [Test]
    public void Unregister_NonGeneric_ClearsGenericFastPath()
    {
        // Arrange
        var handler = new DynamicUserDataHandler(typeof(ReflectionTarget));
        LuaTypeRegistry.Register<ReflectionTarget>(handler);
        var target = new ReflectionTarget("cached", 7);

        Assert.That(LuaTypeServices.TryPush(L, target.GetType(), target), Is.True);
        lua_pop(L);

        // Act
        var removed = LuaTypeRegistry.Unregister(typeof(ReflectionTarget));

        // Assert
        Assert.That(removed, Is.True);
        Assert.That(LuaTypeServices.TryPush(L, target.GetType(), target), Is.False);
    }

    [Test]
    public void ConfigureDynamic_PreviouslyRegisteredType_RefreshesGenericFastPathCache()
    {
        // Arrange
        LuaTypeRegistry.Unregister<ReflectionTarget>();

        var originalOptions = new DynamicUserDataOptions
        {
            NamingPolicy = LuaNamingPolicy.Original
        };
        var originalHandler = new DynamicUserDataHandler(typeof(ReflectionTarget), originalOptions);
        LuaTypeRegistry.Register<ReflectionTarget>(originalHandler);
        LuaTypeRegistry.ConfigureDynamic<ReflectionTarget>(options =>
        {
            options.NamingPolicy = LuaNamingPolicy.LowerSnakeCase;
        });

        var target = new ReflectionTarget("Steve", 7);

        try
        {
            // Act
            Assert.That(LuaTypeServices.TryPush(L, target.GetType(), target), Is.True);

            lua_setglobal(L, "obj");

            var snakeCaseNameMatches = Lua.Evaluate<bool>("return obj.name == 'Steve'");
            var pascalCaseNameMissing = Lua.Evaluate<bool>("return obj.Name == nil");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(snakeCaseNameMatches, Is.True);
                Assert.That(pascalCaseNameMissing, Is.True);
            });
        }
        finally
        {
            LuaTypeRegistry.Unregister<ReflectionTarget>();
        }
    }

    [Test]
    public void Register_NonGenericAfterGenericRegistration_ClearsStaleFastPathCache()
    {
        // Arrange
        LuaTypeRegistry.Unregister<ReflectionTarget>();

        var originalHandler = new DynamicUserDataHandler(typeof(ReflectionTarget), new DynamicUserDataOptions
        {
            NamingPolicy = LuaNamingPolicy.Original
        });
        LuaTypeRegistry.Register<ReflectionTarget>(originalHandler);

        var replacementHandler = new DynamicUserDataHandler(typeof(ReflectionTarget), new DynamicUserDataOptions
        {
            NamingPolicy = LuaNamingPolicy.LowerSnakeCase
        });
        LuaTypeRegistry.Register(typeof(ReflectionTarget), replacementHandler);

        var target = new ReflectionTarget("Steve", 7);

        try
        {
            // Act
            Assert.That(LuaTypeServices.TryPush(L, target.GetType(), target), Is.True);

            lua_setglobal(L, "obj");

            var snakeCaseNameMatches = Lua.Evaluate<bool>("return obj.name == 'Steve'");
            var pascalCaseNameMissing = Lua.Evaluate<bool>("return obj.Name == nil");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(snakeCaseNameMatches, Is.True);
                Assert.That(pascalCaseNameMissing, Is.True);
            });
        }
        finally
        {
            LuaTypeRegistry.Unregister<ReflectionTarget>();
        }
    }

    [Test]
    [Explicit]
    public void GetAssignableHandlers_CacheMiss_WaitsForRegistryLock()
    {
        // Arrange
        LuaTypeRegistry.Unregister<BlockingAssignableTarget>();

        using var typeReadStarted = new ManualResetEventSlim(false);
        using var continueTypeRead = new ManualResetEventSlim(false);

        var handler = new BlockingTypeHandler(typeof(BlockingAssignableTarget), typeReadStarted, continueTypeRead);
        LuaTypeRegistry.Register<BlockingAssignableTarget>(handler);

        var handlerLock = GetHandlerLock();
        var getAssignableHandlersMethod = GetAssignableHandlersMethod();
        Task<ILuaUserDataHandler[]>? getAssignableHandlersTask = null;
        var lockTaken = false;

        try
        {
            Monitor.Enter(handlerLock, ref lockTaken);

            // Act
            getAssignableHandlersTask = Task.Run(() =>
            {
                return (ILuaUserDataHandler[]) getAssignableHandlersMethod.Invoke(null, new object?[] { typeof(IBlockingAssignableTarget) })!;
            });

            var reachedAssignableScanWhileLockHeld = typeReadStarted.Wait(TimeSpan.FromSeconds(1));

            // Assert
            Assert.That(reachedAssignableScanWhileLockHeld, Is.False);
        }
        finally
        {
            if (lockTaken)
            {
                Monitor.Exit(handlerLock);
            }

            continueTypeRead.Set();
        }

        try
        {
            Assert.That(getAssignableHandlersTask, Is.Not.Null);
            Assert.That(getAssignableHandlersTask!.Wait(TimeSpan.FromSeconds(5)), Is.True);
        }
        finally
        {
            LuaTypeRegistry.Unregister<BlockingAssignableTarget>();
        }
    }

    [Test]
    [Explicit]
    public void GetOrCreate_CacheMiss_WaitsForRegistryLock()
    {
        // Arrange
        LuaTypeRegistry.Unregister<GetOrCreateLockTarget>();

        using var memberFilterStarted = new ManualResetEventSlim(false);
        using var continueMemberFilter = new ManualResetEventSlim(false);

        var blockingOptions = new DynamicUserDataOptions
        {
            MemberFilter = _ =>
            {
                memberFilterStarted.Set();
                continueMemberFilter.Wait();
                return true;
            }
        };
        var perTypeDynamicOptions = GetPerTypeDynamicOptions();

        Task<ILuaUserDataHandler>? getOrCreateTask;
        var handlerLock = GetHandlerLock();
        var lockTaken = false;

        try
        {
            perTypeDynamicOptions[typeof(GetOrCreateLockTarget)] = blockingOptions;

            Monitor.Enter(handlerLock, ref lockTaken);

            // Act
            getOrCreateTask = Task.Run(static () => LuaTypeServices.GetOrCreate(typeof(GetOrCreateLockTarget)));

            var reachedHandlerCreationWhileLockHeld = memberFilterStarted.Wait(TimeSpan.FromSeconds(1));

            // Assert
            Assert.That(reachedHandlerCreationWhileLockHeld, Is.False);
        }
        finally
        {
            if (lockTaken)
            {
                Monitor.Exit(handlerLock);
            }

            continueMemberFilter.Set();
            perTypeDynamicOptions.TryRemove(typeof(GetOrCreateLockTarget), out _);
        }

        try
        {
            Assert.That(getOrCreateTask, Is.Not.Null);
            Assert.That(getOrCreateTask!.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(LuaTypeRegistry.IsRegistered(typeof(GetOrCreateLockTarget)), Is.True);
        }
        finally
        {
            LuaTypeRegistry.Unregister<GetOrCreateLockTarget>();
        }
    }

    [Test]
    public void ConfigureDynamic_ConcurrentCalls_UseDistinctSnapshotsAndPreserveBothUpdates()
    {
        // Arrange
        LuaTypeRegistry.Unregister<ReflectionTarget>();

        using var startSignal = new ManualResetEventSlim(false);
        DynamicUserDataOptions? firstOptions = null;
        DynamicUserDataOptions? secondOptions = null;

        var firstTask = Task.Run(() =>
        {
            startSignal.Wait();
            LuaTypeRegistry.ConfigureDynamic<ReflectionTarget>(options =>
            {
                firstOptions = options;
                options.NamingPolicy = LuaNamingPolicy.LowerSnakeCase;
            });
        });

        var secondTask = Task.Run(() =>
        {
            startSignal.Wait();
            LuaTypeRegistry.ConfigureDynamic<ReflectionTarget>(options =>
            {
                secondOptions = options;
                options.Members = LuaTypeMembers.Properties;
            });
        });

        try
        {
            // Act
            startSignal.Set();
            Task.WaitAll(firstTask, secondTask);

            Lua["obj"] = new ReflectionTarget("Steve", 7);

            var snakeCasePropertyMatches = Lua.Evaluate<bool>("return obj.name == 'Steve'");
            var methodsAreHidden = Lua.Evaluate<bool>("return obj.Add == nil");

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(firstOptions, Is.Not.Null);
                Assert.That(secondOptions, Is.Not.Null);
                Assert.That(ReferenceEquals(firstOptions, secondOptions), Is.False);
                Assert.That(snakeCasePropertyMatches, Is.True);
                Assert.That(methodsAreHidden, Is.True);
            });
        }
        finally
        {
            LuaTypeRegistry.Unregister<ReflectionTarget>();
        }
    }

    [Test]
    public void Unregister_NonGeneric_PrunesTypedHandlerFieldCache()
    {
        // Arrange
        LuaTypeRegistry.Unregister<ReflectionTarget>();

        var handler = new DynamicUserDataHandler(typeof(ReflectionTarget));
        LuaTypeRegistry.Register<ReflectionTarget>(handler);
        var typedHandlerFields = GetTypedHandlerFieldsCache();

        // Act
        var removed = LuaTypeRegistry.Unregister(typeof(ReflectionTarget));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(typedHandlerFields.ContainsKey(typeof(ReflectionTarget)), Is.False);
        });
    }

    [Test]
    public void Unregister_NotRegistered_ReturnsFalse()
    {
        // Arrange
        LuaTypeRegistry.Unregister<ReflectionTarget>();

        // Act
        var removed = LuaTypeRegistry.Unregister<ReflectionTarget>();

        // Assert
        Assert.That(removed, Is.False);
    }

    [Test]
    public void Unregister_ThenPush_ReregistersAutomatically()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<ReflectionTarget>();

        LuaTypeRegistry.Unregister<ReflectionTarget>();

        // Act
        var target = new ReflectionTarget("auto", 7);
        Lua["obj"] = target;
        var name = Lua.Evaluate<string>("return obj.Name");

        // Assert
        Assert.That(name, Is.EqualTo("auto"));
        Assert.That(LuaTypeRegistry.IsRegistered(typeof(ReflectionTarget)), Is.True);
    }

    [Test]
    public void Unregister_ThenReconfigure_RestoresOptions()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<UnregisterReconfigureTarget>(opts =>
        {
            opts.NamingPolicy = LuaNamingPolicy.LowerSnakeCase;
        });

        var target = new UnregisterReconfigureTarget
        {
            Name = "first",
            Value = 1
        };

        Lua["obj"] = target;

        // Verify snake_case works
        var snakeName = Lua.Evaluate<string>("return obj.name");
        Assert.That(snakeName, Is.EqualTo("first"));

        // Act
        LuaTypeRegistry.Unregister<UnregisterReconfigureTarget>();
        LuaTypeRegistry.ConfigureDynamic<UnregisterReconfigureTarget>(opts =>
        {
            opts.NamingPolicy = LuaNamingPolicy.Original;
        });

        var target2 = new UnregisterReconfigureTarget
        {
            Name = "second",
            Value = 2
        };

        Lua["obj2"] = target2;

        // After reconfigure, PascalCase "Name" should work
        var pascalName = Lua.Evaluate<string>("return obj2.Name");

        // Assert
        Assert.That(pascalName, Is.EqualTo("second"));

        // Cleanup
        LuaTypeRegistry.Unregister<UnregisterReconfigureTarget>();
    }

    [Test]
    public void Unregister_ThenReconfigure_IgnoredMembersReset()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<UnregisterIgnoreTarget>(opts =>
        {
            opts.Ignore("Name");
        });

        var target = new UnregisterIgnoreTarget
        {
            Name = "hidden",
            Value = 42
        };

        Lua["obj"] = target;
        var isNil = Lua.Evaluate<bool>("return obj.Name == nil");
        Assert.That(isNil, Is.True);

        // Act
        LuaTypeRegistry.Unregister<UnregisterIgnoreTarget>();
        LuaTypeRegistry.ConfigureDynamic<UnregisterIgnoreTarget>();

        var target2 = new UnregisterIgnoreTarget
        {
            Name = "visible",
            Value = 99
        };

        Lua["obj2"] = target2;
        var name = Lua.Evaluate<string>("return obj2.Name");

        // Assert
        Assert.That(name, Is.EqualTo("visible"));

        // Cleanup
        LuaTypeRegistry.Unregister<UnregisterIgnoreTarget>();
    }

    [Test]
    public void Unregister_ThenReconfigure_FeaturesReset()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<UnregisterFeaturesTarget>(opts =>
        {
            opts.Features = LuaTypeFeatures.All & ~LuaTypeFeatures.ToString;
        });

        var target = new UnregisterFeaturesTarget
        {
            Name = "no-tostring"
        };

        Lua["obj"] = target;

        // Act
        Assert.DoesNotThrow(() => Lua.Execute("tostring(obj)"));

        LuaTypeRegistry.Unregister<UnregisterFeaturesTarget>();
        LuaTypeRegistry.ConfigureDynamic<UnregisterFeaturesTarget>(opts =>
        {
            opts.Features = LuaTypeFeatures.All;
        });

        var target2 = new UnregisterFeaturesTarget
        {
            Name = "with-tostring"
        };

        Lua["obj2"] = target2;
        var str = Lua.Evaluate<string>("return tostring(obj2)");

        // Assert
        Assert.That(str, Is.Not.Null);
        Assert.That(str, Does.Contain("UnregisterFeaturesTarget"));

        // Cleanup
        LuaTypeRegistry.Unregister<UnregisterFeaturesTarget>();
    }

    [Test]
    public void DynamicTypeCall_SingleArg_ReturnsComputedValue()
    {
        // Arrange
        var handler = new DynamicUserDataHandler(typeof(DynamicCallableTarget));
        LuaTypeRegistry.Register<DynamicCallableTarget>(handler);

        // Act
        var obj = new DynamicCallableTarget
        {
            BaseValue = 10
        };

        handler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Assert
        var result = Lua.Evaluate<int>("return obj(5)");
        Assert.That(result, Is.EqualTo(15));

        LuaTypeRegistry.Unregister<DynamicCallableTarget>();
    }

    [Test]
    public void DynamicTypeCall_TwoArgs_ReturnsSum()
    {
        // Arrange
        var handler = new DynamicUserDataHandler(typeof(DynamicCallableTarget));
        LuaTypeRegistry.Register<DynamicCallableTarget>(handler);

        // Act
        var obj = new DynamicCallableTarget
        {
            BaseValue = 10
        };

        handler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Assert
        var result = Lua.Evaluate<int>("return obj(3, 7)");
        Assert.That(result, Is.EqualTo(20));

        LuaTypeRegistry.Unregister<DynamicCallableTarget>();
    }

    [Test]
    public void DynamicTypeCall_MethodsNotExposedAsRegular()
    {
        // Arrange
        var handler = new DynamicUserDataHandler(typeof(DynamicCallableTarget));
        LuaTypeRegistry.Register<DynamicCallableTarget>(handler);

        // Act
        var obj = new DynamicCallableTarget
        {
            BaseValue = 10
        };

        handler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:Compute(5)"));

        LuaTypeRegistry.Unregister<DynamicCallableTarget>();
    }

    [Test]
    public void DynamicTypeCall_WrongArgCount_Errors()
    {
        // Arrange
        var handler = new DynamicUserDataHandler(typeof(DynamicCallableTarget));
        LuaTypeRegistry.Register<DynamicCallableTarget>(handler);

        // Act
        var obj = new DynamicCallableTarget
        {
            BaseValue = 10
        };

        handler.Push(L, obj);
        lua_setglobal(L, "obj");

        // Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj()"));

        LuaTypeRegistry.Unregister<DynamicCallableTarget>();
    }

    [Test]
    public void DynamicObjectMembers_All_ToStringExposed()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicObjectMembersTarget>(opts =>
        {
            opts.ObjectMembers = LuaSystemObjectMembers.All;
        });

        // Act
        var obj = new DynamicObjectMembersTarget
        {
            Value = 42
        };

        Lua["obj"] = obj;

        // Assert
        var result = Lua.Evaluate<string>("return obj:ToString()");
        Assert.That(result, Is.EqualTo("DynObj(42)"));

        LuaTypeRegistry.Unregister<DynamicObjectMembersTarget>();
    }

    [Test]
    public void DynamicObjectMembers_All_EqualsExposed()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicObjectMembersTarget>(opts =>
        {
            opts.ObjectMembers = LuaSystemObjectMembers.All;
        });

        // Act
        var obj1 = new DynamicObjectMembersTarget
        {
            Value = 42
        };

        var obj2 = new DynamicObjectMembersTarget
        {
            Value = 42
        };

        Lua["a"] = obj1;
        Lua["b"] = obj2;

        // Assert
        var result = Lua.Evaluate<bool>("return a:Equals(b)");
        Assert.That(result, Is.True);

        LuaTypeRegistry.Unregister<DynamicObjectMembersTarget>();
    }

    [Test]
    public void DynamicObjectMembers_All_GetHashCodeExposed()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicObjectMembersTarget>(opts =>
        {
            opts.ObjectMembers = LuaSystemObjectMembers.All;
        });

        // Act
        var obj = new DynamicObjectMembersTarget
        {
            Value = 42
        };

        Lua["obj"] = obj;

        // Assert
        var result = Lua.Evaluate<long>("return obj:GetHashCode()");
        Assert.That(result, Is.EqualTo(42));

        LuaTypeRegistry.Unregister<DynamicObjectMembersTarget>();
    }

    [Test]
    public void DynamicObjectMembers_None_ToStringNotExposed()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicObjectMembersTarget>(opts =>
        {
            opts.ObjectMembers = LuaSystemObjectMembers.None;
        });

        // Act
        var obj = new DynamicObjectMembersTarget
        {
            Value = 5
        };

        Lua["obj"] = obj;

        // Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:ToString()"));
        Assert.That(ex, Is.Not.Null);

        LuaTypeRegistry.Unregister<DynamicObjectMembersTarget>();
    }

    [Test]
    public void DynamicObjectMembers_None_EqualsNotExposed()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicObjectMembersTarget>(opts =>
        {
            opts.ObjectMembers = LuaSystemObjectMembers.None;
        });

        // Act
        var obj = new DynamicObjectMembersTarget
        {
            Value = 5
        };

        Lua["obj"] = obj;

        // Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:Equals(obj)"));
        Assert.That(ex, Is.Not.Null);

        LuaTypeRegistry.Unregister<DynamicObjectMembersTarget>();
    }

    [Test]
    public void DynamicObjectMembers_None_GetHashCodeNotExposed()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicObjectMembersTarget>(opts =>
        {
            opts.ObjectMembers = LuaSystemObjectMembers.None;
        });

        // Act
        var obj = new DynamicObjectMembersTarget
        {
            Value = 5
        };

        Lua["obj"] = obj;

        // Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("obj:GetHashCode()"));
        Assert.That(ex, Is.Not.Null);

        LuaTypeRegistry.Unregister<DynamicObjectMembersTarget>();
    }

    [Test]
    public void DynamicObjectMembers_ToStringOnly_OnlyToStringExposed()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicObjectMembersTarget>(opts =>
        {
            opts.ObjectMembers = LuaSystemObjectMembers.ToString;
        });

        // Act
        var obj = new DynamicObjectMembersTarget
        {
            Value = 7
        };

        Lua["obj"] = obj;

        var result = Lua.Evaluate<string>("return obj:ToString()");
        Assert.That(result, Is.EqualTo("DynObj(7)"));

        // Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj:Equals(obj)"));
        Assert.Throws<LuaException>(() => Lua.Execute("obj:GetHashCode()"));

        LuaTypeRegistry.Unregister<DynamicObjectMembersTarget>();
    }

    [Test]
    public void DynamicObjectMembers_None_ToStringMetamethodStillWorks()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicObjectMembersTarget>(opts =>
        {
            opts.ObjectMembers = LuaSystemObjectMembers.None;
        });

        // Act
        var obj = new DynamicObjectMembersTarget
        {
            Value = 99
        };

        Lua["obj"] = obj;

        // Assert
        // __tostring metamethod is controlled by Features.ToString, not ObjectMembers
        var result = Lua.Evaluate<string>("return tostring(obj)");
        Assert.That(result, Is.EqualTo("DynObj(99)"));

        LuaTypeRegistry.Unregister<DynamicObjectMembersTarget>();
    }

    [Test]
    public void DynamicObjectMembers_DefaultIsAll()
    {
        // Arrange
        // Default DynamicUserDataOptions.ObjectMembers is All
        LuaTypeRegistry.ConfigureDynamic<DynamicObjectMembersTarget>();

        // Act
        var obj = new DynamicObjectMembersTarget
        {
            Value = 10
        };

        Lua["obj"] = obj;

        // All object members should be accessible
        var toString = Lua.Evaluate<string>("return obj:ToString()");
        Assert.That(toString, Is.EqualTo("DynObj(10)"));

        // Assert
        var hash = Lua.Evaluate<long>("return obj:GetHashCode()");
        Assert.That(hash, Is.EqualTo(10));

        LuaTypeRegistry.Unregister<DynamicObjectMembersTarget>();
    }

    [Test]
    public void DynamicConstructor_OptionalParams_AllDefaults()
    {
        // Arrange
        Lua["CtorOptionalTarget"] = typeof(CtorOptionalTarget);

        // Act
        Lua.Execute("obj = CtorOptionalTarget('hello')");

        // Assert
        var obj = (CtorOptionalTarget) Lua.GetGlobal<object>("obj");
        Assert.Multiple(() =>
        {
            Assert.That(obj.Name, Is.EqualTo("hello"));
            Assert.That(obj.Value, Is.EqualTo(10));
            Assert.That(obj.Tag, Is.EqualTo("default"));
        });
    }

    [Test]
    public void DynamicConstructor_OptionalParams_PartialDefaults()
    {
        // Arrange
        Lua["CtorOptionalTarget"] = typeof(CtorOptionalTarget);

        // Act
        Lua.Execute("obj = CtorOptionalTarget('hello', 42)");

        // Assert
        var obj = (CtorOptionalTarget) Lua.GetGlobal<object>("obj");
        Assert.Multiple(() =>
        {
            Assert.That(obj.Name, Is.EqualTo("hello"));
            Assert.That(obj.Value, Is.EqualTo(42));
            Assert.That(obj.Tag, Is.EqualTo("default"));
        });
    }

    [Test]
    public void DynamicConstructor_OptionalParams_AllProvided()
    {
        // Arrange
        Lua["CtorOptionalTarget"] = typeof(CtorOptionalTarget);

        // Act
        Lua.Execute("obj = CtorOptionalTarget('hello', 42, 'custom')");

        // Assert
        var obj = (CtorOptionalTarget) Lua.GetGlobal<object>("obj");
        Assert.Multiple(() =>
        {
            Assert.That(obj.Name, Is.EqualTo("hello"));
            Assert.That(obj.Value, Is.EqualTo(42));
            Assert.That(obj.Tag, Is.EqualTo("custom"));
        });
    }

    [Test]
    public void DynamicConstructor_ParamsArray_Empty()
    {
        // Arrange
        Lua["CtorParamsTarget"] = typeof(CtorParamsTarget);

        // Act
        Lua.Execute("obj = CtorParamsTarget()");

        // Assert
        var obj = (CtorParamsTarget) Lua.GetGlobal<object>("obj");
        Assert.That(obj.Values, Is.Empty);
    }

    [Test]
    public void DynamicConstructor_ParamsArray_Multiple()
    {
        // Arrange
        Lua["CtorParamsTarget"] = typeof(CtorParamsTarget);

        // Act
        Lua.Execute("obj = CtorParamsTarget(1, 2, 3)");

        // Assert
        var obj = (CtorParamsTarget) Lua.GetGlobal<object>("obj");
        Assert.That(obj.Values, Is.EqualTo(new[]
        {
            1, 2, 3
        }));
    }

    [Test]
    public void DynamicConstructor_MixedParamsArray()
    {
        // Arrange
        Lua["CtorMixedParamsTarget"] = typeof(CtorMixedParamsTarget);

        // Act
        Lua.Execute("obj = CtorMixedParamsTarget('test', 10, 20, 30)");

        // Assert
        var obj = (CtorMixedParamsTarget) Lua.GetGlobal<object>("obj");
        Assert.Multiple(() =>
        {
            Assert.That(obj.Label, Is.EqualTo("test"));
            Assert.That(obj.Values, Is.EqualTo(new[]
            {
                10, 20, 30
            }));
        });
    }

    [Test]
    public void DynamicConfig_OperatorsFilter_DisablesSpecificOperator()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DisabledOpsTarget>(opts =>
        {
            opts.Features = LuaTypeFeatures.All & ~LuaTypeFeatures.Add;
        });

        // Act
        var a = new DisabledOpsTarget
        {
            Value = 1
        };

        var b = new DisabledOpsTarget
        {
            Value = 2
        };

        Lua["a"] = a;
        Lua["b"] = b;

        // Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("return a + b"));
        Assert.That(ex!.Message, Does.Contain("attempt to perform arithmetic"));
    }

    [Test]
    public void DynamicConfig_FactoryConstruction_Works()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicFactoryTarget>(opts =>
        {
            opts.Features = LuaTypeFeatures.All;
            opts.ConstructionMode = LuaConstructionMode.Factory;
        });

        // Act
        Lua["DynamicFactoryTarget"] = typeof(DynamicFactoryTarget);

        Lua.Execute("obj = DynamicFactoryTarget.new('hello', 42)");

        // Assert
        var obj = (DynamicFactoryTarget) Lua.GetGlobal<object>("obj");
        Assert.That(obj.Name, Is.EqualTo("hello"));
        Assert.That(obj.Value, Is.EqualTo(42));
    }

    [Test]
    public void DynamicConfig_FactoryConstruction_DirectCallDisabled()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicFactoryTarget>(opts =>
        {
            opts.Features = LuaTypeFeatures.All;
            opts.ConstructionMode = LuaConstructionMode.Factory;
        });

        // Act
        Lua["DynamicFactoryTarget"] = typeof(DynamicFactoryTarget);

        // Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("DynamicFactoryTarget('hello')"));
        Assert.That(ex!.Message, Does.Contain("attempt to call"));
    }

    [Test]
    public void DynamicConfig_FactoryCustomName_Works()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicFactoryTarget>(opts =>
        {
            opts.Features = LuaTypeFeatures.All;
            opts.ConstructionMode = LuaConstructionMode.Factory;
            opts.FactoryMethodName = "create";
        });

        // Act
        Lua["DynamicFactoryTarget"] = typeof(DynamicFactoryTarget);

        Lua.Execute("obj = DynamicFactoryTarget.create('test')");

        // Assert
        var obj = (DynamicFactoryTarget) Lua.GetGlobal<object>("obj");
        Assert.That(obj.Name, Is.EqualTo("test"));
    }

    [Test]
    public void DynamicConfig_ConstructionDisabled_NoCallNoFactory()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicFactoryTarget>(opts =>
        {
            opts.Members = LuaTypeMembers.All & ~LuaTypeMembers.Constructors;
        });

        // Act
        Lua["DynamicFactoryTarget"] = typeof(DynamicFactoryTarget);

        // Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("DynamicFactoryTarget()"));
        Assert.That(ex!.Message, Does.Contain("attempt to call"));
    }

    [Test]
    public void DynamicConfig_OnMetatableCreated_HookCalled()
    {
        // Arrange
        var hookCalled = false;
        LuaTypeRegistry.ConfigureDynamic<ReflectionTarget>(opts =>
        {
            opts.OnMetatableCreated = (_, metatable) =>
            {
                hookCalled = true;
                Assert.That(metatable.Type, Is.EqualTo(LuaType.Table));
            };
        });

        // Act
        Lua["p"] = new ReflectionTarget("test");

        // Assert
        Assert.That(hookCalled, Is.True);
    }

    [Test]
    public void DynamicConfig_OnDefinitionMetatableCreated_HookCalled()
    {
        // Arrange
        var hookCalled = false;
        LuaTypeRegistry.ConfigureDynamic<ReflectionTarget>(opts =>
        {
            opts.OnDefinitionMetatableCreated = (_, metatable) =>
            {
                hookCalled = true;
                Assert.That(metatable.Type, Is.EqualTo(LuaType.Table));
            };
        });

        // Act
        Lua["ReflectionTarget"] = typeof(ReflectionTarget);

        // Assert
        Assert.That(hookCalled, Is.True);
    }

    [Test]
    public void DynamicTableToPoco_NumericFields_MaterializesAllSupportedTypes()
    {
        // Arrange
        Lua["dummy"] = new DynamicNumericTableTarget();
        Lua.Execute("t = { UIntValue = 1, ULongValue = 2, UShortValue = 3, SByteValue = 4, DecimalValue = 5.5 }");

        // Act
        using var _ = Lua.Stack.SnapshotCount();
        lua_getglobal(L, "t");
        var result = Lua.Stack[-1].GetValue<DynamicNumericTableTarget>(LuaAllowedValueConversions.TableToObject);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.UIntValue, Is.EqualTo(1u));
            Assert.That(result.ULongValue, Is.EqualTo(2ul));
            Assert.That(result.UShortValue, Is.EqualTo((ushort) 3));
            Assert.That(result.SByteValue, Is.EqualTo((sbyte) 4));
            Assert.That(result.DecimalValue, Is.EqualTo(5.5m));
        });
    }

    [Test]
    public void DynamicOverloadResolution_BestMatch_ReversedDeclaration_SelectsMostSpecificOverload()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicOverloadResolutionBestMatchReversedTarget>(options =>
            options.OverloadResolution = LuaOverloadResolution.BestMatch);

        Lua["obj"] = new DynamicOverloadResolutionBestMatchReversedTarget();

        try
        {
            // Act & Assert
            Lua.Execute(@"
                assert(obj:Resolve(11) == 'int:11')
                assert(obj:Resolve(3.5) == 'double:3.5')
            ");
        }
        finally
        {
            LuaTypeRegistry.Unregister<DynamicOverloadResolutionBestMatchReversedTarget>();
        }
    }

    [Test]
    public void DynamicOverloadResolution_SByteParameter_IntegerArgument_Matches()
    {
        // Arrange
        Lua["obj"] = new DynamicSByteOverloadResolutionTarget();

        // Act
        var result = Lua.Evaluate<string>("return obj:Resolve(7)");

        // Assert
        Assert.That(result, Is.EqualTo("sbyte:7"));
    }

    [Test]
    public void DynamicOverloadResolution_UShortParameter_IntegerArgument_Matches()
    {
        // Arrange
        Lua["obj"] = new DynamicUShortOverloadResolutionTarget();

        // Act
        var result = Lua.Evaluate<string>("return obj:Resolve(7)");

        // Assert
        Assert.That(result, Is.EqualTo("ushort:7"));
    }

    [Test]
    public void Dynamic_UserDataMultiIndexer_DispatchesByKeyType()
    {
        // Arrange
        var obj = new DynamicUserDataMultiIndexer();
        var dog = new DynamicDog
        {
            Name = "Barker"
        };
        var cat = new DynamicCat
        {
            Name = "Churu"
        };

        Lua["obj"] = obj;
        Lua["dog"] = dog;
        Lua["cat"] = cat;

        // Act
        Lua.Execute("obj[dog] = 'dog'; obj[cat] = 'cat'");
        var dogValue = Lua.Evaluate<string>("return obj[dog]");
        var catValue = Lua.Evaluate<string>("return obj[cat]");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(dogValue, Is.EqualTo("dog"));
            Assert.That(catValue, Is.EqualTo("cat"));
        });
    }

    [Test]
    public void Constructor_IncompatibleTableArgument_RaisesNoConstructorError()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicCtorIntOnlyTarget>(static options =>
        {
            options.BindingConversions = LuaAllowedValueConversions.All;
            options.OverloadResolution = LuaOverloadResolution.BestMatch;
        });

        Lua["CtorType"] = typeof(DynamicCtorIntOnlyTarget);

        // Act
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("CtorType({})"));

        // Assert
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Does.Contain("no constructor"));
    }

    [Test]
    public void TableToObject_FailedMemberConversion_FailsWithoutPartialObject()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicPartialMaterializationTarget>();
        Lua.Execute("t = { Number = 42, Text = {} }");

        // Act
        using var _ = Lua.Stack.SnapshotCount();
        lua_getglobal(L, "t");
        var result = Lua.Stack[-1].TryGetValue(LuaAllowedValueConversions.TableToObject, out DynamicPartialMaterializationTarget? value);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(value, Is.Null);
        });
    }

    [Test]
    public void Assignment_LuaTableProperty_KeepsStoredReferenceAlive()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicLuaReferenceAssignmentTarget>();
        var obj = new DynamicLuaReferenceAssignmentTarget();
        Lua["obj"] = obj;

        try
        {
            // Act
            Lua.Execute("obj.Table = {}");

            // Assert
            Assert.That(obj.Table, Is.Not.Null);
            Assert.DoesNotThrow(() => Lua["stored"] = obj.Table);
        }
        finally
        {
            obj.Table?.Dispose();
        }
    }

    [Test]
    public void Unregister_AfterPush_HandlerStaysAliveForLuaStateLifetime()
    {
        // Arrange
        Lua["target"] = new ReflectionTarget
        {
            Value = 42
        };

        LuaTypeRegistry.Unregister<ReflectionTarget>();

        for (var i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        // Act & Assert
        Assert.That(Lua.Evaluate<int>("return target.Value"), Is.EqualTo(42));
    }
}
