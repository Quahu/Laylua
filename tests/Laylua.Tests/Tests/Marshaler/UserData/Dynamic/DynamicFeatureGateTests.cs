using Laylua.Marshaling;

namespace Laylua.Tests;

public class DynamicGateTarget
{
    public int Value { get; set; } = 42;

    public int Field = 99;

    public static int StaticValue { get; set; } = 10;

    public static int StaticField = 20;

    public int GetValue()
    {
        return Value;
    }

    public static int GetStaticValue()
    {
        return StaticValue;
    }

    public event Action? TestEvent;

    public static event Action? StaticTestEvent;

    public void FireEvent()
    {
        TestEvent?.Invoke();
    }

    public static void FireStaticEvent()
    {
        StaticTestEvent?.Invoke();
    }

    public static void ResetStatic()
    {
        StaticValue = 10;
        StaticField = 20;
        StaticTestEvent = null;
    }
}

public class DynamicGateNoPropertiesTarget
{
    public int Value { get; set; } = 42;

    public int GetValue()
    {
        return Value;
    }
}

public class DynamicGateNoMethodsTarget
{
    public int Value { get; set; } = 42;

    public int GetValue()
    {
        return Value;
    }
}

public class DynamicGateNoFieldsTarget
{
    public int Value { get; set; } = 42;

    public int Field = 99;
}

public class DynamicGateNoEventsTarget
{
    public event Action? TestEvent;

    public int Value { get; set; } = 42;

    public void FireEvent()
    {
        TestEvent?.Invoke();
    }
}

public class DynamicGatePropertiesOnlyTarget
{
    public int Value { get; set; } = 42;

    public int GetValue()
    {
        return Value;
    }

    public event Action? TestEvent;
}

public class DynamicGateMethodsOnlyTarget
{
    public int Value { get; set; } = 42;

    public int GetValue()
    {
        return Value;
    }
}

public class DynamicGateNoConstantsTarget
{
    public const int ConstantValue = 42;

    public static int StaticValue { get; set; } = 7;
}

public class DynamicFeatureGateTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
        Marshaler.AllowsDynamicMarshaling = true;
    }

    public override void TearDown()
    {
        Marshaler.AllowsDynamicMarshaling = false;
        base.TearDown();
    }

    [Test]
    public void DynamicGate_NoProperties_PropertyNotAccessible()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicGateNoPropertiesTarget>(opts =>
        {
            opts.Members = LuaTypeMembers.All & ~LuaTypeMembers.Properties;
        });

        var obj = new DynamicGateNoPropertiesTarget();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj.Value == nil)");
    }

    [Test]
    public void DynamicGate_NoProperties_MethodStillWorks()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicGateNoPropertiesTarget>(opts =>
        {
            opts.Members = LuaTypeMembers.All & ~LuaTypeMembers.Properties;
        });

        var obj = new DynamicGateNoPropertiesTarget();
        Lua["obj"] = obj;

        // Act
        var value = Lua.Evaluate<int>("return obj:GetValue()");

        // Assert
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void DynamicGate_NoMethods_MethodNotAccessible()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicGateNoMethodsTarget>(opts =>
        {
            opts.Members = LuaTypeMembers.All & ~LuaTypeMembers.Methods;
        });

        var obj = new DynamicGateNoMethodsTarget();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj.GetValue == nil)");
    }

    [Test]
    public void DynamicGate_NoMethods_PropertyStillWorks()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicGateNoMethodsTarget>(opts =>
        {
            opts.Members = LuaTypeMembers.All & ~LuaTypeMembers.Methods;
        });

        var obj = new DynamicGateNoMethodsTarget();
        Lua["obj"] = obj;

        // Act
        var value = Lua.Evaluate<int>("return obj.Value");

        // Assert
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void DynamicGate_NoFields_FieldNotAccessible()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicGateNoFieldsTarget>(opts =>
        {
            opts.Members = LuaTypeMembers.All & ~LuaTypeMembers.Fields;
        });

        var obj = new DynamicGateNoFieldsTarget();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj.Field == nil)");
    }

    [Test]
    public void DynamicGate_NoFields_PropertyStillWorks()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicGateNoFieldsTarget>(opts =>
        {
            opts.Members = LuaTypeMembers.All & ~LuaTypeMembers.Fields;
        });

        var obj = new DynamicGateNoFieldsTarget();
        Lua["obj"] = obj;

        // Act
        var value = Lua.Evaluate<int>("return obj.Value");

        // Assert
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void DynamicGate_NoEvents_EventNotAccessible()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicGateNoEventsTarget>(opts =>
        {
            opts.Members = LuaTypeMembers.All & ~LuaTypeMembers.Events;
        });

        var obj = new DynamicGateNoEventsTarget();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj.TestEvent == nil)");
    }

    [Test]
    public void DynamicGate_NoEvents_PropertyStillWorks()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicGateNoEventsTarget>(opts =>
        {
            opts.Members = LuaTypeMembers.All & ~LuaTypeMembers.Events;
        });

        var obj = new DynamicGateNoEventsTarget();
        Lua["obj"] = obj;

        // Act
        var value = Lua.Evaluate<int>("return obj.Value");

        // Assert
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void DynamicGate_PropertiesOnly_OnlyPropertiesAccessible()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicGatePropertiesOnlyTarget>(opts =>
        {
            opts.Members = LuaTypeMembers.Properties;
        });

        var obj = new DynamicGatePropertiesOnlyTarget();
        Lua["obj"] = obj;

        // Act
        var value = Lua.Evaluate<int>("return obj.Value");

        // Assert
        Assert.That(value, Is.EqualTo(42));
        Lua.Execute("assert(obj.GetValue == nil)");
        Lua.Execute("assert(obj.TestEvent == nil)");
    }

    [Test]
    public void DynamicGate_MethodsOnly_OnlyMethodsAccessible()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicGateMethodsOnlyTarget>(opts =>
        {
            opts.Members = LuaTypeMembers.Methods;
        });

        var obj = new DynamicGateMethodsOnlyTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute("assert(obj.Value == nil)");
        var value = Lua.Evaluate<int>("return obj:GetValue()");

        // Assert
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void DynamicGate_None_NothingAccessible()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicGateTarget>(opts =>
        {
            opts.Members = LuaTypeMembers.None;
            opts.Features = LuaTypeFeatures.None;
        });

        var obj = new DynamicGateTarget();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj.Value == nil)");
        Lua.Execute("assert(obj.Field == nil)");
        Lua.Execute("assert(obj.GetValue == nil)");
        Lua.Execute("assert(obj.TestEvent == nil)");
    }

    [Test]
    public void DynamicGate_NoConstants_ConstantNotAccessible()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicGateNoConstantsTarget>(opts =>
        {
            opts.Members = LuaTypeMembers.All & ~LuaTypeMembers.Constants;
        });

        Lua["DynamicGateNoConstantsTarget"] = typeof(DynamicGateNoConstantsTarget);

        // Act & Assert
        Lua.Execute("assert(DynamicGateNoConstantsTarget.ConstantValue == nil)");
    }

    [Test]
    public void DynamicGate_NoConstants_StaticPropertyStillWorks()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicGateNoConstantsTarget>(opts =>
        {
            opts.Members = LuaTypeMembers.All & ~LuaTypeMembers.Constants;
        });

        Lua["DynamicGateNoConstantsTarget"] = typeof(DynamicGateNoConstantsTarget);

        // Act
        var value = Lua.Evaluate<int>("return DynamicGateNoConstantsTarget.StaticValue");

        // Assert
        Assert.That(value, Is.EqualTo(7));
    }
}
