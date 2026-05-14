using System;
using Laylua.Moon;
using Qommon;

namespace Laylua.Marshaling;

/// <summary>
///     Describes a failed Lua-to-CLR conversion.
/// </summary>
public sealed class LuaConversionError
{
    private static string FormatLuaType(LuaType luaType)
    {
        return luaType.ToString().ToLowerInvariant();
    }

    /// <summary>
    ///     Gets the human-readable conversion failure message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     Gets the Lua table path where the conversion failed.
    /// </summary>
    public string Path { get; }

    /// <summary>
    ///     Gets the CLR type being converted to.
    /// </summary>
    public Type TargetType { get; }

    /// <summary>
    ///     Gets the Lua type of the value that failed conversion.
    /// </summary>
    public LuaType LuaType { get; }

    /// <summary>
    ///     Gets the member name associated with the failure, if any.
    /// </summary>
    public string? MemberName { get; }

    /// <summary>
    ///     Gets the nested conversion failure, if any.
    /// </summary>
    public LuaConversionError? InnerError { get; }

    public LuaConversionError(
        string message,
        string? path,
        Type targetType,
        LuaType luaType,
        string? memberName = null,
        LuaConversionError? innerError = null)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Path = NormalizePath(path);
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        LuaType = luaType;
        MemberName = memberName;
        InnerError = innerError;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (InnerError == null)
        {
            return $"{Path}: {Message}";
        }

