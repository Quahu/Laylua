namespace Laylua.Tests;

public unsafe class TypedUserDataConfigTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
    }

    [Test]
    public void SnakeCaseNaming_ConvertsPropertyNames()
    {
        // Arrange
        var obj = new TestSnakeCaseType
        {
            MaxHealth = 100,
            FirstName = "Test",
            IsActive = true
        };

        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute(@"
            assert(obj.max_health == 100)
            assert(obj.first_name == 'Test')
            assert(obj.is_active == true)
        ");
    }

    [Test]
    public void LowerCaseNaming_ConvertsPropertyNames()
    {
        // Arrange
        var obj = new TestLowerCaseType
        {
            MaxHealth = 200,
            FirstName = "Lower"
        };

        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute(@"
            assert(obj.maxhealth == 200)
            assert(obj.firstname == 'Lower')
        ");
    }

    [Test]
    public void OriginalNaming_PreservesPropertyNames()
    {
        // Arrange
        var obj = new TestOriginalNamingType
        {
            Health = 50,
            Name = "Test"
        };

        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute(@"
            assert(obj.Health == 50)
            assert(obj.Name == 'Test')
        ");
    }

    [Test]
    public void CamelCaseNaming_WithAcronyms_UsesStableWordBoundaries()
    {
        // Arrange
        var obj = new TestCamelCaseAcronymType
        {
            HTTPRequestCount = 4,
            URLValue = 9
        };

        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute(@"
            assert(obj.httpRequestCount == 4)
            assert(obj.urlValue == 9)
            assert(obj.hTTPRequestCount == nil)
        ");
    }

    [Test]
    public void SnakeCaseNaming_WithAcronyms_UsesStableWordBoundaries()
    {
        // Arrange
        var obj = new TestSnakeCaseAcronymType
        {
            HTTPRequestCount = 7,
            URLValue = 3
        };

        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute(@"
            assert(obj.http_request_count == 7)
            assert(obj.url_value == 3)
            assert(obj.h_t_t_p_request_count == nil)
        ");
    }

    [Test]
    public void LuaName_OnProperty_AccessibleByAlias()
    {
        // Arrange
        var obj = new TestLuaNameProperty
        {
            Health = 100,
            PositionX = 3.5
        };

        Lua["obj"] = obj;

        // Act
        var hp = Lua.Evaluate<int>("return obj.hp");
        var posX = Lua.Evaluate<double>("return obj.pos_x");

        Assert.That(hp, Is.EqualTo(100));
        Assert.That(posX, Is.EqualTo(3.5));

        Lua.Execute("obj.hp = 50");

        // Assert
        Assert.That(obj.Health, Is.EqualTo(50));
    }

    [Test]
    public void LuaName_OnProperty_ClrNameNotAccessible()
    {
        // Arrange
        var obj = new TestLuaNameProperty
        {
            Health = 100
        };

        Lua["obj"] = obj;

        // Act
        var healthIsNil = Lua.Evaluate<bool>("return obj.Health == nil");

        // Assert
        Assert.That(healthIsNil, Is.True);
    }

    [Test]
    public void NoToString_FallsBackToDefaultRepresentation()
    {
        // Arrange
        var obj = new TestNoToString
        {
            Value = 42
        };

        Lua["obj"] = obj;

        // Act
        Lua.Execute("result = tostring(obj)");
        lua_getglobal(L, "result");
        var result = lua_tostring(L, -1).ToString()!;
        lua_pop(L);

        // Assert
        Assert.That(result, Does.Not.Contain("NoToString("));
    }

    [Test]
    public void NoDispose_DoesNotCallDispose_OnGc()
    {
        // Arrange
        var obj = new TestNoDispose
        {
            Value = 1
        };

        Lua["obj"] = obj;

        // Act
        Lua.Execute("obj = nil");
        lua_gc(L, LuaGCOperation.Collect);

        // Assert
        Assert.That(obj.WasDisposed, Is.False);
    }

    [Test]
    public void NoFeatures_ToStringReturnsDefaultRepresentation()
    {
        // Arrange
        var obj = new TestNoFeatures
        {
            Value = 42
        };

        Lua["obj"] = obj;

        // Act
        Lua.Execute("result = tostring(obj)");
        lua_getglobal(L, "result");
        var result = lua_tostring(L, -1).ToString()!;
        lua_pop(L);

        // Assert
        Assert.That(result, Does.Not.Contain("NoFeatures("));
    }

    [Test]
    public void Indexer_GetFromLua_ReturnsValue()
    {
        // Arrange
        var obj = new TestWithIndexer();
        obj["hp"] = 100;
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj.hp == 100)");
    }

    [Test]
    public void Indexer_SetFromLua_UpdatesValue()
    {
        // Arrange
        var obj = new TestWithIndexer();
        Lua["obj"] = obj;

        // Act
        Lua.Execute("obj.mana = 50");

        // Assert
        Assert.That(obj["mana"], Is.EqualTo(50));
    }

    [Test]
    public void Indexer_SetFromLua_NumberToEnumDisabled_RaisesLuaError()
    {
        // Arrange
        var obj = new TestEnumIndexerNoNumberConversion();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.That(
            () => Lua.Execute("obj.state = 2"),
            Throws.TypeOf<LuaException>());
    }

    [Test]
    public void Indexer_NamedPropertyTakesPrecedence()
    {
        // Arrange
        var obj = new TestWithIndexer();
        obj["count"] = 999;
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj.Count == 1)");
    }

    [Test]
    public void IndexMembers_PropertiesOnly_MethodsNotAccessible()
    {
        // Arrange
        var obj = new TestIndexPropertiesOnly
        {
            Health = 50
        };

        Lua.Globals["obj"] = obj;

        // Act
        var health = Lua.Evaluate<int>("return obj.Health");
        var getHealthIsNil = Lua.Evaluate<bool>("return obj.GetHealth == nil");

        Assert.That(health, Is.EqualTo(50));

        // Assert
        Assert.That(getHealthIsNil, Is.True);
    }

    [Test]
    public void IndexMembers_MethodsOnly_PropertiesNotAccessible()
    {
        // Arrange
        var obj = new TestIndexMethodsOnly
        {
            Health = 75
        };

        Lua.Globals["obj"] = obj;

        // Act
        var healthIsNil = Lua.Evaluate<bool>("return obj.Health == nil");
        var health = Lua.Evaluate<int>("return obj:GetHealth()");

        Assert.That(healthIsNil, Is.True);

        // Assert
        Assert.That(health, Is.EqualTo(75));
    }

    [Test]
    public void IndexMembers_IndexerOnly_PropertiesAndMethodsNotAccessible()
    {
        // Arrange
        var obj = new TestIndexIndexerOnly();
        Lua.Globals["obj"] = obj;

        // Act
        Lua.Execute("obj['hp'] = 42");
        var hpValue = Lua.Evaluate<int>("return obj['hp']");
        var countValue = Lua.Evaluate<int>("return obj.count");
        var getCountValue = Lua.Evaluate<int>("return obj.getCount");

        Assert.That(hpValue, Is.EqualTo(42));

        Assert.That(countValue, Is.EqualTo(0));
        Assert.That(obj.Count, Is.EqualTo(1));

        // Assert
        Assert.That(getCountValue, Is.EqualTo(0));
    }

    [Test]
    public void IndexPriority_IndexerFirst_IndexerTakesPriority()
    {
        // Arrange
        var obj = new TestIndexerFirst();
        Lua.Globals["obj"] = obj;

        // Act
        Lua.Execute("obj['count'] = 99");
        var countValue = Lua.Evaluate<int>("return obj['count']");

        // Assert
        Assert.That(countValue, Is.EqualTo(99));
        Assert.That(obj.Count, Is.EqualTo(1));
    }

    [Test]
    public void StrictStringIndexer_UnknownNumericRead_RaisesError()
    {
        // Arrange
        Lua["obj"] = new TestStrictStringIndexer();

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("local value = obj[1]"));
    }

    [Test]
    public void StrictStringIndexer_UnknownNumericWrite_RaisesError()
    {
        // Arrange
        Lua["obj"] = new TestStrictStringIndexer();

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj[1] = 5"));
    }

    [Test]
    public void StrictIndexerFirst_UnknownNumericRead_RaisesError()
    {
        // Arrange
        Lua["obj"] = new TestStrictIndexerFirst();

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("local value = obj[1]"));
    }

    [Test]
    public void StrictIndexerFirst_UnknownNumericWrite_RaisesError()
    {
        // Arrange
        Lua["obj"] = new TestStrictIndexerFirst();

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj[1] = 5"));
    }

    [Test]
    public void MultiIndexer_StringKey_GetAndSet()
    {
        // Arrange
        var obj = new TestMultiIndexer();
        Lua.Globals["obj"] = obj;

        // Act
        Lua.Execute("obj['hello'] = 'world'");
        var result = Lua.Evaluate<string>("return obj['hello']");

        // Assert
        Assert.That(result, Is.EqualTo("world"));
    }

    [Test]
    public void MultiIndexer_IntKey_GetAndSet()
    {
        // Arrange
        var obj = new TestMultiIndexer();
        Lua.Globals["obj"] = obj;

        // Act
        Lua.Execute("obj[1] = 'first'");
        var result = Lua.Evaluate<string>("return obj[1]");

        // Assert
        Assert.That(result, Is.EqualTo("first"));
    }

    [Test]
    public void MultiIndexer_MixedKeys_DispatchCorrectly()
    {
        // Arrange
        var obj = new TestMultiIndexer();
        Lua.Globals["obj"] = obj;

        // Act
        Lua.Execute("obj['key'] = 'by_string'; obj[42] = 'by_int'");
        var byString = Lua.Evaluate<string>("return obj['key']");
        var byInt = Lua.Evaluate<string>("return obj[42]");

        // Assert
        Assert.That(byString, Is.EqualTo("by_string"));
        Assert.That(byInt, Is.EqualTo("by_int"));
    }

    [Test]
    public void StrictType_UnknownProperty_RaisesError()
    {
        // Arrange
        var obj = new TestStrictType
        {
            Value = 5
        };

        Lua["obj"] = obj;

        // Act
        Lua.Execute("assert(obj.Value == 5)");

        // Assert
        Assert.Throws<LuaException>(() => Lua.Execute("local x = obj.nonexistent"));
    }

    [Test]
    public void StrictType_UnknownNewIndex_RaisesError()
    {
        // Arrange
        var obj = new TestStrictType
        {
            Value = 5
        };

        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("obj.nonexistent = 10"));
    }

    [Test]
    public void OverloadResolution_BestMatch_SelectsMostSpecificOverload()
    {
        // Arrange
        var obj = new TestOverloadResolutionBestMatch();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute(@"
            assert(obj:Resolve(7) == 'int:7')
            assert(obj:Resolve(3.5) == 'double:3.5')
            assert(obj:Resolve('hello') == 'string:hello')
        ");
    }

    [Test]
    public void OverloadResolution_Error_AmbiguousCallRaisesLuaError()
    {
        // Arrange
        var obj = new TestOverloadResolutionError();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("return obj:Resolve(7)"));
    }

    [Test]
    public void OverloadResolution_Error_NonAmbiguousCallSucceeds()
    {
        // Arrange
        var obj = new TestOverloadResolutionError();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj:Resolve('ok') == 'string:ok')");
    }

    [Test]
    public void OverloadResolution_Error_StaticOverload_AmbiguousCallRaisesLuaError()
    {
        // Arrange
        Lua["TestOverloadResolutionError"] = typeof(TestOverloadResolutionError);

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("return TestOverloadResolutionError.ResolveStatic(7)"));
    }

    [Test]
    public void OverloadResolution_Default_InheritsAssemblyBestMatch()
    {
        // Arrange
        var obj = new TestOverloadResolutionDefault();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute("assert(obj:Resolve(9) == 'int:9')");
    }

    [Test]
    public void PairsFeature_Disabled_ErrorsOnPairs()
    {
        // Arrange
        var obj = new TestPairsNone
        {
            X = 1,
            Y = 2
        };

        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("for k, v in pairs(obj) do end"));
    }

    [Test]
    public void PairsBehavior_IfEnumerable_NonEnumerable_ErrorsOnPairs()
    {
        // Arrange
        var obj = new TestPairsIfEnumerable
        {
            Length = 5
        };

        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Execute("for k, v in pairs(obj) do end"));
    }

    [Test]
    public void PairsBehavior_IfEnumerable_WithImpl_Enumerates()
    {
        // Arrange
        var obj = new TestPairsIfEnumerableList();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            result = {}
            for i, v in pairs(obj) do
                result[i] = v
            end
        ");

        var result1 = Lua.Evaluate<int>("return result[1]");
        var result2 = Lua.Evaluate<int>("return result[2]");
        var result3 = Lua.Evaluate<int>("return result[3]");

        // Assert
        Assert.That(result1, Is.EqualTo(10));
        Assert.That(result2, Is.EqualTo(20));
        Assert.That(result3, Is.EqualTo(30));
    }

    [Test]
    public void PairsBehavior_IfEnumerable_DisposesEnumeratorOnCompletion()
    {
        // Arrange
        var obj = new TestPairsDisposableEnumerable();
        Lua["obj"] = obj;

        // Act
        Lua.Execute(@"
            for i, v in pairs(obj) do
            end
        ");

        // Assert
        Assert.That(obj.EnumeratorDisposeCount, Is.EqualTo(1));
    }

    [Test]
    public void Pairs_IteratesInstanceProperties()
    {
        // Arrange
        var player = new TestPlayer
        {
            Name = "Steve",
            Health = 100,
            Speed = 5.5,
            IsAlive = true
        };

        Lua["player"] = player;

        // Act
        Lua.Execute(@"
            keys = {}
            for k, v in pairs(player) do
                keys[k] = v
            end
        ");

        // Assert
        Lua.Execute(@"
            assert(keys.Name == 'Steve')
            assert(keys.Health == 100)
        ");
    }

    [Test]
    public void Pairs_IgnoredProperties_NotIterated()
    {
        // Arrange
        var player = new TestPlayer
        {
            InternalId = 42,
            Name = "Steve"
        };

        Lua["player"] = player;

        // Act
        Lua.Execute(@"
            count = 0
            for k, v in pairs(player) do
                count = count + 1
                assert(k ~= 'InternalId', 'ignored property appeared in pairs')
            end
        ");

        // Assert
        Lua.Execute("assert(count > 0)");
    }

    [Test]
    public void PushDefinition_ToString_ReturnsStableTypeName()
    {
        // Arrange
        Lua["TestEventStatic"] = typeof(TestEventStatic);

        // Act
        var result = Lua.Evaluate<string>("return tostring(TestEventStatic)");

        // Assert
        Assert.That(result, Is.EqualTo("TestEventStatic"));
    }

    [Test]
    public void Len_CountProperty_ReturnsCount()
    {
        // Arrange
        var obj = new TestWithCount
        {
            Name = "test"
        };

        obj.Add("a");
        obj.Add("b");
        obj.Add("c");
        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<long>("return #obj");

        // Assert
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Len_LengthProperty_ReturnsLength()
    {
        // Arrange
        var obj = new TestWithLength
        {
            Data = [1, 2, 3, 4, 5]
        };

        Lua["obj"] = obj;

        // Act
        var result = Lua.Evaluate<long>("return #obj");

        // Assert
        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void Len_Disabled_HashOperatorFails()
    {
        // Arrange
        var obj = new TestLenDisabled();
        Lua["obj"] = obj;

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Evaluate("return #obj"));
    }

    [Test]
    public void OverloadResolution_BestMatch_ReversedDeclaration_SelectsMostSpecificOverload()
    {
        // Arrange
        var obj = new TestOverloadResolutionBestMatchReversed();
        Lua["obj"] = obj;

        // Act & Assert
        Lua.Execute(@"
            assert(obj:Resolve(7) == 'int:7')
            assert(obj:Resolve(3.5) == 'double:3.5')
        ");
    }

    [Test]
    public void MultiIndexer_UserDataKeys_DispatchCorrectly()
    {
        // Arrange
        var obj = new TestUserDataMultiIndexer();
        var dog = new TestDog
        {
            Name = "Barker"
        };
        var cat = new TestCat
        {
            Name = "Churu"
        };

        Lua.Globals["obj"] = obj;
        Lua.Globals["dog"] = dog;
        Lua.Globals["cat"] = cat;

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
}
