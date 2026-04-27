using System;
using System.Buffers;
using System.Reflection;
using Laylua.Moon;

namespace Laylua.Marshaling;

public unsafe partial class DefaultLuaMarshaler
{
    /// <inheritdoc/>
    public override LuaMarshalingCompatibility GetMarshalingCompatibility(LuaThread thread, int stackIndex, Type targetType)
    {
        var L = thread.State.L;
        targetType = LuaTypeServices.NormalizeBoundaryType(targetType);

        var luaType = lua_type(L, stackIndex);
        if (luaType == LuaType.Nil)
        {
            return LuaMarshalingCompatibility.Incompatible;
        }

        var direct = GetDirectMarshalingCompatibility(L, stackIndex, targetType, luaType);
        if (direct.IsCompatible)
        {
            return direct;
        }

        return GetAdaptedMarshalingCompatibility(L, stackIndex, targetType, luaType);
    }

    private static LuaMarshalingCompatibility GetDirectMarshalingCompatibility(lua_State* L, int stackIndex, Type targetType, LuaType luaType)
    {
        switch (luaType)
        {
            case LuaType.Boolean:
            {
                if (targetType == typeof(bool))
                {
                    return LuaMarshalingCompatibility.ExactPrimitive();
                }

                break;
            }
            case LuaType.LightUserData:
            {
                if (targetType == typeof(IntPtr) || targetType == typeof(UIntPtr))
                {
                    return LuaMarshalingCompatibility.ExactPrimitive();
                }

                break;
            }
            case LuaType.Number:
            {
                return GetNumericMarshalingCompatibility(L, stackIndex, targetType);
            }
            case LuaType.String:
            {
                if (targetType == typeof(string) || targetType == typeof(LuaString))
                {
                    return LuaMarshalingCompatibility.ExactPrimitive();
                }

                break;
            }
            case LuaType.UserData:
            {
                return GetUserDataMarshalingCompatibility(L, stackIndex, targetType);
            }
            case LuaType.Table:
            {
                return GetTableDirectMarshalingCompatibility(targetType);
            }
            case LuaType.Function:
            {
                return GetFunctionDirectMarshalingCompatibility(targetType);
            }
            case LuaType.Thread:
            {
                return GetThreadDirectMarshalingCompatibility(targetType);
            }
        }

        return LuaMarshalingCompatibility.Incompatible;
    }

    private static LuaMarshalingCompatibility GetNumericMarshalingCompatibility(lua_State* L, int stackIndex, Type targetType)
    {
        if (!LuaTypeServices.IsNumericType(targetType))
        {
            return LuaMarshalingCompatibility.Incompatible;
        }

        var isLuaInteger = lua_isinteger(L, stackIndex);
        if (isLuaInteger)
        {
            if (LuaTypeServices.IsIntegerType(targetType))
            {
                const LuaMarshalingCompatibilityFlags flags = LuaMarshalingCompatibilityFlags.ValueDependent | LuaMarshalingCompatibilityFlags.NeedsRangeValidation;

                return LuaMarshalingCompatibility.ExactPrimitive(flags);
            }

            var wideningFlags = LuaMarshalingCompatibilityFlags.ValueDependent;
            if (targetType == typeof(float) || targetType == typeof(decimal))
            {
                wideningFlags |= LuaMarshalingCompatibilityFlags.NeedsRangeValidation;
            }

            return new LuaMarshalingCompatibility(LuaMarshalingCompatibilityKind.NumericWidening, wideningFlags);
        }

        if (targetType == typeof(float))
        {
            return LuaMarshalingCompatibility.ExactPrimitive(
                LuaMarshalingCompatibilityFlags.ValueDependent
                | LuaMarshalingCompatibilityFlags.NeedsRangeValidation);
        }

        if (targetType == typeof(double))
        {
            return LuaMarshalingCompatibility.ExactPrimitive(
                LuaMarshalingCompatibilityFlags.ValueDependent);
        }

        if (targetType == typeof(decimal))
        {
            return new LuaMarshalingCompatibility(LuaMarshalingCompatibilityKind.NumericWidening,
                LuaMarshalingCompatibilityFlags.ValueDependent
                | LuaMarshalingCompatibilityFlags.NeedsRangeValidation);
        }

        if (LuaTypeServices.IsIntegerType(targetType))
        {
            _ = lua_tointegerx(L, stackIndex, out var isConvertible);
            if (isConvertible)
            {
                return new LuaMarshalingCompatibility(LuaMarshalingCompatibilityKind.NumericNarrowingChecked,
                    LuaMarshalingCompatibilityFlags.ValueDependent
                    | LuaMarshalingCompatibilityFlags.NeedsRangeValidation);
            }
        }

        return LuaMarshalingCompatibility.Incompatible;
    }

