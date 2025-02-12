﻿using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Laylua.Moon;
using Qommon;
using Qommon.Pooling;

namespace Laylua;

public unsafe partial class LuaThread
{
    private static ReadOnlySpan<byte> Utf8Preamble => [0xEF, 0xBB, 0xBF];

    /// <inheritdoc cref="Evaluate{T}(ReadOnlySpan{char},ReadOnlySpan{char})"/>
    public T? Evaluate<T>(string code, string? chunkName = null)
    {
        return Evaluate<T>(code.AsSpan(), chunkName.AsSpan());
    }

    /// <summary>
    ///     Loads a Lua chunk from the specified string and immediately calls it,
    ///     converting the first returned value to <typeparamref name="T"/> and and returning it.
    ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#3.3.2">Lua manual</a> for more information about chunks. </para>
    /// </summary>
    /// <param name="code"> The string containing the Lua chunk. </param>
    /// <param name="chunkName"> The name of the chunk. </param>
    /// <returns>
    ///     The first result of calling the chunk.
    /// </returns>
    public T? Evaluate<T>(ReadOnlySpan<char> code, ReadOnlySpan<char> chunkName = default)
    {
        using (var results = Evaluate(code, chunkName))
        {
            if (results.Count == 0)
            {
                Throw.InvalidOperationException("The code evaluation succeeded, but returned no results.");
            }

            return results.First.GetValue<T>();
        }
    }

    /// <summary>
    ///     Loads a Lua chunk from the specified string and immediately calls it,
    ///     converting the first returned value to <typeparamref name="T"/> and and returning it.
    ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#3.3.2">Lua manual</a> for more information about chunks. </para>
    /// </summary>
    /// <param name="utf8Code"> The string containing the Lua chunk. </param>
    /// <param name="chunkName"> The name of the chunk. </param>
    /// <returns>
    ///     The first result of calling the chunk.
    /// </returns>
    public T? Evaluate<T>(ReadOnlySpan<byte> utf8Code, ReadOnlySpan<char> chunkName = default)
    {
        using (var results = Evaluate(utf8Code, chunkName))
        {
            if (results.Count == 0)
            {
                Throw.InvalidOperationException("The code evaluation succeeded, but returned no results.");
            }

            return results.First.GetValue<T>();
        }
    }

    /// <inheritdoc cref="Evaluate(ReadOnlySpan{char},ReadOnlySpan{char})"/>
    public LuaFunctionResults Evaluate(string code, string? chunkName = null)
    {
        return Evaluate(code.AsSpan(), chunkName.AsSpan());
    }

    /// <summary>
    ///     Loads a Lua chunk from the specified string and immediately calls it,
    ///     pushing the returned values onto the stack.
    ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#3.3.2">Lua manual</a> for more information about chunks. </para>
    /// </summary>
    /// <param name="code"> The string containing the Lua chunk. </param>
    /// <param name="chunkName"> The name of the chunk. </param>
    /// <returns>
    ///     The results of calling the chunk.
    /// </returns>
    /// <seealso cref="LuaFunctionResults"/>
    public LuaFunctionResults Evaluate(ReadOnlySpan<char> code, ReadOnlySpan<char> chunkName = default)
    {
        var top = Stack.Count;
        LoadString(code, chunkName);
        return LuaFunction.PCall(this, top, 0);
    }

    /// <summary>
    ///     Loads a Lua chunk from the specified string and immediately calls it,
    ///     pushing the returned values onto the stack.
    ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#3.3.2">Lua manual</a> for more information about chunks. </para>
    /// </summary>
    /// <param name="utf8Code"> The string containing the Lua chunk. </param>
    /// <param name="chunkName"> The name of the chunk. </param>
    /// <returns>
    ///     The results of calling the chunk.
    /// </returns>
    /// <seealso cref="LuaFunctionResults"/>
    public LuaFunctionResults Evaluate(ReadOnlySpan<byte> utf8Code, ReadOnlySpan<char> chunkName = default)
    {
        var top = Stack.Count;
        LoadUtf8String(utf8Code, chunkName);
        return LuaFunction.PCall(this, top, 0);
    }

    /// <inheritdoc cref="Execute(ReadOnlySpan{char},ReadOnlySpan{char})"/>
    public void Execute(string code, string? chunkName = null)
    {
        Execute(code.AsSpan(), chunkName.AsSpan());
    }

