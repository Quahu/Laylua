﻿using System.Collections;
using System.Collections.Generic;
using Laylua.Moon;

namespace Laylua;

public sealed unsafe partial class LuaFunction
{
    public readonly struct UpvalueCollection
    {
        private readonly LuaFunction _function;

        internal UpvalueCollection(LuaFunction function)
        {
            _function = function;
        }

        public int Count()
        {
            using (_function.Thread.Stack.SnapshotCount())
            {
                _function.Thread.Stack.Push(_function);

                var L = _function.Thread.State.L;
                var index = 1;
                while (lua_getupvalue(L, -1, lua_upvalueindex(index)).Pointer != null)
                {
                    lua_pop(L, 1);
                    index++;
                }

                return index - 1;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_function);
        }

        public struct Enumerator : IEnumerator<KeyValuePair<LuaString, LuaStackValue>>
        {
            /// <inheritdoc/>
            public readonly KeyValuePair<LuaString, LuaStackValue> Current => _current;

            /// <inheritdoc/>
            object IEnumerator.Current => Current;

            private int _index;
            private KeyValuePair<LuaString, LuaStackValue> _current;
            private readonly int _functionIndex;
            private readonly LuaFunction _function;

            public Enumerator(LuaFunction function)
            {
                _function = function;
                function.Thread.Stack.Push(_function);
                _functionIndex = lua_absindex(_function.Thread.State.L, -1);
            }

            /// <inheritdoc/>
            public bool MoveNext()
            {
                var L = _function.Thread.State.L;
                if (_index != 0)
                {
                    lua_remove(L, _current.Value.Index);
                }

                var name = lua_getupvalue(L, _functionIndex, lua_upvalueindex(_index++) + 1);
                if (name.Pointer == null)
                {
                    _current = default;
                    return false;
                }

                _current = new(name, new LuaStackValue(_function.Thread, -1));
                return true;
            }

            /// <inheritdoc/>
            public void Reset()
            {
                if (_index != 0)
                {
                    var L = _function.Thread.State.L;
                    lua_remove(L, _current.Value.Index);
                }

                _index = 0;
                _current = default;
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                var L = _function.Thread.State.L;
                lua_remove(L, _functionIndex);
            }
        }
    }
}
