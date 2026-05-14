using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Laylua.Marshaling;
using Laylua.Moon;

namespace Laylua.Tests;

[LuaType]
public partial class TestConstructable
{
    public string Name { get; set; } = "default";

    public int Value { get; set; }

    public TestConstructable()
    { }

    public TestConstructable(string name, int value)
    {
        Name = name;
        Value = value;
    }
}

[LuaType]
public partial class TestReadOnly
{
    public string Name { get; set; } = "";

    public int Health { get; }

    public int DoubleHealth => Health * 2;

    public TestReadOnly()
    { }

    public TestReadOnly(int health)
    {
        Health = health;
    }
}

[LuaType]
public partial class TestThrowingType
{
    public int Value { get; set; }

    public int GetValueOrThrow(bool shouldThrow)
    {
        if (shouldThrow)
        {
            throw new InvalidOperationException("Deliberate test exception");
        }

        return Value;
    }

    public string ThrowingProperty => throw new InvalidOperationException("Property getter exception");
}

[LuaType]
public partial class TestOverloaded
{
    public int Compute(int value)
    {
        return value * 2;
    }

    public int Compute(int value, int multiplier)
    {
        return value * multiplier;
    }

    public string Describe(string text)
    {
        return "text:" + text;
    }

    public string Describe(int number)
    {
        return "number:" + number;
    }

    public string Describe(bool flag)
    {
        return "bool:" + flag.ToString().ToLowerInvariant();
    }

    public string Typed(double value)
    {
        return "double";
    }

    public string Typed(string value)
    {
        return "string";
    }
}

[LuaType]
public partial class TestNullable
{
    public string? Label { get; set; }

    public int? OptionalValue { get; set; }

    public TestVector? Position { get; set; }
}

[LuaType]
public partial struct TestPoint
{
    public double X { get; set; }

    public double Y { get; set; }
}

[LuaType]
public partial struct TestImplicitStructConstructor
{
    public int Value { get; set; }

    public TestImplicitStructConstructor(int value)
    {
        Value = value;
    }
}

[LuaType]
public partial struct TestExplicitParameterlessStructConstructor
{
    public int Value { get; set; }

    public TestExplicitParameterlessStructConstructor()
    {
        Value = 123;
    }

    public TestExplicitParameterlessStructConstructor(int value)
    {
        Value = value;
    }
}

[LuaType]
public partial class TestImplicitClassConstructorDisabled
{
    public int Value { get; set; }
}

[LuaType]
public partial class TestCloseable : IDisposable
{
    public bool WasClosed { get; private set; }

    public int Value { get; set; }

    public void Dispose()
    {
        WasClosed = true;
    }
}

public interface ITestAnimal
{
    string Name { get; }

    string Speak();
}

[LuaType]
public partial class TestDog : ITestAnimal
{
    public string Name { get; set; } = "";

    public int Age { get; set; }

    public string Speak()
    {
        return "Woof!";
    }
}

[LuaType]
public partial class TestCat : ITestAnimal
{
    public string Name { get; set; } = "";

    public string Speak()
    {
        return "Meow!";
    }
}

[LuaType]
public partial class TestWithList
{
    public string Name { get; set; } = "";

    public List<int> Scores { get; set; } = [];
}

[LuaType(PairsBehavior = LuaPairsBehavior.Properties)]
public partial class TestPlayer
{
    public string Name { get; set; } = "";

    public int Health { get; set; }

    public double Speed { get; set; }

    public bool IsAlive { get; set; } = true;

    [LuaIgnore]
    public int InternalId { get; set; }

    public int Field;

    public void TakeDamage(int amount)
    {
        Health -= amount;
        if (Health <= 0)
        {
            IsAlive = false;
        }
    }

    [LuaName("heal")]
    public void RestoreHealth(int amount)
    {
        Health += amount;
    }

    public int GetHealth()
    {
        return Health;
    }

    public override string ToString()
    {
        return $"Player({Name})";
    }
}

[LuaType]
public partial class TestEntity
{
    public string Tag { get; set; } = "";

    public int Id { get; set; }

    public string Describe()
    {
        return $"{Tag}#{Id}";
    }
}

[LuaType]
public partial class TestDerivedEntity : TestEntity
{
    public int Level { get; set; }
}

[LuaType(NamingPolicy = LuaNamingPolicy.LowerSnakeCase)]
public partial class TestSnakeCaseType
{
    public int MaxHealth { get; set; }

    public string FirstName { get; set; } = "";

    public bool IsActive { get; set; }
}

[LuaType(NamingPolicy = LuaNamingPolicy.LowerCase)]
public partial class TestLowerCaseType
{
    public int MaxHealth { get; set; }

    public string FirstName { get; set; } = "";
}

[LuaType(NamingPolicy = LuaNamingPolicy.Original)]
public partial class TestOriginalNamingType
{
    public int Health { get; set; }

    public string Name { get; set; } = "";
}

[LuaType(NamingPolicy = LuaNamingPolicy.CamelCase)]
public partial class TestCamelCaseAcronymType
{
    public int HTTPRequestCount { get; set; }

    public int URLValue { get; set; }
}