    /// <summary>
    ///     Loads a Lua chunk from the specified string and immediately calls it,
    ///     discarding the returned values.
    ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#3.3.2">Lua manual</a> for more information about chunks. </para>
    /// </summary>
    /// <param name="code"> The string containing the Lua chunk. </param>
    /// <param name="chunkName"> The name of the chunk. </param>
    public void Execute(ReadOnlySpan<char> code, ReadOnlySpan<char> chunkName = default)
    {
        var L = State.L;
        LoadString(code, chunkName);
        var status = lua_pcall(L, 0, 0, 0);
        if (status.IsError())
        {
            ThrowLuaException(this, status);
        }
    }

    /// <summary>
    ///     Loads a Lua chunk from the specified string and immediately calls it,
    ///     discarding the returned values.
    ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#3.3.2">Lua manual</a> for more information about chunks. </para>
    /// </summary>
    /// <param name="utf8Code"> The string containing the Lua chunk. </param>
    /// <param name="chunkName"> The name of the chunk. </param>
    public void Execute(ReadOnlySpan<byte> utf8Code, ReadOnlySpan<char> chunkName = default)
    {
        var L = State.L;
        LoadUtf8String(utf8Code, chunkName);
        var status = lua_pcall(L, 0, 0, 0);
        if (status.IsError())
        {
            ThrowLuaException(this, status);
        }
    }

    /// <inheritdoc cref="Load(ReadOnlySpan{char},ReadOnlySpan{char})"/>
    public LuaFunction Load(string code, string? chunkName = null)
    {
        return Load(code.AsSpan(), chunkName.AsSpan());
    }

    /// <summary>
    ///     Loads a Lua chunk from the specified string.
    ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#3.3.2">Lua manual</a> for more information about chunks. </para>
    /// </summary>
    /// <param name="code"> The string containing the Lua chunk. </param>
    /// <param name="chunkName"> The name of the chunk. </param>
    /// <returns>
    ///     A <see cref="LuaFunction"/> representing the loaded chunk.
    /// </returns>
    public LuaFunction Load(ReadOnlySpan<char> code, ReadOnlySpan<char> chunkName = default)
    {
        using (Stack.SnapshotCount())
        {
            LoadString(code, chunkName);
            return Stack[-1].GetValue<LuaFunction>()!;
        }
    }

    /// <summary>
    ///     Loads a Lua chunk from the specified string.
    ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#3.3.2">Lua manual</a> for more information about chunks. </para>
    /// </summary>
    /// <param name="utf8Code"> The UTF-8 string containing the Lua chunk. </param>
    /// <param name="chunkName"> The name of the chunk. </param>
    /// <returns>
    ///     A <see cref="LuaFunction"/> representing the loaded chunk.
    /// </returns>
    public LuaFunction Load(ReadOnlySpan<byte> utf8Code, ReadOnlySpan<char> chunkName = default)
    {
        using (Stack.SnapshotCount())
        {
            LoadUtf8String(utf8Code, chunkName);
            return Stack[-1].GetValue<LuaFunction>()!;
        }
    }

    private void LoadString(ReadOnlySpan<char> code, ReadOnlySpan<char> chunkName)
    {
        Stack.EnsureFreeCapacity(1);

        using (var bytes = RentedArray<byte>.Rent(Encoding.UTF8.GetByteCount(code)))
        {
            Encoding.UTF8.GetBytes(code, bytes);
            LoadUtf8String(bytes, chunkName);
        }
    }

    private void LoadUtf8String(ReadOnlySpan<byte> code, ReadOnlySpan<char> chunkName)
    {
        Stack.EnsureFreeCapacity(1);

        LuaStatus status;
        fixed (byte* codePtr = code)
        {
            status = luaL_loadbuffer(State.L, codePtr, (nuint) code.Length, chunkName);
        }

        if (status.IsError())
        {
            ThrowLuaException(this, status);
        }
    }

