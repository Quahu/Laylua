using System;
using System.Collections.Generic;
using System.Linq;

namespace Laylua.Generators;

public partial class LuaTypeGenerator
{
    private static void EmitPartialHookDeclarations(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        writer.Code("""
            /// <summary>
            ///     Called when the Lua metatable for instances of this type is first created.
            ///     Use this to add custom metamethods or metatable entries.
            /// </summary>
            static partial void OnMetatableCreated(LuaThread thread, LuaStackValue metatable);

            /// <summary>
            ///     Called when the Lua metatable for the type definition is first created.
            ///     Use this to add custom metamethods or metatable entries to the definition.
            /// </summary>
            static partial void OnDefinitionMetatableCreated(LuaThread thread, LuaStackValue metatable);
            """);

        if (typeInfo.EmitGeneratedFromTableConversion)
        {
            writer.Line();
            writer.Code($"""
                /// <summary>
                ///     Called before the generated table-to-object conversion runs.
                /// </summary>
                static partial void OnBeforeTryGetFromTable(LuaThread thread, LuaTable table);

                /// <summary>
                ///     Called after the generated table-to-object conversion succeeds.
                /// </summary>
                static partial void OnAfterTryGetFromTable(LuaThread thread, LuaTable table, ref {typeInfo.Name} value);
                """);
        }
    }

    private static void EmitCallbackFields(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        writer.Code("""
            private static volatile bool _initialized;
            private static readonly object _initLock = new();
            private static nint _indexFunctionPtr;
            private static nint _newIndexFunctionPtr;
            private static nint _gcFunctionPtr;
            """);

        if (typeInfo.EmitToString)
        {
            writer.Line("private static nint _toStringFunctionPtr;");
        }

        foreach (var method in typeInfo.Methods)
        {
            writer.Line($"private static nint _method{GetMethodGeneratedIdentifier(method)}FunctionPtr;");
        }

        var operatorGroups = typeInfo.Operators.GroupBy(op => op.MetamethodName).ToList();
        foreach (var group in operatorGroups)
        {
            writer.Line($"private static nint _{OperatorCamelName(group.First())}FunctionPtr;");
        }

        if (typeInfo.HasDefinition)
        {
            writer.Code("""
                private static nint _definitionIndexFunctionPtr;
                private static nint _definitionNewIndexFunctionPtr;
                private static nint _definitionGcFunctionPtr;
                private static nint _definitionToStringFunctionPtr;
                """);

            foreach (var method in typeInfo.StaticMethods)
            {
                writer.Line($"private static nint _definitionMethod{GetMethodGeneratedIdentifier(method)}FunctionPtr;");
            }
        }

        if (typeInfo.EmitDirectCall || typeInfo.EmitFactory)
        {
            writer.Line("private static nint _callFunctionPtr;");
        }

        if (typeInfo.ImplementsDisposable)
        {
            writer.Line("private static nint _closeFunctionPtr;");
        }

        if (typeInfo.EmitPairs)
        {
            writer.Line("private static nint _pairsFunctionPtr;");
        }

        if (typeInfo.EmitPairs && typeInfo.PairsBehavior == PairsIfEnumerable && typeInfo.ImplementsEnumerable)
        {
            writer.Code("""
                private static nint _pairsIteratorFunctionPtr;
                private static nint _pairsEnumeratorGcFunctionPtr;
                """);
        }

        if (typeInfo.EmitLength)
        {
            writer.Line("private static nint _lenFunctionPtr;");
        }

        if (typeInfo.TypeCallMethods.Count > 0)
        {
            writer.Line("private static nint _typeCallFunctionPtr;");
        }

        writer.Line();

        writer.Code("""
            private static LuaCFunction? _indexFunction;
            private static LuaCFunction? _newIndexFunction;
            private static LuaCFunction? _gcFunction;
            """);

        if (typeInfo.EmitToString)
        {
            writer.Line("private static LuaCFunction? _toStringFunction;");
        }

        foreach (var method in typeInfo.Methods)
        {
            writer.Line($"private static LuaCFunction? _method{GetMethodGeneratedIdentifier(method)}Function;");
        }

        foreach (var group in operatorGroups)
        {
            writer.Line($"private static LuaCFunction? _{OperatorCamelName(group.First())}Function;");
        }

        if (typeInfo.HasDefinition)
        {
            writer.Code("""
                private static LuaCFunction? _definitionIndexFunction;
                private static LuaCFunction? _definitionNewIndexFunction;
                private static LuaCFunction? _definitionGcFunction;
                private static LuaCFunction? _definitionToStringFunction;
                """);

            foreach (var method in typeInfo.StaticMethods)
            {
                writer.Line($"private static LuaCFunction? _definitionMethod{GetMethodGeneratedIdentifier(method)}Function;");
            }
        }

        if (typeInfo.EmitDirectCall || typeInfo.EmitFactory)
        {
            writer.Line("private static LuaCFunction? _callFunction;");
        }

        if (typeInfo.ImplementsDisposable)
        {
            writer.Line("private static LuaCFunction? _closeFunction;");
        }

        if (typeInfo.EmitPairs)
        {
            writer.Line("private static LuaCFunction? _pairsFunction;");
        }

        if (typeInfo.EmitPairs && typeInfo.PairsBehavior == PairsIfEnumerable && typeInfo.ImplementsEnumerable)
        {
            writer.Code("""
                private static LuaCFunction? _pairsIteratorFunction;
                private static LuaCFunction? _pairsEnumeratorGcFunction;
                """);
        }

        if (typeInfo.EmitLength)
        {
            writer.Line("private static LuaCFunction? _lenFunction;");
        }

        if (typeInfo.TypeCallMethods.Count > 0)
        {
            writer.Line("private static LuaCFunction? _typeCallFunction;");
        }

        var allEvents = typeInfo.Events.Concat(typeInfo.StaticEvents).ToList();
        if (allEvents.Count > 0)
        {
            writer.Line("private static LuaEventSubscriptionTracker? _eventTracker;");

            if (typeInfo.StaticEvents.Count > 0)
            {
                writer.Line("private static readonly object _staticEventSubscriptionKey = new();");
            }

            foreach (var eventInfo in allEvents)
            {
                writer.Line($"private static EventInfo? _event{eventInfo.ClrName}Info;");
            }
        }
    }

