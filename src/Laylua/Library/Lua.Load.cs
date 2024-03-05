using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Laylua.Marshaling;
using Laylua.Moon;
using Qommon;
using Qommon.Pooling;

namespace Laylua;

public unsafe partial class Lua
{
    public T? Evaluate<T>(string code, string? chunkName = null)
    {
        return Evaluate<T>(code.AsSpan(), chunkName.AsSpan());
    }

    public T? Evaluate<T>(ReadOnlySpan<char> code, ReadOnlySpan<char> chunkName = default)
    {
        using (var results = Evaluate(code, chunkName))
        {
            if (results.Count == 0)
            {
                Throw.InvalidOperationException("The evaluation returned no results.");
            }

            return results.First.GetValue<T>();
        }
    }

    public LuaFunctionResults Evaluate(string code, string? chunkName = null)
    {
        return Evaluate(code.AsSpan(), chunkName.AsSpan());
    }

    public LuaFunctionResults Evaluate(ReadOnlySpan<char> code, ReadOnlySpan<char> chunkName = default)
    {
        var top = Stack.Count;
        LoadString(code, chunkName);
        return LuaFunction.PCall(this, top, 0);
    }

    public void Execute(string code, string? chunkName = null)
    {
        Execute(code.AsSpan(), chunkName.AsSpan());
    }

    public void Execute(ReadOnlySpan<char> code, ReadOnlySpan<char> chunkName = default)
    {
        var L = State.L;
        LoadString(code, chunkName);
        var status = lua_pcall(L, 0, 0, 0);
        if (status.IsError())
        {
            ThrowLuaException(status);
        }
    }

    public LuaFunction Load(string code, string? chunkName = null)
    {
        return Load(code.AsSpan(), chunkName.AsSpan());
    }

    public LuaFunction Load(ReadOnlySpan<char> code, ReadOnlySpan<char> chunkName = default)
    {
        LoadString(code, chunkName);
        return Marshaler.PopValue<LuaFunction>()!;
    }

    public LuaFunction Load(Stream code, ReadOnlySpan<char> chunkName = default)
    {
        LoadStream(code, chunkName);
        return Marshaler.PopValue<LuaFunction>()!;
    }

    public LuaFunction Load(LuaChunkReader reader, ReadOnlySpan<char> chunkName = default)
    {
        LoadReader(reader, chunkName);
        return Marshaler.PopValue<LuaFunction>()!;
    }

    private void LoadString(ReadOnlySpan<char> code, ReadOnlySpan<char> chunkName)
    {
        Stack.EnsureFreeCapacity(1);

        using (var bytes = RentedArray<byte>.Rent(Encoding.UTF8.GetByteCount(code)))
        {
            Encoding.UTF8.GetBytes(code, bytes);
            LuaStatus status;
            fixed (byte* bytesPtr = bytes)
            {
                status = luaL_loadbuffer(State.L, bytesPtr, (nuint) bytes.Length, chunkName);
            }

            if (status.IsError())
            {
                ThrowLuaException(status);
            }
        }
    }

    private void LoadStream(Stream code, ReadOnlySpan<char> chunkName)
    {
        Guard.IsNotNull(code);
        Guard.CanRead(code);

        Stack.EnsureFreeCapacity(1);

        var L = State.L;
        LuaStatus status;
        if (code is MemoryStream memoryStream && memoryStream.TryGetBuffer(out var buffer))
        {
            var bufferSpan = buffer.AsSpan();
            fixed (byte* bufferPtr = bufferSpan)
            {
                status = luaL_loadbuffer(L, bufferPtr, (nuint) bufferSpan.Length, chunkName);
            }
        }
        else
        {
            using (var state = new LuaStreamReader.State(code))
            {
                status = lua_load(L, &LuaStreamReader.Read, &state, chunkName, null);
            }
        }

        if (status.IsError())
        {
            ThrowLuaException(status);
        }
    }

    private static class LuaStreamReader
    {
        private const int BufferSize = 4096;

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static byte* Read(lua_State* L, void* ud, nuint* sz)
        {
            ref var state = ref Unsafe.AsRef<State>(ud);
            var stream = Unsafe.As<Stream>(state.StreamHandle.Target!);
            var bufferSpan = new Span<byte>(state.Buffer, BufferSize);
            *sz = (nuint) stream.Read(bufferSpan);
            return state.Buffer;
        }

        public struct State(Stream stream) : IDisposable
        {
            public GCHandle StreamHandle = GCHandle.Alloc(stream);

            public byte* Buffer = (byte*) NativeMemory.Alloc(BufferSize);

            public void Dispose()
            {
                StreamHandle.Free();
                NativeMemory.Free(Buffer);
                Buffer = null;
            }
        }
    }

    private void LoadReader(LuaChunkReader reader, ReadOnlySpan<char> chunkName)
    {
        Guard.IsNotNull(reader);

        Stack.EnsureFreeCapacity(1);

        var L = State.L;
        LuaStatus status;
        var readerHandle = GCHandle.Alloc(reader);
        try
        {
            status = lua_load(L, &ReadWithCustomReader, (void*) (IntPtr) readerHandle, chunkName, null);
        }
        finally
        {
            readerHandle.Free();
        }

        if (status.IsError())
        {
            ThrowLuaException(status);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static byte* ReadWithCustomReader(lua_State* L, void* ud, nuint* sz)
        {
            ref var size = ref Unsafe.AsRef<nuint>(sz);
            var reader = Unsafe.As<LuaChunkReader>(GCHandle.FromIntPtr((IntPtr) ud).Target!);
            return reader.Read(L, out size);
        }
    }
}
