using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Laylua.Generators;

public partial class LuaTypeGenerator
{
    private static readonly SymbolDisplayFormat DiagnosticMethodSignatureFormat = SymbolDisplayFormat.MinimallyQualifiedFormat
        .WithGenericsOptions(SymbolDisplayGenericsOptions.IncludeTypeParameters)
        .WithMemberOptions(SymbolDisplayMemberOptions.IncludeExplicitInterface | SymbolDisplayMemberOptions.IncludeParameters)
        .WithParameterOptions(
            SymbolDisplayParameterOptions.IncludeType
            | SymbolDisplayParameterOptions.IncludeName
            | SymbolDisplayParameterOptions.IncludeParamsRefOut
            | SymbolDisplayParameterOptions.IncludeExtensionThis)
        .WithMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes
            | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private static bool UseMarshaledInterop(LuaTypeInfo typeInfo, LuaValueKind kind)
    {
        if (kind == LuaValueKind.Void)
        {
            return false;
        }

        return (typeInfo.Optimizations & OptimizationPrimitiveMarshalerBypass) == 0
            || kind == LuaValueKind.Marshaled;
    }

    private static LuaValueKind GetInteropKind(LuaTypeInfo typeInfo, LuaValueKind kind)
    {
        return UseMarshaledInterop(typeInfo, kind)
            ? LuaValueKind.Marshaled
            : kind;
    }

    private static void EmitPushExpression(CodeWriter writer, LuaTypeInfo typeInfo, LuaValueKind kind, string typeName, string expression, bool threadAvailable = false, bool scopeThread = true)
    {
        EmitPushExpression(writer, GetInteropKind(typeInfo, kind), typeName, expression, threadAvailable, scopeThread);
    }

    private static void EmitPushExpression(CodeWriter writer, LuaValueKind kind, string typeName, string expression, bool threadAvailable = false, bool scopeThread = true)
    {
        switch (kind)
        {
            case LuaValueKind.Integer:
            {
                writer.Line($"lua_pushinteger(L, (long) {expression});");
                break;
            }
            case LuaValueKind.Number:
            {
                writer.Line($"lua_pushnumber(L, {expression});");
                break;
            }
            case LuaValueKind.String:
            {
                writer.Line($"lua_pushstring(L, {expression});");
                break;
            }
            case LuaValueKind.Boolean:
            {
                writer.Line($"lua_pushboolean(L, {expression});");
                break;
            }
            case LuaValueKind.Marshaled:
            {
                if (threadAvailable)
                {
                    writer.Line($"thread.Stack.Push({expression});");
                }
                else if (scopeThread)
                {
                    writer.Line("{");
                    writer.Indent();
                    writer.Line("using var thread = LuaThread.FromExtraSpace(L);");
                    writer.Line($"thread.Stack.Push({expression});");
                    writer.Dedent();
                    writer.Line("}");
                }
                else
                {
                    writer.Line("using var thread = LuaThread.FromExtraSpace(L);");
                    writer.Line($"thread.Stack.Push({expression});");
                }

                break;
            }
            default:
            {
                writer.Line($"lua_pushnil(L); // Unsupported type: {typeName}");
                break;
            }
        }
    }

    private static string EmitReadExpression(LuaValueKind kind, string typeName, int stackIndex)
    {
        return EmitReadExpression(kind, typeName, stackIndex.ToString());
    }

    private static string EmitReadExpression(LuaTypeInfo typeInfo, LuaValueKind kind, string typeName, int stackIndex)
    {
        return EmitReadExpression(typeInfo, kind, typeName, stackIndex.ToString());
    }

    private static string EmitReadExpression(LuaTypeInfo typeInfo, LuaValueKind kind, string typeName, string stackIndexExpr)
    {
        var interopKind = GetInteropKind(typeInfo, kind);
        if (interopKind == LuaValueKind.Marshaled)
        {
            return $"thread.Stack[{stackIndexExpr}].GetValue<{typeName}>((global::Laylua.Marshaling.LuaAllowedValueConversions) BindingConversions)!";
        }

        if (!CanUseDirectPrimitiveRead(interopKind, typeName))
        {
            return $"LuaTypeServices.GetValue<{typeName}>(L, {stackIndexExpr}, (global::Laylua.Marshaling.LuaAllowedValueConversions) BindingConversions)!";
        }

        return $"({EmitExactReadCondition(interopKind, stackIndexExpr)}"
            + $" ? {EmitDirectReadExpression(interopKind, typeName, stackIndexExpr)}"
            + $" : LuaTypeServices.GetValue<{typeName}>(L, {stackIndexExpr}, (global::Laylua.Marshaling.LuaAllowedValueConversions) BindingConversions)!)";
    }

