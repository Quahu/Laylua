using System.Collections.Generic;
using System.Linq;

namespace Laylua.Generators;

public partial class LuaTypeGenerator
{
    private static void EmitPartialHookDeclarations(CodeWriter writer)
    {
        writer.Line("/// <summary>");
        writer.Line("///     Called when the Lua metatable for instances of this type is first created.");
        writer.Line("///     Use this to add custom metamethods or metatable entries.");
        writer.Line("/// </summary>");
        writer.Line("static partial void OnMetatableCreated(LuaThread thread, LuaStackValue metatable);");
        writer.Line();
        writer.Line("/// <summary>");
        writer.Line("///     Called when the Lua metatable for the type definition is first created.");
        writer.Line("///     Use this to add custom metamethods or metatable entries to the definition.");
        writer.Line("/// </summary>");
        writer.Line("static partial void OnDefinitionMetatableCreated(LuaThread thread, LuaStackValue metatable);");
    }

    private static void EmitCallbackFields(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        writer.Line("private static volatile bool _initialized;");
        writer.Line("private static readonly object _initLock = new();");
        writer.Line("private static nint _indexFunctionPtr;");
        writer.Line("private static nint _newIndexFunctionPtr;");
        writer.Line("private static nint _gcFunctionPtr;");

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
            writer.Line("private static nint _definitionIndexFunctionPtr;");
            writer.Line("private static nint _definitionNewIndexFunctionPtr;");
            writer.Line("private static nint _definitionGcFunctionPtr;");
            writer.Line("private static nint _definitionToStringFunctionPtr;");
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
            writer.Line("private static nint _pairsIteratorFunctionPtr;");
            writer.Line("private static nint _pairsEnumeratorGcFunctionPtr;");
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

        writer.Line("private static LuaCFunction? _indexFunction;");
        writer.Line("private static LuaCFunction? _newIndexFunction;");
        writer.Line("private static LuaCFunction? _gcFunction;");

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
            writer.Line("private static LuaCFunction? _definitionIndexFunction;");
            writer.Line("private static LuaCFunction? _definitionNewIndexFunction;");
            writer.Line("private static LuaCFunction? _definitionGcFunction;");
            writer.Line("private static LuaCFunction? _definitionToStringFunction;");
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
            writer.Line("private static LuaCFunction? _pairsIteratorFunction;");
            writer.Line("private static LuaCFunction? _pairsEnumeratorGcFunction;");
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
        writer.Line("private static void EnsureInitialized()");
        writer.OpenBrace();
        writer.Line("if (_initialized)");
        writer.OpenBrace();
        writer.Line("return;");
        writer.CloseBrace();
        writer.Line();
        writer.Line("lock (_initLock)");
        writer.OpenBrace();
        writer.Line("if (_initialized)");
        writer.OpenBrace();
        writer.Line("return;");
        writer.CloseBrace();
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
        writer.Line("_initialized = true;");
        writer.CloseBrace();
        writer.CloseBrace();
    }

    private static void EmitInitCallback(CodeWriter writer, string camelName, string methodName)
    {
        writer.Line($"_{camelName}Function = {methodName};");
        writer.Line($"_{camelName}FunctionPtr = CreateSafeCFunction(_{camelName}Function);");
    }

