using System;
using System.Text;
using Laylua.Moon;
using Qommon;
using Qommon.Pooling;

namespace Laylua;

public unsafe partial class Lua
{
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
}