    /// <summary>
    ///     Loads a Lua chunk from the specified file.
    ///     This method will skip the UTF-8 byte-order mark (BOM).
    ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#3.3.2">Lua manual</a> for more information about chunks. </para>
    /// </summary>
    /// <param name="filePath"> The path to the file to load the Lua chunk from.  </param>
    /// <param name="chunkName"> The name of the chunk. If not specified, will default to <paramref name="filePath"/>. </param>
    /// <returns>
    ///     A <see cref="LuaFunction"/> representing the loaded chunk.
    /// </returns>
    public LuaFunction LoadFile(string filePath, ReadOnlySpan<char> chunkName = default)
    {
        using (var fileStream = File.OpenRead(filePath))
        {
            return LoadFile(fileStream, chunkName);
        }
    }

    /// <summary>
    ///     Loads a Lua chunk from the specified file.
    ///     This method will skip the UTF-8 byte-order mark (BOM).
    ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#3.3.2">Lua manual</a> for more information about chunks. </para>
    /// </summary>
    /// <param name="stream"> The stream with the file contents to load the Lua chunk from.  </param>
    /// <param name="chunkName"> The name of the chunk. If not specified, will default to <see cref="FileStream.Name"/> if <paramref name="stream"/> is a <see cref="FileStream"/>. </param>
    /// <returns>
    ///     A <see cref="LuaFunction"/> representing the loaded chunk.
    /// </returns>
    public LuaFunction LoadFile(Stream stream, ReadOnlySpan<char> chunkName = default)
    {
        Guard.CanSeek(stream);

        SkipPreamble(stream);
        return Load(stream, chunkName.Length == 0 && stream is FileStream fileStream ? $"@{fileStream.Name}" : chunkName);

        static void SkipPreamble(Stream stream)
        {
            if (stream.Length < Utf8Preamble.Length)
                return;

            var preambleBuffer = (stackalloc byte[4])[..Utf8Preamble.Length];
            var bytesRead = stream.Read(preambleBuffer);
            Debug.Assert(bytesRead == Utf8Preamble.Length);

            if (preambleBuffer.SequenceEqual(Utf8Preamble))
            {
                stream.Seek(Utf8Preamble.Length, SeekOrigin.Begin);
            }
        }
    }

    /// <summary>
    ///     Loads a Lua chunk using the specified chunk reader.
    ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#3.3.2">Lua manual</a> for more information about chunks. </para>
    /// </summary>
    /// <param name="reader"> The chunk reader to load the Lua chunk from. </param>
    /// <param name="chunkName"> The name of the chunk. </param>
    /// <returns>
    ///     A <see cref="LuaFunction"/> representing the loaded chunk.
    /// </returns>
    /// <seealso cref="LuaChunkReader"/>
    public LuaFunction Load(LuaChunkReader reader, ReadOnlySpan<char> chunkName = default)
    {
        using (Stack.SnapshotCount())
        {
            LoadReader(reader, chunkName);
            return Stack[-1].GetValue<LuaFunction>()!;
        }
    }

    private void LoadReader(LuaChunkReader reader, ReadOnlySpan<char> chunkName)
    {
        Guard.IsNotNull(reader);

        Stack.EnsureFreeCapacity(1);

        var L = State.L;
        LuaStatus status;
        var readerHandle = GCHandle.Alloc(reader);
        var buffer = stackalloc byte[ReaderState.BufferSize];
        var state = new ReaderState(readerHandle, buffer);
        try
        {
            status = lua_load(L, &ReadWithCustomReader, &state, chunkName, null);
        }
        finally
        {
            readerHandle.Free();
        }

        if (status.IsError())
        {
            ThrowLuaException(this, status);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static byte* ReadWithCustomReader(lua_State* L, void* ud, nuint* sz)
        {
            var state = *(ReaderState*) ud;
            var reader = Unsafe.As<LuaChunkReader>(state.ReaderHandle.Target!);
            try
            {
                *sz = (nuint) reader.Read(new Span<byte>(state.BufferPtr, ReaderState.BufferSize));
                return state.BufferPtr;
            }
            catch (Exception ex)
            {
                LuaException.RaiseErrorInfo(L, "An exception occurred while reading from the chunk reader.", ex);
                return default;
            }
        }
    }

    private readonly struct ReaderState(GCHandle readerHandle, byte* bufferPtr)
    {
        public const int BufferSize = 256;

        public readonly GCHandle ReaderHandle = readerHandle;
        public readonly byte* BufferPtr = bufferPtr;
    }

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