    private static string GetReadNullForgivingSuffix(bool isNonNullableReference)
    {
        return isNonNullableReference ? "!" : "";
    }

    private static string EmitReadExpression(LuaValueKind kind, string typeName, string stackIndexExpr)
    {
        return kind switch
        {
            LuaValueKind.Integer => EmitDirectReadExpression(kind, typeName, stackIndexExpr),
            LuaValueKind.Number => EmitDirectReadExpression(kind, typeName, stackIndexExpr),
            LuaValueKind.String => EmitDirectReadExpression(kind, typeName, stackIndexExpr),
            LuaValueKind.Boolean => EmitDirectReadExpression(kind, typeName, stackIndexExpr),
            _ => $"default({typeName})! /* Unsupported type */"
        };
    }

    private static string EmitDirectReadExpression(LuaValueKind kind, string typeName, string stackIndexExpr)
    {
        return kind switch
        {
            LuaValueKind.Integer => $"({typeName}) lua_tointeger(L, {stackIndexExpr})",
            LuaValueKind.Number => $"({typeName}) lua_tonumber(L, {stackIndexExpr})",
            LuaValueKind.String => $"lua_tostring(L, {stackIndexExpr}).ToString()!",
            LuaValueKind.Boolean => $"lua_toboolean(L, {stackIndexExpr})",
            _ => $"default({typeName})! /* Unsupported type */"
        };
    }

    private static string EmitExactReadCondition(LuaValueKind kind, string stackIndexExpr)
    {
        return kind switch
        {
            LuaValueKind.Integer => $"lua_type(L, {stackIndexExpr}) == LuaType.Number && lua_isinteger(L, {stackIndexExpr})",
            LuaValueKind.Number => $"lua_type(L, {stackIndexExpr}) == LuaType.Number",
            LuaValueKind.String => $"lua_type(L, {stackIndexExpr}) == LuaType.String",
            LuaValueKind.Boolean => $"lua_type(L, {stackIndexExpr}) == LuaType.Boolean",
            _ => "false"
        };
    }

    /// <summary>
    ///     Returns a C# expression that reads a Lua string and converts it to the target [LuaString] type.
    /// </summary>
    private static string EmitLuaStringReadExpression(string typeName, int stackIndex)
    {
        return typeName switch
        {
            "global::System.ReadOnlySpan<byte>" => $"luaL_checkluastring(L, {stackIndex})",
            "global::System.ReadOnlySpan<char>" => $"luaL_checkstring(L, {stackIndex})!.AsSpan()",
            _ => $"luaL_checkstring(L, {stackIndex}) /* Unsupported [LuaString] type: {typeName} */",
        };
    }

    /// <summary>
    ///     Returns a C# expression that pushes a [LuaString]-attributed member as a Lua string.
    /// </summary>
    private static string EmitLuaStringPushExpression(string memberAccess, string typeName)
    {
        return typeName switch
        {
            "global::System.ReadOnlySpan<byte>" => $"lua_pushstring(L, {memberAccess})",
            "global::System.ReadOnlySpan<char>" => $"lua_pushstring(L, new string({memberAccess}))",
            _ => $"lua_pushnil(L) /* Unsupported [LuaString] type: {typeName} */",
        };
    }

    /// <summary>
    ///     Returns a C# expression that reads AND validates a value from the Lua stack.
    ///     Uses luaL_check* functions which raise Lua errors on type mismatch.
    ///     Boolean uses lua_toboolean directly (Lua truthiness conversion is always valid).
    /// </summary>
    private static string EmitCheckedReadExpression(LuaValueKind kind, string typeName, int stackIndex)
    {
        if (!CanUseDirectPrimitiveRead(kind, typeName))
        {
            return $"LuaTypeServices.GetValue<{typeName}>(L, {stackIndex}, (global::Laylua.Marshaling.LuaAllowedValueConversions) BindingConversions)!";
        }

        return kind switch
        {
            LuaValueKind.Integer => $"({typeName}) luaL_checkinteger(L, {stackIndex})",
            LuaValueKind.Number => $"({typeName}) luaL_checknumber(L, {stackIndex})",
            LuaValueKind.String => $"luaL_checkstring(L, {stackIndex})!",
            LuaValueKind.Boolean => $"lua_toboolean(L, {stackIndex})",
            _ => $"default({typeName})! /* Unsupported type */"
        };
    }

