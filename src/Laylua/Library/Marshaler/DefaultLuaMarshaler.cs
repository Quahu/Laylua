using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Laylua.Moon;
using Qommon;
using Qommon.Collections.ThreadSafe;
#if !NET7_0_OR_GREATER
using System.Globalization;
using System.Runtime.InteropServices;
#endif

namespace Laylua.Marshaling;

public unsafe class DefaultLuaMarshaler : LuaMarshaler
{
    protected delegate void PushGenericEnumerableDelegate(DefaultLuaMarshaler marshaler, IEnumerable enumerable);

    protected static IThreadSafeDictionary<Type, PushGenericEnumerableDelegate?> PushGenericEnumerableActions { get; } = ThreadSafeDictionary.ConcurrentDictionary.Create<Type, PushGenericEnumerableDelegate?>();

    private readonly Dictionary<object, UserDataHandle> _userDataHandles;

    public DefaultLuaMarshaler(Lua lua, UserDataDescriptorProvider userDataDescriptorProvider)
        : base(lua, userDataDescriptorProvider)
    {
        _userDataHandles = new();
    }

    internal override void RemoveUserDataHandle(UserDataHandle handle)
    {
        _userDataHandles.Remove(handle.Target);
    }

    /// <inheritdoc/>
    public override bool TryToObject<T>(int stackIndex, out T? obj)
        where T : default
    {
        var L = Lua.GetStatePointer();
        var luaType = lua_type(L, stackIndex);
        if (luaType == LuaType.None)
        {
            // No value at the index.
            obj = default;
            return false;
        }

        if (luaType == LuaType.Nil)
        {
            // Null value at the index.
            obj = default;
            return default(T) == null;
        }

        var clrType = typeof(T);
        if (clrType.TryGetNullableUnderlyingType(out var nullableType))
        {
            clrType = nullableType;
        }

        switch (luaType)
        {
            case LuaType.Boolean:
            {
                var boolValue = lua_toboolean(L, stackIndex);
                if (clrType == typeof(bool) || clrType == typeof(object))
                {
                    obj = (T) (object) boolValue;
                    return true;
                }

                if (clrType == typeof(string))
                {
                    obj = (T) (object) (boolValue ? "true" : "false");
                    return true;
                }

                obj = default;
                return false;
            }
            case LuaType.LightUserData:
            {
                if (clrType == typeof(IntPtr) || clrType == typeof(object))
                {
                    obj = (T) (object) (IntPtr) lua_touserdata(L, stackIndex);
                    return true;
                }

                if (clrType == typeof(UIntPtr))
                {
                    obj = (T) (object) (UIntPtr) lua_touserdata(L, stackIndex);
                    return true;
                }

                obj = default;
                return false;
            }
            case LuaType.Number:
            {
                if (lua_isinteger(L, stackIndex))
                {
                    var longValue = lua_tointeger(L, stackIndex);
                    if (clrType == typeof(int))
                    {
                        obj = (T) (object) (int) longValue;
                        return true;
                    }

                    if (clrType == typeof(long) || clrType == typeof(object))
                    {
                        obj = (T) (object) longValue;
                        return true;
                    }

                    if (clrType == typeof(uint))
                    {
                        obj = (T) (object) (uint) longValue;
                        return true;
                    }

                    if (clrType == typeof(ulong))
                    {
                        obj = (T) (object) (ulong) longValue;
                        return true;
                    }

                    if (clrType == typeof(double))
                    {
                        obj = (T) (object) (double) longValue;
                        return true;
                    }

                    if (clrType == typeof(float))
                    {
                        obj = (T) (object) (float) longValue;
                        return true;
                    }

                    if (clrType == typeof(sbyte))
                    {
                        obj = (T) (object) (sbyte) longValue;
                        return true;
                    }

                    if (clrType == typeof(byte))
                    {
                        obj = (T) (object) (byte) longValue;
                        return true;
                    }

                    if (clrType == typeof(short))
                    {
                        obj = (T) (object) (short) longValue;
                        return true;
                    }

                    if (clrType == typeof(ushort))
                    {
                        obj = (T) (object) (ushort) longValue;
                        return true;
                    }

                    if (clrType == typeof(decimal))
                    {
                        obj = (T) (object) (decimal) longValue;
                        return true;
                    }

                    if (clrType == typeof(string))
                    {
                        obj = (T) (object) longValue.ToString(Lua.Culture);
                        return true;
                    }
                }
                else
                {
                    var doubleValue = lua_tonumber(L, stackIndex);
                    if (clrType == typeof(int))
                    {
                        obj = (T) (object) (int) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(long))
                    {
                        obj = (T) (object) (long) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(uint))
                    {
                        obj = (T) (object) (uint) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(ulong))
                    {
                        obj = (T) (object) (ulong) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(double) || clrType == typeof(object))
                    {
                        obj = (T) (object) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(float))
                    {
                        obj = (T) (object) (float) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(sbyte))
                    {
                        obj = (T) (object) (sbyte) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(byte))
                    {
                        obj = (T) (object) (byte) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(short))
                    {
                        obj = (T) (object) (short) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(ushort))
                    {
                        obj = (T) (object) (ushort) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(decimal))
                    {
                        obj = (T) (object) (decimal) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(string))
                    {
                        obj = (T) (object) doubleValue.ToString(Lua.Culture);
                        return true;
                    }
                }

                obj = default;
                return false;
            }
            case LuaType.String:
            {
                var nativeStringValue = lua_tostring(L, stackIndex);
                if (clrType == typeof(string) || clrType == typeof(object))
                {
                    obj = (T) (object) nativeStringValue.ToString();
                    return true;
                }

                if (clrType == typeof(LuaString))
                {
                    obj = (T) (object) nativeStringValue;
                    return true;
                }

                var charCount = nativeStringValue.CharLength;
                char[]? rentedArray = null;
                scoped var charSpan = charCount > 256
                    ? (rentedArray = ArrayPool<char>.Shared.Rent(charCount)).AsSpan(0, charCount)
                    : stackalloc char[charCount];

                try
                {
                    nativeStringValue.GetChars(charSpan);
#if NET7_0_OR_GREATER
                    if (clrType == typeof(int) && int.TryParse(charSpan, Lua.Culture, out var intValue))
                    {
                        obj = (T) (object) intValue;
                        return true;
                    }

                    if (clrType == typeof(uint) && uint.TryParse(charSpan, Lua.Culture, out var uintValue))
                    {
                        obj = (T) (object) uintValue;
                        return true;
                    }

                    if (clrType == typeof(long) && long.TryParse(charSpan, Lua.Culture, out var longValue))
                    {
                        obj = (T) (object) longValue;
                        return true;
                    }

                    if (clrType == typeof(ulong) && ulong.TryParse(charSpan, Lua.Culture, out var ulongValue))
                    {
                        obj = (T) (object) ulongValue;
                        return true;
                    }

                    if (clrType == typeof(double) && double.TryParse(charSpan, Lua.Culture, out var doubleValue))
                    {
                        obj = (T) (object) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(float) && float.TryParse(charSpan, Lua.Culture, out var floatValue))
                    {
                        obj = (T) (object) floatValue;
                        return true;
                    }

                    if (clrType == typeof(sbyte) && sbyte.TryParse(charSpan, Lua.Culture, out var sbyteValue))
                    {
                        obj = (T) (object) sbyteValue;
                        return true;
                    }

                    if (clrType == typeof(byte) && byte.TryParse(charSpan, Lua.Culture, out var byteValue))
                    {
                        obj = (T) (object) byteValue;
                        return true;
                    }

                    if (clrType == typeof(short) && short.TryParse(charSpan, Lua.Culture, out var shortValue))
                    {
                        obj = (T) (object) shortValue;
                        return true;
                    }

                    if (clrType == typeof(ushort) && ushort.TryParse(charSpan, Lua.Culture, out var ushortValue))
                    {
                        obj = (T) (object) ushortValue;
                        return true;
                    }

                    if (clrType == typeof(decimal) && decimal.TryParse(charSpan, Lua.Culture, out var decimalValue))
                    {
                        obj = (T) (object) decimalValue;
                        return true;
                    }
#else
                    if (clrType == typeof(int) && int.TryParse(charSpan, NumberStyles.Integer, Lua.Culture, out var intValue))
                    {
                        obj = (T) (object) intValue;
                        return true;
                    }

                    if (clrType == typeof(uint) && uint.TryParse(charSpan, NumberStyles.Integer, Lua.Culture, out var uintValue))
                    {
                        obj = (T) (object) uintValue;
                        return true;
                    }

                    if (clrType == typeof(long) && long.TryParse(charSpan, NumberStyles.Integer, Lua.Culture, out var longValue))
                    {
                        obj = (T) (object) longValue;
                        return true;
                    }

                    if (clrType == typeof(ulong) && ulong.TryParse(charSpan, NumberStyles.Integer, Lua.Culture, out var ulongValue))
                    {
                        obj = (T) (object) ulongValue;
                        return true;
                    }

                    if (clrType == typeof(double) && double.TryParse(charSpan, NumberStyles.Float | NumberStyles.AllowThousands, Lua.Culture, out var doubleValue))
                    {
                        obj = (T) (object) doubleValue;
                        return true;
                    }

                    if (clrType == typeof(float) && float.TryParse(charSpan, NumberStyles.Float | NumberStyles.AllowThousands, Lua.Culture, out var floatValue))
                    {
                        obj = (T) (object) floatValue;
                        return true;
                    }

                    if (clrType == typeof(sbyte) && sbyte.TryParse(charSpan, NumberStyles.Integer, Lua.Culture, out var sbyteValue))
                    {
                        obj = (T) (object) sbyteValue;
                        return true;
                    }

                    if (clrType == typeof(byte) && byte.TryParse(charSpan, NumberStyles.Integer, Lua.Culture, out var byteValue))
                    {
                        obj = (T) (object) byteValue;
                        return true;
                    }

                    if (clrType == typeof(short) && short.TryParse(charSpan, NumberStyles.Integer, Lua.Culture, out var shortValue))
                    {
                        obj = (T) (object) shortValue;
                        return true;
                    }

                    if (clrType == typeof(ushort) && ushort.TryParse(charSpan, NumberStyles.Integer, Lua.Culture, out var ushortValue))
                    {
                        obj = (T) (object) ushortValue;
                        return true;
                    }

                    if (clrType == typeof(decimal) && decimal.TryParse(charSpan, NumberStyles.Number, Lua.Culture, out var decimalValue))
                    {
                        obj = (T) (object) decimalValue;
                        return true;
                    }
#endif

                    obj = default;
                    return false;
                }
                finally
                {
                    if (rentedArray != null)
                    {
                        ArrayPool<char>.Shared.Return(rentedArray);
                    }
                }
            }
            case LuaType.Table:
            {
                if (typeof(LuaTable).IsAssignableTo(clrType) || clrType == typeof(object))
                {
                    if (LuaReference.TryCreate(L, stackIndex, out var reference))
                    {
                        obj = (T) (object) CreateTable(reference);
                        return true;
                    }
                }

                obj = default;
                return false;
            }
            case LuaType.Function:
            {
                if (typeof(LuaFunction).IsAssignableTo(clrType) || clrType == typeof(object))
                {
                    if (LuaReference.TryCreate(L, stackIndex, out var reference))
                    {
                        obj = (T) (object) CreateFunction(reference);
                        return true;
                    }
                }

                obj = default;
                return false;
            }
            case LuaType.UserData:
            {
                var ptr = lua_touserdata(L, stackIndex);
                if (ptr != null)
                {
                    if (lua_rawlen(L, stackIndex) == UserDataHandle.Size
                        && ((IntPtr*) ptr)[1] == UserDataHandle.IdentifierPtr)
                    {
                        if (UserDataHandle.TryFromUserDataPointer(ptr, out var handle)
                            && (handle.Target is T || clrType == typeof(object)))
                        {
                            obj = (T) handle.Target;
                            return true;
                        }
                    }

                    if (typeof(LuaUserData).IsAssignableTo(clrType) || clrType == typeof(object))
                    {
                        if (LuaReference.TryCreate(L, stackIndex, out var reference))
                        {
                            obj = (T) (object) CreateUserData(reference, (IntPtr) ptr);
                            return true;
                        }
                    }
                }

                obj = default;
                return false;
            }
            case LuaType.Thread:
            {
                if (clrType.IsAssignableTo(typeof(LuaReference)) || clrType == typeof(object))
                {
                    var threadPtr = lua_tothread(L, stackIndex);
                    if (threadPtr == L)
                    {
                        obj = (T) (object) Lua.MainThread;
                        return true;
                    }

                    if (LuaReference.TryCreate(L, stackIndex, out var reference))
                    {
                        obj = (T) (object) CreateThread(reference, threadPtr);
                        return true;
                    }
                }

                obj = default;
                return false;
            }
            default:
            {
                throw new ArgumentOutOfRangeException(nameof(luaType), luaType, "Unsupported Lua type.");
            }
        }
    }