    private static void EmitPush(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        writer.Line("/// <summary>");
        writer.Line($"///     Pushes a <see cref=\"{typeInfo.Name}\"/> instance onto the Lua stack as userdata.");
        writer.Line("/// </summary>");
        writer.Line("/// <remarks>");
        writer.Line("///     This is the low-level push; each call allocates a new GCHandle and userdata.");
        writer.Line("///     For identity-preserving pushes (same object -> same Lua userdata), use the marshaler.");
        writer.Line("/// </remarks>");
        writer.Line($"public static void Push(lua_State* L, {typeInfo.Name} value)");
        writer.OpenBrace();
        writer.Line("EnsureInitialized();");
        writer.Line();
        writer.Line("if (!lua_checkstack(L, 4))");
        writer.OpenBrace();
        writer.Line("throw new InvalidOperationException(\"The stack could not be resized to fit 4 extra values.\");");
        writer.CloseBrace();
        writer.Line();
        writer.Line("var ud = LuaTypeServices.NewUserData(L, value);");
        writer.Line();
        writer.Line("if (luaL_newmetatable(L, MetatableName))");
        writer.OpenBrace();
        writer.Line("try");
        writer.OpenBrace();

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
        writer.Line("lua_pushstring(L, \"__metatable\"u8);");
        writer.Line("lua_pushboolean(L, false);");
        writer.Line("lua_rawset(L, -3);");

        writer.Line();
        writer.Line("using var thread = LuaThread.FromExtraSpace(L);");
        writer.Line("OnMetatableCreated(thread, thread.Stack[-1]);");

        writer.CloseBrace();
        writer.Line("catch");
        writer.OpenBrace();
        writer.Line("lua_pushnil(L);");
        writer.Line("lua_setfield(L, LuaRegistry.Index, MetatableName);");
        writer.Line("LuaTypeServices.TryFreeUserData(ud);");
        writer.Line("lua_pop(L, 2);");
        writer.Line("throw;");
        writer.CloseBrace();
        writer.CloseBrace();
        writer.Line();
        writer.Line("lua_setmetatable(L, -2);");
        writer.CloseBrace();
    }

    private static void EmitPushDefinition(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        writer.Line("/// <summary>");
        writer.Line($"///     Pushes the <see cref=\"{typeInfo.Name}\"/> type definition onto the Lua stack.");
        writer.Line("///     Exposes static members for access from Lua.");
        writer.Line("/// </summary>");
        writer.Line("public static void PushDefinition(lua_State* L)");
        writer.OpenBrace();
        writer.Line("EnsureInitialized();");
        writer.Line();
        writer.Line("if (!lua_checkstack(L, 4))");
        writer.OpenBrace();
        writer.Line("throw new InvalidOperationException(\"The stack could not be resized to fit 4 extra values.\");");
        writer.CloseBrace();
        writer.Line();
        writer.Line($"var ud = LuaTypeServices.NewUserData(L, typeof({typeInfo.Name}));");
        writer.Line();
        writer.Line("if (luaL_newmetatable(L, DefinitionMetatableName))");
        writer.OpenBrace();
        writer.Line("try");
        writer.OpenBrace();

        EmitSetMetamethod(writer, "__index", "_definitionIndexFunctionPtr");
        EmitSetMetamethod(writer, "__newindex", "_definitionNewIndexFunctionPtr");
        EmitSetMetamethod(writer, "__gc", "_definitionGcFunctionPtr");
        EmitSetMetamethod(writer, "__tostring", "_definitionToStringFunctionPtr");

        if (typeInfo.EmitDirectCall)
        {
            EmitSetMetamethod(writer, "__call", "_callFunctionPtr");
        }

        writer.Line();
        writer.Line("lua_pushstring(L, \"__metatable\"u8);");
        writer.Line("lua_pushboolean(L, false);");
        writer.Line("lua_rawset(L, -3);");

        writer.Line();
        writer.Line("using var thread = LuaThread.FromExtraSpace(L);");
        writer.Line("OnDefinitionMetatableCreated(thread, thread.Stack[-1]);");

        writer.CloseBrace();
        writer.Line("catch");
        writer.OpenBrace();
        writer.Line("lua_pushnil(L);");
        writer.Line("lua_setfield(L, LuaRegistry.Index, DefinitionMetatableName);");
        writer.Line("LuaTypeServices.TryFreeUserData(ud);");
        writer.Line("lua_pop(L, 2);");
        writer.Line("throw;");
        writer.CloseBrace();
        writer.CloseBrace();
        writer.Line();
        writer.Line("lua_setmetatable(L, -2);");
        writer.CloseBrace();
    }

    private static void EmitSetMetamethod(CodeWriter writer, string name, string ptrField)
    {
        writer.Line($"lua_pushstring(L, \"{name}\"u8);");
        writer.Line($"lua_pushcfunction(L, {ptrField});");
        writer.Line("lua_rawset(L, -3);");
    }