    private static string EmitCheckedReadExpression(LuaTypeInfo typeInfo, LuaValueKind kind, string typeName, int stackIndex)
    {
        var interopKind = GetInteropKind(typeInfo, kind);
        if (interopKind == LuaValueKind.Marshaled)
        {
            return $"thread.Stack[{stackIndex}].GetValue<{typeName}>((global::Laylua.Marshaling.LuaAllowedValueConversions) BindingConversions)!";
        }

        if (!CanUseDirectPrimitiveRead(interopKind, typeName))
        {
            return $"LuaTypeServices.GetValue<{typeName}>(L, {stackIndex}, (global::Laylua.Marshaling.LuaAllowedValueConversions) BindingConversions)!";
        }

        return $"({EmitExactReadCondition(interopKind, stackIndex.ToString())}"
            + $" ? {EmitDirectReadExpression(interopKind, typeName, stackIndex.ToString())}"
            + $" : LuaTypeServices.GetValue<{typeName}>(L, {stackIndex}, (global::Laylua.Marshaling.LuaAllowedValueConversions) BindingConversions)!)";
    }

    private static bool RequiresImplicitConversionRead(LuaTypeInfo typeInfo, LuaValueKind kind, string typeName)
    {
        if (kind == LuaValueKind.String && IsStringTypeName(typeName))
        {
            return true;
        }

        if (kind == LuaValueKind.Integer && !IsBuiltInIntegerTypeName(typeName))
        {
            return true;
        }

        return false;
    }

    private static string EmitImplicitConversionReadExpression(string typeName, string stackIndexExpr)
    {
        return $"LuaTypeServices.GetValue<{typeName}>(L, {stackIndexExpr}, (global::Laylua.Marshaling.LuaAllowedValueConversions) BindingConversions)!";
    }

    private static bool IsStringTypeName(string typeName)
    {
        return typeName is "string" or "global::System.String";
    }

    private static bool IsBuiltInIntegerTypeName(string typeName)
    {
        return typeName is "byte"
            or "sbyte"
            or "short"
            or "ushort"
            or "int"
            or "uint"
            or "long"
            or "ulong"
            or "global::System.Byte"
            or "global::System.SByte"
            or "global::System.Int16"
            or "global::System.UInt16"
            or "global::System.Int32"
            or "global::System.UInt32"
            or "global::System.Int64"
            or "global::System.UInt64";
    }

    private static bool CanUseDirectPrimitiveRead(LuaValueKind kind, string typeName)
    {
        switch (kind)
        {
            case LuaValueKind.Integer:
            {
                return IsBuiltInIntegerTypeName(typeName);
            }
            case LuaValueKind.Number:
            {
                return typeName is "float"
                    or "double"
                    or "global::System.Single"
                    or "global::System.Double";
            }
            case LuaValueKind.String:
            {
                return IsStringTypeName(typeName);
            }
            case LuaValueKind.Boolean:
            {
                return typeName is "bool" or "global::System.Boolean";
            }
            default:
            {
                return false;
            }
        }
    }

