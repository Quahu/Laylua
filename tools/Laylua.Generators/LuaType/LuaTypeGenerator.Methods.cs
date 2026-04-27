using System.Collections.Generic;
using System.Linq;

namespace Laylua.Generators;

public partial class LuaTypeGenerator
{
    private static void EmitToStringCallback(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        writer.Line("private static int __ToString(lua_State* L)");
        writer.OpenBrace();

        writer.Line("if (!LuaTypeServices.TryGetUserDataTarget((nint*) lua_touserdata(L, 1), out var __target))");
        writer.OpenBrace();
        writer.Line($"lua_pushstring(L, \"{typeInfo.Name}\"u8);");
        writer.Line("return 1;");
        writer.CloseBrace();
        writer.Line();

        if (typeInfo.IsValueType)
        {
            writer.Line($"if (__target is not {typeInfo.Name})");
            writer.OpenBrace();
            writer.Line($"return luaL_error(L, \"{typeInfo.Name}: 'self' is not a valid {typeInfo.Name} instance\");");
            writer.CloseBrace();
            writer.Line();
            writer.Line($"ref var self = ref Unsafe.Unbox<{typeInfo.Name}>(__target!);");
        }
        else
        {
            writer.Line($"if (__target is not {typeInfo.Name} self)");
            writer.OpenBrace();
            writer.Line($"return luaL_error(L, \"{typeInfo.Name}: 'self' is not a valid {typeInfo.Name} instance\");");
            writer.CloseBrace();
        }

        writer.Line();
        writer.Line("lua_pushstring(L, self.ToString());");
        writer.Line("return 1;");
        writer.CloseBrace();
    }

    private static void EmitDefinitionToStringCallback(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        writer.Line("private static int __DefinitionToString(lua_State* L)");
        writer.OpenBrace();
        writer.Line($"lua_pushstring(L, \"{typeInfo.Name}\"u8);");
        writer.Line("return 1;");
        writer.CloseBrace();
    }

    private static void EmitMethodCallback(
        CodeWriter writer,
        LuaTypeInfo typeInfo,
        LuaMethodInfo method,
        bool isStatic,
        string? callbackNameOverride = null)
    {
        var callbackName = callbackNameOverride
            ?? (isStatic
                ? $"__Definition_{GetMethodGeneratedIdentifier(method)}"
                : $"__Instance_{GetMethodGeneratedIdentifier(method)}");

        writer.Line($"private static int {callbackName}(lua_State* L)");
        writer.OpenBrace();

        if (method.Overloads != null && method.Overloads.Count > 0)
        {
            // Overloaded method - dispatch on argument count
            var allOverloads = new List<LuaMethodInfo> { method };
            allOverloads.AddRange(method.Overloads);

            int stackOffset;
            if (isStatic)
            {
                stackOffset = 1;
            }
            else
            {
                EmitSelfVariable(writer, typeInfo);
                stackOffset = 2;
            }

            writer.Line("using var thread = LuaThread.FromExtraSpace(L);");

            var __argOffset = stackOffset - 1;
            writer.Line(__argOffset > 0
                ? $"var __argCount = lua_gettop(L) - {__argOffset};"
                : "var __argCount = lua_gettop(L);");

            writer.Line("switch (__argCount)");
            writer.OpenBrace();

            var caseMap = new Dictionary<int, List<LuaMethodInfo>>();
            foreach (var overload in allOverloads)
            {
                var stackParams = overload.Parameters.Where(p => !p.IsCallingThread && p.RefKind != RefKindOut).ToList();
                var hasParams = stackParams.Any(p => p.IsParams);
                var fixedParams = stackParams.Where(p => !p.IsParams).ToList();
                var requiredCount = fixedParams.Count(p => !p.IsOptional);
                var maxCount = hasParams ? fixedParams.Count : stackParams.Count;
                for (var i = requiredCount; i <= maxCount; i++)
                {
                    if (!caseMap.TryGetValue(i, out var list))
                    {
                        list = new List<LuaMethodInfo>();
                        caseMap[i] = list;
                    }

                    if (!list.Contains(overload))
                    {
                        list.Add(overload);
                    }
                }
            }

            foreach (var kvp in caseMap.OrderBy(static x => x.Key))
            {
                writer.Line($"case {kvp.Key}:");
                writer.OpenBrace();

                if (kvp.Value.Count == 1)
                {
                    EmitOverloadCaseBody(writer, typeInfo, kvp.Value[0], isStatic, stackOffset);
                }
                else
                {
                    EmitTypeDispatchedOverloads(writer, typeInfo, kvp.Value, isStatic, stackOffset, method.LuaName, kvp.Key);
                }

                writer.CloseBrace();
            }

            var paramsOverload = allOverloads.FirstOrDefault(o => o.Parameters.Any(p => p.IsParams));
            writer.Line("default:");
            if (paramsOverload != null)
            {
                writer.OpenBrace();
                EmitOverloadCaseBody(writer, typeInfo, paramsOverload, isStatic, stackOffset);
                writer.CloseBrace();
            }
            else
            {
                writer.OpenBrace();
                writer.Line($"return luaL_error(L, \"{method.LuaName}: unexpected number of arguments\");");
                writer.CloseBrace();
            }

            writer.CloseBrace();
        }
        else
        {
            // Single method - existing behavior
            int stackOffset;
            if (isStatic)
            {
                stackOffset = 1;
            }
            else
            {
                EmitSelfVariable(writer, typeInfo);
                stackOffset = 2;
            }

            EmitSingleMethodBody(writer, typeInfo, method, isStatic, stackOffset);
        }

        writer.CloseBrace();
    }