    protected static PushGenericEnumerableDelegate? GetPushGenericEnumerableDelegate(Type enumerableType)
    {
        return PushGenericEnumerableActions.GetOrAdd(enumerableType, static enumerableType =>
        {
            static Type[]? GetElementTypes(Type enumerableType)
            {
                if (enumerableType.IsArray)
                {
                    if (enumerableType.GetArrayRank() != 1)
                        throw new RankException("Marshaling multidimensional arrays is not supported.");

                    var elementType = enumerableType.GetElementType();
                    if (elementType != null && elementType.IsValueType)
                        return new[] { elementType };

                    return null;
                }

                var interfaces = enumerableType.GetInterfaces();
                foreach (var @interface in interfaces)
                {
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        var innerType = @interface.GenericTypeArguments[0];
                        if (innerType.IsValueType)
                        {
                            if (innerType.IsGenericType && innerType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                            {
                                var typeArguments = innerType.GenericTypeArguments;
                                return new[] { typeArguments[0], typeArguments[1] };
                            }

                            return new[] { innerType };
                        }
                    }
                }

                return null;
            }

            var elementTypes = GetElementTypes(enumerableType);
            if (elementTypes == null)
                return null;

            var marshalerParameterExpression = Expression.Parameter(typeof(DefaultLuaMarshaler), "marshaler");
            var arrayParameterExpression = Expression.Parameter(typeof(IEnumerable), "enumerable");
            var convertArrayExpression = Expression.Convert(arrayParameterExpression, enumerableType);
            var callExpression = Expression.Call(typeof(DefaultLuaMarshaler), nameof(PushGenericEnumerable), elementTypes, marshalerParameterExpression, convertArrayExpression);
            var lambda = Expression.Lambda<PushGenericEnumerableDelegate>(callExpression, marshalerParameterExpression, arrayParameterExpression);
            return lambda.Compile();
        });
    }

