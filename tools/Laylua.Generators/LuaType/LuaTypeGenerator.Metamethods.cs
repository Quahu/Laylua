using System;
using System.Collections.Generic;
using System.Linq;

namespace Laylua.Generators;

public partial class LuaTypeGenerator
{
    private static void EmitOperatorCallback(
        CodeWriter writer,
        LuaTypeInfo typeInfo,
        List<LuaOperatorInfo> overloads)
    {
        if (overloads.Count == 1)
        {
            EmitSingleOperatorCallback(writer, typeInfo, overloads[0]);
            return;
        }

        var first = overloads[0];
        writer.Line($"private static int {OperatorCallbackName(first)}(lua_State* L)");
        writer.OpenBrace();

        for (var overloadIndex = 0; overloadIndex < overloads.Count; overloadIndex++)
        {
            var op = overloads[overloadIndex];
            var isUnary = op.Parameters.Count == 1;

            var conditions = new List<string>();
            for (var i = 0; i < op.Parameters.Count; i++)
            {
                var param = op.Parameters[i];
                var stackIndex = i + 1;
                if (param.IsAnnotatedType)
                {
                    conditions.Add($"lua_type(L, {stackIndex}) == LuaType.UserData");
                }
                else
                {
                    switch (param.ValueKind)
                    {
                        case LuaValueKind.Integer:
                        {
                            conditions.Add($"lua_isinteger(L, {stackIndex})");
                            break;
                        }
                        case LuaValueKind.Number:
                        {
                            conditions.Add($"lua_type(L, {stackIndex}) == LuaType.Number");
                            break;
                        }
                        case LuaValueKind.String:
                        {
                            conditions.Add($"lua_type(L, {stackIndex}) == LuaType.String");
                            break;
                        }
                        case LuaValueKind.Boolean:
                        {
                            conditions.Add($"lua_type(L, {stackIndex}) == LuaType.Boolean");
                            break;
                        }
                        default:
                        {
                            conditions.Add($"lua_type(L, {stackIndex}) == LuaType.UserData");
                            break;
                        }
                    }
                }
            }

            var keyword = overloadIndex == 0 ? "if" : "else if";
            writer.Line($"{keyword} ({string.Join(" && ", conditions)})");
            writer.OpenBrace();

            for (var i = 0; i < op.Parameters.Count; i++)
            {
                var param = op.Parameters[i];
                var stackIndex = i + 1;
                var varName = isUnary ? "operand" : (i == 0 ? "left" : "right");

                if (param.IsAnnotatedType)
                {
                    writer.Code($$"""
                        if (!LuaTypeServices.TryGetUserDataTarget((nint*) lua_touserdata(L, {{stackIndex}}), out var __{{varName}}Target))
                        {
                            return luaL_error(L, "{{typeInfo.Name}}: '{{varName}}' is not a valid userdata");
                        }

                        if (__{{varName}}Target is not {{typeInfo.Name}} {{varName}})
                        {
                            return luaL_error(L, "{{typeInfo.Name}}: '{{varName}}' is not a valid {{typeInfo.Name}} instance");
                        }
                        """);

                    writer.Line();
                }
                else
                {
                    writer.Line($"var {varName} = {EmitReadExpression(typeInfo, param.ValueKind, param.TypeName, stackIndex)};");
                }
            }

            string operatorExpression;
            if (isUnary)
            {
                var opSymbol = op.MethodName switch
                {
                    "op_UnaryNegation" => "-",
                    "op_OnesComplement" => "~",
                    _ => throw new InvalidOperationException($"Unknown unary operator method: {op.MethodName}")
                };

                operatorExpression = $"{opSymbol}operand";
            }
            else
            {
                var opSymbol = GetOperatorSymbol(op.MethodName);
                operatorExpression = $"left {opSymbol} right";
            }

            writer.Line($"var result = {operatorExpression};");

            if (op.ReturnsAnnotatedType)
            {
                writer.Line("Push(L, result);");
            }
            else
            {
                EmitPushExpression(writer, typeInfo, op.ReturnValueKind, op.ReturnType, "result", scopeThread: false);
            }

            writer.Line("return 1;");
            writer.CloseBrace();
        }

        writer.Line();
        writer.Code($"return luaL_error(L, \"{typeInfo.Name}: no matching operator overload for '{first.MetamethodName}'\");");
        writer.CloseBrace();
    }

