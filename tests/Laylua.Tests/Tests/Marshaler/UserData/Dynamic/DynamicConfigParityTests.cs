using Laylua.Marshaling;

namespace Laylua.Tests;

public class DynamicLowerCaseNamingTarget
{
    public int MaxHealth { get; set; }

    public string FirstName { get; set; } = "";
}

public class DynamicOriginalNamingTarget
{
    public int Health { get; set; }

    public string Name { get; set; } = "";
}

public class DynamicCamelCaseAcronymTarget
{
    public int HTTPRequestCount { get; set; }

    public int URLValue { get; set; }
}

public class DynamicSnakeCaseAcronymTarget
{
    public int HTTPRequestCount { get; set; }

    public int URLValue { get; set; }
}

public class DynamicLuaNameAliasTarget
{
    [LuaName("hp")]
    public int Health { get; set; }
}

public class DynamicDisabledToStringTarget
{
    public int Value { get; set; }

    public override string ToString()
    {
        return $"DynamicDisabledToString({Value})";
    }
}

public class DynamicDisabledDisposeTarget : IDisposable
{
    public bool WasDisposed { get; private set; }

    public void Dispose()
    {
        WasDisposed = true;
    }
}

public class DynamicNoFeaturesTarget
{
    public int Value { get; set; }

    public override string ToString()
    {
        return $"DynamicNoFeatures({Value})";
    }
}

public class DynamicIndexMembersTarget
{
    private readonly Dictionary<string, int> _data = new();

    public int Count { get; set; } = 1;

    public int Health { get; set; }

    public int GetHealth()
    {
        return Health;
    }

    public int GetCount()
    {
        return Count;
    }

    public int this[string key]
    {
        get
        {
            return _data.TryGetValue(key, out var value) ? value : 0;
        }
        set
        {
            _data[key] = value;
        }
    }
}

public class DynamicStrictIndexerFirstTarget
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

public class DynamicEnumerableBehaviorTarget
{
    public int Length { get; set; }
}

public class DynamicIgnoredPairsTarget
{
    public int InternalId { get; set; }

    public string Name { get; set; } = "";
}

public class DynamicLenDisabledTarget
{
    public int Length { get; set; } = 5;
}