    private static string ApplyNamingPolicy(string name, int policy)
    {
        switch (policy)
        {
            case PolicyOriginal:
            {
                return name;
            }
            case PolicyLowerCase:
            {
                return name.ToLowerInvariant();
            }
            case PolicyUpperCase:
            {
                return name.ToUpperInvariant();
            }
            case PolicyCamelCase:
            {
                return ToCamelCase(name);
            }
            case PolicyLowerSnakeCase:
            {
                return ToSnakeCase(name, uppercase: false);
            }
            case PolicyUpperSnakeCase:
            {
                return ToSnakeCase(name, uppercase: true);
            }
            case PolicyPascalCase:
            {
                return ToPascalCase(name);
            }
            default:
            {
                throw new ArgumentOutOfRangeException(nameof(policy), policy, null);
            }
        }
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        if (char.IsLower(name[0]))
        {
            return name;
        }

        var uppercasePrefixLength = 0;
        for (var i = 0; i < name.Length && char.IsUpper(name[i]); i++)
        {
            uppercasePrefixLength++;
        }

        if (uppercasePrefixLength <= 1)
        {
            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        var loweredLength = uppercasePrefixLength < name.Length
            ? uppercasePrefixLength - 1
            : uppercasePrefixLength;

        return name.Substring(0, loweredLength).ToLowerInvariant() + name.Substring(loweredLength);
    }

    private static string ToPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        if (char.IsUpper(name[0]))
        {
            return name;
        }

        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    private static string ToSnakeCase(string name, bool uppercase)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var builder = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var character = name[i];
            if (char.IsUpper(character))
            {
                if (i > 0)
                {
                    var previousIsLower = char.IsLower(name[i - 1]);
                    var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);

                    if (previousIsLower || nextIsLower)
                    {
                        builder.Append('_');
                    }
                }

                builder.Append(uppercase
                    ? char.ToUpperInvariant(character)
                    : char.ToLowerInvariant(character));
            }
            else
            {
                builder.Append(uppercase
                    ? char.ToUpperInvariant(character)
                    : character);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    ///     Emits validated self variable extraction from userdata at the given stack index.
    ///     Validates sentinel and type before casting, returning luaL_error on mismatch.
    /// </summary>
    private static void EmitSelfVariable(CodeWriter writer, LuaTypeInfo typeInfo, int stackIndex = 1)
    {
        writer.Line($"if (!LuaTypeServices.TryGetUserDataTarget((nint*) lua_touserdata(L, {stackIndex}), out var __target))");
        writer.OpenBrace();
        writer.Line($"return luaL_error(L, \"{typeInfo.Name}: 'self' is not a valid userdata\");");
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
            writer.Line();
        }
    }

    /// <summary>
    ///     Returns the integer range bounds (min, max) for a narrowing numeric type, or null if no range check is needed.
    ///     Only applies to types narrower than <c>long</c> for integers or <c>double</c> for floating-point.
    /// </summary>
    private static (string Min, string Max)? GetIntegerRangeBounds(string typeName)
    {
        return typeName switch
        {
            "byte" => ("byte.MinValue", "byte.MaxValue"),
            "sbyte" => ("sbyte.MinValue", "sbyte.MaxValue"),
            "short" => ("short.MinValue", "short.MaxValue"),
            "ushort" => ("ushort.MinValue", "ushort.MaxValue"),
            "int" => ("int.MinValue", "int.MaxValue"),
            "uint" => ("uint.MinValue", "uint.MaxValue"),
            "ulong" => ("0", "long.MaxValue"), // lua_Integer is signed long; negative values are out of range for ulong
            _ => null // long, enums, etc. - no narrowing needed
        };
    }

    /// <summary>
    ///     Returns the floating-point range bounds (min, max) for a narrowing numeric type, or null if no range check is needed.
    /// </summary>
    private static (string Min, string Max)? GetNumberRangeBounds(string typeName)
    {
        return typeName switch
        {
            "float" => ("-float.MaxValue", "float.MaxValue"),
            _ => null // double - no narrowing needed
        };
    }

    private static bool IsUnsupportedType(ITypeSymbol type)
    {
        return type.IsRefLikeType || type.TypeKind == TypeKind.Pointer || type.TypeKind == TypeKind.FunctionPointer;
    }

    private static bool IsLuaStringCandidate(ITypeSymbol type)
    {
        var display = type.ToDisplayString();
        return display is "System.ReadOnlySpan<byte>" or "System.ReadOnlySpan<char>";
    }

    private static LuaValueKind ClassifyType(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Enum)
        {
            return LuaValueKind.Integer;
        }