[LuaType(NamingPolicy = LuaNamingPolicy.LowerSnakeCase)]
public partial class TestSnakeCaseAcronymType
{
    public int HTTPRequestCount { get; set; }

    public int URLValue { get; set; }
}

[LuaType]
public partial class TestWithInclude
{
    [LuaInclude]
    internal int Secret { get; set; }

    public int Visible { get; set; }
}

[LuaType]
public partial class TestVector
{
    public double X { get; set; }

    public double Y { get; set; }

    public static TestVector operator +(TestVector left, TestVector right)
    {
        return new TestVector
        {
            X = left.X + right.X,
            Y = left.Y + right.Y
        };
    }

    public static TestVector operator -(TestVector left, TestVector right)
    {
        return new TestVector
        {
            X = left.X - right.X,
            Y = left.Y - right.Y
        };
    }

    public static TestVector operator -(TestVector operand)
    {
        return new TestVector
        {
            X = -operand.X,
            Y = -operand.Y
        };
    }

    public static bool operator ==(TestVector left, TestVector right)
    {
        return left.X == right.X && left.Y == right.Y;
    }

    public static bool operator !=(TestVector left, TestVector right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        return obj is TestVector other && this == other;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}

[LuaType]
public partial class TestWithStatics
{
    public static int MaxInstances { get; set; } = 100;

    public static string TypeName => "TestWithStatics";

    public int Value { get; set; }

    public static int Add(int a, int b)
    {
        return a + b;
    }
}

[LuaType]
public partial class TestWithOptionalParams
{
    public int Calculate(int value, int multiplier = 2)
    {
        return value * multiplier;
    }
}

public enum TestColor
{
    Red = 0,
    Green = 1,
    Blue = 2,
}

[LuaType]
public partial class TestWithEnum
{
    public TestColor Color { get; set; }
}

[LuaType]
public partial class TestDisposable : IDisposable
{
    public bool WasDisposed { get; private set; }

    public int Value { get; set; }

    public void Dispose()
    {
        WasDisposed = true;
    }
}

[LuaType]
public partial class TestWithIndexer
{
    private readonly Dictionary<string, int> _data = new();

    public int Count => _data.Count;

    public int this[string key]
    {
        get => _data.TryGetValue(key, out var value) ? value : 0;
        set => _data[key] = value;
    }
}

[LuaType(IndexMembers = LuaIndexMembers.Properties)]
public partial class TestIndexPropertiesOnly
{
    public int Health { get; set; } = 100;

    public int GetHealth()
    {
        return Health;
    }
}

[LuaType(IndexMembers = LuaIndexMembers.Methods)]
public partial class TestIndexMethodsOnly
{
    public int Health { get; set; } = 100;

    public int GetHealth()
    {
        return Health;
    }
}

[LuaType(IndexMembers = LuaIndexMembers.Indexer)]
public partial class TestIndexIndexerOnly
{
    private readonly Dictionary<string, int> _data = new();

    public int Count => _data.Count;

    public int GetCount()
    {
        return _data.Count;
    }

    public int this[string key]
    {
        get => _data.TryGetValue(key, out var value) ? value : 0;
        set => _data[key] = value;
    }
}

[LuaType(IndexMembers = LuaIndexMembers.Properties | LuaIndexMembers.Indexer, IndexPriority = LuaIndexPriority.IndexerFirst)]
public partial class TestIndexerFirst
{
    private readonly Dictionary<string, int> _data = new();

    public int Count => _data.Count;

    public int this[string key]
    {
        get => _data.TryGetValue(key, out var value) ? value : 0;
        set => _data[key] = value;
    }
}

[LuaType(UnknownPropertyBehavior = LuaUnknownPropertyBehavior.Error)]
public partial class TestStrictStringIndexer
{
    private readonly Dictionary<string, int> _data = new();

    public int this[string key]
    {
        get => _data.TryGetValue(key, out var value) ? value : 0;
        set => _data[key] = value;
    }
}

[LuaType(IndexPriority = LuaIndexPriority.IndexerFirst, UnknownPropertyBehavior = LuaUnknownPropertyBehavior.Error)]
public partial class TestStrictIndexerFirst
{
    private readonly Dictionary<string, int> _data = new();

    public int this[string key]
    {
        get => _data.TryGetValue(key, out var value) ? value : 0;
        set => _data[key] = value;
    }
}

[LuaType]
public partial class TestMultiIndexer
{
    private readonly Dictionary<string, string> _stringData = new();
    private readonly Dictionary<int, string> _intData = new();

    public string this[string key]
    {
        get => _stringData.TryGetValue(key, out var value) ? value : "";
        set => _stringData[key] = value;
    }

    public string this[int index]
    {
        get => _intData.TryGetValue(index, out var value) ? value : "";
        set => _intData[index] = value;
    }
}

[LuaType]
public partial class TestWithNestedType
{
    public string Label { get; set; } = "";

