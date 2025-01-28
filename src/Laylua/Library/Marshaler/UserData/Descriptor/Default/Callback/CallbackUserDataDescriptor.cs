using System;
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
    public override void OnMetatableCreated(LuaThread lua, LuaStackValue metatable)
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
                    using var thread = LuaThread.FromExtraSpace(L);
                    return Pairs(lua, lua.Stack[1]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__pairs, _pairs, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Add))
            {
                _add ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return Add(lua, lua.Stack[1], lua.Stack[2]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__add, _add, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Subtract))
            {
                _subtract ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return Subtract(lua, lua.Stack[1], lua.Stack[2]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__sub, _subtract, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Multiply))
            {
                _multiply ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return Multiply(lua, lua.Stack[1], lua.Stack[2]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__mul, _multiply, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Modulo))
            {
                _modulo ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return Modulo(lua, lua.Stack[1], lua.Stack[2]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__mod, _modulo, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Power))
            {
                _power ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return Power(lua, lua.Stack[1], lua.Stack[2]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__pow, _power, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Divide))
            {
                _divide ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return Divide(lua, lua.Stack[1], lua.Stack[2]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__div, _divide, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.FloorDivide))
            {
                _floorDivide ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return FloorDivide(lua, lua.Stack[1], lua.Stack[2]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__idiv, _floorDivide, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.BitwiseAnd))
            {
                _bitwiseAnd ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return BitwiseAnd(lua, lua.Stack[1], lua.Stack[2]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__band, _bitwiseAnd, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.BitwiseOr))
            {
                _bitwiseOr ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return BitwiseOr(lua, lua.Stack[1], lua.Stack[2]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__bor, _bitwiseOr, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.BitwiseExclusiveOr))
            {
                _bitwiseExclusiveOr ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return BitwiseExclusiveOr(lua, lua.Stack[1], lua.Stack[2]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__bxor, _bitwiseExclusiveOr, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.ShiftLeft))
            {
                _shiftLeft ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return ShiftLeft(lua, lua.Stack[1], lua.Stack[2]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__shl, _shiftLeft, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.ShiftRight))
            {
                _shiftRight ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return ShiftRight(lua, lua.Stack[1], lua.Stack[2]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__shr, _shiftRight, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Negate))
            {
                _negate ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return Negate(lua, lua.Stack[1]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__unm, _negate, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.BitwiseNot))
            {
                _bitwiseNot ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return BitwiseNot(lua, lua.Stack[1]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__bnot, _bitwiseNot, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Concat))
            {
                _concat ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return Concat(lua, lua.Stack[1], lua.Stack[2]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__concat, _concat, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Length))
            {
                _length ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return Length(lua, lua.Stack[1]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__len, _length, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Equal))
            {
                _equal ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return Equal(lua, lua.Stack[1], lua.Stack[2]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__eq, _equal, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.LessThan))
            {
                _lessThan ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return LessThan(lua, lua.Stack[1], lua.Stack[2]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__lt, _lessThan, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.LessThanOrEqual))
            {
                _lessThanOrEqual ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return LessThanOrEqual(lua, lua.Stack[1], lua.Stack[2]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__le, _lessThanOrEqual, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Index))
            {
                _index ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return Index(lua, lua.Stack[1], lua.Stack[2]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__index, _index, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.NewIndex))
            {
                _newIndex ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return NewIndex(lua, lua.Stack[1], lua.Stack[2], lua.Stack[3]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__newindex, _newIndex, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Call))
            {
                _call ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    var top = lua_gettop(L);
                    var arguments = top == 1
                        ? LuaStackValueRange.Empty
                        : lua.Stack.GetRange(2);

                    return Call(lua, lua.Stack[1], arguments);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__call, _call, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.Close))
            {
                _close ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return Close(lua, lua.Stack[1], lua.Stack[2]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__close, _close, metatableIndex);
            }

            if (callbacks.HasFlag(CallbackUserDataDescriptorFlags.ToString))
            {
                _toString ??= L =>
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    return ToString(lua, lua.Stack[1]);
                };

                SetFunction(L, LuaMetatableKeysUtf8.__tostring, _toString, metatableIndex);
            }
        }

        return;

        static void SetFunction(lua_State* L, ReadOnlySpan<byte> name, LuaCFunction function, int metatableIndex)
        {
            lua_pushstring(L, name);
            lua_pushcfunction(L, function);
            lua_rawset(L, metatableIndex);
        }
    }
}