    protected static void PushGenericEnumerable<T>(DefaultLuaMarshaler marshaler, IEnumerable<T> enumerable)
    {
        marshaler.Lua.Stack.EnsureFreeCapacity(1);

        var L = marshaler.Lua.GetStatePointer();
        if (enumerable is T[] array)
        {
            var length = array.Length;
            lua_createtable(L, length, 0);
            var tableIndex = lua_gettop(L);
            try
            {
                for (var i = 0; i < length; i++)
                {
                    var item = array[i];
                    marshaler.PushObject(item);

                    lua_rawseti(L, tableIndex, i + 1);
                }
            }
            catch
            {
                lua_settop(L, tableIndex - 1);
                throw;
            }
        }
        else if (enumerable is List<T> stdList)
        {
            var count = stdList.Count;
            lua_createtable(L, count, 0);
            var tableIndex = lua_gettop(L);
            try
            {
                for (var i = 0; i < count; i++)
                {
                    var item = stdList[i];
                    marshaler.PushObject(item);

                    lua_rawseti(L, tableIndex, i + 1);
                }
            }
            catch
            {
                lua_settop(L, tableIndex - 1);
                throw;
            }
        }
        else if (enumerable is IList<T> list)
        {
            var count = list.Count;
            lua_createtable(L, count, 0);
            var tableIndex = lua_gettop(L);
            try
            {
                for (var i = 0; i < count; i++)
                {
                    var item = list[i];
                    marshaler.PushObject(item);

                    lua_rawseti(L, tableIndex, i + 1);
                }
            }
            catch
            {
                lua_settop(L, tableIndex - 1);
                throw;
            }
        }
        else
        {
            lua_createtable(L, enumerable.TryGetNonEnumeratedCount(out var count) ? count : 0, 0);
            var tableIndex = lua_gettop(L);
            try
            {
                var i = 1;
                foreach (var item in enumerable)
                {
                    marshaler.PushObject(item);

                    lua_rawseti(L, tableIndex, i++);
                }
            }
            catch
            {
                lua_settop(L, tableIndex - 1);
                throw;
            }
        }
    }