    private static void EmitEnsureInitialized(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        writer.Code("""
            private static void EnsureInitialized()
            {
                if (_initialized)
                {
                    return;
                }

                lock (_initLock)
                {
                    if (_initialized)
                    {
                        return;
                    }
            """);

        writer.Line();

        EmitInitCallback(writer, "index", "__Index");
        EmitInitCallback(writer, "newIndex", "__NewIndex");
        EmitInitCallback(writer, "gc", "__Gc");

        if (typeInfo.EmitToString)
        {
            EmitInitCallback(writer, "toString", "__ToString");
        }

        foreach (var method in typeInfo.Methods)
        {
            EmitInitCallback(writer,
                $"method{GetMethodGeneratedIdentifier(method)}",
                $"__Instance_{GetMethodGeneratedIdentifier(method)}");
        }

        foreach (var group in typeInfo.Operators.GroupBy(op => op.MetamethodName))
        {
            var first = group.First();
            EmitInitCallback(writer,
                OperatorCamelName(first),
                OperatorCallbackName(first));
        }

        if (typeInfo.HasDefinition)
        {
            EmitInitCallback(writer, "definitionIndex", "__DefinitionIndex");
            EmitInitCallback(writer, "definitionNewIndex", "__DefinitionNewIndex");
            EmitInitCallback(writer, "definitionGc", "__DefinitionGc");
            EmitInitCallback(writer, "definitionToString", "__DefinitionToString");
            foreach (var method in typeInfo.StaticMethods)
            {
                EmitInitCallback(writer,
                    $"definitionMethod{GetMethodGeneratedIdentifier(method)}",
                    $"__Definition_{GetMethodGeneratedIdentifier(method)}");
            }
        }

        if (typeInfo.EmitDirectCall)
        {
            EmitInitCallback(writer, "call", "__DefinitionCall");
        }
        else if (typeInfo.EmitFactory)
        {
            EmitInitCallback(writer, "call", "__DefinitionFactoryConstruct");
        }

        if (typeInfo.ImplementsDisposable)
        {
            EmitInitCallback(writer, "close", "__Close");
        }

        if (typeInfo.EmitPairs)
        {
            EmitInitCallback(writer, "pairs", "__Pairs");
        }

        if (typeInfo.EmitPairs && typeInfo.PairsBehavior == PairsIfEnumerable && typeInfo.ImplementsEnumerable)
        {
            EmitInitCallback(writer, "pairsIterator", "__PairsIterator");
            EmitInitCallback(writer, "pairsEnumeratorGc", "__PairsEnumeratorGc");
        }

        if (typeInfo.EmitLength)
        {
            EmitInitCallback(writer, "len", "__Len");
        }

        if (typeInfo.TypeCallMethods.Count > 0)
        {
            EmitInitCallback(writer, "typeCall", "__TypeCall");
        }

        var allEvents = typeInfo.Events.Concat(typeInfo.StaticEvents).ToList();
        if (allEvents.Count > 0)
        {
            writer.Line("_eventTracker = new LuaEventSubscriptionTracker();");
            foreach (var eventInfo in allEvents)
            {
                var bindingFlags = eventInfo.IsStatic
                    ? "BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy"
                    : "BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance";

                writer.Line($"_event{eventInfo.ClrName}Info = typeof({typeInfo.Name}).GetEvent(\"{eventInfo.ClrName}\", {bindingFlags})!;");
            }
        }

        // Register method pointers for MethodInfo/open delegate marshaling
        foreach (var method in typeInfo.Methods)
        {
            var names = CollectClrNames(method);
            writer.Line($"LuaTypeServices.RegisterMethodPointer(_method{GetMethodGeneratedIdentifier(method)}FunctionPtr, typeof({typeInfo.Name}), new[] {{ {names} }}, false);");
        }

        foreach (var method in typeInfo.StaticMethods)
        {
            var names = CollectClrNames(method);
            writer.Line($"LuaTypeServices.RegisterMethodPointer(_definitionMethod{GetMethodGeneratedIdentifier(method)}FunctionPtr, typeof({typeInfo.Name}), new[] {{ {names} }}, true);");
        }

        writer.Line();
        writer.Code("_initialized = true;");
        writer.CloseBrace();
        writer.CloseBrace();
    }

