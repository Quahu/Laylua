using System;
using System.IO;
using System.Runtime.CompilerServices;
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
    /// <returns>
    ///     <see langword="0"/> if the operation succeeded.
    ///     Otherwise, any other value, indicating failure.
    /// </returns>
    public int Dump(Stream stream, bool stripDebugInformation = false)
    {
        return DumpInternal(stream, stripDebugInformation);
    }

    /// <summary>
    ///     Dumps the binary Lua code of this function to the specified stream.
    /// </summary>
    /// <param name="writer"> The writer to dump this function with. </param>
    /// <param name="stripDebugInformation"> Whether some debug information should be stripped to save on space. </param>
    /// <returns>
    ///     <see langword="0"/> if the operation succeeded.
    ///     Otherwise, any other value, indicating failure.
    /// </returns>
    public int Dump(LuaWriter writer, bool stripDebugInformation = false)
    {
        return DumpInternal(writer, stripDebugInformation);
    }

    private int DumpInternal(object target, bool stripDebugInformation)
    {
        Lua.Stack.EnsureFreeCapacity(1);

        using (Lua.Stack.SnapshotCount())
        {
            delegate* unmanaged[Cdecl]<lua_State*, void*, nuint, void*, int> writerFunctionPtr;
            if (target is Stream)
            {
                writerFunctionPtr = &WriteToStream;
            }
            else if (target is LuaWriter)
            {
                writerFunctionPtr = &WriteToCustomWriter;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(target));
            }

            var handle = GCHandle.Alloc(target);
            try
            {
                var L = Lua.GetStatePointer();
                return lua_dump(L, writerFunctionPtr, (void*) (IntPtr) handle, stripDebugInformation);
            }
            finally
            {
                handle.Free();
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static int WriteToStream(lua_State* L, void* p, nuint sz, void* ud)
        {
            try
            {
                var stream = Unsafe.As<Stream>(GCHandle.FromIntPtr((IntPtr) ud).Target!);
                stream.Write(new Span<byte>(p, (int) sz));
                return 0;
            }
            catch
            {
                return 1;
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static int WriteToCustomWriter(lua_State* L, void* p, nuint sz, void* ud)
        {
            var writer = Unsafe.As<LuaWriter>(GCHandle.FromIntPtr((IntPtr) ud).Target!);
            try
            {
                return writer.Write(L, (byte*) p, sz);
            }
            catch
            {
                return 1;
            }
        }
    }
}
