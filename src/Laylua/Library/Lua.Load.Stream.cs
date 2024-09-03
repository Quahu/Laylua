using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Laylua.Moon;
using Qommon;

namespace Laylua;

public unsafe partial class Lua
{
    /// <summary>
    ///     Loads a Lua chunk from the specified UTF-8-encoded stream.
    ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#3.3.2">Lua manual</a> for more information about chunks. </para>
    /// </summary>
    /// <param name="utf8Code"> The UTF-8-encoded stream containing the Lua chunk. </param>
    /// <param name="chunkName"> The name of the chunk. </param>
    /// <returns>
    ///     A <see cref="LuaFunction"/> representing the loaded chunk.
    /// </returns>
    public LuaFunction Load(Stream utf8Code, ReadOnlySpan<char> chunkName = default)
    {
        using (Stack.SnapshotCount())
        {
            LoadStream(utf8Code, chunkName);
            return Stack[-1].GetValue<LuaFunction>()!;
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
            ThrowLuaException(this, status);
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
            try
            {
                *sz = (nuint) stream.Read(bufferSpan);
            }
            catch (Exception ex)
            {
                LuaException.RaiseErrorInfo(L, "An exception occurred while reading from the stream.", ex);
            }

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
}