    private static void EmitInitCallback(CodeWriter writer, string camelName, string methodName)
    {
        writer.Code($"""
            _{camelName}Function = {methodName};
            _{camelName}FunctionPtr = CreateSafeCFunction(_{camelName}Function);
            """);
    }

    private static void EmitPush(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        writer.Code($$"""
            /// <summary>
            ///     Pushes a <see cref="{{typeInfo.Name}}"/> instance onto the Lua stack as userdata.
            /// </summary>
            /// <remarks>
            ///     This is the low-level push; each call allocates a new GCHandle and userdata.
            ///     For identity-preserving pushes (same object -> same Lua userdata), use the marshaler.
            /// </remarks>
            public static void Push(lua_State* L, {{typeInfo.Name}} value)
            {
                EnsureInitialized();

                if (!lua_checkstack(L, 4))
                {
                    throw new InvalidOperationException("The stack could not be resized to fit 4 extra values.");
                }

                var ud = LuaTypeServices.NewUserData(L, value);

                if (luaL_newmetatable(L, MetatableName))
                {
                    try
                    {
            """);

        EmitSetMetamethod(writer, "__index", "_indexFunctionPtr");
        EmitSetMetamethod(writer, "__newindex", "_newIndexFunctionPtr");
        EmitSetMetamethod(writer, "__gc", "_gcFunctionPtr");

        if (typeInfo.EmitToString)
        {
            EmitSetMetamethod(writer, "__tostring", "_toStringFunctionPtr");
        }

        foreach (var group in typeInfo.Operators.GroupBy(op => op.MetamethodName))
        {
            var first = group.First();
            EmitSetMetamethod(writer, first.MetamethodName, $"_{OperatorCamelName(first)}FunctionPtr");
        }

        if (typeInfo.EmitDispose)
        {
            EmitSetMetamethod(writer, "__close", "_closeFunctionPtr");
        }

        if (typeInfo.EmitPairs)
        {
            EmitSetMetamethod(writer, "__pairs", "_pairsFunctionPtr");
        }

        if (typeInfo.EmitLength)
        {
            EmitSetMetamethod(writer, "__len", "_lenFunctionPtr");
        }

        if (typeInfo.TypeCallMethods.Count > 0)
        {
            EmitSetMetamethod(writer, "__call", "_typeCallFunctionPtr");
        }

        writer.Line();
        writer.Code("""
            lua_pushstring(L, "__metatable"u8);
            lua_pushboolean(L, false);
            lua_rawset(L, -3);
            """);

        writer.Line();
        writer.Code("""
                    using var thread = LuaThread.FromExtraSpace(L);
                    OnMetatableCreated(thread, thread.Stack[-1]);
                }
                catch
                {
                    lua_pushnil(L);
                    lua_setfield(L, LuaRegistry.Index, MetatableName);
                    LuaTypeServices.TryFreeUserData(ud);
                    lua_pop(L, 2);
                    throw;
                }
            """);

        writer.CloseBrace();

        writer.Line();
        writer.Line("lua_setmetatable(L, -2);");
        writer.CloseBrace();
    }

