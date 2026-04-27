using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Laylua.Marshaling;

namespace Laylua.Moon;

/// <summary>
///     Low-level helpers and marshaling operations used by source-generated, dynamic, and custom userdata handlers.
/// </summary>
public static unsafe class LuaTypeServices
{
    private const int UserDataSlotCount = 2;
    private const int TargetHandleIndex = 0;
    private const int SentinelIndex = 1;

    /// <summary>
    ///     Sentinel indicating that the value is not compatible under the given policy.
    /// </summary>
    internal const int CostIncompatible = -1;

    /// <summary>
    ///     Static function pointer repurposed as the sentinel stored at <c>userData[SentinelIndex]</c> of every
    ///     Laylua-managed userdata block.
    /// </summary>
    private static readonly nint Sentinel = LayluaNative.getPotentialPanicPtr;

    /// <summary>
    ///     Allocates a new Laylua-managed userdata block on the Lua stack and stores the given
    ///     managed object by allocating a <see cref="GCHandle"/>.
    /// </summary>
    public static nint* NewUserData(lua_State* L, object target)
    {
        var handle = GCHandle.Alloc(target);
        var userData = (nint*) lua_newuserdatauv(L, (nuint) (sizeof(nint) * UserDataSlotCount), 0);
        userData[TargetHandleIndex] = GCHandle.ToIntPtr(handle);
        userData[SentinelIndex] = Sentinel;
        return userData;
    }

    /// <summary>
    ///     Frees the handles stored in a Laylua userdata block and clears their slots.
    /// </summary>
    /// <returns>
    ///     Returns <c>false</c> if <paramref name="userData"/> is <see langword="null"/>, not a Laylua userdata block, or has no live handles.
    /// </returns>
    public static bool TryFreeUserData(nint* userData)
    {
        if (userData == null || userData[SentinelIndex] != Sentinel)
        {
            return false;
        }

        if (userData[TargetHandleIndex] != 0)
        {
            GCHandle.FromIntPtr(userData[TargetHandleIndex]).Free();
            userData[TargetHandleIndex] = 0;
            return true;
        }

        return false;
    }

