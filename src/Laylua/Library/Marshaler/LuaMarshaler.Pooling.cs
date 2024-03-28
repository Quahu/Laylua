using System;
using System.Runtime.CompilerServices;
using Laylua.Moon;
using Qommon.Pooling;

namespace Laylua.Marshaling;

public abstract partial class LuaMarshaler
{
    internal sealed class LuaReferencePool : IDisposable
    {
        private ObjectPool<LuaTable> _tables;
        private ObjectPool<LuaFunction> _functions;
        private ObjectPool<LuaUserData> _userData;
        private ObjectPool<LuaThread> _threads;

        public LuaReferencePool(LuaMarshalerEntityPoolConfiguration configuration)
        {
            _tables = CreatePool(LuaTableObjectPolicy.Instance, configuration.TablePoolCapacity);
            _functions = CreatePool(LuaFunctionObjectPolicy.Instance, configuration.FunctionPoolCapacity);
            _userData = CreatePool(LuaUserDataObjectPolicy.Instance, configuration.UserDataPoolCapacity);
            _threads = CreatePool(LuaThreadObjectPolicy.Instance, configuration.ThreadPoolCapacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ObjectPool<T> CreatePool<T>(PooledObjectPolicy<T> policy, int capacity)
            where T : LuaReference
        {
            return new DefaultObjectPool<T>(policy, capacity);
        }

        public LuaTable RentTable(Lua lua, int reference)
        {
            var table = _tables.Rent();
            table.Lua = lua;
            table.Reference = reference;
            return table;
        }

        public LuaFunction RentFunction(Lua lua, int reference)
        {
            var function = _functions.Rent();
            function.Lua = lua;
            function.Reference = reference;
            return function;
        }

        public LuaUserData RentUserData(Lua lua, int reference, IntPtr ptr)
        {
            var userData = _userData.Rent();
            userData.Lua = lua;
            userData.Reference = reference;
            userData.Pointer = ptr;
            return userData;
        }

        public unsafe LuaThread RentThread(Lua lua, int reference, lua_State* L)
        {
            var thread = _threads.Rent();
            thread.Lua = lua;
            thread.Reference = reference;
            thread.State = L;
            return thread;
        }

        public bool Return(LuaReference reference)
        {
            return reference switch
            {
                LuaUserData userData => _userData.Return(userData),
                LuaTable table => _tables.Return(table),
                LuaFunction function => _functions.Return(function),
                LuaThread thread => _threads.Return(thread),
                _ => false
            };
        }

        public void Dispose()
        {
            _tables = null!;
            _functions = null!;
            _userData = null!;
            _threads = null!;
        }

        private sealed class LuaTableObjectPolicy : PooledObjectPolicy<LuaTable>
        {
            public static readonly LuaTableObjectPolicy Instance = new();

            private LuaTableObjectPolicy()
            { }

            public override LuaTable Create()
            {
                return new LuaTable();
            }

            public override bool OnReturn(LuaTable obj)
            {
                obj.Reset();
                return true;
            }
        }

        private sealed class LuaFunctionObjectPolicy : PooledObjectPolicy<LuaFunction>
        {
            public static readonly LuaFunctionObjectPolicy Instance = new();

            private LuaFunctionObjectPolicy()
            { }

            public override LuaFunction Create()
            {
                return new LuaFunction();
            }

            public override bool OnReturn(LuaFunction obj)
            {
                obj.Reset();
                return true;
            }
        }

        private sealed class LuaUserDataObjectPolicy : PooledObjectPolicy<LuaUserData>
        {
            public static readonly LuaUserDataObjectPolicy Instance = new();

            private LuaUserDataObjectPolicy()
            { }

            public override LuaUserData Create()
            {
                return new LuaUserData();
            }

            public override bool OnReturn(LuaUserData obj)
            {
                obj.Reset();
                return true;
            }
        }

        private sealed class LuaThreadObjectPolicy : PooledObjectPolicy<LuaThread>
        {
            public static readonly LuaThreadObjectPolicy Instance = new();

            private LuaThreadObjectPolicy()
            { }

            public override LuaThread Create()
            {
                return new LuaThread();
            }

            public override bool OnReturn(LuaThread obj)
            {
                obj.Reset();
                return true;
            }
        }
    }
}
