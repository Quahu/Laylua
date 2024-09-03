using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Laylua.Moon;

namespace Laylua;

public unsafe partial class LuaFunction
{
    /// <summary>
    ///     Dumps the binary Lua code of this function to the specified stream.
    /// </summary>
    /// <param name="stream"> The stream to dump this function to. </param>
    /// <param name="stripDebugInformation"> Whether some debug information should be stripped to save on space. </param>
    /// <exception cref="LuaException"> Thrown when an exception the given stream's <see cref="Stream.Read(byte[],int,int)"/> method . </exception>
    /// <returns>
    ///     <see langword="0"/> if the operation succeeded.
    ///     Otherwise, any other value, indicating failure.
    /// </returns>
    public int Dump(Stream stream, bool stripDebugInformation = false)
    {
        return DumpInternal(stream, stripDebugInformation);
    }

    /// <summary>
    ///     Dumps the binary Lua code of this function using the specified chunk writer.
    /// </summary>
    /// <param name="chunkWriter"> The chunk writer to dump this function with. </param>
    /// <param name="stripDebugInformation"> Whether some debug information should be stripped to save on space. </param>
    /// <returns>
    ///     <see langword="0"/> if the operation succeeded.
    ///     Otherwise, any other value, indicating failure.
    /// </returns>
    public int Dump(LuaChunkWriter chunkWriter, bool stripDebugInformation = false)
    {
        return DumpInternal(chunkWriter, stripDebugInformation);
    }

    private int DumpInternal(object target, bool stripDebugInformation)
    {
        Lua.Stack.EnsureFreeCapacity(1);

        using (Lua.Stack.SnapshotCount())
        {
            PushValue(this);

            delegate* unmanaged[Cdecl]<lua_State*, void*, nuint, void*, int> writerFunctionPtr;
            if (target is Stream)
            {
                writerFunctionPtr = &WriteToStream;
            }
            else if (target is LuaChunkWriter)
            {
                writerFunctionPtr = &WriteToCustomWriter;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(target));
            }

            var state = new WriteState(target);
            var handle = GCHandle.Alloc(state);
            try
            {
                var L = Lua.GetStatePointer();
                var result = lua_dump(L, writerFunctionPtr, (void*) (IntPtr) handle, stripDebugInformation);
                if (state.Exception != null)
                {
                    ExceptionDispatchInfo.Capture(state.Exception).Throw();
                }

                return result;
            }
            finally
            {
                handle.Free();
            }
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static int WriteToStream(lua_State* L, void* p, nuint sz, void* ud)
    {
        var state = Unsafe.As<WriteState>(GCHandle.FromIntPtr((IntPtr) ud).Target!);
        var stream = Unsafe.As<Stream>(state.Target);
        try
        {
            stream.Write(new Span<byte>(p, (int) sz));
            return 0;
        }
        catch (Exception ex)
        {
            state.Exception = new LuaException("An exception occurred while writing to the stream.", ex);
            return 1;
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static int WriteToCustomWriter(lua_State* L, void* p, nuint sz, void* ud)
    {
        var state = Unsafe.As<WriteState>(GCHandle.FromIntPtr((IntPtr) ud).Target!);
        var writer = Unsafe.As<LuaChunkWriter>(state.Target);
        try
        {
            return writer.Write(L, (byte*) p, sz);
        }
        catch (Exception ex)
        {
            state.Exception = new LuaException("An exception occurred while writing to the chunk writer.", ex);
            return 1;
        }
    }

    private sealed class WriteState(object target)
    {
        public readonly object Target = target;

        public Exception? Exception;
    }
}