    private static void EmitPushDefinition(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        writer.Code($$"""
            /// <summary>
            ///     Pushes the <see cref="{{typeInfo.Name}}"/> type definition onto the Lua stack.
            ///     Exposes static members for access from Lua.
            /// </summary>
            public static void PushDefinition(lua_State* L)
            {
                EnsureInitialized();

                if (!lua_checkstack(L, 4))
                {
                    throw new InvalidOperationException("The stack could not be resized to fit 4 extra values.");
                }

                var ud = LuaTypeServices.NewUserData(L, typeof({{typeInfo.Name}}));

                if (luaL_newmetatable(L, DefinitionMetatableName))
                {
                    try
                    {
            """);

        EmitSetMetamethod(writer, "__index", "_definitionIndexFunctionPtr");
        EmitSetMetamethod(writer, "__newindex", "_definitionNewIndexFunctionPtr");
        EmitSetMetamethod(writer, "__gc", "_definitionGcFunctionPtr");
        EmitSetMetamethod(writer, "__tostring", "_definitionToStringFunctionPtr");

        if (typeInfo.EmitDirectCall)
        {
            EmitSetMetamethod(writer, "__call", "_callFunctionPtr");
        }

        writer.Line();
        writer.Code("""
            lua_pushstring(L, "__metatable"u8);
            lua_pushboolean(L, false);
            lua_rawset(L, -3);
            """);

        writer.Line();
        writer.Code("""
                    using var thread = LuaThread.FromExtraSpace(L);
                    OnDefinitionMetatableCreated(thread, thread.Stack[-1]);
                }
                catch
                {
                    lua_pushnil(L);
                    lua_setfield(L, LuaRegistry.Index, DefinitionMetatableName);
                    LuaTypeServices.TryFreeUserData(ud);
                    lua_pop(L, 2);
                    throw;
                }
            """);

        writer.CloseBrace();

        writer.Line();
        writer.Line("lua_setmetatable(L, -2);");
        writer.CloseBrace();
    }

    private static void EmitSetMetamethod(CodeWriter writer, string name, string ptrField)
    {
        writer.Code($"""
            lua_pushstring(L, "{name}"u8);
            lua_pushcfunction(L, {ptrField});
            lua_rawset(L, -3);
            """);
    }

    private static void EmitTryGet(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        var nullableSuffix = typeInfo.IsValueType ? "" : "?";
        writer.Code($$"""
            /// <summary>
            ///     Attempts to get a <see cref="{{typeInfo.Name}}"/> from the Lua stack at the given index.
            /// </summary>
            public static bool TryGet(lua_State* L, int stackIndex, out {{typeInfo.Name}}{{nullableSuffix}} value)
            {
                if (LuaTypeServices.TryGetUserDataTarget((nint*) lua_touserdata(L, stackIndex), out var __udTarget))
                {
                    if (__udTarget is {{typeInfo.Name}} typed)
                    {
                        value = typed;
                        return true;
                    }
                }

                value = default;
                return false;
            }
            """);
    }