    private static LuaMarshalingCompatibility GetUserDataMarshalingCompatibility(lua_State* L, int stackIndex, Type targetType)
    {
        if (LuaTypeRegistry.TryGet(L, stackIndex, targetType, out _))
        {
            return new LuaMarshalingCompatibility(LuaMarshalingCompatibilityKind.ExactTypedUserData);
        }

        if (targetType.IsAssignableFrom(typeof(LuaUserData)))
        {
            return LuaMarshalingCompatibility.ExactLuaReference();
        }

        if (targetType == typeof(LuaWeakReference<LuaUserData>)
            || targetType == typeof(LuaWeakReference<LuaReference>))
        {
            return LuaMarshalingCompatibility.LuaReferenceWrapper();
        }

        return LuaMarshalingCompatibility.Incompatible;
    }

    private static LuaMarshalingCompatibility GetTableDirectMarshalingCompatibility(Type targetType)
    {
        if (targetType.IsAssignableFrom(typeof(LuaTable)))
        {
            return LuaMarshalingCompatibility.ExactLuaReference();
        }

        if (targetType == typeof(LuaWeakReference<LuaTable>)
            || targetType == typeof(LuaWeakReference<LuaReference>))
        {
            return LuaMarshalingCompatibility.LuaReferenceWrapper();
        }

        return LuaMarshalingCompatibility.Incompatible;
    }

    private static LuaMarshalingCompatibility GetFunctionDirectMarshalingCompatibility(Type targetType)
    {
        if (targetType.IsAssignableFrom(typeof(LuaFunction)))
        {
            return LuaMarshalingCompatibility.ExactLuaReference();
        }

        if (targetType == typeof(LuaWeakReference<LuaFunction>)
            || targetType == typeof(LuaWeakReference<LuaReference>))
        {
            return LuaMarshalingCompatibility.LuaReferenceWrapper();
        }

        return LuaMarshalingCompatibility.Incompatible;
    }

    private static LuaMarshalingCompatibility GetThreadDirectMarshalingCompatibility(Type targetType)
    {
        if (targetType.IsAssignableFrom(typeof(LuaThread)))
        {
            return LuaMarshalingCompatibility.ExactLuaReference();
        }

        if (targetType == typeof(LuaWeakReference<LuaThread>)
            || targetType == typeof(LuaWeakReference<LuaReference>))
        {
            return LuaMarshalingCompatibility.LuaReferenceWrapper();
        }

        return LuaMarshalingCompatibility.Incompatible;
    }