    private static void EmitSingleOperatorCallback(
        CodeWriter writer,
        LuaTypeInfo typeInfo,
        LuaOperatorInfo operatorInfo)
    {
        writer.Line($"private static int {OperatorCallbackName(operatorInfo)}(lua_State* L)");
        writer.OpenBrace();

        var isUnary = operatorInfo.Parameters.Count == 1;

        for (var i = 0; i < operatorInfo.Parameters.Count; i++)
        {
            var parameter = operatorInfo.Parameters[i];
            var stackIndex = i + 1;
            var variableName = isUnary ? "operand" : (i == 0 ? "left" : "right");

            if (parameter.IsAnnotatedType)
            {
                writer.Code($$"""
                    if (!LuaTypeServices.TryGetUserDataTarget((nint*) lua_touserdata(L, {{stackIndex}}), out var __{{variableName}}Target))
                    {
                        return luaL_error(L, "{{typeInfo.Name}}: '{{variableName}}' is not a valid userdata");
                    }

                    if (__{{variableName}}Target is not {{typeInfo.Name}} {{variableName}})
                    {
                        return luaL_error(L, "{{typeInfo.Name}}: '{{variableName}}' is not a valid {{typeInfo.Name}} instance");
                    }
                    """);

                writer.Line();
            }
            else
            {
                writer.Line($"var {variableName} = {EmitReadExpression(typeInfo, parameter.ValueKind, parameter.TypeName, stackIndex)};");
            }
        }

        string operatorExpression;
        if (isUnary)
        {
            var op = operatorInfo.MethodName switch
            {
                "op_UnaryNegation" => "-",
                "op_OnesComplement" => "~",
                _ => throw new InvalidOperationException($"Unknown unary operator method: {operatorInfo.MethodName}")
            };

            operatorExpression = $"{op}operand";
        }
        else
        {
            var op = GetOperatorSymbol(operatorInfo.MethodName);
            operatorExpression = $"left {op} right";
        }

        writer.Line($"var result = {operatorExpression};");

        if (operatorInfo.ReturnsAnnotatedType)
        {
            writer.Line("Push(L, result);");
        }
        else
        {
            EmitPushExpression(writer, typeInfo, operatorInfo.ReturnValueKind, operatorInfo.ReturnType, "result", scopeThread: false);
        }

        writer.Line("return 1;");
        writer.CloseBrace();
    }

    private static void EmitGcCallback(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        writer.Code($$"""
            private static int __Gc(lua_State* L)
            {
                var ud = (nint*) lua_touserdata(L, 1);
            """);

        var hasEvents = typeInfo.Events.Count > 0 || typeInfo.StaticEvents.Count > 0;
        var needsCleanupException = hasEvents || typeInfo.EmitDispose;
        if (needsCleanupException)
        {
            writer.Line("Exception? cleanupException = null;");
        }

        if (hasEvents)
        {
            writer.Code("""
                if (_eventTracker != null && ud != null && ud[0] != 0)
                {
                    try
                    {
                        _eventTracker.CleanupAll(L, (nint) ud);
                    }
                    catch (Exception ex)
                    {
                        cleanupException = LuaTypeServices.CombineCleanupException(cleanupException, ex);
                    }
                }
                """);
        }

        if (typeInfo.EmitDispose)
        {
            writer.Code("""
                if (LuaTypeServices.TryGetUserDataTarget(ud, out var target))
                {
                    try
                    {
                        ((IDisposable) target).Dispose();
                    }
                    catch (Exception ex)
                    {
                        cleanupException = LuaTypeServices.CombineCleanupException(cleanupException, ex);
                    }
                    finally
                    {
                        LuaTypeServices.TryFreeUserData(ud);
                    }
                }
                else
                {
                    LuaTypeServices.TryFreeUserData(ud);
                }
                """);
        }
        else
        {
            writer.Line("LuaTypeServices.TryFreeUserData(ud);");
        }

        if (needsCleanupException)
        {
            writer.Line("LuaTypeServices.ThrowCleanupException(cleanupException);");
            writer.Line();
        }

        writer.Line("return 0;");
        writer.CloseBrace();
    }