    private static void EmitTryGet(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        writer.Line("/// <summary>");
        writer.Line($"///     Attempts to get a <see cref=\"{typeInfo.Name}\"/> from the Lua stack at the given index.");
        writer.Line("/// </summary>");
        var nullableSuffix = typeInfo.IsValueType ? "" : "?";
        writer.Line($"public static bool TryGet(lua_State* L, int stackIndex, out {typeInfo.Name}{nullableSuffix} value)");
        writer.OpenBrace();
        writer.Line("if (LuaTypeServices.TryGetUserDataTarget((nint*) lua_touserdata(L, stackIndex), out var __udTarget))");
        writer.OpenBrace();
        writer.Line($"if (__udTarget is {typeInfo.Name} typed)");
        writer.OpenBrace();
        writer.Line("value = typed;");
        writer.Line("return true;");
        writer.CloseBrace();
        writer.CloseBrace();
        writer.Line();

        writer.Line("value = default;");
        writer.Line("return false;");
        writer.CloseBrace();
    }

    private static void EmitTryGetFromTable(
        CodeWriter writer,
        LuaTypeInfo typeInfo,
        List<LuaMemberInfo> tableMembers)
    {
        writer.Line("/// <summary>");
        writer.Line($"///     Attempts to read a Lua table into a <see cref=\"{typeInfo.Name}\"/> instance.");
        writer.Line("/// </summary>");
        var nullableSuffix = typeInfo.IsValueType ? "" : "?";
        writer.Line($"public static bool TryGetFromTable(lua_State* L, int stackIndex, out {typeInfo.Name}{nullableSuffix} value)");
        writer.OpenBrace();
        writer.Line("if (lua_type(L, stackIndex) != LuaType.Table)");
        writer.OpenBrace();
        writer.Line("value = default;");
        writer.Line("return false;");
        writer.CloseBrace();
        writer.Line();
        writer.Line("if (!lua_checkstack(L, 1))");
        writer.OpenBrace();
        writer.Line("throw new InvalidOperationException(\"The stack could not be resized to fit 1 extra value.\");");
        writer.CloseBrace();
        writer.Line();
        writer.Line($"var result = new {typeInfo.Name}();");

        var needsThread = tableMembers.Any(member => member.CanWrite && UseMarshaledInterop(typeInfo, member.ValueKind));
        if (needsThread)
        {
            writer.Line("using var thread = LuaThread.FromExtraSpace(L);");
        }

        foreach (var member in tableMembers.Where(static member => member.CanWrite))
        {
            writer.Line();
            writer.Line($"lua_getfield(L, stackIndex, \"{member.LuaName}\");");
            writer.Line("try");
            writer.OpenBrace();
            writer.Line("if (lua_type(L, -1) != LuaType.Nil)");
            writer.OpenBrace();

            if (member.IsObsolete)
                writer.DisableWarning("CS0618");

            writer.Line($"result.{member.ClrName} = {EmitReadExpression(typeInfo, member.ValueKind, member.TypeName, -1)};");

            if (member.IsObsolete)
                writer.RestoreWarning("CS0618");

            writer.CloseBrace();
            writer.CloseBrace();
            writer.Line("finally");
            writer.OpenBrace();
            writer.Line("lua_pop(L);");
            writer.CloseBrace();
        }

        writer.Line();
        writer.Line("value = result;");
        writer.Line("return true;");
        writer.CloseBrace();
    }

