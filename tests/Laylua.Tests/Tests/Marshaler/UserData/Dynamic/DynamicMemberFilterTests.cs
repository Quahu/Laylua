using System.Reflection;
using Laylua.Marshaling;

namespace Laylua.Tests;

public class DynamicMemberFilterTarget
{
    private readonly Dictionary<string, int> _values = new(StringComparer.Ordinal)
    {
        ["hp"] = 10
    };

    public string Name { get; set; } = "Steve";

    public int Field = 7;

    public int Count { get; set; } = 2;

    public event Action? FilteredEvent;

    public DynamicMemberFilterTarget()
    { }

    public DynamicMemberFilterTarget(string name)
    {
        Name = name;
    }

    public int GetValue()
    {
        return 42;
    }

    public int this[string key]
    {
        get
        {
            return _values[key];
        }
        set
        {
            _values[key] = value;
        }
    }

    public void FireEvent()
    {
        FilteredEvent?.Invoke();
    }

    public static DynamicMemberFilterTarget operator +(DynamicMemberFilterTarget left, DynamicMemberFilterTarget right)
    {
        return new DynamicMemberFilterTarget
        {
            Name = left.Name + right.Name,
            Count = left.Count + right.Count,
            Field = left.Field + right.Field
        };
    }
}

public class DynamicStaticMemberFilterTarget
{
    public static event Action? StaticEvent;

    public static void Reset()
    {
        StaticEvent = null;
    }
}

public class DynamicMemberFilterTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
        Marshaler.AllowsDynamicMarshaling = true;
        LuaTypeRegistry.Unregister<DynamicMemberFilterTarget>();
        LuaTypeRegistry.Unregister<DynamicStaticMemberFilterTarget>();
    }

    public override void TearDown()
    {
        LuaTypeRegistry.Unregister<DynamicMemberFilterTarget>();
        LuaTypeRegistry.Unregister<DynamicStaticMemberFilterTarget>();
        DynamicStaticMemberFilterTarget.Reset();
        Marshaler.AllowsDynamicMarshaling = false;

        base.TearDown();
    }

    [Test]
    public void DynamicMemberFilter_Property_Field_Method_And_Event_AreFiltered()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicMemberFilterTarget>(opts =>
        {
            opts.MemberFilter = static member => member.Name is not nameof(DynamicMemberFilterTarget.Name)
                and not nameof(DynamicMemberFilterTarget.Field)
                and not nameof(DynamicMemberFilterTarget.GetValue)
                and not nameof(DynamicMemberFilterTarget.FilteredEvent)
                and not "Item";
        });

        Lua["obj"] = new DynamicMemberFilterTarget();

        // Act
        var nameIsNil = Lua.Evaluate<bool>("return obj.Name == nil");
        var fieldIsNil = Lua.Evaluate<bool>("return obj.Field == nil");
        var methodIsNil = Lua.Evaluate<bool>("return obj.GetValue == nil");
        var eventIsNil = Lua.Evaluate<bool>("return obj.FilteredEvent == nil");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(nameIsNil, Is.True);
            Assert.That(fieldIsNil, Is.True);
            Assert.That(methodIsNil, Is.True);
            Assert.That(eventIsNil, Is.True);
        });
    }

    [Test]
    public void DynamicMemberFilter_Indexer_IsFiltered()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicMemberFilterTarget>(opts =>
        {
            opts.MemberFilter = static member => member.Name != "Item";
        });

        Lua["obj"] = new DynamicMemberFilterTarget();

        // Act
        var valueIsNil = Lua.Evaluate<bool>("return obj['hp'] == nil");

        // Assert
        Assert.That(valueIsNil, Is.True);
    }

    [Test]
    public void DynamicMemberFilter_LengthProperty_IsFiltered()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicMemberFilterTarget>(opts =>
        {
            opts.MemberFilter = static member => member.Name != nameof(DynamicMemberFilterTarget.Count);
        });

        Lua["obj"] = new DynamicMemberFilterTarget();

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Evaluate<int>("return #obj"));
    }

    [Test]
    public void DynamicMemberFilter_Operator_IsFiltered()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicMemberFilterTarget>(opts =>
        {
            opts.MemberFilter = static member => member.Name != "op_Addition";
        });

        Lua["left"] = new DynamicMemberFilterTarget
        {
            Name = "A",
            Count = 1,
            Field = 1
        };
        Lua["right"] = new DynamicMemberFilterTarget
        {
            Name = "B",
            Count = 2,
            Field = 2
        };

        // Act
        var exception = Assert.Throws<LuaException>(() => Lua.Execute("return left + right"));

        // Assert
        Assert.That(exception!.Message, Does.Contain("attempt to perform arithmetic"));
    }

    [Test]
    public void DynamicMemberFilter_Constructor_IsFiltered()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicMemberFilterTarget>(opts =>
        {
            opts.MemberFilter = static member => member is not ConstructorInfo;
        });

        Lua["DynamicMemberFilterTarget"] = typeof(DynamicMemberFilterTarget);

        // Act
        var exception = Assert.Throws<LuaException>(() => Lua.Execute("DynamicMemberFilterTarget('Alex')"));

        // Assert
        Assert.That(exception!.Message, Does.Contain("attempt to call"));
    }

    [Test]
    public void DynamicMemberFilter_StaticEvent_IsFiltered()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicStaticMemberFilterTarget>(opts =>
        {
            opts.MemberFilter = static member => member.Name != nameof(DynamicStaticMemberFilterTarget.StaticEvent);
        });

        Lua["DynamicStaticMemberFilterTarget"] = typeof(DynamicStaticMemberFilterTarget);

        // Act
        var eventIsNil = Lua.Evaluate<bool>("return DynamicStaticMemberFilterTarget.StaticEvent == nil");

        // Assert
        Assert.That(eventIsNil, Is.True);
    }

    [Test]
    public void DynamicMemberFilter_Unregister_ThenReconfigure_FilterResets()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicMemberFilterTarget>(opts =>
        {
            opts.MemberFilter = static member => member.Name is not nameof(DynamicMemberFilterTarget.Name)
                and not "Item";
        });

        Lua["obj"] = new DynamicMemberFilterTarget();
        var originalNameIsNil = Lua.Evaluate<bool>("return obj.Name == nil");
        Assert.That(originalNameIsNil, Is.True);

        // Act
        LuaTypeRegistry.Unregister<DynamicMemberFilterTarget>();
        LuaTypeRegistry.ConfigureDynamic<DynamicMemberFilterTarget>(static _ =>
        {
        });

        Lua["obj2"] = new DynamicMemberFilterTarget();
        var reconfiguredName = Lua.Evaluate<string>("return obj2.Name");

        // Assert
        Assert.That(reconfiguredName, Is.EqualTo("Steve"));
    }
}