    private static void EmitCloseCallback(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        writer.Code("""
            private static int __Close(lua_State* L)
            {
                var ud = (nint*) lua_touserdata(L, 1);
                Exception? cleanupException = null;
            """);

        var hasEvents = typeInfo.Events.Count > 0 || typeInfo.StaticEvents.Count > 0;
        if (hasEvents)
        {
            writer.Code("""
                if (_eventTracker != null && ud != null && ud[0] != 0)
                {
                    try
                    {
                        _eventTracker.CleanupAll(L, (nint) ud);
                    }
                    catch (Exception ex)
                    {
                        cleanupException = LuaTypeServices.CombineCleanupException(cleanupException, ex);
                    }
                }
                """);
        }

        writer.Code("""
            if (LuaTypeServices.TryGetUserDataTarget(ud, out var target))
            {
                try
                {
                    ((IDisposable) target).Dispose();
                }
                catch (Exception ex)
                {
                    cleanupException = LuaTypeServices.CombineCleanupException(cleanupException, ex);
                }
                finally
                {
                    LuaTypeServices.TryFreeUserData(ud);
                }
            }
            else
            {
                LuaTypeServices.TryFreeUserData(ud);
            }
            """);

        writer.Code("""
                LuaTypeServices.ThrowCleanupException(cleanupException);
                return 0;
            }
        """);
    }

    private static void EmitLenCallback(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        writer.Line("private static int __Len(lua_State* L)");
        writer.OpenBrace();
        EmitSelfVariable(writer, typeInfo);
        writer.Code($$"""
                lua_pushinteger(L, {{typeInfo.LengthPropertyName}});
                return 1;
            }
        """);
    }

    private static void EmitPairsCallback(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        writer.Line("private static int __Pairs(lua_State* L)");
        writer.OpenBrace();

        if (typeInfo.PairsBehavior == PairsProperties)
        {
            var readableMembers = typeInfo.ReadableMembers;

            if (readableMembers.Count == 0)
            {
                writer.Code("""
                        lua_pushcfunction(L, static (lua_State* L) => 0);
                        lua_pushnil(L);
                        lua_pushnil(L);
                        return 3;
                    }
                """);

                return;
            }

            EmitSelfVariable(writer, typeInfo);
            writer.Line();

            writer.Code($"""
                luaL_checkstack(L, 3, null);
                lua_createtable(L, 0, {readableMembers.Count});
                """);

            foreach (var member in readableMembers)
            {
                writer.Line($"lua_pushstring(L, \"{member.LuaName}\"u8);");

                if (member.IsObsolete)
                    writer.DisableWarning("CS0618");

                var memberAccess = $"self.{member.ClrName}";
                if (member.IsLuaString)
                {
                    writer.Line($"{EmitLuaStringPushExpression(memberAccess, member.TypeName)};");
                }
                else
                {
                    EmitPushExpression(writer, typeInfo, member.ValueKind, member.TypeName, memberAccess);
                }

                if (member.IsObsolete)
                    writer.RestoreWarning("CS0618");

                writer.Line("lua_rawset(L, -3);");
            }

            writer.Line();
            writer.Code("""
                lua_getglobal(L, "next");
                lua_pushvalue(L, -2);
                lua_remove(L, -3);
                lua_pushnil(L);
                return 3;
                """);
        }
        else if (typeInfo.PairsBehavior == PairsIfEnumerable && typeInfo.ImplementsEnumerable)
        {
            EmitSelfVariable(writer, typeInfo);
            writer.Code("""
                var enumerator = ((IEnumerable) self).GetEnumerator();
                GCHandle enumHandle = default;
                nint* enumUd = null;
                var isEnumUdOnStack = false;
                var isMetatableOnStack = false;

                try
                {
                    enumHandle = GCHandle.Alloc(new object[] { enumerator, 0 });

                    enumUd = (nint*) lua_newuserdatauv(L, (nuint) sizeof(nint), 0);
                    isEnumUdOnStack = true;
                    *enumUd = GCHandle.ToIntPtr(enumHandle);

                    if (luaL_newmetatable(L, "__Laylua:PairsEnumerator"))
                    {
                        isMetatableOnStack = true;
                        try
                        {
                            lua_pushcfunction(L, _pairsEnumeratorGcFunctionPtr);
                            lua_setfield(L, -2, "__gc");
                            lua_pushboolean(L, false);
                            lua_setfield(L, -2, "__metatable");
                        }
                        catch
                        {
                            lua_pushnil(L);
                            lua_setfield(L, LuaRegistry.Index, "__Laylua:PairsEnumerator");
                            var popCount = (isMetatableOnStack ? 1 : 0) + (isEnumUdOnStack ? 1 : 0);
                            if (popCount > 0)
                            {
                                lua_pop(L, popCount);
                                isMetatableOnStack = false;
                                isEnumUdOnStack = false;
                            }
                            throw;
                        }
                    }
                    else
                    {
                        isMetatableOnStack = true;
                    }

                    lua_setmetatable(L, -2);
                    isMetatableOnStack = false;

                    lua_pushcclosure(L, _pairsIteratorFunctionPtr, 1);
                    isEnumUdOnStack = false;
                    lua_pushnil(L);
                    lua_pushnil(L);
                    enumHandle = default;
                    return 3;
                }
                catch
                {
                    var popCount = (isMetatableOnStack ? 1 : 0) + (isEnumUdOnStack ? 1 : 0);
                    if (popCount > 0)
                    {
                        lua_pop(L, popCount);
                    }
                    if (enumHandle.IsAllocated)
                    {
                        enumHandle.Free();
                        if (enumUd != null)
                        {
                            *enumUd = 0;
                        }
                    }
                    if (enumerator is IDisposable disposable)
                    {
                        try
                        {
                            disposable.Dispose();
                        }
                        catch
                        { }
                    }
                    throw;
                }
                """);
        }

        writer.CloseBrace();
    }