    private static void EmitOuterTryGetFromTable(
        CodeWriter writer,
        LuaTypeInfo typeInfo,
        List<LuaMemberInfo> tableMembers)
    {
        var bindableMembers = tableMembers.Where(static member => member.CanTableWrite).ToList();
        var requiredMembers = tableMembers.Where(static member => member.IsRequired).ToList();
        var memberIdentifiers = CreateTableMemberIdentifiers(bindableMembers);
        var needsThread = bindableMembers.Any(member => UseMarshaledInterop(typeInfo, member.ValueKind));

        writer.Code($$"""
            /// <summary>
            ///     Runs the generated built-in Lua table conversion for <see cref="{{typeInfo.Name}}"/>.
            /// </summary>
            private static bool TryGetFromTableDefault(LuaThread thread, LuaTable table, [MaybeNullWhen(false)] out {{typeInfo.Name}} value, [NotNullWhen(false)] out LuaConversionError? error)
            {
                thread.Stack.EnsureFreeCapacity(1);

                using (thread.Stack.SnapshotCount())
                {
                    thread.Stack.Push(table);
                    return TryGetFromTableDefaultCore(thread.State.L, -1, out value, out error);
                }
            }
            """);

        writer.Line();
        writer.Code($$"""
            private static bool TryGetFromTableDefaultCore(lua_State* L, int stackIndex, [MaybeNullWhen(false)] out {{typeInfo.Name}} value, [NotNullWhen(false)] out LuaConversionError? error)
            {
                if (lua_type(L, stackIndex) != LuaType.Table)
                {
                    value = default;
                    error = LuaConversionError.ExpectedTable(L, stackIndex, typeof({{typeInfo.Name}}));
                    return false;
                }
            """);

        writer.Line();
        writer.Line("var tableIndex = lua_absindex(L, stackIndex);");

        if (typeInfo.UnknownPropertyBehavior == UnknownPropError)
        {
            writer.Line();
            writer.Code("""
                lua_pushnil(L);
                while (lua_next(L, tableIndex))
                {
                    try
                    {
                """);

            if (bindableMembers.Count == 0)
            {
                writer.Code($$"""
                    if (lua_type(L, -2) != LuaType.String)
                    {
                        value = default;
                        error = LuaConversionError.UnexpectedTableKeyType(typeof({{typeInfo.Name}}), lua_type(L, -2), "$");
                        return false;
                    }

                    var key = lua_tolstring(L, -2, out _);
                    var keyName = key.ToString()!;
                    value = default;
                    error = LuaConversionError.UnknownMember(L, -2, typeof({{typeInfo.Name}}), keyName, "$." + keyName);
                    return false;
                    """);
            }
            else
            {
                writer.Code($$"""
                    if (lua_type(L, -2) != LuaType.String)
                    {
                        value = default;
                        error = LuaConversionError.UnexpectedTableKeyType(typeof({{typeInfo.Name}}), lua_type(L, -2), "$");
                        return false;
                    }

                    var key = lua_tolstring(L, -2, out _);
                    if (key.Pointer == null
                    """);

                writer.Indent();
                for (var i = 0; i < bindableMembers.Count; i++)
                {
                    var joiner = i == 0 ? "|| (" : "&& ";
                    var suffix = i == bindableMembers.Count - 1 ? "))" : "";
                    writer.Line($"{joiner}key != \"{bindableMembers[i].LuaName}\"u8{suffix}");
                }

                writer.Dedent();
                writer.Code($$"""
                    {
                        var keyName = key.ToString()!;
                        value = default;
                        error = LuaConversionError.UnknownMember(L, -2, typeof({{typeInfo.Name}}), keyName, "$." + keyName);
                        return false;
                    }
                    """);
            }

            writer.Code("""
                    }
                    finally
                    {
                        lua_pop(L);
                    }
                }
                """);
        }

        if (needsThread)
        {
            writer.Line();
            writer.Line("using var thread = LuaThread.FromExtraSpace(L);");
        }

        for (var i = 0; i < bindableMembers.Count; i++)
        {
            var member = bindableMembers[i];
            var memberIdentifier = memberIdentifiers[i];
            writer.Line();
            writer.Code($"""
                var {memberIdentifier}WasProvided = false;
                {member.TypeName} {memberIdentifier} = default!;
                """);
        }

        for (var i = 0; i < bindableMembers.Count; i++)
        {
            var member = bindableMembers[i];
            var memberIdentifier = memberIdentifiers[i];
            writer.Line();
            writer.Code($$"""
                lua_getfield(L, tableIndex, "{{member.LuaName}}");
                try
                {
                    if (lua_type(L, -1) != LuaType.Nil)
                    {
                        {{memberIdentifier}}WasProvided = true;
                """);

            if (member.IsObsolete)
            {
                writer.DisableWarning("CS0618");
            }

            writer.Code($$"""
                if (!LuaTypeServices.TryGetValue<{{member.TypeName}}>(L, -1, (global::Laylua.Marshaling.LuaAllowedValueConversions) __LayluaBindingConversions, out var {{memberIdentifier}}Value, out var {{memberIdentifier}}Error))
                {
                    value = default;
                """);

            writer.Line($"error = LuaConversionError.MemberConversionFailed(L, -1, typeof({typeInfo.Name}), typeof({member.TypeName}), \"{member.LuaName}\", \"$.{member.LuaName}\", {memberIdentifier}Error?.WithPathPrefix(\"$.{member.LuaName}\"));");

            writer.Code($$"""
                    return false;
                }

                {{memberIdentifier}} = {{memberIdentifier}}Value!;
                """);

            if (member.IsObsolete)
            {
                writer.RestoreWarning("CS0618");
            }

            writer.Code("""
                    }
                }
                finally
                {
                    lua_pop(L);
                }
                """);
        }

        foreach (var member in requiredMembers)
        {
            var memberIndex = bindableMembers.IndexOf(member);
            var memberIdentifier = memberIdentifiers[memberIndex];
            writer.Line();
            writer.Code($$"""
                if (!{{memberIdentifier}}WasProvided)
                {
                    value = default;
                    error = LuaConversionError.MissingRequiredMember(typeof({{typeInfo.Name}}), "{{member.LuaName}}", "$.{{member.LuaName}}");
                    return false;
                }
                """);
        }

        writer.Line();
        EmitTableResultConstruction(writer, typeInfo, bindableMembers, memberIdentifiers);

        for (var i = 0; i < bindableMembers.Count; i++)
        {
            var member = bindableMembers[i];
            var memberIdentifier = memberIdentifiers[i];
            if (member.IsRequired || member.IsInitOnly)
            {
                continue;
            }

            writer.Line();
            writer.Code($$"""
                if ({{memberIdentifier}}WasProvided)
                {
                """);

            if (member.IsObsolete)
            {
                writer.DisableWarning("CS0618");
            }

            writer.Line($"result.{member.ClrName} = {memberIdentifier};");

            if (member.IsObsolete)
            {
                writer.RestoreWarning("CS0618");
            }

            writer.CloseBrace();
        }

        writer.Line();
        writer.Code("""
                    value = result;
                    error = null;
                    return true;
                }
            """);
    }