    protected static void PushGenericEnumerable<TKey, TValue>(DefaultLuaMarshaler marshaler, IEnumerable<KeyValuePair<TKey, TValue>> enumerable)
        where TKey : notnull
    {
        marshaler.Lua.Stack.EnsureFreeCapacity(3);

        var L = marshaler.Lua.GetStatePointer();

        if (enumerable is Dictionary<TKey, TValue> stdDictionary)
        {
            lua_createtable(L, 0, stdDictionary.Count);
            var tableIndex = lua_gettop(L);
            try
            {
                foreach (var kvp in stdDictionary)
                {
                    marshaler.PushObject(kvp.Key);
                    marshaler.PushObject(kvp.Value);

                    lua_rawset(L, tableIndex);
                }
            }
            catch
            {
                lua_settop(L, tableIndex - 1);
                throw;
            }
        }
        else
        {
            lua_createtable(L, 0, enumerable.TryGetNonEnumeratedCount(out var count) ? count : 0);
            var tableIndex = lua_gettop(L);
            try
            {
                foreach (var kvp in enumerable)
                {
                    marshaler.PushObject(kvp.Key);
                    marshaler.PushObject(kvp.Value);

                    lua_rawset(L, tableIndex);
                }
            }
            catch
            {
                lua_settop(L, tableIndex - 1);
                throw;
            }
        }
    }