    private static void EmitPairsIteratorCallback(CodeWriter writer)
    {
        writer.Line("private static int __PairsIterator(lua_State* L)");
        writer.OpenBrace();
        writer.Code("""
            var enumUd = *(nint*) lua_touserdata(L, lua_upvalueindex(1));
            var state = (object[]) GCHandle.FromIntPtr(enumUd).Target!;
            var enumerator = (IEnumerator) state[0];
            if (!enumerator.MoveNext())
            {
                LuaTypeServices.DisposePairsEnumeratorState((nint*) lua_touserdata(L, lua_upvalueindex(1)));
                return 0;
            }

            var index = (int) state[1] + 1;
            state[1] = index;
            lua_pushinteger(L, index);
            using var thread = LuaThread.FromExtraSpace(L);
            thread.Stack.Push(enumerator.Current);
            return 2;
            """);

        writer.CloseBrace();
    }

    private static void EmitPairsEnumeratorGcCallback(CodeWriter writer)
    {
        writer.Line("private static int __PairsEnumeratorGc(lua_State* L)");
        writer.OpenBrace();
        writer.Code("""
            var ud = (nint*) lua_touserdata(L, 1);
            if (ud == null)
            {
                return 0;
            }

            var ptr = *ud;
            if (ptr != 0)
            {
                var handle = GCHandle.FromIntPtr(ptr);
                try
                {
                    var state = (object[]) handle.Target!;
                    if (state[0] is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                finally
                {
                    handle.Free();
                    *ud = 0;
                }
            }

            return 0;
            """);

        writer.CloseBrace();
    }