        return type.SpecialType switch
        {
            SpecialType.System_Byte => LuaValueKind.Integer,
            SpecialType.System_SByte => LuaValueKind.Integer,
            SpecialType.System_Int16 => LuaValueKind.Integer,
            SpecialType.System_UInt16 => LuaValueKind.Integer,
            SpecialType.System_Int32 => LuaValueKind.Integer,
            SpecialType.System_UInt32 => LuaValueKind.Integer,
            SpecialType.System_Int64 => LuaValueKind.Integer,
            SpecialType.System_UInt64 => LuaValueKind.Integer,
            SpecialType.System_Single => LuaValueKind.Number,
            SpecialType.System_Double => LuaValueKind.Number,
            SpecialType.System_String => LuaValueKind.String,
            SpecialType.System_Boolean => LuaValueKind.Boolean,
            _ => LuaValueKind.Marshaled,
        };
    }

    private static bool ImplementsInterface(INamedTypeSymbol symbol, string interfaceName)
    {
        return symbol.AllInterfaces.Any(@interface => @interface.ToDisplayString() == interfaceName);
    }

    private static bool InheritsFromLuaReference(ITypeSymbol symbol)
    {
        return InheritsFrom(symbol, "Laylua.LuaReference");
    }

    private static bool InheritsFrom(ITypeSymbol symbol, string baseTypeName)
    {
        var current = symbol.BaseType;
        while (current != null)
        {
            if (current.ToDisplayString() == baseTypeName)
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    private static bool HasAttribute(ISymbol symbol, string attributeName)
    {
        return symbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == attributeName);
    }

    private static bool HasBaseWithAttribute(INamedTypeSymbol symbol, string attributeName)
    {
        var baseType = symbol.BaseType;
        while (baseType != null && baseType.SpecialType != SpecialType.System_Object)
        {
            if (HasAttribute(baseType, attributeName))
                return true;

            baseType = baseType.BaseType;
        }

        return false;
    }

    private static bool HasReturnAttribute(IMethodSymbol symbol, string attributeName)
    {
        return symbol.GetReturnTypeAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == attributeName);
    }

    private static void AddUnsupportedMethodDiagnostic(
        LuaTypeInfo info,
        INamedTypeSymbol annotatedType,
        INamedTypeSymbol declaringType,
        IMethodSymbol method,
        string unsupportedMember,
        ITypeSymbol unsupportedType)
    {
        var isInherited = !SymbolEqualityComparer.Default.Equals(declaringType, annotatedType);
        var methodSignature = FormatMethodSignature(method);

        info.Diagnostics.Add(new LuaDiagnosticInfo
        {
            Id = isInherited ? "LAYLUA101" : "LAYLUA100",
            Message =
                $"{methodSignature}|{info.Name}|{unsupportedMember}|{unsupportedType.ToDisplayString()}|{declaringType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}",
            Location = GetMethodDiagnosticLocation(method, info.TypeLocation),
            Properties = ImmutableDictionary<string, string?>.Empty
                .Add("methodName", method.Name)
                .Add("methodSignature", methodSignature),
        });
    }

    private static string FormatMethodSignature(IMethodSymbol method)
    {
        return method.ToDisplayString(DiagnosticMethodSignatureFormat);
    }

    private static Location? GetMethodDiagnosticLocation(IMethodSymbol method, Location? fallback)
    {
        foreach (var location in method.Locations)
        {
            if (location.IsInSource)
            {
                return location;
            }
        }

        return fallback;
    }

    private static bool IsIntegerType(ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_Byte => true,
            SpecialType.System_SByte => true,
            SpecialType.System_Int16 => true,
            SpecialType.System_UInt16 => true,
            SpecialType.System_Int32 => true,
            SpecialType.System_UInt32 => true,
            SpecialType.System_Int64 => true,
            SpecialType.System_UInt64 => true,
            _ => false,
        };
    }

    /// <summary>
    ///     Returns a camelCase name for an operator, e.g. "addOp", "unmOp".
    /// </summary>
    private static string OperatorCamelName(LuaOperatorInfo operatorInfo)
    {
        var meta = operatorInfo.MetamethodName.Substring(2);
        return meta + "Op";
    }

    /// <summary>
    ///     Returns the callback method name for an operator, e.g. "__OpAdd", "__OpUnm".
    /// </summary>
    private static string OperatorCallbackName(LuaOperatorInfo operatorInfo)
    {
        var meta = operatorInfo.MetamethodName.Substring(2);
        return $"__Op{char.ToUpperInvariant(meta[0])}{meta.Substring(1)}";
    }

    private static string GetMethodGeneratedIdentifier(LuaMethodInfo method)
    {
        if (!string.IsNullOrEmpty(method.GeneratedIdentifier))
        {
            return method.GeneratedIdentifier;
        }

        return method.ClrName;
    }

    private static string GetOperatorSymbol(string methodName)
    {
        return methodName switch
        {
            "op_Addition" => "+",
            "op_Subtraction" => "-",
            "op_Multiply" => "*",
            "op_Division" => "/",
            "op_Modulus" => "%",
            "op_Equality" => "==",
            "op_LessThan" => "<",
            "op_LessThanOrEqual" => "<=",
            "op_BitwiseAnd" => "&",
            "op_BitwiseOr" => "|",
            "op_ExclusiveOr" => "^",
            "op_LeftShift" => "<<",
            "op_RightShift" => ">>",
            _ => throw new InvalidOperationException($"Unknown operator method: {methodName}")
        };
    }
}