    public TestVector? Position { get; set; }
}

[LuaType(Optimizations = LuaTypeOptimizations.None)]
public partial class TestNoPrimitiveOptimization
{
    public int Value { get; set; }
}

[LuaType(BindingConversions = LuaAllowedValueConversions.None)]
public partial class TestNoImplicitConversionWithOptimization
{
    public int Value { get; set; }
}

[LuaType(Members = LuaTypeMembers.None, Features = LuaTypeFeatures.None)]
public partial class TestNoFeatures
{
    public int Value { get; set; }

    public override string ToString()
    {
        return $"NoFeatures({Value})";
    }
}

[LuaType(Features = LuaTypeFeatures.All & ~LuaTypeFeatures.ToString)]
public partial class TestNoToString
{
    public int Value { get; set; }

    public override string ToString()
    {
        return $"NoToString({Value})";
    }
}

[LuaType(Features = LuaTypeFeatures.All & ~LuaTypeFeatures.Dispose)]
public partial class TestNoDispose : IDisposable
{
    public bool WasDisposed { get; private set; }

    public int Value { get; set; }

    public void Dispose()
    {
        WasDisposed = true;
    }
}

[LuaType(Features = LuaTypeFeatures.All & ~LuaTypeFeatures.Operators | LuaTypeFeatures.Add)]
public partial class TestSelectiveOps
{
    public int Value { get; set; }

    public static TestSelectiveOps operator +(TestSelectiveOps left, TestSelectiveOps right)
    {
        return new TestSelectiveOps
        {
            Value = left.Value + right.Value
        };
    }

    public static TestSelectiveOps operator -(TestSelectiveOps left, TestSelectiveOps right)
    {
        return new TestSelectiveOps
        {
            Value = left.Value - right.Value
        };
    }

    public static bool operator ==(TestSelectiveOps left, TestSelectiveOps right)
    {
        return left.Value == right.Value;
    }

    public static bool operator !=(TestSelectiveOps left, TestSelectiveOps right)
    {
        return !(left == right);
    }

    public override bool Equals(object? obj)
    {
        return obj is TestSelectiveOps other && this == other;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}

[LuaType(Features = LuaTypeFeatures.All & ~LuaTypeFeatures.Operators)]
public partial class TestNoOps
{
    public int Value { get; set; }

    public static TestNoOps operator +(TestNoOps left, TestNoOps right)
    {
        return new TestNoOps
        {
            Value = left.Value + right.Value
        };
    }

    public override string ToString()
    {
        return $"NoOps({Value})";
    }
}

[LuaType]
public partial class TestWithCallingThread
{
    public string Name { get; set; } = "";

    [return: LuaReturnCount]
    public int PushMultipleValues([LuaCallingThread] LuaThread lua, int count)
    {
        for (var i = 1; i <= count; i++)
        {
            lua.Stack.Push(Name + "_" + i);
        }

        return count;
    }

    public int MultiplyByTwo([LuaCallingThread] LuaThread lua, int value)
    {
        return value * 2;
    }

    public void SetNameViaThread([LuaCallingThread] LuaThread lua, string newName)
    {
        Name = newName;
    }
}

[LuaType(UnknownPropertyBehavior = LuaUnknownPropertyBehavior.Error)]
public partial class TestStrictType
{
    public int Value { get; set; }
}

[LuaType(OverloadResolution = LuaOverloadResolution.BestMatch)]
public partial class TestOverloadResolutionBestMatch
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
}

[LuaType(OverloadResolution = LuaOverloadResolution.Error)]
public partial class TestOverloadResolutionError
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

[LuaType]
public partial class TestOverloadResolutionDefault
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
}

public enum ImplicitConversionStatus
{
    Alpha = 1,
    Beta = 2,
}

[Flags]
public enum ImplicitConversionFlags
{
    None = 0,
    Read = 1 << 0,
    Write = 1 << 1,
}

[LuaType]
public partial class TypedImplicitConversionTarget
{
    public string Name { get; set; } = "";

    public int Value { get; set; }

    public TypedImplicitConversionTarget()
    { }

    public TypedImplicitConversionTarget(string name, int value)
    {
        Name = name;
        Value = value;
    }
}

public class TypedImplicitConversionBase
{
    public string Dispatch(string text)
    {
        return $"string:{text}";
    }

    public string Dispatch(int number)
    {
        return $"int:{number}";
    }

    public string Dispatch(object fallback)
    {
        return $"object:{fallback}";
    }

    public string NumericOnly(int number)
    {
        return $"int:{number}";
    }

    public string NumericOnly(double number)
    {
        return $"double:{number}";
    }

    public string ByteOnly(byte number)
    {
        return $"byte:{number}";
    }

    public string NarrowOrWide(byte number)
    {
        return $"byte:{number}";
    }

    public string NarrowOrWide(long number)
    {
        return $"long:{number}";
    }

    public string AcceptsDelegate(Action callback)
    {
        callback();
        return "delegate-called";
    }

    public string AcceptsTarget(TypedImplicitConversionTarget target)
    {
        return $"target:{target.Name}:{target.Value}";
    }

    public string NumericOrObject(double number)
    {
        return $"double:{number}";
    }

    public string NumericOrObject(object fallback)
    {
        return $"object:{fallback}";
    }

    public string NilOrObject(int number)
    {
        return $"int:{number}";
    }