    private static void EmitConstructorCallback(CodeWriter writer, LuaTypeInfo typeInfo, string methodName, int selfAdjust)
    {
        writer.Line($"private static int {methodName}(lua_State* L)");
        writer.OpenBrace();

        var stackOffset = selfAdjust + 1;

        if (typeInfo.Constructors.Count == 1)
        {
            var constructor = typeInfo.Constructors[0];
            var fixedParameters = constructor.Parameters.Where(static parameter => !parameter.IsParams).ToList();
            var requiredCount = fixedParameters.Count(static parameter => !parameter.IsOptional);
            var maxCount = fixedParameters.Count;
            var hasParams = constructor.Parameters.Any(static parameter => parameter.IsParams);
            if (requiredCount > 0 || !hasParams)
            {
                if (selfAdjust > 0)
                {
                    writer.Line($"var argCount = lua_gettop(L) - {selfAdjust};");
                }
                else
                {
                    writer.Line("var argCount = lua_gettop(L);");
                }

                if (requiredCount > 0)
                {
                    writer.Code($$"""
                        if (argCount < {{requiredCount}})
                        {
                            return luaL_error(L, "{{typeInfo.Name}}: expected at least {{requiredCount}} argument(s)");
                        }
                        """);
                }

                if (!hasParams)
                {
                    writer.Code($$"""
                        if (argCount > {{maxCount}})
                        {
                            return luaL_error(L, "{{typeInfo.Name}}: invalid number of arguments");
                        }
                        """);
                }
            }

            EmitConstructorBody(writer, typeInfo, constructor, stackOffset);
        }
        else
        {
            writer.Line("using var thread = LuaThread.FromExtraSpace(L);");

            if (selfAdjust > 0)
            {
                writer.Line($"var argCount = lua_gettop(L) - {selfAdjust};");
            }
            else
            {
                writer.Line("var argCount = lua_gettop(L);");
            }

            writer.Line("switch (argCount)");
            writer.OpenBrace();

            var caseMap = new Dictionary<int, List<LuaConstructorInfo>>();
            foreach (var constructor in typeInfo.Constructors)
            {
                var fixedParameters = constructor.Parameters.Where(static parameter => !parameter.IsParams).ToList();
                var requiredCount = fixedParameters.Count(static parameter => !parameter.IsOptional);
                var maxCount = fixedParameters.Count;
                for (var i = requiredCount; i <= maxCount; i++)
                {
                    if (!caseMap.TryGetValue(i, out var list))
                    {
                        list = new List<LuaConstructorInfo>();
                        caseMap[i] = list;
                    }

                    if (!list.Contains(constructor))
                    {
                        list.Add(constructor);
                    }
                }
            }

            foreach (var kvp in caseMap.OrderBy(static x => x.Key))
            {
                writer.Line($"case {kvp.Key}:");
                writer.OpenBrace();
                if (kvp.Value.Count == 1)
                {
                    EmitConstructorBody(writer, typeInfo, kvp.Value[0], stackOffset, threadAvailable: true);
                }
                else
                {
                    var orderedConstructors = kvp.Value
                        .OrderBy(static constructor => constructor.Parameters.Count)
                        .ToList();

                    writer.Code("""
                        var __bestMatch = -1;
                        var __bestCost = int.MaxValue;
                        var __isCompatibleCount = 0;
                        """);

                    for (var constructorIndex = 0; constructorIndex < orderedConstructors.Count; constructorIndex++)
                    {
                        var constructor = orderedConstructors[constructorIndex];
                        var fixedParameters = constructor.Parameters.Where(static parameter => !parameter.IsParams).ToList();
                        var paramsParameter = constructor.Parameters.Count > 0 && constructor.Parameters[constructor.Parameters.Count - 1].IsParams
                            ? constructor.Parameters[constructor.Parameters.Count - 1]
                            : null;

                        writer.Line("{");
                        writer.Indent();
                        writer.Code("""
                            var __candidateCost = 0;
                            var __isCompatible = true;
                            """);

                        for (var argumentIndex = 0; argumentIndex < kvp.Key; argumentIndex++)
                        {
                            LuaParameterInfo parameter;
                            var isParamsElement = false;
                            if (argumentIndex < fixedParameters.Count)
                            {
                                parameter = fixedParameters[argumentIndex];
                            }
                            else if (paramsParameter != null)
                            {
                                parameter = paramsParameter;
                                isParamsElement = true;
                            }
                            else
                            {
                                continue;
                            }

                            writer.Code($$"""
                                if (__isCompatible)
                                {
                                    var __parameterCost = {{EmitCompatibilityCostExpression(typeInfo, parameter, stackOffset + argumentIndex, isParamsElement)}};
                                    if (__parameterCost < 0)
                                    {
                                        __isCompatible = false;
                                    }
                                    else
                                    {
                                        __candidateCost += __parameterCost;
                                    }
                                }
                                """);
                        }

                        writer.Code($$"""
                            if (__isCompatible)
                            {
                                __isCompatibleCount++;
                                if (__candidateCost < __bestCost)
                                {
                                    __bestMatch = {{constructorIndex}};
                                    __bestCost = __candidateCost;
                                }
                            }
                            """);

                        writer.Dedent();
                        writer.Line("}");
                        writer.Line();
                    }

                    writer.Line("switch (__bestMatch)");
                    writer.OpenBrace();
                    for (var constructorIndex = 0; constructorIndex < orderedConstructors.Count; constructorIndex++)
                    {
                        writer.Line($"case {constructorIndex}:");
                        writer.OpenBrace();
                        EmitConstructorBody(writer, typeInfo, orderedConstructors[constructorIndex], stackOffset, threadAvailable: true);
                        writer.CloseBrace();
                    }

                    writer.Code($$"""
                        default:
                        {
                            return luaL_error(L, "{{typeInfo.Name}}: incompatible argument types");
                        }
                        """);

                    writer.CloseBrace();
                }

                writer.CloseBrace();
            }

            var paramsConstructor = typeInfo.Constructors.FirstOrDefault(static constructor => constructor.Parameters.Any(static parameter => parameter.IsParams));
            writer.Line("default:");
            if (paramsConstructor != null)
            {
                writer.OpenBrace();
                EmitConstructorBody(writer, typeInfo, paramsConstructor, stackOffset, threadAvailable: true);
                writer.CloseBrace();
            }
            else
            {
                writer.Code($$"""
                    {
                        return luaL_error(L, "{{typeInfo.Name}}: invalid number of arguments");
                    }
                    """);
            }

            writer.CloseBrace();
        }

        writer.CloseBrace();
    }