public unsafe class DynamicConfigParityTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
        Marshaler.AllowsDynamicMarshaling = true;
        LuaTypeRegistry.Unregister<DynamicLowerCaseNamingTarget>();
        LuaTypeRegistry.Unregister<DynamicOriginalNamingTarget>();
        LuaTypeRegistry.Unregister<DynamicCamelCaseAcronymTarget>();
        LuaTypeRegistry.Unregister<DynamicSnakeCaseAcronymTarget>();
        LuaTypeRegistry.Unregister<DynamicLuaNameAliasTarget>();
        LuaTypeRegistry.Unregister<DynamicDisabledToStringTarget>();
        LuaTypeRegistry.Unregister<DynamicDisabledDisposeTarget>();
        LuaTypeRegistry.Unregister<DynamicNoFeaturesTarget>();
        LuaTypeRegistry.Unregister<DynamicIndexMembersTarget>();
        LuaTypeRegistry.Unregister<DynamicStrictIndexerFirstTarget>();
        LuaTypeRegistry.Unregister<DynamicEnumerableBehaviorTarget>();
        LuaTypeRegistry.Unregister<DynamicIgnoredPairsTarget>();
        LuaTypeRegistry.Unregister<DynamicLenDisabledTarget>();
    }

    public override void TearDown()
    {
        Marshaler.AllowsDynamicMarshaling = false;
        base.TearDown();
    }

    [Test]
    public void DynamicConfig_LowerCase_NamingPolicy()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicLowerCaseNamingTarget>(opts =>
        {
            opts.NamingPolicy = LuaNamingPolicy.LowerCase;
        });

        Lua["obj"] = new DynamicLowerCaseNamingTarget
        {
            MaxHealth = 200,
            FirstName = "Lower"
        };

        // Act & Assert
        Lua.Execute(@"
            assert(obj.maxhealth == 200)
            assert(obj.firstname == 'Lower')
            assert(obj.MaxHealth == nil)
        ");
    }

    [Test]
    public void DynamicConfig_OriginalNaming_PreservesPropertyNames()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicOriginalNamingTarget>(opts =>
        {
            opts.NamingPolicy = LuaNamingPolicy.Original;
        });

        Lua["obj"] = new DynamicOriginalNamingTarget
        {
            Health = 50,
            Name = "Test"
        };

        // Act & Assert
        Lua.Execute(@"
            assert(obj.Health == 50)
            assert(obj.Name == 'Test')
            assert(obj.health == nil)
        ");
    }

    [Test]
    public void DynamicConfig_CamelCase_WithAcronyms_UsesStableWordBoundaries()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicCamelCaseAcronymTarget>(opts =>
        {
            opts.NamingPolicy = LuaNamingPolicy.CamelCase;
        });

        Lua["obj"] = new DynamicCamelCaseAcronymTarget
        {
            HTTPRequestCount = 4,
            URLValue = 9
        };

        // Act & Assert
        Lua.Execute(@"
            assert(obj.httpRequestCount == 4)
            assert(obj.urlValue == 9)
            assert(obj.hTTPRequestCount == nil)
        ");
    }

    [Test]
    public void DynamicConfig_LowerSnakeCase_WithAcronyms_UsesStableWordBoundaries()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicSnakeCaseAcronymTarget>(opts =>
        {
            opts.NamingPolicy = LuaNamingPolicy.LowerSnakeCase;
        });

        Lua["obj"] = new DynamicSnakeCaseAcronymTarget
        {
            HTTPRequestCount = 7,
            URLValue = 3
        };

        // Act & Assert
        Lua.Execute(@"
            assert(obj.http_request_count == 7)
            assert(obj.url_value == 3)
            assert(obj.h_t_t_p_request_count == nil)
        ");
    }

    [Test]
    public void Dynamic_UnknownPropertyBehavior_Error_OnWrite()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicOriginalNamingTarget>(opts =>
        {
            opts.UnknownPropertyBehavior = LuaUnknownPropertyBehavior.Error;
        });

        Lua["obj"] = new DynamicOriginalNamingTarget();

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj.Unknown = 5"));
    }

    [Test]
    public void DynamicConfig_LuaName_ClrNameNotAccessible()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicLuaNameAliasTarget>(static _ => { });

        Lua["obj"] = new DynamicLuaNameAliasTarget
        {
            Health = 100
        };

        // Act
        var originalNameIsNil = Lua.Evaluate<bool>("return obj.Health == nil");

        // Assert
        Assert.That(originalNameIsNil, Is.True);
    }

    [Test]
    public void DynamicConfig_ToStringDisabled_FallsBackToDefaultRepresentation()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicDisabledToStringTarget>(opts =>
        {
            opts.Features &= ~LuaTypeFeatures.ToString;
        });

        Lua["obj"] = new DynamicDisabledToStringTarget
        {
            Value = 42
        };

        // Act
        Lua.Execute("result = tostring(obj)");
        var result = GetLuaString("result");

        // Assert
        Assert.That(result, Does.Not.Contain("DynamicDisabledToString("));
    }

    [Test]
    public void DynamicConfig_DisposeDisabled_DoesNotCallDispose_OnGc()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicDisabledDisposeTarget>(opts =>
        {
            opts.Features &= ~LuaTypeFeatures.Dispose;
        });

        var obj = new DynamicDisabledDisposeTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute("obj = nil");
        lua_gc(L, LuaGCOperation.Collect);

        // Assert
        Assert.That(obj.WasDisposed, Is.False);
    }

    [Test]
    public void DynamicConfig_NoFeatures_ToStringReturnsDefaultRepresentation()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicNoFeaturesTarget>(opts =>
        {
            opts.Features = LuaTypeFeatures.None;
        });

        Lua["obj"] = new DynamicNoFeaturesTarget
        {
            Value = 42
        };

        // Act
        Lua.Execute("result = tostring(obj)");
        var result = GetLuaString("result");

        // Assert
        Assert.That(result, Does.Not.Contain("DynamicNoFeatures("));
    }

    [Test]
    public void Dynamic_IndexMembers_PropertiesOnly_MethodsNotAccessible()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicIndexMembersTarget>(opts =>
        {
            opts.IndexMembers = LuaIndexMembers.Properties;
        });

        Lua["obj"] = new DynamicIndexMembersTarget
        {
            Health = 50
        };

        // Act
        var health = Lua.Evaluate<int>("return obj.Health");
        var getHealthIsNil = Lua.Evaluate<bool>("return obj.GetHealth == nil");

        // Assert
        Assert.That(health, Is.EqualTo(50));
        Assert.That(getHealthIsNil, Is.True);
    }

    [Test]
    public void Dynamic_IndexMembers_MethodsOnly_PropertiesNotAccessible()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicIndexMembersTarget>(opts =>
        {
            opts.IndexMembers = LuaIndexMembers.Methods;
        });

        Lua["obj"] = new DynamicIndexMembersTarget
        {
            Health = 75
        };

        // Act
        var healthIsNil = Lua.Evaluate<bool>("return obj.Health == nil");
        var health = Lua.Evaluate<int>("return obj:GetHealth()");

        // Assert
        Assert.That(healthIsNil, Is.True);
        Assert.That(health, Is.EqualTo(75));
    }

    [Test]
    public void Dynamic_IndexMembers_IndexerOnly_PropertiesAndMethodsNotAccessible()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicIndexMembersTarget>(opts =>
        {
            opts.IndexMembers = LuaIndexMembers.Indexer;
        });

        var obj = new DynamicIndexMembersTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute("obj.hp = 42");
        var hpValue = Lua.Evaluate<int>("return obj.hp");
        var countValue = Lua.Evaluate<int>("return obj.Count");
        var getCountValue = Lua.Evaluate<int>("return obj.GetCount");

        // Assert
        Assert.That(hpValue, Is.EqualTo(42));
        Assert.That(countValue, Is.EqualTo(0));
        Assert.That(getCountValue, Is.EqualTo(0));
        Assert.That(obj.Count, Is.EqualTo(1));
    }

    [Test]
    public void Dynamic_IndexPriority_IndexerFirst_IndexerTakesPriority()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicIndexMembersTarget>(opts =>
        {
            opts.IndexPriority = LuaIndexPriority.IndexerFirst;
        });

        var obj = new DynamicIndexMembersTarget();
        Lua["obj"] = obj;

        // Act
        Lua.Execute("obj.Count = 99");
        var countValue = Lua.Evaluate<int>("return obj.Count");

        // Assert
        Assert.That(countValue, Is.EqualTo(99));
        Assert.That(obj.Count, Is.EqualTo(1));
    }

    [Test]
    public void Dynamic_StrictIndexerFirst_UnknownNumericRead_RaisesError()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicStrictIndexerFirstTarget>(opts =>
        {
            opts.IndexPriority = LuaIndexPriority.IndexerFirst;
            opts.UnknownPropertyBehavior = LuaUnknownPropertyBehavior.Error;
            opts.BindingConversions = LuaAllowedValueConversions.Default & ~LuaAllowedValueConversions.PrimitiveToString;
        });

        Lua["obj"] = new DynamicStrictIndexerFirstTarget();

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("local value = obj[1]"));
    }

    [Test]
    public void Dynamic_StrictIndexerFirst_UnknownNumericWrite_RaisesError()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicStrictIndexerFirstTarget>(opts =>
        {
            opts.IndexPriority = LuaIndexPriority.IndexerFirst;
            opts.UnknownPropertyBehavior = LuaUnknownPropertyBehavior.Error;
            opts.BindingConversions = LuaAllowedValueConversions.Default & ~LuaAllowedValueConversions.PrimitiveToString;
        });

        Lua["obj"] = new DynamicStrictIndexerFirstTarget();

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj[1] = 5"));
    }

    [Test]
    public void Dynamic_PairsBehavior_Enumerable_NonEnumerable_ErrorsOnPairs()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicEnumerableBehaviorTarget>(opts =>
        {
            opts.PairsBehavior = LuaPairsBehavior.Enumerable;
        });

        Lua["obj"] = new DynamicEnumerableBehaviorTarget
        {
            Length = 5
        };

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("for k, v in pairs(obj) do end"));
    }

    [Test]
    public void Dynamic_Pairs_Properties_IgnoredMembers_NotIterated()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicIgnoredPairsTarget>(opts =>
        {
            opts.PairsBehavior = LuaPairsBehavior.Properties;
            opts.Ignore("InternalId");
        });

        Lua["obj"] = new DynamicIgnoredPairsTarget
        {
            InternalId = 42,
            Name = "Steve"
        };

        // Act
        Lua.Execute(@"
            count = 0
            for k, v in pairs(obj) do
                count = count + 1
                assert(k ~= 'InternalId', 'ignored property appeared in pairs')
            end
        ");

        // Assert
        Lua.Execute("assert(count > 0)");
    }

    [Test]
    public void DynamicLen_Disabled_HashOperatorFails()
    {
        // Arrange
        LuaTypeRegistry.ConfigureDynamic<DynamicLenDisabledTarget>(opts =>
        {
            opts.Features &= ~LuaTypeFeatures.Length;
        });

        Lua["obj"] = new DynamicLenDisabledTarget();

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("local _ = #obj"));
    }

    private string GetLuaString(string name)
    {
        lua_getglobal(L, name);
        var result = lua_tostring(L, -1).ToString()!;
        lua_pop(L);
        return result;
    }
}