    public static void DisposePairsEnumeratorState(nint* userData)
    {
        if (userData == null)
        {
            return;
        }

        var ptr = *userData;
        if (ptr == 0)
        {
            return;
        }

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
            *userData = 0;
        }
    }

    /// <summary>
    ///     Retrieves the managed target stored in a Laylua userdata block.
    /// </summary>
    /// <returns>
    ///     Returns <c>false</c> if <paramref name="userData"/> is <see langword="null"/>, not a Laylua userdata block, has no target handle, or the target is <see langword="null"/>.
    /// </returns>
    public static bool TryGetUserDataTarget(nint* userData, [MaybeNullWhen(false)] out object target)
    {
        if (userData == null || userData[SentinelIndex] != Sentinel)
        {
            target = null;
            return false;
        }

        var handle = userData[TargetHandleIndex];
        if (handle == 0)
        {
            target = null;
            return false;
        }

        target = GCHandle.FromIntPtr(handle).Target;
        return target != null;
    }

    /// <summary>
    ///     Combines cleanup failures into a single exception value.
    /// </summary>
    public static Exception CombineCleanupException(Exception? existingException, Exception exception)
    {
        const string message = "One or more errors occurred during userdata cleanup.";

        if (existingException == null)
        {
            return exception;
        }

        if (existingException is AggregateException aggregateException)
        {
            var innerExceptions = new Exception[aggregateException.InnerExceptions.Count + 1];
            aggregateException.InnerExceptions.CopyTo(innerExceptions, 0);
            innerExceptions[^1] = exception;
            return new AggregateException(message, innerExceptions);
        }

        return new AggregateException(message, existingException, exception);
    }

    /// <summary>
    ///     Rethrows a cleanup exception while preserving its stack trace.
    /// </summary>
    public static void ThrowCleanupException(Exception? exception)
    {
        if (exception == null)
        {
            return;
        }

        ExceptionDispatchInfo.Capture(exception).Throw();
    }

    /// <inheritdoc cref="LuaTypeRegistry.TryPush{T}(lua_State*, T)" />
    public static bool TryPush<T>(lua_State* L, T value)
        where T : notnull
    {
        return LuaTypeRegistry.TryPush(L, value);
    }

    /// <inheritdoc cref="LuaTypeRegistry.TryPush(lua_State*, Type, object)" />
    public static bool TryPush(lua_State* L, Type type, object value)
    {
        return LuaTypeRegistry.TryPush(L, type, value);
    }

    /// <inheritdoc cref="LuaTypeRegistry.TryPushDefinition(lua_State*, Type)" />
    public static bool TryPushDefinition(lua_State* L, Type type)
    {
        return LuaTypeRegistry.TryPushDefinition(L, type);
    }

    /// <inheritdoc cref="LuaTypeRegistry.PushDynamicDefinition(lua_State*, Type)" />
    public static void PushDynamicDefinition(lua_State* L, Type type)
    {
        LuaTypeRegistry.PushDynamicDefinition(L, type);
    }

    /// <inheritdoc cref="LuaTypeRegistry.TryGet{T}(lua_State*, int, out T)" />
    public static bool TryGet<T>(lua_State* L, int stackIndex, out T? value)
    {
        return LuaTypeRegistry.TryGet(L, stackIndex, out value);
    }

    /// <summary>
    ///     Reads a value from the Lua stack using conversion rules derived from the given implicit-conversion policy.
    /// </summary>
    public static T? GetValue<T>(lua_State* L, int stackIndex, LuaAllowedValueConversions allowedConversions)
    {
        using var thread = LuaThread.FromExtraSpace(L);
        return thread.Stack[stackIndex].GetValue<T>(allowedConversions);
    }

    /// <inheritdoc cref="LuaTypeRegistry.TryGet(lua_State*, int, Type, out object)" />
    public static bool TryGet(lua_State* L, int stackIndex, Type type, out object? value)
    {
        return LuaTypeRegistry.TryGet(L, stackIndex, type, out value);
    }

    /// <inheritdoc cref="LuaTypeRegistry.TryGetFromTable(lua_State*, int, Type, out object)" />
    public static bool TryGetFromTable(lua_State* L, int stackIndex, Type type, out object? value)
    {
        return LuaTypeRegistry.TryGetFromTable(L, stackIndex, type, out value);
    }

    /// <inheritdoc cref="LuaTypeRegistry.TryGetFromTable{T}(lua_State*, int, out T)" />
    public static bool TryGetFromTable<T>(lua_State* L, int stackIndex, out T? value)
    {
        return LuaTypeRegistry.TryGetFromTable(L, stackIndex, out value);
    }

    /// <inheritdoc cref="LuaTypeRegistry.GetHandler(Type)" />
    public static ILuaUserDataHandler? GetHandler(Type type)
    {
        return LuaTypeRegistry.GetHandler(type);
    }

    /// <inheritdoc cref="LuaTypeRegistry.GetOrCreate(Type)" />
    public static ILuaUserDataHandler GetOrCreate(Type type)
    {
        return LuaTypeRegistry.GetOrCreate(type);
    }

    /// <inheritdoc cref="LuaTypeRegistry.RegisterMethodPointer(nint, Type, string[], bool)" />
    public static void RegisterMethodPointer(nint functionPtr, Type declaringType, string[] clrMethodNames, bool isStatic)
    {
        LuaTypeRegistry.RegisterMethodPointer(functionPtr, declaringType, clrMethodNames, isStatic);
    }

    /// <inheritdoc cref="LuaTypeRegistry.TryGetMethodPointerInfo(nint, out LuaTypeRegistry.MethodPointerInfo)" />
    public static bool TryGetMethodPointerInfo(nint functionPtr, out LuaTypeRegistry.MethodPointerInfo info)
    {
        return LuaTypeRegistry.TryGetMethodPointerInfo(functionPtr, out info);
    }

    /// <summary>
    ///     Returns the marshaler-driven implicit compatibility cost for binding a Lua value to a CLR target type
    ///     under the given <see cref="LuaAllowedValueConversions"/> policy.
    /// </summary>
    /// <param name="thread">The Lua thread that owns the value.</param>
    /// <param name="stackIndex">The Lua stack index of the value.</param>
    /// <param name="targetType">The CLR target type (already normalized past byref).</param>
    /// <param name="allowedConversions">The implicit conversion policy in effect.</param>
    /// <param name="validateNumericRange">
    ///     When <see langword="true"/> and the result has <see cref="LuaMarshalingCompatibilityFlags.NeedsRangeValidation"/>,
    ///     performs a runtime range check against the actual Lua value and returns <see cref="CostIncompatible"/> if the value
    ///     is out of range.
    /// </param>
    /// <returns>
    ///     A non-negative cost (lower is better), or <see cref="CostIncompatible"/> if the value
    ///     cannot bind to the target type under the given policy.
    /// </returns>
    public static int GetImplicitCompatibilityCost(LuaThread thread, int stackIndex, Type targetType, LuaAllowedValueConversions allowedConversions, bool validateNumericRange = false)
    {
        targetType = NormalizeBoundaryType(targetType);
        var L = thread.State.L;

        var result = thread.Marshaler.GetMarshalingCompatibility(thread, stackIndex, targetType);
        if (!result.IsAllowedBy(allowedConversions) || validateNumericRange
            && (result.Flags & LuaMarshalingCompatibilityFlags.NeedsRangeValidation) != 0
            && !IsValueInRange(L, stackIndex, targetType))
        {
            return CostIncompatible;
        }

        return result.Cost;
    }

    /// <summary>
    ///     Returns whether a Lua value is implicitly compatible with a CLR target type
    ///     under the given <see cref="LuaAllowedValueConversions"/> policy, using the marshaler-driven dispatch API.
    /// </summary>
    /// <param name="thread">The Lua thread that owns the value.</param>
    /// <param name="stackIndex">The Lua stack index of the value.</param>
    /// <param name="targetType">The CLR target type.</param>
    /// <param name="allowedConversions">The implicit conversion policy in effect.</param>
    /// <returns>
    ///     <see langword="true"/> if the value is compatible under the given policy;
    ///     <see langword="false"/> otherwise.
    /// </returns>
    public static bool IsImplicitlyCompatible(LuaThread thread, int stackIndex, Type targetType, LuaAllowedValueConversions allowedConversions)
    {
        return GetImplicitCompatibilityCost(thread, stackIndex, targetType, allowedConversions) >= 0;
    }

    private static bool IsValueInRange(lua_State* L, int stackIndex, Type targetType)
    {
        if (IsIntegerType(targetType))
        {
            var integerValue = lua_tointegerx(L, stackIndex, out var isNumber);
            return isNumber && IsIntegerValueInRange(integerValue, targetType);
        }

        if (targetType == typeof(float))
        {
            var numberValue = lua_tonumberx(L, stackIndex, out var isNumber);
            return isNumber && IsSingleValueInRange(numberValue);
        }

        if (targetType == typeof(double))
        {
            return true;
        }

        if (targetType == typeof(decimal))
        {
            var numberValue = lua_tonumberx(L, stackIndex, out var isNumber);
            return isNumber && IsDecimalValueInRange(numberValue);
        }

        return true;
    }

    internal static bool IsIntegerValueInRange(lua_Integer value, Type targetType)
    {
        if (targetType == typeof(byte))
        {
            return value is >= byte.MinValue and <= byte.MaxValue;
        }

        if (targetType == typeof(sbyte))
        {
            return value is >= sbyte.MinValue and <= sbyte.MaxValue;
        }

        if (targetType == typeof(short))
        {
            return value is >= short.MinValue and <= short.MaxValue;
        }

        if (targetType == typeof(ushort))
        {
            return value is >= ushort.MinValue and <= ushort.MaxValue;
        }

        if (targetType == typeof(int))
        {
            return value is >= int.MinValue and <= int.MaxValue;
        }

        if (targetType == typeof(uint))
        {
            return value is >= uint.MinValue and <= uint.MaxValue;
        }

        if (targetType == typeof(ulong))
        {
            return value >= 0;
        }

        return true;
    }

    internal static bool IsSingleValueInRange(lua_Number value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value)
            && value is >= -float.MaxValue and <= float.MaxValue;
    }

    internal static bool IsDecimalValueInRange(lua_Number value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value)
            && value is >= (double) decimal.MinValue and <= (double) decimal.MaxValue;
    }

    internal static Type NormalizeBoundaryType(Type type)
    {
        if (type.IsByRef)
        {
            type = type.GetElementType()!;
        }

        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
        {
            return underlying;
        }

        return type;
    }

    internal static bool IsIntegerType(Type type)
    {
        return type == typeof(int) || type == typeof(long) || type == typeof(short)
            || type == typeof(byte) || type == typeof(uint) || type == typeof(ulong)
            || type == typeof(ushort) || type == typeof(sbyte);
    }

    internal static bool IsNumericType(Type type)
    {
        return IsIntegerType(type)
            || type == typeof(float) || type == typeof(double) || type == typeof(decimal);
    }

    internal static bool TryGetCharacter(LuaString luaString, out char value)
    {
        if (luaString.CharLength != 1)
        {
            value = default;
            return false;
        }

        Span<char> character = stackalloc char[1];
        luaString.GetChars(character);
        value = character[0];
        return true;
    }

    internal static bool TryGetDefinedEnumValue(lua_State* L, int stackIndex, Type enumType, out object? value)
    {
        var integerValue = lua_tointegerx(L, stackIndex, out var isNumber);
        if (!isNumber)
        {
            value = null;
            return false;
        }

        return TryGetDefinedEnumValue(enumType, integerValue, out value);
    }

    internal static bool TryGetDefinedEnumValue(Type enumType, lua_Integer integerValue, out object? value)
    {
        var underlyingType = Enum.GetUnderlyingType(enumType);
        if (!TryConvertIntegerValue(integerValue, underlyingType, out var underlyingValue) || !Enum.IsDefined(enumType, underlyingValue!))
        {
            value = null;
            return false;
        }

        value = Enum.ToObject(enumType, underlyingValue!);
        return true;
    }

    internal static bool TryGetDefinedEnumValue(Type enumType, LuaString name, out object? value)
    {
        return TryGetDefinedEnumValue(enumType, name.ToString()!, out value);
    }

    internal static bool TryGetDefinedEnumValue(Type enumType, string name, out object? value)
    {
        if (!Enum.IsDefined(enumType, name))
        {
            value = null;
            return false;
        }

        value = Enum.Parse(enumType, name, ignoreCase: false);
        return true;
    }

    private static bool TryConvertIntegerValue(lua_Integer integerValue, Type targetType, out object? value)
    {
        if (targetType == typeof(int))
        {
            if (integerValue is < int.MinValue or > int.MaxValue)
            {
                value = null;
                return false;
            }

            value = (int) integerValue;
            return true;
        }

        if (targetType == typeof(long))
        {
            value = integerValue;
            return true;
        }

        if (targetType == typeof(byte))
        {
            if (integerValue is < byte.MinValue or > byte.MaxValue)
            {
                value = null;
                return false;
            }

            value = (byte) integerValue;
            return true;
        }

        if (targetType == typeof(sbyte))
        {
            if (integerValue is < sbyte.MinValue or > sbyte.MaxValue)
            {
                value = null;
                return false;
            }

            value = (sbyte) integerValue;
            return true;
        }

        if (targetType == typeof(short))
        {
            if (integerValue is < short.MinValue or > short.MaxValue)
            {
                value = null;
                return false;
            }

            value = (short) integerValue;
            return true;
        }

        if (targetType == typeof(ushort))
        {
            if (integerValue is < ushort.MinValue or > ushort.MaxValue)
            {
                value = null;
                return false;
            }

            value = (ushort) integerValue;
            return true;
        }

        if (targetType == typeof(uint))
        {
            if (integerValue is < uint.MinValue or > uint.MaxValue)
            {
                value = null;
                return false;
            }

            value = (uint) integerValue;
            return true;
        }

        if (targetType == typeof(ulong))
        {
            if (integerValue < 0)
            {
                value = null;
                return false;
            }

            value = (ulong) integerValue;
            return true;
        }

        value = null;
        return false;
    }
}