    private static void EmitTableResultConstruction(
        CodeWriter writer,
        LuaTypeInfo typeInfo,
        List<LuaMemberInfo> bindableMembers,
        List<string> memberIdentifiers)
    {
        var initializerMembers = bindableMembers
            .Where(static member => member.IsRequired || member.IsInitOnly)
            .ToList();

        if (initializerMembers.Count == 0)
        {
            writer.Line($"var result = new {typeInfo.Name}();");
            return;
        }

        writer.Line($"var result = new {typeInfo.Name}");
        EmitObjectInitializer(writer, initializerMembers, bindableMembers, memberIdentifiers);
    }

    private static void EmitObjectInitializer(
        CodeWriter writer,
        List<LuaMemberInfo> initializerMembers,
        List<LuaMemberInfo> bindableMembers,
        List<string> memberIdentifiers)
    {
        writer.OpenBrace();
        foreach (var member in initializerMembers)
        {
            var memberIdentifier = memberIdentifiers[bindableMembers.IndexOf(member)];
            writer.Line($"{member.ClrName} = {memberIdentifier},");
        }

        writer.Code("};");
    }

    private static List<string> CreateTableMemberIdentifiers(List<LuaMemberInfo> members)
    {
        var identifiers = new List<string>(members.Count);
        var usedIdentifiers = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < members.Count; i++)
        {
            var identifier = "__" + SanitizeGeneratedIdentifier(ToCamelCase(members[i].ClrName));
            var uniqueIdentifier = identifier;
            var suffix = 2;
            while (!usedIdentifiers.Add(uniqueIdentifier))
            {
                uniqueIdentifier = identifier + suffix.ToString();
                suffix++;
            }

            identifiers.Add(uniqueIdentifier);
        }