    public string NilOrObject(object value)
    {
        return $"object:{value}";
    }

    public string StringOnly(string text)
    {
        return $"string:{text}";
    }

    public string LuaStringOnly(LuaString text)
    {
        return $"luastring:{text}";
    }

    public string PointerOnly(IntPtr pointer)
    {
        return $"ptr:0x{pointer.ToString("x")}";
    }

    public string CharOnly(char value)
    {
        return $"char:{value}";
    }

    public string CharOrString(char value)
    {
        return $"char:{value}";
    }

    public string CharOrString(string text)
    {
        return $"string:{text}";
    }

    public string EnumOnly(ImplicitConversionStatus value)
    {
        return $"enum:{value}";
    }

    public string FlagsOnly(ImplicitConversionFlags value)
    {
        return $"flags:{value}";
    }

    public string StringOrEnum(string text)
    {
        return $"string:{text}";
    }

    public string StringOrEnum(ImplicitConversionStatus value)
    {
        return $"enum:{value}";
    }
}

[LuaType(OverloadResolution = LuaOverloadResolution.BestMatch, BindingConversions = LuaAllowedValueConversions.None)]
public partial class TypedImplicitConversionNone : TypedImplicitConversionBase
{ }

[LuaType(OverloadResolution = LuaOverloadResolution.BestMatch, BindingConversions = LuaAllowedValueConversions.Default)]
public partial class TypedImplicitConversionDefault : TypedImplicitConversionBase
{ }

[LuaType(OverloadResolution = LuaOverloadResolution.BestMatch, BindingConversions = LuaAllowedValueConversions.All)]
public partial class TypedImplicitConversionAll : TypedImplicitConversionBase
{ }

[LuaType(BindingConversions = LuaAllowedValueConversions.Default)]
public partial class TypedImplicitConversionConstructableDefault
{
    public string Summary { get; }

    public TypedImplicitConversionConstructableDefault(TypedImplicitConversionTarget target)
    {
        Summary = $"target:{target.Name}:{target.Value}";
    }
}

[LuaType(BindingConversions = LuaAllowedValueConversions.All)]
public partial class TypedImplicitConversionConstructable
{
    public string Summary { get; }

    public TypedImplicitConversionConstructable(TypedImplicitConversionTarget target)
    {
        Summary = $"target:{target.Name}:{target.Value}";
    }
}

[LuaType(BindingConversions = LuaAllowedValueConversions.None)]
public partial class TestEnumPropertyNoNumberConversion
{
    public ImplicitConversionStatus Value { get; set; }
}

[LuaType]
public partial class TestEnumPropertyValidation
{
    public ImplicitConversionStatus Value { get; set; }
}

[LuaType(BindingConversions = LuaAllowedValueConversions.None)]
public partial class TestConstructableEnumNoNumberConversion
{
    public ImplicitConversionStatus Value { get; }

    public TestConstructableEnumNoNumberConversion(ImplicitConversionStatus value)
    {
        Value = value;
    }
}

[LuaType]
public partial class TestConstructableEnumValidation
{
    public ImplicitConversionStatus Value { get; }

    public TestConstructableEnumValidation(ImplicitConversionStatus value)
    {
        Value = value;
    }
}

[LuaType(BindingConversions = LuaAllowedValueConversions.None)]
public partial class TestConstructableEnumParamsNoNumberConversion
{
    public ImplicitConversionStatus[] Values { get; }

    public TestConstructableEnumParamsNoNumberConversion(params ImplicitConversionStatus[] values)
    {
        Values = values;
    }
}

[LuaType(BindingConversions = LuaAllowedValueConversions.None)]
public partial class TestEnumIndexerNoNumberConversion
{
    private readonly Dictionary<string, ImplicitConversionStatus> _values = new();

    public ImplicitConversionStatus this[string key]
    {
        get
        {
            if (_values.TryGetValue(key, out var value))
            {
                return value;
            }

            return default;
        }
        set
        {
            _values[key] = value;
        }
    }
}

[LuaType]
public partial class TestMethodAliasCallbacks
{
    [LuaName("from_number")]
    public string Dispatch(int value)
    {
        return $"int:{value}";
    }

    [LuaName("from_text")]
    public string Dispatch(string text)
    {
        return $"string:{text}";
    }
}

[LuaType]
public partial class TestWithParams
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

    public string Join(string separator, params string[] parts)
    {
        return string.Join(separator, parts);
    }

    public string Format(int value)
    {
        return $"single:{value}";
    }

    public string Format(int value, params int[] rest)
    {
        return $"multi:{value}+{string.Join(",", rest)}";
    }
}

[LuaType]
public partial struct TestMutableStruct
{
    public float X { get; set; }

    public float Y { get; set; }

    public void Scale(float factor)
    {
        X *= factor;
        Y *= factor;
    }
}

[LuaType]
public partial class TestWithRefOut
{
    public bool TryParse(string input, out int result)
    {
        if (int.TryParse(input, out result))
        {
            return true;
        }

        result = -1;
        return false;
    }

    public void Swap(ref int a, ref int b)
    {
        var temp = a;
        a = b;
        b = temp;
    }

