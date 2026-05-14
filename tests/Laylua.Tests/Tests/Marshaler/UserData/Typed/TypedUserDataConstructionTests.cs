using Laylua.Marshaling;

namespace Laylua.Tests;

public unsafe class TypedUserDataConstructionTests : LuaTestBase
{
    public override void SetUp()
    {
        base.SetUp();
        Lua.OpenStandardLibraries();
    }

    [Test]
    public void Constructor_ParameterlessViaCall_CreatesInstance()
    {
        // Arrange
        Lua["TestConstructable"] = typeof(TestConstructable);

        // Act
        Lua.Execute("obj = TestConstructable()");

        // Assert
        var obj = Lua.GetGlobal<TestConstructable>("obj");
        Assert.That(obj, Is.Not.Null);
        Assert.That(obj.Name, Is.EqualTo("default"));
    }

    [Test]
    public void Constructor_WithParametersViaCall_CreatesInstanceWithValues()
    {
        // Arrange
        Lua["TestConstructable"] = typeof(TestConstructable);

        // Act
        Lua.Execute("obj = TestConstructable('hello', 42)");

        // Assert
        var obj = Lua.GetGlobal<TestConstructable>("obj");
        Assert.That(obj, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(obj.Name, Is.EqualTo("hello"));
            Assert.That(obj.Value, Is.EqualTo(42));
        });
    }

    [Test]
    public void Constructor_ResultIsFullUserdata_SupportsPropertyAccess()
    {
        // Arrange
        Lua["TestConstructable"] = typeof(TestConstructable);

        // Act
        Lua.Execute(@"
            obj = TestConstructable()
            obj.Name = 'constructed'
            obj.Value = 99
        ");

        // Assert
        var obj = Lua.GetGlobal<TestConstructable>("obj");
        Assert.Multiple(() =>
        {
            Assert.That(obj.Name, Is.EqualTo("constructed"));
            Assert.That(obj.Value, Is.EqualTo(99));
        });
    }

    [Test]
    public void Construction_Factory_DefaultName()
    {
        // Arrange
        Lua["TestFactoryDefault"] = typeof(TestFactoryDefault);

        // Act
        var result = Lua.Evaluate<TestFactoryDefault>("return TestFactoryDefault.new('hello', 42)");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("hello"));
        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void Construction_Factory_DirectCallDisabled()
    {
        // Arrange
        Lua["TestFactoryDefault"] = typeof(TestFactoryDefault);

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Evaluate("return TestFactoryDefault('hello', 42)"));
    }

    [Test]
    public void Construction_Factory_CustomName()
    {
        // Arrange
        Lua["TestFactoryCustomName"] = typeof(TestFactoryCustomName);

        // Act
        var result = Lua.Evaluate<TestFactoryCustomName>("return TestFactoryCustomName.create('world')");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Label, Is.EqualTo("world"));
    }

    [Test]
    public void Construction_None_DirectCallDisabled()
    {
        // Arrange
        Lua["TestNoConstruction"] = typeof(TestNoConstruction);

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Evaluate("return TestNoConstruction()"));
    }

    [Test]
    public void Construction_None_StaticMembersStillAccessible()
    {
        // Arrange
        Lua["TestNoConstruction"] = typeof(TestNoConstruction);
        TestNoConstruction.Counter = 0;

        // Act
        Lua.Execute("TestNoConstruction.Counter = 7");
        Assert.That(TestNoConstruction.Counter, Is.EqualTo(7));

        // Assert
        var result = Lua.Evaluate<long>("return TestNoConstruction.Counter");
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void PushDefinition_ExposesStaticProperties()
    {
        // Arrange
        TestWithStatics.LuaUserDataHandler.PushDefinition(L);
        lua_setglobal(L, "TestWithStatics");

        // Act & Assert
        Lua.Execute(@"
            assert(TestWithStatics.MaxInstances == 100)
            assert(TestWithStatics.TypeName == 'TestWithStatics')
        ");
    }

    [Test]
    public void PushDefinition_ExposesStaticMethods()
    {
        // Arrange
        TestWithStatics.LuaUserDataHandler.PushDefinition(L);
        lua_setglobal(L, "TestWithStatics");

        // Act & Assert
        Lua.Execute("assert(TestWithStatics.Add(2, 3) == 5)");
    }

    [Test]
    public void PushDefinition_ViaMarshalerTypeOf_WorksAutomatically()
    {
        // Arrange
        Lua["TestWithStatics"] = typeof(TestWithStatics);

        // Act & Assert
        Lua.Execute("assert(TestWithStatics.MaxInstances == 100)");
    }

    [Test]
    public void TypeOf_StaticProperties_Accessible()
    {
        // Arrange
        Lua["TestWithStatics"] = typeof(TestWithStatics);

        // Act & Assert
        Lua.Execute(@"
            assert(TestWithStatics.MaxInstances == 100)
            assert(TestWithStatics.TypeName == 'TestWithStatics')
        ");
    }

    [Test]
    public void TypeOf_StaticMethods_Callable()
    {
        // Arrange
        Lua["TestWithStatics"] = typeof(TestWithStatics);

        // Act & Assert
        Lua.Execute("assert(TestWithStatics.Add(2, 3) == 5)");
    }

    [Test]
    public void GeneratedTableToPoco_ConstructableType_Materializes()
    {
        // Arrange
        Lua.Execute("t = { Name = 'from_table', Value = 42 }");

        // Act
        using var _ = Lua.Stack.SnapshotCount();
        lua_getglobal(L, "t");
        var result = Lua.Stack[-1].GetValue<TestConstructable>(LuaAllowedValueConversions.TableToObject);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Name, Is.EqualTo("from_table"));
            Assert.That(result.Value, Is.EqualTo(42));
        });
    }

    [Test]
    public void GeneratedTableToStruct_Materializes()
    {
        // Arrange
        Lua.Execute("t = { X = 1.5, Y = 2.5 }");

        // Act
        using var _ = Lua.Stack.SnapshotCount();
        lua_getglobal(L, "t");
        var result = Lua.Stack[-1].GetValue<TestPoint>(LuaAllowedValueConversions.TableToObject);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.X, Is.EqualTo(1.5));
            Assert.That(result.Y, Is.EqualTo(2.5));
        });
    }

    [Test]
    public void GeneratedTableToPoco_InitOnlyRequiredMember_Materializes()
    {
        // Act
        var result = Lua.Evaluate<TestTableRequiredInitTarget>("return { Name = 'from_table', Health = 15 }");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Name, Is.EqualTo("from_table"));
            Assert.That(result.Health, Is.EqualTo(15));
        });
    }

    [Test]
    public void GeneratedTableToPoco_RequiredSetMember_Materializes()
    {
        // Act
        var result = Lua.Evaluate<TestTableRequiredSetTarget>("return { Name = 'from_table', Health = 15 }");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Name, Is.EqualTo("from_table"));
            Assert.That(result.Health, Is.EqualTo(15));
        });
    }

    [Test]
    public void GeneratedTableToPoco_MissingRequiredMember_Throws()
    {
        // Act & Assert
        var exception = Assert.Throws<LuaConversionException>(() =>
            Lua.Evaluate<TestTableRequiredInitTarget>("return { Health = 15 }"));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Error.Message, Does.Contain("missing required member 'Name'"));
            Assert.That(exception.Error.Path, Is.EqualTo("$.Name"));
            Assert.That(exception.Error.TargetType, Is.EqualTo(typeof(TestTableRequiredInitTarget)));
        });
    }

    [Test]
    public void GeneratedTableToPoco_NilRequiredMember_Throws()
    {
        // Act & Assert
        var exception = Assert.Throws<LuaConversionException>(() =>
            Lua.Evaluate<TestTableRequiredInitTarget>("return { Name = nil, Health = 15 }"));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Error.Message, Does.Contain("missing required member 'Name'"));
            Assert.That(exception.Error.Path, Is.EqualTo("$.Name"));
            Assert.That(exception.Error.TargetType, Is.EqualTo(typeof(TestTableRequiredInitTarget)));
        });
    }

    [Test]
    public void GeneratedTableToStruct_RequiredInitMember_Materializes()
    {
        // Act
        var result = Lua.Evaluate<TestTableRequiredInitStruct>("return { Value = 42, Health = 15 }");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(42));
            Assert.That(result.Health, Is.EqualTo(15));
        });
    }

    [Test]
    public void GeneratedTableToPoco_InheritedRequiredInitMember_Materializes()
    {
        // Act
        var result = Lua.Evaluate<TestTableInheritedRequiredInitDerived>("return { Name = 'base', BaseHealth = 10, DerivedHealth = 20 }");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Name, Is.EqualTo("base"));
            Assert.That(result.BaseHealth, Is.EqualTo(10));
            Assert.That(result.DerivedHealth, Is.EqualTo(20));
        });
    }

    [Test]
    public void GeneratedTableToPoco_StrictNumericKey_FailsWithoutCorruptingStack()
    {
        // Arrange
        Lua.Execute("t = { 1, Value = 10 }");

        // Act
        using var _ = Lua.Stack.SnapshotCount();
        lua_getglobal(L, "t");
        var result = Lua.Stack[-1].TryGetValue(LuaAllowedValueConversions.TableToObject, out TestStrictType? value);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(value, Is.Null);
            Assert.That(lua_gettop(L), Is.EqualTo(1));
        });
    }

    [Test]
    public void GeneratedTableToPoco_MissingRequiredMember_ReturnsConversionError()
    {
        // Arrange
        Lua.Execute("t = { Health = 15 }");

        // Act
        using var _ = Lua.Stack.SnapshotCount();
        lua_getglobal(L, "t");
        var result = Lua.Stack[-1].TryGetValue(LuaAllowedValueConversions.TableToObject, out TestTableRequiredInitTarget? value, out var error);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(value, Is.Null);
            Assert.That(error, Is.Not.Null);
            Assert.That(error!.Message, Does.Contain("missing required member 'Name'"));
            Assert.That(error.Path, Is.EqualTo("$.Name"));
            Assert.That(error.TargetType, Is.EqualTo(typeof(TestTableRequiredInitTarget)));
            Assert.That(error.MemberName, Is.EqualTo("Name"));
            Assert.That(error.InnerError, Is.Null);
        });
    }

    [Test]
    public void GeneratedTableToPoco_StrictUnknownStringKey_ReturnsConversionError()
    {
        // Arrange
        Lua.Execute("t = { Value = 10, Extra = 20 }");

        // Act
        using var _ = Lua.Stack.SnapshotCount();
        lua_getglobal(L, "t");
        var result = Lua.Stack[-1].TryGetValue(LuaAllowedValueConversions.TableToObject, out TestStrictType? value, out var error);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(value, Is.Null);
            Assert.That(error, Is.Not.Null);
            Assert.That(error!.Message, Does.Contain("unknown member 'Extra'"));
            Assert.That(error.Path, Is.EqualTo("$.Extra"));
            Assert.That(error.TargetType, Is.EqualTo(typeof(TestStrictType)));
            Assert.That(error.MemberName, Is.EqualTo("Extra"));
        });
    }

    [Test]
    public void GeneratedTableToPoco_StrictUnexpectedNonStringKey_ReturnsConversionError()
    {
        // Arrange
        Lua.Execute("t = { Value = 10, [1] = 20 }");

        // Act
        using var _ = Lua.Stack.SnapshotCount();
        lua_getglobal(L, "t");
        var result = Lua.Stack[-1].TryGetValue(LuaAllowedValueConversions.TableToObject, out TestStrictType? value, out var error);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(value, Is.Null);
            Assert.That(error, Is.Not.Null);
            Assert.That(error!.Message, Does.Contain("table keys must be strings"));
            Assert.That(error.Message, Does.Not.Contain("failed to convert"));
            Assert.That(error.Path, Is.EqualTo("$"));
            Assert.That(error.TargetType, Is.EqualTo(typeof(TestStrictType)));
            Assert.That(error.LuaType, Is.EqualTo(LuaType.Number));
        });
    }

    [Test]
    public void GeneratedTableToPoco_NestedMemberConversionFailure_ReturnsConversionErrorPath()
    {
        // Arrange
        Lua.Execute("t = { Content = 'Foo', Embed = { Description = { } } }");

        // Act
        using var _ = Lua.Stack.SnapshotCount();
        lua_getglobal(L, "t");
        var result = Lua.Stack[-1].TryGetValue(LuaAllowedValueConversions.TableToObject, out TestTableNestedMessageTarget? value, out var error);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(value, Is.Null);
            Assert.That(error, Is.Not.Null);
            Assert.That(error!.Message, Does.Contain("failed to convert member 'Embed'"));
            Assert.That(error.Path, Is.EqualTo("$.Embed"));
            Assert.That(error.MemberName, Is.EqualTo("Embed"));
            Assert.That(error.InnerError, Is.Not.Null);
            Assert.That(error.InnerError!.Path, Is.EqualTo("$.Embed.Description"));
            Assert.That(error.InnerError.Message, Does.Contain("failed to convert member 'Description'"));
        });
    }

    [Test]
    public void GeneratedTableToStruct_ExplicitParameterlessConstructor_UsesInitializedValue()
    {
        // Act
        var result = Lua.Evaluate<TestExplicitParameterlessStructConstructor>("return { }");

        // Assert
        Assert.That(result.Value, Is.EqualTo(123));
    }

    [Test]
    public void GeneratedTableToPoco_WithoutParameterlessConstruction_Fails()
    {
        // Arrange
        Lua.Execute("t = { Name = 'constructor' }");

        // Act
        using var _ = Lua.Stack.SnapshotCount();
        lua_getglobal(L, "t");
        var result = Lua.Stack[-1].TryGetValue(LuaAllowedValueConversions.TableToObject, out TestTableNoParameterlessTarget? value);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(value, Is.Null);
        });
    }

    [Test]
    public void GeneratedTableToPoco_DisabledFromTableConversion_Fails()
    {
        // Arrange
        Lua.Execute("t = { Name = 'disabled' }");

        // Act
        using var _ = Lua.Stack.SnapshotCount();
        lua_getglobal(L, "t");
        var result = Lua.Stack[-1].TryGetValue(LuaAllowedValueConversions.TableToObject, out TestTableDisabledTarget? value);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(value, Is.Null);
        });
    }

    [Test]
    public void GeneratedTableToPoco_BeforeAfterHooks_RunAroundBuiltInConversion()
    {
        // Act
        var result = Lua.Evaluate<TestTableHookTarget>("return { Name = 'hooked' }");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Name, Is.EqualTo("hooked!"));
            Assert.That(result.Health, Is.EqualTo(25));
        });
    }

    [Test]
    public void GeneratedTableToPoco_InterfaceOverride_CanReuseBuiltInHelper()
    {
        // Act
        var result = Lua.Evaluate<TestTableOverrideTarget>("return { Name = 'override', Health = 12 }");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Name, Is.EqualTo("override"));
            Assert.That(result.Health, Is.EqualTo(17));
            Assert.That(result.OverrideUsed, Is.True);
        });
    }

    [Test]
    public void GeneratedTableToPoco_ExplicitInterfaceOverride_IsUsed()
    {
        // Act
        var result = Lua.Evaluate<TestTableExplicitInterfaceTarget>("return { Name = 'explicit' }");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Name, Is.EqualTo("explicit"));
            Assert.That(result.ExplicitInterfaceUsed, Is.True);
        });
    }

    [Test]
    public void GeneratedTableToPoco_ExplicitInterfaceOverride_PropagatesCustomError()
    {
        // Arrange
        Lua.Execute("t = { Name = 'wrong' }");

        // Act
        using var _ = Lua.Stack.SnapshotCount();
        lua_getglobal(L, "t");
        var result = Lua.Stack[-1].TryGetValue(LuaAllowedValueConversions.TableToObject, out TestTableInterfaceErrorTarget? value, out var error);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(value, Is.Null);
            Assert.That(error, Is.Not.Null);
            Assert.That(error!.Message, Does.Contain("only accepts Name = 'ok'"));
            Assert.That(error.Message, Does.Not.Contain("failed to convert"));
            Assert.That(error.Path, Is.EqualTo("$.Name"));
            Assert.That(error.TargetType, Is.EqualTo(typeof(TestTableInterfaceErrorTarget)));
            Assert.That(error.MemberName, Is.EqualTo("Name"));
        });
    }

    [Test]
    public void GeneratedTableToPoco_CustomStaticMethodWithoutInterface_IsIgnored()
    {
        // Act
        var result = Lua.Evaluate<TestTableCustomMethodWithoutInterface>("return { Name = 'custom' }");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Name, Is.EqualTo("custom"));
            Assert.That(result.CustomMethodUsed, Is.False);
        });
    }

    [Test]
    public void Constructor_ImplicitStructViaCall_RejectsZeroArgCallButKeepsExplicitConstructor()
    {
        // Arrange
        Lua["TestImplicitStructConstructor"] = typeof(TestImplicitStructConstructor);

        // Act & Assert
        Assert.Throws<LuaException>(() => Lua.Evaluate("return TestImplicitStructConstructor()"));

        var explicitValue = Lua.Evaluate<TestImplicitStructConstructor>("return TestImplicitStructConstructor(42)");

        // Assert
        Assert.That(explicitValue.Value, Is.EqualTo(42));
    }

    [Test]
    public void Constructor_ExplicitParameterlessStruct_RemainsCallable()
    {
        // Arrange
        Lua["TestExplicitParameterlessStructConstructor"] = typeof(TestExplicitParameterlessStructConstructor);

        // Act
        var defaultValue = Lua.Evaluate<TestExplicitParameterlessStructConstructor>("return TestExplicitParameterlessStructConstructor()");

        // Assert
        Assert.That(defaultValue.Value, Is.EqualTo(123));
    }

    [Test]
    public void Constructor_ImplicitClass_DirectCallIsNotExposed()
    {
        // Arrange
        TestDelegate action = () =>
        {
            Lua["TestImplicitClassConstructorDisabled"] = typeof(TestImplicitClassConstructorDisabled);
        };

        // Act & Assert
        Assert.Throws<NotSupportedException>(action);
    }

    [Test]
    public void Constructor_WithNilArgument_UsesMarshaledOverload()
    {
        // Arrange
        Lua["TestConstructableMarshaledOverloads"] = typeof(TestConstructableMarshaledOverloads);

        // Act
        var result = Lua.Evaluate<TestConstructableMarshaledOverloads>("return TestConstructableMarshaledOverloads(nil)");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Kind, Is.EqualTo("nil"));
    }

    [Test]
    public void Constructor_WithStringArgument_PrefersStringOverMarshaledOverload()
    {
        // Arrange
        Lua["TestConstructableMarshaledOverloads"] = typeof(TestConstructableMarshaledOverloads);

        // Act
        var result = Lua.Evaluate<TestConstructableMarshaledOverloads>("return TestConstructableMarshaledOverloads('hello')");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Kind, Is.EqualTo("string"));
            Assert.That(result.Text, Is.EqualTo("hello"));
        });
    }

    [Test]
    public void Constructor_ParamsArray_NoArguments_CreatesEmptyArray()
    {
        // Arrange
        Lua["TestConstructableParams"] = typeof(TestConstructableParams);

        // Act
        var result = Lua.Evaluate<TestConstructableParams>("return TestConstructableParams()");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Values, Is.Empty);
    }

    [Test]
    public void Constructor_ParamsArray_MultipleArguments_PacksRemainingValues()
    {
        // Arrange
        Lua["TestConstructableParams"] = typeof(TestConstructableParams);

        // Act
        var result = Lua.Evaluate<TestConstructableParams>("return TestConstructableParams(1, 2, 3)");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Values, Is.EqualTo(new[]
        {
            1,
            2,
            3
        }));
    }

    [Test]
    public void Constructor_MixedParamsArray_RemainingArgumentsPacked()
    {
        // Arrange
        Lua["TestConstructableMixedParams"] = typeof(TestConstructableMixedParams);

        // Act
        var result = Lua.Evaluate<TestConstructableMixedParams>("return TestConstructableMixedParams('test', 10, 20, 30)");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Label, Is.EqualTo("test"));
            Assert.That(result.Values, Is.EqualTo(new[]
            {
                10,
                20,
                30
            }));
        });
    }

    [Test]
    public void Constructor_StrictByteArgument_OutOfRange_RaisesLuaError()
    {
        // Arrange
        Lua["TestStrictConstructableByte"] = typeof(TestStrictConstructableByte);

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("return TestStrictConstructableByte(300)"));
        Assert.That(ex!.Message, Does.Contain("out of range"));
    }

    [Test]
    public void GeneratedTableToPoco_IncludedNonPublicParameterlessConstructor_Materializes()
    {
        // Arrange
        Lua.Execute("t = { Name = 'hidden_ctor' }");

        // Act
        using var _ = Lua.Stack.SnapshotCount();
        lua_getglobal(L, "t");
        var result = Lua.Stack[-1].GetValue<TestIncludedNonPublicParameterlessConstructor>(LuaAllowedValueConversions.TableToObject);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("hidden_ctor"));
    }

    [Test]
    public void Constructor_SignedEnumOptionalDefault_UsesDeclaredDefaultValue()
    {
        // Arrange
        Lua["TestConstructableSignedEnumDefault"] = typeof(TestConstructableSignedEnumDefault);

        // Act
        var result = Lua.Evaluate<TestConstructableSignedEnumDefault>("return TestConstructableSignedEnumDefault()");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.EqualTo(TestSignedEnum.Min));
    }

    [Test]
    public void Constructor_UnsignedEnumOptionalDefault_UsesDeclaredDefaultValue()
    {
        // Arrange
        Lua["TestConstructableUnsignedEnumDefault"] = typeof(TestConstructableUnsignedEnumDefault);

        // Act
        var result = Lua.Evaluate<TestConstructableUnsignedEnumDefault>("return TestConstructableUnsignedEnumDefault()");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.EqualTo(TestUnsignedEnum.Max));
    }

    [Test]
    public void Constructor_EnumParameter_NumberToEnumDisabled_RaisesLuaError()
    {
        // Arrange
        Lua["TestConstructableEnumNoNumberConversion"] = typeof(TestConstructableEnumNoNumberConversion);

        // Act & Assert
        Assert.That(
            () => Lua.Execute("return TestConstructableEnumNoNumberConversion(2)"),
            Throws.TypeOf<LuaException>());
    }

    [Test]
    public void Constructor_EnumParameter_UndefinedNumericValue_RaisesLuaError()
    {
        // Arrange
        Lua["TestConstructableEnumValidation"] = typeof(TestConstructableEnumValidation);

        // Act & Assert
        Assert.That(
            () => Lua.Execute("return TestConstructableEnumValidation(3)"),
            Throws.TypeOf<LuaException>());
    }

    [Test]
    public void Constructor_ParamsEnumArray_NumberToEnumDisabled_RaisesLuaError()
    {
        // Arrange
        Lua["TestConstructableEnumParamsNoNumberConversion"] = typeof(TestConstructableEnumParamsNoNumberConversion);

        // Act & Assert
        Assert.That(
            () => Lua.Execute("return TestConstructableEnumParamsNoNumberConversion(1, 2)"),
            Throws.TypeOf<LuaException>());
    }

    [Test]
    public void DefinitionNewIndex_ReadOnlyStaticProperty_WithWritablePeers_RaisesReadOnlyError()
    {
        // Arrange
        Lua["TestWithStatics"] = typeof(TestWithStatics);

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("TestWithStatics.TypeName = 'blocked'"));
        Assert.That(ex!.Message, Does.Contain("read-only"));
    }

    [Test]
    public void DefinitionNewIndex_ReadOnlyOnlyStaticProperty_RaisesReadOnlyError()
    {
        // Arrange
        Lua["TestStaticReadOnlyOnly"] = typeof(TestStaticReadOnlyOnly);

        // Act & Assert
        var ex = Assert.Throws<LuaException>(() => Lua.Execute("TestStaticReadOnlyOnly.TypeName = 'blocked'"));
        Assert.That(ex!.Message, Does.Contain("read-only"));
    }
}