        return identifiers;
    }

    private static string SanitizeGeneratedIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "member";
        }

        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_')
            {
                chars[i] = '_';
            }
        }

        if (char.IsDigit(chars[0]))
        {
            return "_" + new string(chars);
        }

        return new string(chars);
    }

    private static void EmitTryGetFromTable(
        CodeWriter writer,
        LuaTypeInfo typeInfo)
    {
        var nullableSuffix = typeInfo.IsValueType ? "" : "?";
        writer.Code($$"""
            /// <summary>
            ///     Attempts to read a Lua table into a <see cref="{{typeInfo.Name}}"/> instance.
            /// </summary>
            public static bool TryGetFromTable(lua_State* L, int stackIndex, out {{typeInfo.Name}}{{nullableSuffix}} value)
            {
                return TryGetFromTable(L, stackIndex, out value, out _);
            }

            /// <summary>
            ///     Attempts to read a Lua table into a <see cref="{{typeInfo.Name}}"/> instance.
            /// </summary>
            public static bool TryGetFromTable(lua_State* L, int stackIndex, out {{typeInfo.Name}}{{nullableSuffix}} value, [NotNullWhen(false)] out LuaConversionError? error)
            """);

        writer.OpenBrace();
        if (!typeInfo.ImplementsLuaUserDataFromTable && !typeInfo.EmitGeneratedFromTableConversion)
        {
            writer.Code($$"""
                        value = default;
                        error = LuaConversionError.NoTableConversionRegistered(L, stackIndex, typeof({{typeInfo.Name}}));
                        return false;
                    }
                """);

            return;
        }

        writer.Code($$"""
            if (lua_type(L, stackIndex) != LuaType.Table)
            {
                value = default;
                error = LuaConversionError.ExpectedTable(L, stackIndex, typeof({{typeInfo.Name}}));
                return false;
            }

            using var thread = LuaThread.FromExtraSpace(L);
            using var table = thread.Stack[stackIndex].GetValue<LuaTable>()!;
            """);

        if (typeInfo.ImplementsLuaUserDataFromTable)
        {
            writer.Code($$"""
                if (__TryGetFromTableInterface<{{typeInfo.Name}}>(thread, table, out value, out error))
                {
                    return true;
                }

                error ??= LuaConversionError.BrokenTableConversionHandler(typeof({{typeInfo.Name}}), LuaType.Table);
                return false;
                """);
        }
        else
        {
            writer.Code("""
                OnBeforeTryGetFromTable(thread, table);

                if (!TryGetFromTableDefault(thread, table, out value, out error))
                {
                    return false;
                }

                OnAfterTryGetFromTable(thread, table, ref value);
                return true;
                """);
        }

        writer.CloseBrace();

        if (typeInfo.ImplementsLuaUserDataFromTable)
        {
            writer.Line();
            writer.Line("private static bool __TryGetFromTableInterface<TValue>(LuaThread thread, LuaTable table, [MaybeNullWhen(false)] out TValue value, [NotNullWhen(false)] out LuaConversionError? error)");
            writer.Indent();
            writer.Line("where TValue : ILuaUserDataFromTable<TValue>");
            writer.Dedent();
            writer.Code("""
                {
                    return TValue.TryGetFromTable(thread, table, out value, out error);
                }
                """);
        }
    }

    private static void EmitInterfaceImplementation(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        var nullableSuffix = typeInfo.IsValueType ? "" : "?";

        writer.Code($$"""
            Type ILuaUserDataHandler.Type => typeof({{typeInfo.Name}});

            void ILuaUserDataHandler.Push(lua_State* L, object value)
            {
                Push(L, ({{typeInfo.Name}}) value);
            }

            void ILuaUserDataHandler<{{typeInfo.Name}}>.Push(lua_State* L, {{typeInfo.Name}} value)
            {
                Push(L, value);
            }

            bool ILuaUserDataHandler.TryGet(lua_State* L, int stackIndex, out object? value)
            {
                if (TryGet(L, stackIndex, out var typed))
                {
                    value = typed;
                    return true;
                }

                value = null;
                return false;
            }

            bool ILuaUserDataHandler<{{typeInfo.Name}}>.TryGet(lua_State* L, int stackIndex, out {{typeInfo.Name}}{{nullableSuffix}} value)
            """);

        writer.OpenBrace();
        if (typeInfo.IsValueType)
        {
            writer.Code("""
                if (TryGet(L, stackIndex, out var typed))
                {
                    value = typed;
                    return true;
                }

                value = default;
                return false;
                """);
        }
        else
        {
            writer.Line("return TryGet(L, stackIndex, out value);");
        }

        writer.CloseBrace();
        writer.Line();

        writer.Code($$"""
            bool ILuaUserDataHandler.TryGetFromTable(lua_State* L, int stackIndex, out object? value, [NotNullWhen(false)] out LuaConversionError? error)
            {
                if (TryGetFromTable(L, stackIndex, out var typed, out error))
                {
                    value = typed;
                    return true;
                }

                value = null;
                return false;
            }

            bool ILuaUserDataHandler<{{typeInfo.Name}}>.TryGetFromTable(lua_State* L, int stackIndex, out {{typeInfo.Name}}{{nullableSuffix}} value, [NotNullWhen(false)] out LuaConversionError? error)
            {
                return TryGetFromTable(L, stackIndex, out value, out error);
            }
            """);

        writer.Line();

        var hasPushDefinition = typeInfo.HasDefinition;
        writer.Line("void ILuaUserDataHandler.PushDefinition(lua_State* L)");
        writer.OpenBrace();
        if (hasPushDefinition)
        {
            writer.Line("PushDefinition(L);");
        }
        else
        {
            writer.Line("throw new NotSupportedException(\"This type does not have a definition metatable.\");");
        }

        writer.CloseBrace();
        writer.Line();

        writer.Code("""
            void ILuaUserDataHandler.CleanUpState(lua_State* L)
            {
                if (!lua_checkstack(L, 1))
                {
                    throw new InvalidOperationException("The stack could not be resized to fit 1 extra value.");
                }
                lua_pushnil(L);
                lua_setfield(L, LuaRegistry.Index, MetatableName);
            """);

        if (hasPushDefinition)
        {
            writer.Code("""
                    lua_pushnil(L);
                    lua_setfield(L, LuaRegistry.Index, DefinitionMetatableName);
                """);
        }

        writer.Line("}");
    }

    private static void EmitRegistration(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        writer.Code($$"""
            file static class __{{typeInfo.SafeName}}LuaTypeRegistration
            {
                [ModuleInitializer]
                internal static void Register()
                {
                    LuaTypeRegistry.Register<{{typeInfo.NameWithinNamespace}}>({{typeInfo.NameWithinNamespace}}.LuaUserDataHandler.Instance);
                }
            }
            """);
    }

    private static string CollectClrNames(LuaMethodInfo method)
    {
        var names = new HashSet<string>
        {
            method.ClrName
        };

        if (method.Overloads != null)
        {
            foreach (var overload in method.Overloads)
            {
                names.Add(overload.ClrName);
            }
        }

        return string.Join(", ", names.Select(static n => $"\"{n}\""));
    }
}