    private LuaMarshalingCompatibility GetAdaptedMarshalingCompatibility(lua_State* L, int stackIndex, Type targetType, LuaType luaType)
    {
        if (luaType == LuaType.String && (LuaTypeServices.IsNumericType(targetType) || targetType == typeof(bool)))
        {
            if (CanConvertStringToPrimitive(L, stackIndex, targetType))
            {
                var flags = LuaMarshalingCompatibilityFlags.UsesStringParsing
                    | LuaMarshalingCompatibilityFlags.ValueDependent;

                if (targetType != typeof(bool))
                {
                    flags |= LuaMarshalingCompatibilityFlags.NeedsRangeValidation;
                }

                return new LuaMarshalingCompatibility(LuaMarshalingCompatibilityKind.StringToPrimitive, flags);
            }
        }

        if (luaType == LuaType.Table)
        {
            if (HasTableMaterializationCapability(targetType))
            {
                return new LuaMarshalingCompatibility(LuaMarshalingCompatibilityKind.TableMaterialization,
                    LuaMarshalingCompatibilityFlags.UsesTableMaterialization);
            }
        }

        if (luaType == LuaType.Function)
        {
            if (targetType == typeof(MethodInfo))
            {
                if (CanConvertFunctionToMethodInfo(L, stackIndex))
                {
                    return new LuaMarshalingCompatibility(LuaMarshalingCompatibilityKind.FunctionToMethodInfo,
                        LuaMarshalingCompatibilityFlags.UsesDelegateBinding);
                }
            }
            else if (targetType.BaseType == typeof(MulticastDelegate))
            {
                if (CanConvertFunctionToDelegate(L, stackIndex, targetType))
                {
                    var kind = TryGetMethodInfoFromFunction(L, stackIndex, targetType, out _)
                        ? LuaMarshalingCompatibilityKind.FunctionToMethodInfo
                        : LuaMarshalingCompatibilityKind.FunctionToDelegate;

                    return new LuaMarshalingCompatibility(kind,
                        LuaMarshalingCompatibilityFlags.UsesDelegateBinding);
                }
            }
        }

        if (luaType is LuaType.Boolean or LuaType.Number && targetType == typeof(string))
        {
            return new LuaMarshalingCompatibility(LuaMarshalingCompatibilityKind.PrimitiveToString,
                LuaMarshalingCompatibilityFlags.UsesPrimitiveToString);
        }

        if (luaType == LuaType.String && targetType == typeof(char))
        {
            var luaString = lua_tostring(L, stackIndex);
            if (LuaTypeServices.TryGetCharacter(luaString, out _))
            {
                return new LuaMarshalingCompatibility(LuaMarshalingCompatibilityKind.StringToChar,
                    LuaMarshalingCompatibilityFlags.UsesStringToChar | LuaMarshalingCompatibilityFlags.ValueDependent);
            }
        }

        if (luaType == LuaType.String && targetType.IsEnum)
        {
            var luaString = lua_tostring(L, stackIndex);
            if (LuaTypeServices.TryGetDefinedEnumValue(targetType, luaString, out _))
            {
                return new LuaMarshalingCompatibility(LuaMarshalingCompatibilityKind.StringToEnum,
                    LuaMarshalingCompatibilityFlags.UsesStringToEnum | LuaMarshalingCompatibilityFlags.ValueDependent);
            }
        }

        if (luaType == LuaType.Number && targetType.IsEnum)
        {
            if (LuaTypeServices.TryGetDefinedEnumValue(L, stackIndex, targetType, out _))
            {
                return new LuaMarshalingCompatibility(LuaMarshalingCompatibilityKind.NumberToEnum,
                    LuaMarshalingCompatibilityFlags.UsesNumberToEnum | LuaMarshalingCompatibilityFlags.ValueDependent);
            }
        }

        if (targetType == typeof(object))
        {
            return new LuaMarshalingCompatibility(LuaMarshalingCompatibilityKind.ObjectFallback,
                LuaMarshalingCompatibilityFlags.UsesObjectFallback);
        }

        return LuaMarshalingCompatibility.Incompatible;
    }

    private bool CanConvertStringToPrimitive(lua_State* L, int stackIndex, Type targetType)
    {
        var nativeString = lua_tostring(L, stackIndex);
        var charCount = nativeString.CharLength;
        char[]? rentedArray = null;
        scoped var charSpan = charCount > 256
            ? (rentedArray = ArrayPool<char>.Shared.Rent(charCount)).AsSpan(0, charCount)
            : stackalloc char[charCount];

        try
        {
            nativeString.GetChars(charSpan);
            if (targetType == typeof(bool))
            {
                return bool.TryParse(charSpan, out _);
            }

            return TryParseNumericValue(charSpan, targetType, out _);
        }
        finally
        {
            if (rentedArray != null)
            {
                ArrayPool<char>.Shared.Return(rentedArray);
            }
        }
    }

    private static bool HasTableMaterializationCapability(Type targetType)
    {
        if (targetType.IsInterface || targetType.IsAbstract || targetType == typeof(object))
        {
            return false;
        }

        var handler = LuaTypeRegistry.GetHandler(targetType);
        return handler != null;
    }
}
