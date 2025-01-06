using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Laylua.Moon;
using Qommon;

namespace Laylua;

public unsafe partial class Lua
{
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
            ThrowLuaException(this, status);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static byte* ReadWithCustomReader(lua_State* L, void* ud, nuint* sz)
        {
            ref var size = ref Unsafe.AsRef<nuint>(sz);
            var reader = Unsafe.As<LuaChunkReader>(GCHandle.FromIntPtr((IntPtr) ud).Target!);
            try
            {
                return reader.Read(L, out size);
            }
            catch (Exception ex)
            {
                LuaException.RaiseErrorInfo(L, "An exception occurred while reading from the chunk reader.", ex);
                return default;
            }
        }
    }
}