    private static void EmitConstructorBody(CodeWriter writer, LuaTypeInfo typeInfo, LuaConstructorInfo constructor, int stackOffset, bool threadAvailable = false)
    {
        var needsTypeValidationThread = (typeInfo.ArgumentValidation & ArgValidationType) != 0
            && constructor.Parameters.Any(static parameter => !parameter.IsOptional && !parameter.IsLuaString && !parameter.IsParams);

        var needsThread = constructor.Parameters.Any(parameter =>
                UseMarshaledInterop(typeInfo, parameter.ValueKind)
                || (parameter.IsParams && UseMarshaledInterop(typeInfo, parameter.ParamsElementKind)))
            || needsTypeValidationThread;

        if (needsThread && !threadAvailable)
        {
            writer.Line("using var thread = LuaThread.FromExtraSpace(L);");
        }

        for (var i = 0; i < constructor.Parameters.Count; i++)
        {
            var parameter = constructor.Parameters[i];
            var stackIndex = i + stackOffset;

            if (parameter.IsParams)
            {
                writer.Code($$"""
                    var __paramsCount = lua_gettop(L) - {{stackIndex}} + 1;
                    var {{parameter.Name}} = new {{parameter.ParamsElementTypeName}}[__paramsCount > 0 ? __paramsCount : 0];
                    for (var __pi = 0; __pi < __paramsCount; __pi++)
                    {
                    """);

                if (UseMarshaledInterop(typeInfo, parameter.ParamsElementKind))
                {
                    writer.Line($"{parameter.Name}[__pi] = thread.Stack[{stackIndex} + __pi].GetValue<{parameter.ParamsElementTypeName}>((global::Laylua.Marshaling.LuaAllowedValueConversions) BindingConversions){GetReadNullForgivingSuffix(parameter.IsParamsElementNonNullableReference)};");
                }
                else
                {
                    writer.Line($"{parameter.Name}[__pi] = {EmitReadExpression(typeInfo, parameter.ParamsElementKind, parameter.ParamsElementTypeName!, $"{stackIndex} + __pi")};");
                }

                writer.CloseBrace();
                writer.Line();
                continue;
            }

            if (parameter.IsOptional)
            {
                var defaultValue = parameter.DefaultValue ?? $"default({parameter.TypeName})";
                var rangeCheck = GetRangeCheckForParameter(typeInfo, parameter);
                if (rangeCheck != null)
                {
                    writer.Code($$"""
                        {{parameter.TypeName}} {{parameter.Name}};
                        if (lua_gettop(L) >= {{stackIndex}})
                        {
                        """);

                    EmitRangeCheckedRead(writer, typeInfo, parameter, typeInfo.Name, stackIndex, rangeCheck.Value, declareVariable: false);
                    writer.Code($$"""
                        }
                        else
                        {
                            {{parameter.Name}} = {{defaultValue}};
                        }
                        """);
                }
                else if (parameter.IsLuaString)
                {
                    writer.Code($"""
                        {parameter.TypeName} {parameter.Name} = lua_gettop(L) >= {stackIndex}
                            ? {EmitLuaStringReadExpression(parameter.TypeName, stackIndex)}
                            : {defaultValue};
                        """);
                }
                else if (UseMarshaledInterop(typeInfo, parameter.ValueKind))
                {
                    writer.Code($"""
                        {parameter.TypeName} {parameter.Name} = lua_gettop(L) >= {stackIndex}
                            ? thread.Stack[{stackIndex}].GetValue<{parameter.TypeName}>((global::Laylua.Marshaling.LuaAllowedValueConversions) BindingConversions){GetReadNullForgivingSuffix(parameter.IsNonNullableReference)}
                            : {defaultValue};
                        """);
                }
                else
                {
                    writer.Code($"""
                        {parameter.TypeName} {parameter.Name} = lua_gettop(L) >= {stackIndex}
                            ? {EmitReadExpression(typeInfo, parameter.ValueKind, parameter.TypeName, stackIndex)}
                            : {defaultValue};
                        """);
                }

                writer.Line();
            }
            else
            {
                var rangeCheck = GetRangeCheckForParameter(typeInfo, parameter);
                if (parameter.IsLuaString)
                {
                    writer.Line($"var {parameter.Name} = {EmitLuaStringReadExpression(parameter.TypeName, stackIndex)};");
                }
                else if (UseMarshaledInterop(typeInfo, parameter.ValueKind))
                {
                    EmitNilCheck(writer, typeInfo, parameter, typeInfo.Name, stackIndex);
                    EmitTypeCheck(writer, typeInfo, parameter, typeInfo.Name, stackIndex);
                    writer.Line($"var {parameter.Name} = thread.Stack[{stackIndex}].GetValue<{parameter.TypeName}>((global::Laylua.Marshaling.LuaAllowedValueConversions) BindingConversions){GetReadNullForgivingSuffix(parameter.IsNonNullableReference)};");
                }
                else
                {
                    EmitNilCheck(writer, typeInfo, parameter, typeInfo.Name, stackIndex);
                    EmitTypeCheck(writer, typeInfo, parameter, typeInfo.Name, stackIndex);
                    if (rangeCheck != null)
                    {
                        EmitRangeCheckedRead(writer, typeInfo, parameter, typeInfo.Name, stackIndex, rangeCheck.Value);
                    }
                    else
                    {
                        writer.Line($"var {parameter.Name} = {EmitCheckedReadExpression(typeInfo, parameter.ValueKind, parameter.TypeName, stackIndex)};");
                    }
                }

                writer.Line();
            }
        }

        var luaRefParams = constructor.Parameters.Where(static p => p.IsLuaReference).ToList();
        if (luaRefParams.Count > 0)
        {
            writer.Line("try");
            writer.OpenBrace();
        }

        var arguments = string.Join(", ", constructor.Parameters.Select(static parameter => parameter.Name));
        writer.Code($"""
            var instance = new {typeInfo.Name}({arguments});
            Push(L, instance);
            return 1;
            """);

        if (luaRefParams.Count > 0)
        {
            writer.CloseBrace();
            writer.Line("finally");
            writer.OpenBrace();
            foreach (var param in luaRefParams)
            {
                writer.Line($"LuaReference.Dispose({param.Name});");
            }

            writer.CloseBrace();
        }
    }