    protected virtual void PushEnumerable(IEnumerable enumerable)
    {
        var enumerableType = enumerable.GetType();
        var pushDelegate = GetPushGenericEnumerableDelegate(enumerableType);
        if (pushDelegate != null)
        {
            pushDelegate(this, enumerable);
            return;
        }

        var L = Lua.GetStatePointer();
        switch (enumerable)
        {
            case Array array:
            {
                Lua.Stack.EnsureFreeCapacity(1);

                var length = array.Length;
                lua_createtable(L, length, 0);
                var tableIndex = lua_gettop(L);
                try
                {
                    for (var i = 0; i < length; i++)
                    {
                        var item = array.GetValue(i);
                        PushObject(item);

                        lua_rawseti(L, tableIndex, i + 1);
                    }
                }
                catch
                {
                    lua_settop(L, tableIndex - 1);
                    throw;
                }

                break;
            }
            case IDictionary dictionary:
            {
                Lua.Stack.EnsureFreeCapacity(2);

                lua_createtable(L, 0, dictionary.Count);
                var tableIndex = lua_gettop(L);
                try
                {
                    var enumerator = dictionary.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        var entry = enumerator.Entry;
                        PushObject(entry.Key);
                        PushObject(entry.Value);

                        lua_rawset(L, tableIndex);
                    }
                }
                catch
                {
                    lua_settop(L, tableIndex - 1);
                    throw;
                }

                break;
            }
            case IList list:
            {
                Lua.Stack.EnsureFreeCapacity(1);

                var count = list.Count;
                lua_createtable(L, count, 0);
                var tableIndex = lua_gettop(L);
                try
                {
                    for (var i = 0; i < count; i++)
                    {
                        var item = list[i];
                        PushObject(item);

                        lua_rawseti(L, tableIndex, i + 1);
                    }
                }
                catch
                {
                    lua_settop(L, tableIndex - 1);
                    throw;
                }

                break;
            }
            default:
            {
                Lua.Stack.EnsureFreeCapacity(1);

                lua_newtable(L);
                var tableIndex = lua_gettop(L);
                try
                {
                    var i = 1;
                    foreach (var item in enumerable)
                    {
                        PushObject(item);

                        lua_rawseti(L, tableIndex, i++);
                    }
                }
                catch
                {
                    lua_settop(L, tableIndex - 1);
                    throw;
                }

                break;
            }
        }
    }

    protected virtual void PushUserData(object obj, UserDataDescriptor descriptor)
    {
        if (!_userDataHandles.TryGetValue(obj, out var handle))
        {
            _userDataHandles[obj] = handle = new UserDataHandle(Lua, obj, descriptor);
        }

        handle.Push();
    }

    /// <inheritdoc/>
    [SkipLocalsInit]
    public override void PushObject<T>(T obj)
    {
        var L = Lua.GetStatePointer();
        switch (obj)
        {
            case null:
            {
                lua_pushnil(L);
                return;
            }
            case bool:
            {
                lua_pushboolean(L, (bool) (object) obj);
                return;
            }
            case sbyte:
            {
                lua_pushinteger(L, (sbyte) (object) obj);
                return;
            }
            case byte:
            {
                lua_pushinteger(L, (byte) (object) obj);
                return;
            }
            case short:
            {
                lua_pushinteger(L, (short) (object) obj);
                return;
            }
            case ushort:
            {
                lua_pushinteger(L, (ushort) (object) obj);
                return;
            }
            case int:
            {
                lua_pushinteger(L, (int) (object) obj);
                return;
            }
            case uint:
            {
                lua_pushinteger(L, (uint) (object) obj);
                return;
            }
            case long:
            {
                lua_pushinteger(L, (long) (object) obj);
                return;
            }
            case ulong:
            {
                lua_pushinteger(L, (long) (ulong) (object) obj);
                return;
            }
            case float:
            {
                lua_pushnumber(L, (float) (object) obj);
                return;
            }
            case double:
            {
                lua_pushnumber(L, (double) (object) obj);
                return;
            }
            case decimal:
            {
                lua_pushnumber(L, (lua_Number) (decimal) (object) obj);
                return;
            }
            case char:
            {
                var charValue = (char) (object) obj;
#if NET7_0_OR_GREATER
                lua_pushstring(L, new ReadOnlySpan<char>(in charValue));
#else
                lua_pushstring(L, MemoryMarshal.CreateReadOnlySpan(ref charValue, 1));
#endif
                return;
            }
            case string:
            {
                lua_pushstring(L, (string) (object) obj);
                return;
            }
            case ReadOnlyMemory<char>:
            {
                lua_pushstring(L, ((ReadOnlyMemory<char>) (object) obj).Span);
                return;
            }
            case IntPtr:
            {
                lua_pushlightuserdata(L, ((IntPtr) (object) obj).ToPointer());
                break;
            }
            case UIntPtr:
            {
                lua_pushlightuserdata(L, ((UIntPtr) (object) obj).ToPointer());
                break;
            }
            case LuaStackValue:
            {
                ((LuaStackValue) (object) obj).PushValue();
                break;
            }
            case LuaStackValueRange:
            {
                ((LuaStackValueRange) (object) obj).PushValues();
                break;
            }
            case LuaReference:
            {
                LuaReference.PushValue((LuaReference) (object) obj);
                break;
            }
            default:
            {
                var userDataDescriptor = UserDataDescriptorProvider.GetDescriptor<T>(obj);
                if (userDataDescriptor != null)
                {
                    PushUserData(obj, userDataDescriptor);
                    return;
                }

                switch (obj)
                {
                    case Delegate:
                    {
                        if (obj is LuaCFunction)
                        {
                            lua_pushcfunction(L, (LuaCFunction) (object) obj);
                        }
                        else
                        {
                            throw new InvalidOperationException("The delegate cannot be marshaled without a user data descriptor.");
                        }

                        break;
                    }
                    case IEnumerable:
                    {
                        PushEnumerable((IEnumerable) obj);
                        break;
                    }
                    case IConvertible:
                    {
                        switch (((IConvertible) obj).GetTypeCode())
                        {
                            case TypeCode.Boolean:
                            {
                                lua_pushboolean(L, ((IConvertible) obj).ToBoolean(Lua.Culture));
                                break;
                            }
                            case TypeCode.SByte:
                            case TypeCode.Byte:
                            case TypeCode.Int16:
                            case TypeCode.UInt16:
                            case TypeCode.Int32:
                            case TypeCode.UInt32:
                            case TypeCode.Int64:
                            case TypeCode.UInt64:
                            {
                                lua_pushinteger(L, ((IConvertible) obj).ToInt64(Lua.Culture));
                                break;
                            }
                            case TypeCode.Single:
                            case TypeCode.Double:
                            case TypeCode.Decimal:
                            {
                                if (typeof(lua_Number) == typeof(double))
                                {
                                    lua_pushnumber(L, ((IConvertible) obj).ToDouble(Lua.Culture));
                                }
                                else
                                {
                                    lua_pushnumber(L, (lua_Number) ((IConvertible) obj).ToType(typeof(lua_Number), Lua.Culture));
                                }

                                break;
                            }
                            case TypeCode.Char:
                            {
                                var charValue = ((IConvertible) obj).ToChar(Lua.Culture);
#if NET7_0_OR_GREATER
                                lua_pushstring(L, new ReadOnlySpan<char>(in charValue));
#else
                                lua_pushstring(L, MemoryMarshal.CreateReadOnlySpan(ref charValue, 1));
#endif
                                break;
                            }
                            case TypeCode.String:
                            {
                                lua_pushstring(L, ((IConvertible) obj).ToString(Lua.Culture));
                                break;
                            }
                            default:
                            {
                                throw new ArgumentOutOfRangeException(nameof(obj), $"The convertible object type '{((IConvertible) obj).GetTypeCode()}' cannot be marshaled.");
                            }
                        }

                        break;
                    }
                    default:
                    {
                        throw new ArgumentException($"The object type {obj.GetType()} cannot be marshaled.", nameof(obj));
                    }
                }

                break;
            }
        }
    }
}
