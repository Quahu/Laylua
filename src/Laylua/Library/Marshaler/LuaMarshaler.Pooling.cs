using System;
using System.Runtime.CompilerServices;
using Laylua.Moon;
using Qommon.Pooling;

namespace Laylua.Marshaling;

public abstract partial class LuaMarshaler
{
    private sealed class EntityPool
    {
        private const int TablePoolCapacity = 256;
        private const int FunctionPoolCapacity = 128;
        private const int UserDataPoolCapacity = 32;
        private const int ThreadPoolCapacity = 4;

        private readonly Lua _lua;
        private readonly ObjectPool<LuaTable> _tables;
        private readonly ObjectPool<LuaFunction> _functions;
        private readonly ObjectPool<LuaUserData> _userData;
        private readonly ObjectPool<LuaThread> _threads;

        public EntityPool(Lua lua)
        {
            _lua = lua;
            _tables = CreatePool(LuaTableObjectPolicy.Instance, TablePoolCapacity);
            _functions = CreatePool(LuaFunctionObjectPolicy.Instance, FunctionPoolCapacity);
            _userData = CreatePool(LuaUserDataObjectPolicy.Instance, UserDataPoolCapacity);
            _threads = CreatePool(LuaThreadObjectPolicy.Instance, ThreadPoolCapacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ObjectPool<T> CreatePool<T>(PooledObjectPolicy<T> policy, int capacity)
            where T : LuaReference
        {
            return new DefaultObjectPool<T>(policy, capacity);
        }

        public LuaTable RentTable(int reference)
        {
            var table = _tables.Rent();
            table.Lua = _lua;
            table.Reference = reference;
            return table;
        }

        public LuaFunction RentFunction(int reference)
        {
            var function = _functions.Rent();
            function.Lua = _lua;
            function.Reference = reference;
            return function;
        }

        public LuaUserData RentUserData(int reference, IntPtr ptr)
        {
            var userData = _userData.Rent();
            userData.Lua = _lua;
            userData.Reference = reference;
            userData.Pointer = ptr;
            return userData;
        }

        public unsafe LuaThread RentThread(int reference, lua_State* L)
        {
            var thread = _threads.Rent();
            thread.Lua = _lua;
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