    public int AddAndGetOriginal(ref int value, int amount)
    {
        var original = value;
        value += amount;
        return original;
    }
}

[LuaType]
public partial class TestCallingThreadOutOverloadTarget
{
    public string Resolve([LuaCallingThread] LuaThread lua, int value)
    {
        return $"int:{value}";
    }

    public string Resolve([LuaCallingThread] LuaThread lua, string value, out int length)
    {
        length = value.Length;
        return $"string:{value}";
    }
}

[LuaType]
public partial class TestConstructableMarshaledOverloads
{
    public string Kind { get; }

    public string? Text { get; }

    public TestConstructableMarshaledOverloads(object? value)
    {
        Kind = value == null ? "nil" : "object";
        Text = value?.ToString();
    }

    public TestConstructableMarshaledOverloads(string text)
    {
        Kind = "string";
        Text = text;
    }
}

[LuaType(Features = LuaTypeFeatures.All & ~LuaTypeFeatures.Pairs)]
public partial class TestPairsNone
{
    public int X { get; set; }

    public int Y { get; set; }
}

[LuaType(PairsBehavior = LuaPairsBehavior.Enumerable)]
public partial class TestPairsIfEnumerable
{
    public int Length { get; set; }
}

[LuaType(PairsBehavior = LuaPairsBehavior.Enumerable)]
public partial class TestPairsIfEnumerableList : IEnumerable<int>
{
    private readonly List<int> _items = [10, 20, 30];

    public int Count => _items.Count;