    private static void EmitDefinitionGcCallback(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        writer.Code($$"""
            private static int __DefinitionGc(lua_State* L)
            {
                var ud = (nint*) lua_touserdata(L, 1);
            """);

        var hasEvents = typeInfo.Events.Count > 0 || typeInfo.StaticEvents.Count > 0;
        if (hasEvents)
        {
            writer.Line("Exception? cleanupException = null;");
        }

        if (hasEvents)
        {
            writer.Code("""
                if (_eventTracker != null && ud != null && ud[0] != 0)
                {
                    try
                    {
                        _eventTracker.CleanupAll(L, (nint) ud);
                    }
                    catch (Exception ex)
                    {
                        cleanupException = LuaTypeServices.CombineCleanupException(cleanupException, ex);
                    }
                }
                """);
        }

        writer.Line("LuaTypeServices.TryFreeUserData(ud);");
        if (hasEvents)
        {
            writer.Line("LuaTypeServices.ThrowCleanupException(cleanupException);");
            writer.Line();
        }

        writer.Line("return 0;");
        writer.CloseBrace();
    }

    private static void EmitDefinitionIndexCallback(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        writer.Line("private static int __DefinitionIndex(lua_State* L)");
        writer.OpenBrace();

        var switchEntries = new List<IndexSwitchEntry>();

        foreach (var property in typeInfo.StaticProperties.Where(static property => property.CanRead))
        {
            switchEntries.Add(new IndexSwitchEntry(property.LuaName, property, null));
        }

        foreach (var field in typeInfo.StaticFields.Where(static field => field.CanRead))
        {
            switchEntries.Add(new IndexSwitchEntry(field.LuaName, field, null));
        }

        foreach (var method in typeInfo.StaticMethods)
        {
            switchEntries.Add(new IndexSwitchEntry(method.LuaName, null, method));
        }

        foreach (var eventInfo in typeInfo.StaticEvents)
        {
            switchEntries.Add(new IndexSwitchEntry(eventInfo.LuaName, null, null, eventInfo));
        }

        if (typeInfo.EmitFactory)
        {
            switchEntries.Add(new IndexSwitchEntry(typeInfo.FactoryMethodName!, null, null)
            {
                IsFactory = true
            });
        }

        if (switchEntries.Count == 0)
        {
            writer.Line("return 0;");
            writer.CloseBrace();
            return;
        }

        writer.Code("""
            if (lua_type(L, 2) != LuaType.String)
            {
                return 0;
            }

            var key = lua_tostring(L, 2).Span;
            """);

        EmitIndexSwitch(writer, typeInfo, switchEntries, null!, isStatic: true);

        writer.Line();
        EmitUnknownPropertyFallback(writer, typeInfo);
        writer.CloseBrace();
    }

