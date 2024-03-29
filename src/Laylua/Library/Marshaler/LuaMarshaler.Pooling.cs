using System.Runtime.CompilerServices;
using Qommon.Pooling;

namespace Laylua.Marshaling;

public abstract partial class LuaMarshaler
{
    internal sealed class LuaReferencePool
    {
        private readonly ObjectPool<LuaTable>? _tables;
        private readonly ObjectPool<LuaFunction>? _functions;
        private readonly ObjectPool<LuaUserData>? _userData;
        private readonly ObjectPool<LuaThread>? _threads;

        public LuaReferencePool(LuaMarshalerEntityPoolConfiguration configuration)
        {
            _tables = CreatePool(LuaTableObjectPolicy.Instance, configuration.TablePoolCapacity);
            _functions = CreatePool(LuaFunctionObjectPolicy.Instance, configuration.FunctionPoolCapacity);
            _userData = CreatePool(LuaUserDataObjectPolicy.Instance, configuration.UserDataPoolCapacity);
            _threads = CreatePool(LuaThreadObjectPolicy.Instance, configuration.ThreadPoolCapacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ObjectPool<T>? CreatePool<T>(PooledObjectPolicy<T> policy, int capacity)
            where T : LuaReference
        {
            if (capacity == 0)
                return null;

            return new DefaultObjectPool<T>(policy, capacity);
        }

        public LuaTable RentTable()
        {
            return _tables?.Rent() ?? new();
        }

        public LuaFunction RentFunction()
        {
            return _functions?.Rent() ?? new();
        }

        public LuaUserData RentUserData()
        {
            return _userData?.Rent() ?? new();
        }

        public LuaThread RentThread()
        {
            return _threads?.Rent() ?? new();
        }

        public bool Return(LuaReference reference)
        {
            return reference switch
            {
                LuaUserData userData => _userData?.Return(userData) ?? false,
                LuaTable table => _tables?.Return(table) ?? false,
                LuaFunction function => _functions?.Return(function) ?? false,
                LuaThread thread => _threads?.Return(thread) ?? false,
                _ => false
            };
        }

        private sealed class LuaTableObjectPolicy : LuaReferenceObjectPolicy<LuaTable>
        {
            public static readonly LuaTableObjectPolicy Instance = new();

            private LuaTableObjectPolicy()
            { }

            public override LuaTable Create()
            {
                return new LuaTable();
            }
        }

        private sealed class LuaFunctionObjectPolicy : LuaReferenceObjectPolicy<LuaFunction>
        {
            public static readonly LuaFunctionObjectPolicy Instance = new();

            private LuaFunctionObjectPolicy()
            { }

            public override LuaFunction Create()
            {
                return new LuaFunction();
            }
        }

        private sealed class LuaUserDataObjectPolicy : LuaReferenceObjectPolicy<LuaUserData>
        {
            public static readonly LuaUserDataObjectPolicy Instance = new();

            private LuaUserDataObjectPolicy()
            { }

            public override LuaUserData Create()
            {
                return new LuaUserData();
            }
        }

        private sealed class LuaThreadObjectPolicy : LuaReferenceObjectPolicy<LuaThread>
        {
            public static readonly LuaThreadObjectPolicy Instance = new();

            private LuaThreadObjectPolicy()
            { }

            public override LuaThread Create()
            {
                return new LuaThread();
            }
        }

        private abstract class LuaReferenceObjectPolicy<TReference> : PooledObjectPolicy<TReference>
            where TReference : LuaReference
        {
            public override bool OnReturn(TReference reference)
            {
                reference.Reset();
                return true;
            }
        }
    }
}