    public IEnumerator<int> GetEnumerator()
    {
        return _items.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

[LuaType(PairsBehavior = LuaPairsBehavior.Enumerable)]
public partial class TestPairsDisposableEnumerable : IEnumerable<int>
{
    public int EnumeratorDisposeCount { get; private set; }

    public IEnumerator<int> GetEnumerator()
    {
        return new TrackingEnumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private sealed class TrackingEnumerator : IEnumerator<int>
    {
        private readonly TestPairsDisposableEnumerable _owner;
        private int _index = -1;

        public int Current => _index + 1;

        object IEnumerator.Current => Current;

        public TrackingEnumerator(TestPairsDisposableEnumerable owner)
        {
            _owner = owner;
        }

        public bool MoveNext()
        {
            if (_index >= 2)
            {
                return false;
            }

            _index++;
            return _index < 3;
        }

        public void Reset()
        {
            _index = -1;
        }

        public void Dispose()
        {
            _owner.EnumeratorDisposeCount++;
        }
    }
}

[LuaType(Construction = LuaConstructionMode.Factory)]
public partial class TestFactoryDefault
{
    public string Name { get; set; } = "";

    public int Value { get; set; }

    public TestFactoryDefault()
    { }

    public TestFactoryDefault(string name, int value)
    {
        Name = name;
        Value = value;
    }
}

[LuaType(Construction = LuaConstructionMode.Factory, FactoryMethodName = "create")]
public partial class TestFactoryCustomName
{
    public string Label { get; set; } = "";

    public TestFactoryCustomName()
    { }

    public TestFactoryCustomName(string label)
    {
        Label = label;
    }
}

[LuaType(Members = LuaTypeMembers.All & ~LuaTypeMembers.Constructors)]
public partial class TestNoConstruction
{
    public string Name { get; set; } = "";

    public static int Counter { get; set; }
}

[LuaType]
public partial class TestLuaNameProperty
{
    [LuaName("hp")]
    public int Health { get; set; }

    [LuaName("pos_x")]
    public double PositionX { get; set; }
}

[LuaType]
public partial class TestWithCount
{
    public string Name { get; set; } = "";

    private readonly List<string> _items = [];

    public int Count => _items.Count;

    public void Add(string item)
    {
        _items.Add(item);
    }
}

[LuaType]
public partial class TestWithLength
{
    public byte[] Data { get; set; } = [];

    public int Length => Data.Length;
}

[LuaType(Features = LuaTypeFeatures.All & ~LuaTypeFeatures.Length)]
public partial class TestLenDisabled
{
    public int Count => 42;
}

[LuaType]
public partial class TestAllOperators
{
    public int Value { get; set; }

    public static TestAllOperators operator +(TestAllOperators a, TestAllOperators b)
    {
        return new()
        {
            Value = a.Value + b.Value
        };
    }

    public static TestAllOperators operator -(TestAllOperators a, TestAllOperators b)
    {
        return new()
        {
            Value = a.Value - b.Value
        };
    }

    public static TestAllOperators operator *(TestAllOperators a, TestAllOperators b)
    {
        return new()
        {
            Value = a.Value * b.Value
        };
    }

    public static TestAllOperators operator /(TestAllOperators a, TestAllOperators b)
    {
        return new()
        {
            Value = a.Value / b.Value
        };
    }

    public static TestAllOperators operator %(TestAllOperators a, TestAllOperators b)
    {
        return new()
        {
            Value = a.Value % b.Value
        };
    }

    public static TestAllOperators operator -(TestAllOperators a)
    {
        return new()
        {
            Value = -a.Value
        };
    }

    public static bool operator ==(TestAllOperators a, TestAllOperators b)
    {
        return a.Value == b.Value;
    }

    public static bool operator !=(TestAllOperators a, TestAllOperators b)
    {
        return a.Value != b.Value;
    }

    public static bool operator <(TestAllOperators a, TestAllOperators b)
    {
        return a.Value < b.Value;
    }

    public static bool operator <=(TestAllOperators a, TestAllOperators b)
    {
        return a.Value <= b.Value;
    }

    public static bool operator >(TestAllOperators a, TestAllOperators b)
    {
        return a.Value > b.Value;
    }

    public static bool operator >=(TestAllOperators a, TestAllOperators b)
    {
        return a.Value >= b.Value;
    }

    public static TestAllOperators operator &(TestAllOperators a, TestAllOperators b)
    {
        return new()
        {
            Value = a.Value & b.Value
        };
    }

    public static TestAllOperators operator |(TestAllOperators a, TestAllOperators b)
    {
        return new()
        {
            Value = a.Value | b.Value
        };
    }

    public static TestAllOperators operator ^(TestAllOperators a, TestAllOperators b)
    {
        return new()
        {
            Value = a.Value ^ b.Value
        };
    }

    public static TestAllOperators operator ~(TestAllOperators a)
    {
        return new()
        {
            Value = ~a.Value
        };
    }

    public static TestAllOperators operator <<(TestAllOperators a, int b)
    {
        return new()
        {
            Value = a.Value << b
        };
    }

    public static TestAllOperators operator >> (TestAllOperators a, int b)
    {
        return new()
        {
            Value = a.Value >> b
        };
    }

    public override bool Equals(object? obj)
    {
        return obj is TestAllOperators other && Value == other.Value;
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

[LuaType]
public partial class TestMixedOperators
{
    public double Value { get; set; }

    public static TestMixedOperators operator *(TestMixedOperators a, TestMixedOperators b)
    {
        return new()
        {
            Value = a.Value * b.Value
        };
    }

    public static TestMixedOperators operator *(TestMixedOperators a, double b)
    {
        return new()
        {
            Value = a.Value * b
        };
    }

    public static TestMixedOperators operator *(double a, TestMixedOperators b)
    {
        return new()
        {
            Value = a * b.Value
        };
    }
}

[LuaType]
public partial class TestCallable
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

    [LuaTypeCall]
    public string Format(string prefix)
    {
        return prefix + BaseValue;
    }
}

[LuaType(ArgumentValidation = LuaArgumentValidation.Strict)]
public partial class TestStrictValidation
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

[LuaType(ObjectMembers = LuaSystemObjectMembers.All)]
public partial class TestObjectMembersAll
{
    public int Value { get; set; }

    public override string ToString()
    {
        return $"ObjAll({Value})";
    }

    public override bool Equals(object? obj)
    {
        return obj is TestObjectMembersAll other && Value == other.Value;
    }

    public override int GetHashCode()
    {
        return Value;
    }
}

[LuaType(ObjectMembers = LuaSystemObjectMembers.ToString)]
public partial class TestObjectMembersToStringOnly
{
    public int Value { get; set; }

    public override string ToString()
    {
        return $"TS({Value})";
    }

    public override bool Equals(object? obj)
    {
        return obj is TestObjectMembersToStringOnly other && Value == other.Value;
    }

    public override int GetHashCode()
    {
        return Value;
    }
}

[LuaType(ObjectMembers = LuaSystemObjectMembers.None)]
public partial class TestObjectMembersNone
{
    public int Value { get; set; }

    public override string ToString()
    {
        return $"NoObj({Value})";
    }

    public override bool Equals(object? obj)
    {
        return obj is TestObjectMembersNone other && Value == other.Value;
    }

    public override int GetHashCode()
    {
        return Value;
    }
}

[LuaType(ArgumentValidation = LuaArgumentValidation.None)]
public partial class TestNoValidation
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

[LuaType(ArgumentValidation = LuaArgumentValidation.Nullability)]
public partial class TestNullabilityOnlyValidation
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

[LuaType]
public partial class TestLuaStringParamUtf8
{
    public int ParseUtf8([LuaString] ReadOnlySpan<byte> utf8Text)
    {
        return int.Parse(utf8Text);
    }

    public string Echo([LuaString] ReadOnlySpan<byte> utf8Text)
    {
        return Encoding.UTF8.GetString(utf8Text);
    }
}

[LuaType]
public partial class TestLuaStringParamUtf16
{
    public int ParseUtf16([LuaString] ReadOnlySpan<char> utf16Text)
    {
        return int.Parse(utf16Text);
    }

    public string Echo([LuaString] ReadOnlySpan<char> utf16Text)
    {
        return utf16Text.ToString();
    }
}

[LuaType]
public partial class TestLuaStringPropertyUtf8
{
    [LuaString]
    public ReadOnlySpan<byte> Utf8Name => "hello"u8;

    public string Name { get; set; } = "world";
}

[LuaType]
public partial class TestLuaStringPropertyUtf16
{
    [LuaString]
    public ReadOnlySpan<char> Utf16Name => "hello";

    public string Name { get; set; } = "world";
}

[LuaType]
public partial class TestLuaStringConstructorUtf16
{
    public string Text { get; }

    public TestLuaStringConstructorUtf16([LuaString] ReadOnlySpan<char> text)
    {
        Text = text.ToString();
    }
}

[LuaType]
public partial struct TestStructNullability
{
    public int Value { get; set; }

    public int Add(int a)
    {
        return Value + a;
    }
}

[LuaType(ArgumentValidation = LuaArgumentValidation.None)]
public partial struct TestStructNullabilityNone
{
    public int Value { get; set; }

    public int Add(int a)
    {
        return Value + a;
    }
}

[LuaType(ArgumentValidation = LuaArgumentValidation.Strict)]
public partial class TestRangeValidation
{
    public byte ToByte(byte value)
    {
        return value;
    }

    public sbyte ToSByte(sbyte value)
    {
        return value;
    }

    public short ToShort(short value)
    {
        return value;
    }

    public ushort ToUShort(ushort value)
    {
        return value;
    }

    public int ToInt(int value)
    {
        return value;
    }

    public uint ToUInt(uint value)
    {
        return value;
    }

    public long ToLong(long value)
    {
        return value;
    }

    public ulong ToULong(ulong value)
    {
        return value;
    }

    public float ToFloat(float value)
    {
        return value;
    }

    public byte AddBytes(byte a, byte b)
    {
        return (byte) (a + b);
    }

    public int OptionalInt(int value = 42)
    {
        return value;
    }
}

[LuaType(ArgumentValidation = LuaArgumentValidation.Nullability | LuaArgumentValidation.Type | LuaArgumentValidation.Count)]
public partial class TestNoRangeValidation
{
    public byte ToByte(byte value)
    {
        return value;
    }

    public int ToInt(int value)
    {
        return value;
    }
}

[LuaType(ObsoleteMembers = LuaObsoleteMemberHandling.Ignore)]
public partial class TestObsoleteIgnored
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

[LuaType(ObsoleteMembers = LuaObsoleteMemberHandling.Include)]
public partial class TestObsoleteIncluded
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

[LuaType(FromTableConversion = LuaFromTableConversion.Disabled)]
public partial class InitOnlyPropertyPlayer
{
    public string Name { get; init; } = "";

    public int Health { get; set; } = 100;
}

[LuaType]
public partial class TestTableRequiredInitTarget
{
    public required string Name { get; init; }

    public int Health { get; set; }
}

[LuaType]
public partial class TestTableRequiredSetTarget
{
    public required string Name { get; set; }

    public int Health { get; set; }
}

[LuaType]
public partial class TestTableNestedMessageTarget
{
    public required string Content { get; init; }

    public TestTableNestedEmbedTarget? Embed { get; set; }
}

[LuaType]
public partial class TestTableNestedEmbedTarget
{
    public string Description { get; set; } = "";
}

[LuaType(FromTableConversion = LuaFromTableConversion.Disabled)]
public partial class TestTableDisabledTarget
{
    public string Name { get; set; } = "";
}

[LuaType]
public partial struct TestTableRequiredInitStruct
{
    public required int Value { get; init; }

    public int Health { get; set; }
}

[LuaType]
public partial class TestTableInheritedRequiredInitBase
{
    public required string Name { get; init; }

    public int BaseHealth { get; set; }
}

[LuaType]
public partial class TestTableInheritedRequiredInitDerived : TestTableInheritedRequiredInitBase
{
    public int DerivedHealth { get; set; }
}

[LuaType]
public partial class TestTableNoParameterlessTarget
{
    public string Name { get; }

    public TestTableNoParameterlessTarget(string name)
    {
        Name = name;
    }
}

[LuaType]
public partial class TestTableHookTarget
{
    public string Name { get; set; } = "";

    public int Health { get; set; }
}

public partial class TestTableHookTarget
{
    static partial void OnBeforeTryGetFromTable(LuaThread thread, LuaTable table)
    {
        table["Health"] = 25;
    }

    static partial void OnAfterTryGetFromTable(LuaThread thread, LuaTable table, ref TestTableHookTarget value)
    {
        value.Name += "!";
    }
}

[LuaType]
public partial class TestTableOverrideTarget : ILuaUserDataFromTable<TestTableOverrideTarget>
{
    public string Name { get; set; } = "";

    public int Health { get; set; }

    public bool OverrideUsed { get; set; }

    public static bool TryGetFromTable(LuaThread luaThread, LuaTable table, [MaybeNullWhen(false)] out TestTableOverrideTarget value)
    {
        return TryGetFromTable(luaThread, table, out value, out _);
    }

    public static bool TryGetFromTable(
        LuaThread luaThread,
        LuaTable table,
        [MaybeNullWhen(false)] out TestTableOverrideTarget value,
        [NotNullWhen(false)] out LuaConversionError? error)
    {
        if (!TryGetFromTableDefault(luaThread, table, out value, out error))
        {
            return false;
        }

        value.OverrideUsed = true;
        value.Health += 5;
        error = null;
        return true;
    }
}

[LuaType]
public partial class TestTableExplicitInterfaceTarget : ILuaUserDataFromTable<TestTableExplicitInterfaceTarget>
{
    public string Name { get; set; } = "";

    public bool ExplicitInterfaceUsed { get; set; }

    static bool ILuaUserDataFromTable<TestTableExplicitInterfaceTarget>.TryGetFromTable(
        LuaThread luaThread,
        LuaTable table,
        [MaybeNullWhen(false)] out TestTableExplicitInterfaceTarget value,
        [NotNullWhen(false)] out LuaConversionError? error)
    {
        value = new TestTableExplicitInterfaceTarget
        {
            Name = table.GetValue<string, string>("Name"),
            ExplicitInterfaceUsed = true,
        };

        error = null;
        return true;
    }
}

[LuaType]
public partial class TestTableInterfaceErrorTarget : ILuaUserDataFromTable<TestTableInterfaceErrorTarget>
{
    public string Name { get; set; } = "";

    static bool ILuaUserDataFromTable<TestTableInterfaceErrorTarget>.TryGetFromTable(
        LuaThread luaThread,
        LuaTable table,
        [MaybeNullWhen(false)] out TestTableInterfaceErrorTarget value,
        [NotNullWhen(false)] out LuaConversionError? error)
    {
        if (table.TryGetValue<string, string>("Name", out var name) && name == "ok")
        {
            value = new TestTableInterfaceErrorTarget
            {
                Name = name,
            };

            error = null;
            return true;
        }

        value = default;
        error = new LuaConversionError(
            "custom table conversion only accepts Name = 'ok'",
            "$.Name",
            typeof(TestTableInterfaceErrorTarget),
            LuaType.String,
            "Name");

        return false;
    }
}

[LuaType]
public partial class TestTableCustomMethodWithoutInterface
{
    public string Name { get; set; } = "";

    public bool CustomMethodUsed { get; set; }

    private static bool TryGetFromTable(LuaThread thread, LuaTable table, [MaybeNullWhen(false)] out TestTableCustomMethodWithoutInterface value)
    {
        value = new TestTableCustomMethodWithoutInterface
        {
            Name = table.GetValue<string, string>("Name"),
            CustomMethodUsed = true,
        };

        return true;
    }
}

[LuaType]
public partial class TestConstructableParams
{
    public int[] Values { get; }

    public TestConstructableParams(params int[] values)
    {
        Values = values;
    }
}

[LuaType]
public partial class TestConstructableMixedParams
{
    public string Label { get; }

    public int[] Values { get; }

    public TestConstructableMixedParams(string label, params int[] values)
    {
        Label = label;
        Values = values;
    }
}

[LuaType(ArgumentValidation = LuaArgumentValidation.Strict)]
public partial class TestStrictConstructableByte
{
    public byte Value { get; }

    public TestStrictConstructableByte(byte value = 1)
    {
        Value = value;
    }
}

[LuaType]
public partial class TestConstructableSignedEnumDefault
{
    public TestSignedEnum Value { get; }

    public TestConstructableSignedEnumDefault(TestSignedEnum value = TestSignedEnum.Min)
    {
        Value = value;
    }
}

[LuaType]
public partial class TestConstructableUnsignedEnumDefault
{
    public TestUnsignedEnum Value { get; }

    public TestConstructableUnsignedEnumDefault(TestUnsignedEnum value = TestUnsignedEnum.Max)
    {
        Value = value;
    }
}

public enum TestSignedEnum : long
{
    Min = long.MinValue
}

public enum TestUnsignedEnum : ulong
{
    Max = ulong.MaxValue
}

[LuaType]
public partial class TestOverloadResolutionBestMatchReversed
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

[LuaType]
public partial class TestUserDataMultiIndexer
{
    private readonly Dictionary<TestDog, string> _dogData = new();
    private readonly Dictionary<TestCat, string> _catData = new();

    public string this[TestDog dog]
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

    public string this[TestCat cat]
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

[LuaType]
public partial class TestStaticReadOnlyOnly
{
    public static string TypeName
    {
        get
        {
            return "TestStaticReadOnlyOnly";
        }
    }
}

[LuaType]
public partial class TestIncludedNonPublicParameterlessConstructor
{
    public string Name { get; set; } = "";

    [LuaInclude]
    private TestIncludedNonPublicParameterlessConstructor()
    { }
}

[LuaType]
public partial class TestWithReturnCountAndOut
{
    [return: LuaReturnCount]
    public int PushValueAndLength([LuaCallingThread] LuaThread lua, string value, out int length)
    {
        length = value.Length;
        lua.Stack.Push(value.ToUpperInvariant());
        return 1;
    }

    [return: LuaReturnCount]
    public int PushValueAndLength([LuaCallingThread] LuaThread lua, int value, out int doubled)
    {
        doubled = value * 2;
        lua.Stack.Push(value);
        return 1;
    }
}


[LuaType]
public partial class TestWithSingleReturnCountAndOut
{
    [return: LuaReturnCount]
    public int PushValueAndLength([LuaCallingThread] LuaThread lua, string value, out int length)
    {
        length = value.Length;
        lua.Stack.Push(value.ToUpperInvariant());
        return 1;
    }
}