    private static void EmitDefinitionNewIndexCallback(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        writer.Line("private static int __DefinitionNewIndex(lua_State* L)");
        writer.OpenBrace();

        var writableMembers = typeInfo.StaticProperties.Where(static property => property.CanWrite)
            .Concat(typeInfo.StaticFields.Where(static field => field.CanWrite))
            .ToList();

        var readOnlyMembers = typeInfo.StaticProperties.Where(static property => property.CanRead && !property.CanWrite)
            .Concat(typeInfo.StaticFields.Where(static field => field.CanRead && !field.CanWrite))
            .ToList();

        if (writableMembers.Count == 0 && readOnlyMembers.Count == 0)
        {
            writer.Line("return 0;");
            writer.CloseBrace();
            return;
        }

        writer.Code("""
            if (lua_type(L, 2) != LuaType.String)
            {
                return 0;
            }

            var key = lua_tostring(L, 2).Span;
            """);

        if (writableMembers.Count > 0)
        {
            EmitNewIndexSwitch(writer, typeInfo, writableMembers, null!, isStatic: true);
        }

        if (readOnlyMembers.Count > 0)
        {
            EmitReadOnlyErrorSwitch(writer, typeInfo.Name, readOnlyMembers);
        }

        writer.Line();
        EmitUnknownPropertyFallback(writer, typeInfo);
        writer.CloseBrace();
    }
}