    private static void EmitInterfaceImplementation(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        var nullableSuffix = typeInfo.IsValueType ? "" : "?";

        writer.Line($"Type ILuaUserDataHandler.Type => typeof({typeInfo.Name});");
        writer.Line();
        writer.Line("void ILuaUserDataHandler.Push(lua_State* L, object value)");
        writer.OpenBrace();
        writer.Line($"Push(L, ({typeInfo.Name}) value);");
        writer.CloseBrace();
        writer.Line();
        writer.Line($"void ILuaUserDataHandler<{typeInfo.Name}>.Push(lua_State* L, {typeInfo.Name} value)");
        writer.OpenBrace();
        writer.Line("Push(L, value);");
        writer.CloseBrace();
        writer.Line();
        writer.Line("bool ILuaUserDataHandler.TryGet(lua_State* L, int stackIndex, out object? value)");
        writer.OpenBrace();
        writer.Line("if (TryGet(L, stackIndex, out var typed))");
        writer.OpenBrace();
        writer.Line("value = typed;");
        writer.Line("return true;");
        writer.CloseBrace();
        writer.Line();
        writer.Line("value = null;");
        writer.Line("return false;");
        writer.CloseBrace();
        writer.Line();
        writer.Line($"bool ILuaUserDataHandler<{typeInfo.Name}>.TryGet(lua_State* L, int stackIndex, out {typeInfo.Name}{nullableSuffix} value)");
        writer.OpenBrace();
        if (typeInfo.IsValueType)
        {
            writer.Line("if (TryGet(L, stackIndex, out var typed))");
            writer.OpenBrace();
            writer.Line("value = typed;");
            writer.Line("return true;");
            writer.CloseBrace();
            writer.Line();
            writer.Line("value = default;");
            writer.Line("return false;");
        }
        else
        {
            writer.Line("return TryGet(L, stackIndex, out value);");
        }

        writer.CloseBrace();
        writer.Line();

        var hasTableMembers = HasAccessibleParameterlessConstructor(typeInfo) && typeInfo.Properties.Concat(typeInfo.Fields)
            .Any(static member => member.CanRead || member.CanWrite);

        writer.Line("bool ILuaUserDataHandler.TryGetFromTable(lua_State* L, int stackIndex, out object? value)");
        writer.OpenBrace();
        if (hasTableMembers)
        {
            writer.Line("if (TryGetFromTable(L, stackIndex, out var typed))");
            writer.OpenBrace();
            writer.Line("value = typed;");
            writer.Line("return true;");
            writer.CloseBrace();
            writer.Line();
        }

        writer.Line("value = null;");
        writer.Line("return false;");
        writer.CloseBrace();
        writer.Line();

        writer.Line($"bool ILuaUserDataHandler<{typeInfo.Name}>.TryGetFromTable(lua_State* L, int stackIndex, out {typeInfo.Name}{nullableSuffix} value)");
        writer.OpenBrace();
        if (hasTableMembers)
        {
            writer.Line("return TryGetFromTable(L, stackIndex, out value);");
        }
        else
        {
            writer.Line("value = default;");
            writer.Line("return false;");
        }

        writer.CloseBrace();
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

        writer.Line("void ILuaUserDataHandler.CleanUpState(lua_State* L)");
        writer.OpenBrace();
        writer.Line("if (!lua_checkstack(L, 1))");
        writer.OpenBrace();
        writer.Line("throw new InvalidOperationException(\"The stack could not be resized to fit 1 extra value.\");");
        writer.CloseBrace();
        writer.Line("lua_pushnil(L);");
        writer.Line("lua_setfield(L, LuaRegistry.Index, MetatableName);");
        if (hasPushDefinition)
        {
            writer.Line("lua_pushnil(L);");
            writer.Line("lua_setfield(L, LuaRegistry.Index, DefinitionMetatableName);");
        }

        writer.CloseBrace();
    }

    private static void EmitRegistration(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        writer.Line($"file static class __{typeInfo.SafeName}LuaTypeRegistration");
        writer.OpenBrace();
        writer.Line("[ModuleInitializer]");
        writer.Line("internal static void Register()");
        writer.OpenBrace();
        writer.Line($"LuaTypeRegistry.Register<{typeInfo.NameWithinNamespace}>({typeInfo.NameWithinNamespace}.LuaUserDataHandler.Instance);");
        writer.CloseBrace();
        writer.CloseBrace();
    }

    private static string CollectClrNames(LuaMethodInfo method)
    {
        var names = new HashSet<string> { method.ClrName };
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