    private static void EmitTypeCallCallback(CodeWriter writer, LuaTypeInfo typeInfo)
    {
        var method = typeInfo.TypeCallMethods[0];
        EmitMethodCallback(writer, typeInfo, method, isStatic: false, callbackNameOverride: "__TypeCall");
    }

    private static void EmitSingleMethodBody(
        CodeWriter writer,
        LuaTypeInfo typeInfo,
        LuaMethodInfo method,
        bool isStatic,
        int stackOffset)
    {
        var hasCallingThread = method.Parameters.Any(p => p.IsCallingThread);
        var stackParams = method.Parameters.Where(p => !p.IsCallingThread && p.RefKind != RefKindOut).ToList();
        var needsTypeValidationThread = (typeInfo.ArgumentValidation & ArgValidationType) != 0
            && stackParams.Any(static parameter => !parameter.IsOptional && !parameter.IsLuaString);
        var needsThread = hasCallingThread
            || method.Parameters.Any(p => !p.IsCallingThread && (UseMarshaledInterop(typeInfo, p.ValueKind) || (p.IsParams && UseMarshaledInterop(typeInfo, p.ParamsElementKind))))
            || (!method.ReturnsVoid && !method.ReturnsRawCount && UseMarshaledInterop(typeInfo, method.ReturnValueKind))
            || method.Parameters.Any(p => (p.RefKind == RefKindOut || p.RefKind == RefKindRef) && UseMarshaledInterop(typeInfo, p.ValueKind))
            || needsTypeValidationThread;

        if (needsThread)
        {
            writer.Line("using var thread = LuaThread.FromExtraSpace(L);");
        }

        var requiredCount = stackParams.Count(p => !p.IsOptional && !p.IsParams);
        if (requiredCount > 0)
        {
            var __argOffset = stackOffset - 1;
            writer.Line(__argOffset > 0
                ? $"var __argCount = lua_gettop(L) - {__argOffset};"
                : "var __argCount = lua_gettop(L);");

            writer.Line($"if (__argCount < {requiredCount})");
            writer.OpenBrace();
            writer.Line($"return luaL_error(L, \"{method.LuaName}: expected at least {requiredCount} argument(s)\");");
            writer.CloseBrace();
            writer.Line();
        }

        EmitParameterReads(writer, typeInfo, method, stackOffset);

        var outParams = method.Parameters.Where(p => p.RefKind == RefKindOut || p.RefKind == RefKindRef).ToList();
        var arguments = string.Join(", ", method.Parameters.Select(static parameter =>
        {
            if (parameter.RefKind == RefKindOut)
            {
                return $"out var {parameter.Name}";
            }

            if (parameter.RefKind == RefKindRef)
            {
                return $"ref {parameter.Name}";
            }

            return parameter.Name;
        }));

        var callExpression = isStatic
            ? $"{typeInfo.Name}.{method.ClrName}({arguments})"
            : $"self.{method.ClrName}({arguments})";

        var luaRefParams = method.Parameters.Where(p => p.IsLuaReference && !p.IsCallingThread).ToList();
        if (luaRefParams.Count > 0)
        {
            writer.Line("try");
            writer.OpenBrace();
        }

        if (method.IsObsolete)
        {
            writer.DisableWarning("CS0618");
        }

        var returnCount = outParams.Count;
        var rawCountVariableName = "__rawCount";
        var capturesRawCount = false;
        if (method.ReturnsRawCount)
        {
            if (outParams.Count > 0)
            {
                writer.Line($"var {rawCountVariableName} = checked((int) ({callExpression}));");
                capturesRawCount = true;
            }
            else
            {
                writer.Line($"return checked((int) ({callExpression}));");
            }
        }
        else if (method.ReturnsVoid)
        {
            writer.Line($"{callExpression};");
        }
        else
        {
            writer.Line($"var __retval = {callExpression};");
            EmitPushExpression(writer, typeInfo, method.ReturnValueKind, method.ReturnType, "__retval", threadAvailable: needsThread);
            returnCount++;
        }

        if (method.IsObsolete)
        {
            writer.RestoreWarning("CS0618");
        }

        foreach (var outParam in outParams)
        {
            EmitPushExpression(writer, typeInfo, outParam.ValueKind, outParam.TypeName, outParam.Name, threadAvailable: needsThread);
        }

        if (capturesRawCount)
        {
            writer.Line($"return checked({rawCountVariableName} + {returnCount});");
        }
        else if (!method.ReturnsRawCount || outParams.Count > 0)
        {
            writer.Line($"return {returnCount};");
        }

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

    private static void EmitOverloadCaseBody(
        CodeWriter writer,
        LuaTypeInfo typeInfo,
        LuaMethodInfo method,
        bool isStatic,
        int stackOffset)
    {
        EmitParameterReads(writer, typeInfo, method, stackOffset);

        var outParams = method.Parameters.Where(p => p.RefKind == RefKindOut || p.RefKind == RefKindRef).ToList();
        var arguments = string.Join(", ", method.Parameters.Select(static parameter =>
        {
            if (parameter.RefKind == RefKindOut)
            {
                return $"out var {parameter.Name}";
            }

            if (parameter.RefKind == RefKindRef)
            {
                return $"ref {parameter.Name}";
            }

            return parameter.Name;
        }));

        var callExpression = isStatic
            ? $"{typeInfo.Name}.{method.ClrName}({arguments})"
            : $"self.{method.ClrName}({arguments})";

        var luaRefParams = method.Parameters.Where(p => p.IsLuaReference && !p.IsCallingThread).ToList();
        if (luaRefParams.Count > 0)
        {
            writer.Line("try");
            writer.OpenBrace();
        }

        if (method.IsObsolete)
        {
            writer.DisableWarning("CS0618");
        }

        var returnCount = outParams.Count;
        var rawCountVariableName = "__rawCount";
        var capturesRawCount = false;
        if (method.ReturnsRawCount)
        {
            if (outParams.Count > 0)
            {
                writer.Line($"var {rawCountVariableName} = checked((int) ({callExpression}));");
                capturesRawCount = true;
            }
            else
            {
                writer.Line($"return checked((int) ({callExpression}));");
            }
        }
        else if (method.ReturnsVoid)
        {
            writer.Line($"{callExpression};");
        }
        else
        {
            writer.Line($"var __retval = {callExpression};");
            EmitPushExpression(writer, typeInfo, method.ReturnValueKind, method.ReturnType, "__retval", threadAvailable: true);
            returnCount++;
        }

        if (method.IsObsolete)
        {
            writer.RestoreWarning("CS0618");
        }

        foreach (var outParam in outParams)
        {
            EmitPushExpression(writer, typeInfo, outParam.ValueKind, outParam.TypeName, outParam.Name, threadAvailable: true);
        }

        if (capturesRawCount)
        {
            writer.Line($"return checked({rawCountVariableName} + {returnCount});");
        }
        else if (!method.ReturnsRawCount || outParams.Count > 0)
        {
            writer.Line($"return {returnCount};");
        }

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

    /// <summary>
    ///     Emits type-based dispatch for overloads with the same parameter count.
    ///     Checks Lua runtime types (boolean, integer, number, string) to pick the right overload.
    ///     Falls back to the least specific overload (e.g., one with Marshaled/object params).
    /// </summary>
    private static void EmitTypeDispatchedOverloads(
        CodeWriter writer,
        LuaTypeInfo typeInfo,
        List<LuaMethodInfo> overloads,
        bool isStatic,
        int stackOffset,
        string luaName,
        int argCount)
    {
        var dispatchIndex = FindDispatchParameterIndex(overloads);
        var sorted = dispatchIndex >= 0
            ? SortOverloadsBySpecificity(overloads, dispatchIndex)
            : overloads;

        writer.Line("var __bestMatch = -1;");
        writer.Line("var __bestCost = int.MaxValue;");
        writer.Line("var __compatibleCount = 0;");
        writer.Line();

        for (var i = 0; i < sorted.Count; i++)
        {
            var overload = sorted[i];
            var stackParameters = overload.Parameters
                .Where(static parameter => !parameter.IsCallingThread && parameter.RefKind != RefKindOut)
                .ToList();
            var fixedParameters = stackParameters.Where(static parameter => !parameter.IsParams).ToList();
            var paramsParameter = stackParameters.Count > 0 && stackParameters[stackParameters.Count - 1].IsParams
                ? stackParameters[stackParameters.Count - 1]
                : null;

            writer.Line("{");
            writer.Indent();
            writer.Line("var __candidateCost = 0;");
            writer.Line("var __compatible = true;");

            for (var argumentIndex = 0; argumentIndex < argCount; argumentIndex++)
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

                writer.Line("if (__compatible)");
                writer.OpenBrace();
                writer.Line($"var __parameterCost = {EmitCompatibilityCostExpression(typeInfo, parameter, stackOffset + argumentIndex, isParamsElement)};");
                writer.Line("if (__parameterCost < 0)");
                writer.OpenBrace();
                writer.Line("__compatible = false;");
                writer.CloseBrace();
                writer.Line("else");
                writer.OpenBrace();
                writer.Line("__candidateCost += __parameterCost;");
                writer.CloseBrace();
                writer.CloseBrace();
            }

            writer.Line("if (__compatible)");
            writer.OpenBrace();
            writer.Line("__compatibleCount++;");
            writer.Line("if (__candidateCost < __bestCost)");
            writer.OpenBrace();
            writer.Line($"__bestMatch = {i};");
            writer.Line("__bestCost = __candidateCost;");
            writer.CloseBrace();
            writer.CloseBrace();
            writer.Dedent();
            writer.Line("}");
            writer.Line();
        }

        if (typeInfo.OverloadResolution == OverloadError)
        {
            writer.Line("if (__compatibleCount > 1)");
            writer.OpenBrace();
            writer.Line($"return luaL_error(L, \"{luaName}: ambiguous overload resolution\");");
            writer.CloseBrace();
            writer.Line();
        }

        writer.Line("switch (__bestMatch)");
        writer.OpenBrace();
        for (var i = 0; i < sorted.Count; i++)
        {
            writer.Line($"case {i}:");
            writer.OpenBrace();
            EmitOverloadCaseBody(writer, typeInfo, sorted[i], isStatic, stackOffset);
            writer.CloseBrace();
        }

        writer.Line("default:");
        writer.OpenBrace();
        writer.Line($"return luaL_error(L, \"{luaName}: incompatible argument types\");");
        writer.CloseBrace();
        writer.CloseBrace();
    }

    /// <summary>
    ///     Finds the first parameter index where overloads differ in <see cref="LuaValueKind"/>.
    /// </summary>
    private static int FindDispatchParameterIndex(List<LuaMethodInfo> overloads)
    {
        var stackParamsPerOverload = overloads
            .Select(static overload => overload.Parameters
                .Where(static parameter => !parameter.IsCallingThread && parameter.RefKind != RefKindOut)
                .ToList())
            .ToList();

        var paramCount = stackParamsPerOverload.Min(static parameters => parameters.Count);
        for (var i = 0; i < paramCount; i++)
        {
            var firstKind = stackParamsPerOverload[0][i].ValueKind;
            if (stackParamsPerOverload.Any(parameters => parameters[i].ValueKind != firstKind))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    ///     Sorts overloads by parameter specificity at the dispatch index.
    ///     Order: Boolean > Integer > Number > String > Marshaled.
    /// </summary>
    private static List<LuaMethodInfo> SortOverloadsBySpecificity(List<LuaMethodInfo> overloads, int dispatchIndex)
    {
        return overloads
            .OrderBy(overload =>
            {
                var stackParameters = overload.Parameters
                    .Where(static parameter => !parameter.IsCallingThread && parameter.RefKind != RefKindOut)
                    .ToList();

                return GetSpecificityRank(stackParameters[dispatchIndex].ValueKind);
            })
            .ToList();
    }

    private static int GetSpecificityRank(LuaValueKind kind)
    {
        return kind switch
        {
            LuaValueKind.Boolean => 0,
            LuaValueKind.Integer => 1,
            LuaValueKind.Number => 2,
            LuaValueKind.String => 3,
            LuaValueKind.Marshaled => 4,
            _ => 5,
        };
    }

    /// <summary>
    ///     Emits parameter reading code for a method callback, including nil checks for non-nullable reference parameters.
    /// </summary>
    private static void EmitParameterReads(
        CodeWriter writer,
        LuaTypeInfo typeInfo,
        LuaMethodInfo method,
        int stackOffset)
    {
        var stackIndex = stackOffset;
        for (var i = 0; i < method.Parameters.Count; i++)
        {
            var parameter = method.Parameters[i];

            if (parameter.IsCallingThread)
            {
                writer.Line($"var {parameter.Name} = thread;");
                continue;
            }

            if (parameter.RefKind == RefKindOut)
            {
                continue;
            }

            if (parameter.IsParams)
            {
                writer.Line($"var __paramsCount = lua_gettop(L) - {stackIndex} + 1;");
                writer.Line($"var {parameter.Name} = new {parameter.ParamsElementTypeName}[__paramsCount > 0 ? __paramsCount : 0];");
                writer.Line("for (var __pi = 0; __pi < __paramsCount; __pi++)");
                writer.OpenBrace();

                if (UseMarshaledInterop(typeInfo, parameter.ParamsElementKind))
                {
                    writer.Line($"{parameter.Name}[__pi] = thread.Stack[{stackIndex} + __pi].GetValue<{parameter.ParamsElementTypeName}>((global::Laylua.Marshaling.LuaAllowedValueConversions) BindingConversions){GetReadNullForgivingSuffix(parameter.IsParamsElementNonNullableReference)};");
                }
                else
                {
                    writer.Line($"{parameter.Name}[__pi] = {EmitReadExpression(typeInfo, parameter.ParamsElementKind, parameter.ParamsElementTypeName!, $"{stackIndex} + __pi")};");
                }

                writer.CloseBrace();
                continue;
            }

            if (parameter.IsOptional)
            {
                var rangeCheck = GetRangeCheckForParameter(typeInfo, parameter);
                if (rangeCheck != null)
                {
                    // Range-checked optional parameter needs multi-line form
                    writer.Line($"{parameter.TypeName} {parameter.Name};");
                    writer.Line($"if (lua_gettop(L) >= {stackIndex})");
                    writer.OpenBrace();
                    EmitRangeCheckedRead(writer, typeInfo, parameter, method.LuaName, stackIndex, rangeCheck.Value, declareVariable: false);
                    writer.CloseBrace();
                    writer.Line("else");
                    writer.OpenBrace();
                    writer.Line($"{parameter.Name} = {parameter.DefaultValue ?? $"default({parameter.TypeName})"};");
                    writer.CloseBrace();
                }
                else if (UseMarshaledInterop(typeInfo, parameter.ValueKind))
                {
                    writer.Line($"var {parameter.Name} = lua_gettop(L) >= {stackIndex}");
                    writer.Line($"    ? thread.Stack[{stackIndex}].GetValue<{parameter.TypeName}>((global::Laylua.Marshaling.LuaAllowedValueConversions) BindingConversions){GetReadNullForgivingSuffix(parameter.IsNonNullableReference)}");
                    writer.Line($"    : {parameter.DefaultValue ?? $"default({parameter.TypeName})"};");
                }
                else
                {
                    writer.Line($"var {parameter.Name} = lua_gettop(L) >= {stackIndex}");
                    writer.Line($"    ? {EmitReadExpression(typeInfo, parameter.ValueKind, parameter.TypeName, stackIndex)}");
                    writer.Line($"    : {parameter.DefaultValue ?? $"default({parameter.TypeName})"};");
                }

                writer.Line();
            }
            else
            {
                if (parameter.IsLuaString)
                {
                    writer.Line($"var {parameter.Name} = {EmitLuaStringReadExpression(parameter.TypeName, stackIndex)};");
                }
                else
                {
                    EmitNilCheck(writer, typeInfo, parameter, method.LuaName, stackIndex);
                    EmitTypeCheck(writer, typeInfo, parameter, method.LuaName, stackIndex);

                    // For nullability validation: nullable reference params use unchecked reads (accept nil)
                    var useUncheckedRead = (typeInfo.ArgumentValidation & ArgValidationNullability) != 0
                        && !parameter.IsNonNullableReference
                        && parameter.ValueKind == LuaValueKind.String;

                    // For value types when Nullability is NOT set: nil -> default(T)
                    var useNilDefault = parameter.IsValueType
                        && (typeInfo.ArgumentValidation & ArgValidationNullability) == 0;

                    var rangeCheck = GetRangeCheckForParameter(typeInfo, parameter);
                    var useImplicitConversionRead = RequiresImplicitConversionRead(typeInfo, parameter.ValueKind, parameter.TypeName);

                    if (UseMarshaledInterop(typeInfo, parameter.ValueKind))
                    {
                        if (useNilDefault)
                        {
                            writer.Line($"var {parameter.Name} = lua_type(L, {stackIndex}) == LuaType.Nil");
                            writer.Line($"    ? default({parameter.TypeName})");
                            writer.Line($"    : thread.Stack[{stackIndex}].GetValue<{parameter.TypeName}>((global::Laylua.Marshaling.LuaAllowedValueConversions) BindingConversions){GetReadNullForgivingSuffix(parameter.IsNonNullableReference)};");
                        }
                        else
                        {
                            writer.Line($"var {parameter.Name} = thread.Stack[{stackIndex}].GetValue<{parameter.TypeName}>((global::Laylua.Marshaling.LuaAllowedValueConversions) BindingConversions){GetReadNullForgivingSuffix(parameter.IsNonNullableReference)};");
                        }
                    }
                    else if (rangeCheck != null)
                    {
                        if (useNilDefault)
                        {
                            writer.Line($"{parameter.TypeName} {parameter.Name};");
                            writer.Line($"if (lua_type(L, {stackIndex}) == LuaType.Nil)");
                            writer.OpenBrace();
                            writer.Line($"{parameter.Name} = default;");
                            writer.CloseBrace();
                            writer.Line("else");
                            writer.OpenBrace();
                            EmitRangeCheckedRead(writer, typeInfo, parameter, method.LuaName, stackIndex, rangeCheck.Value);
                            writer.CloseBrace();
                        }
                        else
                        {
                            EmitRangeCheckedRead(writer, typeInfo, parameter, method.LuaName, stackIndex, rangeCheck.Value);
                        }
                    }
                    else if (useUncheckedRead)
                    {
                        writer.Line($"var {parameter.Name} = {EmitReadExpression(typeInfo, parameter.ValueKind, parameter.TypeName, stackIndex)};");
                    }
                    else if (useNilDefault)
                    {
                        writer.Line($"var {parameter.Name} = lua_type(L, {stackIndex}) == LuaType.Nil");
                        writer.Line($"    ? default({parameter.TypeName})");
                        writer.Line($"    : {(useImplicitConversionRead ? EmitImplicitConversionReadExpression(parameter.TypeName, stackIndex.ToString()) : EmitCheckedReadExpression(typeInfo, parameter.ValueKind, parameter.TypeName, stackIndex))};");
                    }
                    else
                    {
                        if (useImplicitConversionRead)
                        {
                            writer.Line($"var {parameter.Name} = lua_type(L, {stackIndex}) == LuaType.Nil");
                            writer.Line($"    ? {EmitCheckedReadExpression(parameter.ValueKind, parameter.TypeName, stackIndex)}");
                            writer.Line($"    : {EmitImplicitConversionReadExpression(parameter.TypeName, stackIndex.ToString())};");
                        }
                        else
                        {
                            writer.Line($"var {parameter.Name} = {EmitCheckedReadExpression(typeInfo, parameter.ValueKind, parameter.TypeName, stackIndex)};");
                        }
                    }
                }
            }

            stackIndex++;
        }
    }

    /// <summary>
    ///     Emits a nil check for a parameter when nullability validation is enabled.
    ///     For non-nullable reference types: errors on nil.
    ///     For value types: errors on nil.
    /// </summary>
    private static void EmitNilCheck(
        CodeWriter writer,
        LuaTypeInfo typeInfo,
        LuaParameterInfo parameter,
        string methodLuaName,
        int stackIndex)
    {
        if ((typeInfo.ArgumentValidation & ArgValidationNullability) == 0)
        {
            return;
        }

        if (!parameter.IsNonNullableReference && !parameter.IsValueType)
        {
            return;
        }

        writer.Line($"if (lua_type(L, {stackIndex}) == LuaType.Nil)");
        writer.OpenBrace();
        writer.Line($"return luaL_error(L, \"{methodLuaName}: argument '{parameter.Name}' cannot be nil\");");
        writer.CloseBrace();
        writer.Line();
    }

    /// <summary>
    ///     Emits an implicit type compatibility check for a parameter when type validation is enabled.
    ///     Only emitted for non-dispatch parameters when the Lua value is not nil.
    /// </summary>
    private static void EmitTypeCheck(
        CodeWriter writer,
        LuaTypeInfo typeInfo,
        LuaParameterInfo parameter,
        string methodLuaName,
        int stackIndex)
    {
        if ((typeInfo.ArgumentValidation & ArgValidationType) == 0)
        {
            return;
        }

        var expectedKind = parameter.ValueKind switch
        {
            LuaValueKind.Boolean => "boolean",
            LuaValueKind.Integer => "number",
            LuaValueKind.Number => "number",
            LuaValueKind.String => "string",
            _ => parameter.TypeName,
        };

        writer.Line($"if (lua_type(L, {stackIndex}) != LuaType.Nil && !LuaTypeServices.IsImplicitlyCompatible(thread, {stackIndex}, typeof({parameter.TypeName}), (LuaAllowedValueConversions) BindingConversions))");
        writer.OpenBrace();
        writer.Line($"return luaL_error(L, \"{methodLuaName}: argument '{parameter.Name}' expected {expectedKind}, got \" + lua_type(L, {stackIndex}));");
        writer.CloseBrace();
        writer.Line();
    }

    private static string EmitCompatibilityCostExpression(
        LuaTypeInfo typeInfo,
        LuaParameterInfo parameter,
        int stackIndex,
        bool isParamsElement = false)
    {
        var targetTypeName = isParamsElement
            ? parameter.ParamsElementTypeName!
            : parameter.TypeName;
        var validateNumericRangeArgument = (typeInfo.ArgumentValidation & ArgValidationRange) != 0
            ? ", validateNumericRange: true"
            : "";
        var compatibilityExpression =
            $"LuaTypeServices.GetImplicitCompatibilityCost(thread, {stackIndex}, typeof({targetTypeName}), (LuaAllowedValueConversions) BindingConversions{validateNumericRangeArgument})";

        if (CanParameterAcceptNil(typeInfo, parameter, isParamsElement))
        {
            return $"lua_type(L, {stackIndex}) == LuaType.Nil ? 0 : {compatibilityExpression}";
        }

        return compatibilityExpression;
    }

    private static bool CanParameterAcceptNil(
        LuaTypeInfo typeInfo,
        LuaParameterInfo parameter,
        bool isParamsElement = false)
    {
        if ((typeInfo.ArgumentValidation & ArgValidationNullability) != 0)
        {
            if (isParamsElement)
            {
                return parameter.ParamsElementKind == LuaValueKind.Marshaled;
            }

            if (parameter.IsNullableValueType)
            {
                return true;
            }

            return !parameter.IsNonNullableReference && !parameter.IsValueType;
        }

        if (parameter.IsValueType)
        {
            return true;
        }

        var valueKind = isParamsElement
            ? parameter.ParamsElementKind
            : parameter.ValueKind;

        return valueKind == LuaValueKind.Marshaled;
    }

    /// <summary>
    ///     Returns the range bounds for a parameter if Range validation is enabled and the type needs narrowing, or null otherwise.
    /// </summary>
    private static (string Min, string Max, string RawType)? GetRangeCheckForParameter(LuaTypeInfo typeInfo, LuaParameterInfo parameter)
    {
        if ((typeInfo.ArgumentValidation & ArgValidationRange) == 0)
        {
            return null;
        }

        if (parameter.ValueKind == LuaValueKind.Integer)
        {
            var bounds = GetIntegerRangeBounds(parameter.TypeName);
            if (bounds != null)
            {
                return (bounds.Value.Min, bounds.Value.Max, "long");
            }
        }
        else if (parameter.ValueKind == LuaValueKind.Number)
        {
            var bounds = GetNumberRangeBounds(parameter.TypeName);
            if (bounds != null)
            {
                return (bounds.Value.Min, bounds.Value.Max, "double");
            }
        }

        return null;
    }

    /// <summary>
    ///     Emits a range-checked numeric parameter read: reads raw value, checks bounds, then casts.
    /// </summary>
    private static void EmitRangeCheckedRead(
        CodeWriter writer,
        LuaTypeInfo typeInfo,
        LuaParameterInfo parameter,
        string methodLuaName,
        int stackIndex,
        (string Min, string Max, string RawType) rangeCheck,
        bool declareVariable = true)
    {
        var rawVar = $"__raw_{parameter.Name}";

        if (rangeCheck.RawType == "long")
        {
            writer.Line($"var {rawVar} = luaL_checkinteger(L, {stackIndex});");
        }
        else
        {
            writer.Line($"var {rawVar} = luaL_checknumber(L, {stackIndex});");
        }

        writer.Line($"if ({rawVar} < {rangeCheck.Min} || {rawVar} > {rangeCheck.Max})");
        writer.OpenBrace();
        writer.Line($"return luaL_error(L, \"{methodLuaName}: argument '{parameter.Name}' is out of range for {parameter.TypeName}\");");
        writer.CloseBrace();
        writer.Line();

        if (declareVariable)
        {
            writer.Line($"var {parameter.Name} = ({parameter.TypeName}) {rawVar};");
        }
        else
        {
            writer.Line($"{parameter.Name} = ({parameter.TypeName}) {rawVar};");
        }
    }
}
