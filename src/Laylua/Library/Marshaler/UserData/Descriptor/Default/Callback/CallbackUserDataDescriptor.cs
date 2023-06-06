using Laylua.Moon;

namespace Laylua.Marshaling;

/// <summary>
///     Represents a <see cref="UserDataDescriptor"/> that is callback-based.
/// </summary>
public abstract unsafe partial class CallbackUserDataDescriptor : UserDataDescriptor
{
    /// <summary>
    ///     Gets the flags of this descriptor.
    /// </summary>
    public abstract CallbackUserDataDescriptorFlags Callbacks { get; }

    private LuaCFunction? _pairs;
    private LuaCFunction? _add;
    private LuaCFunction? _subtract;
    private LuaCFunction? _multiply;
    private LuaCFunction? _modulo;
    private LuaCFunction? _power;
    private LuaCFunction? _divide;
    private LuaCFunction? _floorDivide;
    private LuaCFunction? _bitwiseAnd;
    private LuaCFunction? _bitwiseOr;
    private LuaCFunction? _bitwiseExclusiveOr;
    private LuaCFunction? _shiftLeft;
    private LuaCFunction? _shiftRight;
    private LuaCFunction? _negate;
    private LuaCFunction? _bitwiseNot;
    private LuaCFunction? _concat;
    private LuaCFunction? _length;
    private LuaCFunction? _equal;
    private LuaCFunction? _lessThan;
    private LuaCFunction? _lessThanOrEqual;
    private LuaCFunction? _index;
    private LuaCFunction? _newIndex;
    private LuaCFunction? _call;
    private LuaCFunction? _close;
    private LuaCFunction? _toString;

    protected CallbackUserDataDescriptor()
    { }

    /// <inheritdoc/>
    public override void OnMetatableCreated(Lua lua, LuaStackValue metatable)
    {
        var L = lua.GetStatePointer();
        var metatableIndex = metatable.Index;
        var callbacks = Callbacks;
        using (lua.Stack.SnapshotCount())
        {
            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Pairs))
            {
                _pairs ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return Pairs(lua, lua.Stack[1]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__pairs);
                lua_pushcfunction(L, _pairs);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Add))
            {
                _add ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return Add(lua, lua.Stack[1], lua.Stack[2]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__add);
                lua_pushcfunction(L, _add);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Subtract))
            {
                _subtract ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return Subtract(lua, lua.Stack[1], lua.Stack[2]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__sub);
                lua_pushcfunction(L, _subtract);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Multiply))
            {
                _multiply ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return Multiply(lua, lua.Stack[1], lua.Stack[2]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__mul);
                lua_pushcfunction(L, _multiply);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Modulo))
            {
                _modulo ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return Modulo(lua, lua.Stack[1], lua.Stack[2]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__mod);
                lua_pushcfunction(L, _modulo);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Power))
            {
                _power ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return Power(lua, lua.Stack[1], lua.Stack[2]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__pow);
                lua_pushcfunction(L, _power);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Divide))
            {
                _divide ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return Divide(lua, lua.Stack[1], lua.Stack[2]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__div);
                lua_pushcfunction(L, _divide);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.FloorDivide))
            {
                _floorDivide ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return FloorDivide(lua, lua.Stack[1], lua.Stack[2]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__idiv);
                lua_pushcfunction(L, _floorDivide);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.BitwiseAnd))
            {
                _bitwiseAnd ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return BitwiseAnd(lua, lua.Stack[1], lua.Stack[2]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__band);
                lua_pushcfunction(L, _bitwiseAnd);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.BitwiseOr))
            {
                _bitwiseOr ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return BitwiseOr(lua, lua.Stack[1], lua.Stack[2]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__bor);
                lua_pushcfunction(L, _bitwiseOr);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.BitwiseExclusiveOr))
            {
                _bitwiseExclusiveOr ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return BitwiseExclusiveOr(lua, lua.Stack[1], lua.Stack[2]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__bxor);
                lua_pushcfunction(L, _bitwiseExclusiveOr);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.ShiftLeft))
            {
                _shiftLeft ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return ShiftLeft(lua, lua.Stack[1], lua.Stack[2]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__shl);
                lua_pushcfunction(L, _shiftLeft);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.ShiftRight))
            {
                _shiftRight ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return ShiftRight(lua, lua.Stack[1], lua.Stack[2]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__shr);
                lua_pushcfunction(L, _shiftRight);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Negate))
            {
                _negate ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return Negate(lua, lua.Stack[1]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__unm);
                lua_pushcfunction(L, _negate);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.BitwiseNot))
            {
                _bitwiseNot ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return BitwiseNot(lua, lua.Stack[1]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__bnot);
                lua_pushcfunction(L, _bitwiseNot);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Concat))
            {
                _concat ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return Concat(lua, lua.Stack[1], lua.Stack[2]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__concat);
                lua_pushcfunction(L, _concat);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Length))
            {
                _length ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return Length(lua, lua.Stack[1]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__len);
                lua_pushcfunction(L, _length);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Equal))
            {
                _equal ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return Equal(lua, lua.Stack[1], lua.Stack[2]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__eq);
                lua_pushcfunction(L, _equal);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.LessThan))
            {
                _lessThan ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return LessThan(lua, lua.Stack[1], lua.Stack[2]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__lt);
                lua_pushcfunction(L, _lessThan);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.LessThanOrEqual))
            {
                _lessThanOrEqual ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return LessThanOrEqual(lua, lua.Stack[1], lua.Stack[2]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__le);
                lua_pushcfunction(L, _lessThanOrEqual);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Index))
            {
                _index ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return Index(lua, lua.Stack[1], lua.Stack[2]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__index);
                lua_pushcfunction(L, _index);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.NewIndex))
            {
                _newIndex ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return NewIndex(lua, lua.Stack[1], lua.Stack[2], lua.Stack[3]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__newindex);
                lua_pushcfunction(L, _newIndex);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Call))
            {
                _call ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    var top = lua_gettop(L);
                    var arguments = top == 1
                        ? LuaStackValueRange.Empty
                        : lua.Stack.GetRange(2);

                    return Call(lua, lua.Stack[1], arguments);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__call);
                lua_pushcfunction(L, _call);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Close))
            {
                _close ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return Close(lua, lua.Stack[1], lua.Stack[2]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__close);
                lua_pushcfunction(L, _close);
                lua_rawset(L, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.ToString))
            {
                _toString ??= L =>
                {
                    var lua = Lua.FromExtraSpace(L);
                    return ToString(lua, lua.Stack[1]);
                };

                lua_pushstring(L, LuaMetatableKeysUtf8.__tostring);
                lua_pushcfunction(L, _toString);
                lua_rawset(L, metatableIndex);
            }
        }
    }
}