        return $"{Path}: {Message}; {InnerError}";
    }

    public static LuaConversionError InvalidStackValue(Type targetType, string? path = null)
    {
        return new LuaConversionError(
            "stack value is not valid",
            path,
            targetType,
            LuaType.None);
    }

    public static unsafe LuaConversionError IncompatibleLuaType(lua_State* L, int stackIndex, Type targetType, string? path = null)
    {
        return IncompatibleLuaType(targetType, lua_type(L, stackIndex), path);
    }

    public static LuaConversionError IncompatibleLuaType(Type targetType, LuaType luaType, string? path = null)
    {
        return new LuaConversionError(
            $"expected {targetType.ToTypeString()}, got {FormatLuaType(luaType)}",
            path,
            targetType,
            luaType);
    }

    public static unsafe LuaConversionError ExpectedTable(lua_State* L, int stackIndex, Type targetType, string? path = null)
    {
        return ExpectedTable(targetType, lua_type(L, stackIndex), path);
    }

    public static LuaConversionError ExpectedTable(Type targetType, LuaType luaType, string? path = null)
    {
        return new LuaConversionError(
            $"expected table, got {FormatLuaType(luaType)}",
            path,
            targetType,
            luaType);
    }

    public static unsafe LuaConversionError NoTableConversionRegistered(lua_State* L, int stackIndex, Type targetType, string? path = null)
    {
        return NoTableConversionRegistered(targetType, lua_type(L, stackIndex), path);
    }

    public static LuaConversionError NoTableConversionRegistered(Type targetType, LuaType luaType, string? path = null)
    {
        return new LuaConversionError(
            $"no table conversion is registered for {targetType.ToTypeString()}",
            path,
            targetType,
            luaType);
    }

    public static LuaConversionError NilCannotBeConverted(Type targetType, string? path = null)
    {
        return new LuaConversionError(
            $"expected {targetType.ToTypeString()}, got nil",
            path,
            targetType,
            LuaType.Nil);
    }

    public static LuaConversionError ConversionPolicyDisabled(Type targetType, LuaType luaType, string conversionDescription, string? path = null)
    {
        return new LuaConversionError(
            $"{conversionDescription} conversion is disabled",
            path,
            targetType,
            luaType);
    }

    public static LuaConversionError StringMustContainSingleCharacter(Type targetType, string? path = null)
    {
        return new LuaConversionError(
            "string must contain exactly one character",
            path,
            targetType,
            LuaType.String);
    }

    public static LuaConversionError UndefinedEnumName(Type targetType, string value, string? path = null)
    {
        return new LuaConversionError(
            $"'{value}' is not a defined name of {targetType.ToTypeString()}",
            path,
            targetType,
            LuaType.String);
    }

    public static LuaConversionError UndefinedEnumValue(Type targetType, long value, string? path = null)
    {
        return new LuaConversionError(
            $"{value} is not a defined value of {targetType.ToTypeString()}",
            path,
            targetType,
            LuaType.Number);
    }

    public static LuaConversionError EnumRequiresDefinedIntegralValue(Type targetType, string? path = null)
    {
        return new LuaConversionError(
            $"expected a defined integral value of {targetType.ToTypeString()}",
            path,
            targetType,
            LuaType.Number);
    }

    public static LuaConversionError InvalidBooleanString(Type targetType, string value, string? path = null)
    {
        return new LuaConversionError(
            $"'{value}' is not a valid {targetType.ToTypeString()} value",
            path,
            targetType,
            LuaType.String);
    }

    public static LuaConversionError InvalidNumericString(Type targetType, string value, string? path = null)
    {
        return new LuaConversionError(
            $"'{value}' is not a valid {targetType.ToTypeString()} value",
            path,
            targetType,
            LuaType.String);
    }

    public static LuaConversionError NumericValueOutOfRange(Type targetType, LuaType luaType = LuaType.Number, string? path = null)
    {
        return new LuaConversionError(
            $"value is outside the range of {targetType.ToTypeString()}",
            path,
            targetType,
            luaType);
    }

    public static LuaConversionError FractionalNumberNotAllowed(Type targetType, string? path = null)
    {
        return new LuaConversionError(
            $"number has a fractional component and truncation is disabled for {targetType.ToTypeString()}",
            path,
            targetType,
            LuaType.Number);
    }

    public static LuaConversionError NonFiniteNumberNotAllowed(Type targetType, string? path = null)
    {
        return new LuaConversionError(
            $"non-finite numbers cannot be converted to {targetType.ToTypeString()}",
            path,
            targetType,
            LuaType.Number);
    }

    public static LuaConversionError UnexpectedTableKeyType(Type targetType, LuaType luaType, string? path = null)
    {
        return new LuaConversionError(
            "table keys must be strings",
            path,
            targetType,
            luaType);
    }

    public static LuaConversionError BrokenTableConversionHandler(Type targetType, LuaType luaType, string? path = null)
    {
        return new LuaConversionError(
            $"table conversion handler for {targetType.ToTypeString()} returned false without an error",
            path,
            targetType,
            luaType);
    }

    public static unsafe LuaConversionError IncompatibleBoxedValue(lua_State* L, int stackIndex, Type targetType, string? path = null)
    {
        return new LuaConversionError(
            $"handler returned incompatible boxed value for {targetType.ToTypeString()}",
            path,
            targetType,
            lua_type(L, stackIndex));
    }

    public static LuaConversionError MissingRequiredMember(Type targetType, string memberName, string path)
    {
        return new LuaConversionError(
            $"missing required member '{memberName}'",
            path,
            targetType,
            LuaType.Nil,
            memberName);
    }

    public static unsafe LuaConversionError UnknownMember(lua_State* L, int stackIndex, Type targetType, string memberName, string path)
    {
        return new LuaConversionError(
            $"unknown member '{memberName}'",
            path,
            targetType,
            lua_type(L, stackIndex),
            memberName);
    }

    public static unsafe LuaConversionError MemberConversionFailed(
        lua_State* L,
        int stackIndex,
        Type targetType,
        Type memberType,
        string memberName,
        string path,
        LuaConversionError? innerError = null)
    {
        return new LuaConversionError(
            $"failed to convert member '{memberName}' to {memberType.ToTypeString()}",
            path,
            targetType,
            lua_type(L, stackIndex),
            memberName,
            innerError: innerError);
    }

    public LuaConversionError WithPathPrefix(string pathPrefix)
    {
        var prefixedPath = Path == "$"
            ? pathPrefix
            : pathPrefix + Path[1..];

        return new LuaConversionError(
            Message,
            prefixedPath,
            TargetType,
            LuaType,
            MemberName,
            InnerError?.WithPathPrefix(pathPrefix));
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "$";
        }

        if (path[0] == '$')
        {
            return path;
        }

        if (path[0] == '.' || path[0] == '[')
        {
            return "$" + path;
        }

        return "$." + path;
    }
}
