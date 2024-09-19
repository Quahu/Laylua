using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Laylua.Moon;
using Qommon.Pooling;

namespace Laylua;

public sealed unsafe partial class Lua
{
    /// <summary>
    ///     Gets or sets whether <see cref="WarningEmitted"/> should fire for emitted warnings. <br/>
    ///     See <a href="https://www.lua.org/manual/5.4/manual.html#2.3">Error Handling (Lua manual)</a> and
    ///     <a href="https://www.lua.org/manual/5.4/manual.html#pdf-warn"><c>warn (msg1, ···) (Lua Manual)</c></a> for more information about warnings.
    /// </summary>
    /// <remarks>
    ///     This can be controlled by the content of warning messages;
    ///     can be disabled using "@off" and enabled using "@on".
    /// </remarks>
    public bool EmitsWarnings { get; set; } = true;

    /// <summary>
    ///     Fired when Lua emits a warning. <br/>
    ///     See <a href="https://www.lua.org/manual/5.4/manual.html#2.3">Error Handling (Lua manual)</a> and
    ///     <a href="https://www.lua.org/manual/5.4/manual.html#pdf-warn"><c>warn (msg1, ···) (Lua Manual)</c></a> for more information about warnings.
    /// </summary>
    /// <remarks>
    ///     Subscribed event handlers must not throw any exceptions.
    /// </remarks>
    public event EventHandler<LuaWarningEmittedEventArgs> WarningEmitted
    {
        add => (_warningHandlers ??= new()).Add(value);
        remove => _warningHandlers?.Remove(value);
    }

    private MemoryStream? _warningBuffer;
    private List<EventHandler<LuaWarningEmittedEventArgs>>? _warningHandlers;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void WarningHandler(void* ud, byte* msg, int tocont)
    {
        var lua = FromExtraSpace((lua_State*) ud);
        var msgSpan = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(msg);
        if (msgSpan.StartsWith("@"u8))
        {
            ProcessControlWarningMessage(lua, msgSpan[1..]);
            return;
        }

        if (!lua.EmitsWarnings || lua._warningHandlers == null || lua._warningHandlers.Count == 0)
        {
            return;
        }

        if (tocont != 0)
        {
            (lua._warningBuffer ??= new(capacity: 128)).Write(msgSpan);
            return;
        }

        RentedArray<char> message;
        var warningBuffer = lua._warningBuffer;
        if (warningBuffer == null || warningBuffer.Length == 0)
        {
            message = CreateWarningMessage(msgSpan);
        }
        else
        {
            warningBuffer.Write(msgSpan);
            _ = warningBuffer.TryGetBuffer(out var buffer);

            message = CreateWarningMessage(buffer);

            ClearWarningBuffer(warningBuffer);
        }

        using (message)
        {
            foreach (var handler in lua._warningHandlers)
            {
                handler.Invoke(lua, new LuaWarningEmittedEventArgs(message));
            }
        }

        static void ProcessControlWarningMessage(Lua lua, ReadOnlySpan<byte> controlMsg)
        {
            if (controlMsg.SequenceEqual("on"u8))
            {
                lua.EmitsWarnings = true;
            }
            else if (controlMsg.SequenceEqual("off"u8))
            {
                lua.EmitsWarnings = false;

                if (lua._warningBuffer != null)
                {
                    ClearWarningBuffer(lua._warningBuffer);
                }
            }
        }

        static RentedArray<char> CreateWarningMessage(ReadOnlySpan<byte> msg)
        {
            var message = RentedArray<char>.Rent(Encoding.UTF8.GetCharCount(msg));
            _ = Encoding.UTF8.GetChars(msg, message);
            return message;
        }

        static void ClearWarningBuffer(MemoryStream warningBuffer)
        {
            warningBuffer.Position = 0;
            warningBuffer.SetLength(0);
        }
    }

    /// <inheritdoc cref="EmitWarning(ReadOnlySpan{char})"/>
    public void EmitWarning(string? message)
    {
        EmitWarning(message.AsSpan());
    }

    /// <summary>
    ///     Emits a Lua warning that can fire <see cref="WarningEmitted"/>. <br/>
    ///     See <a href="https://www.lua.org/manual/5.4/manual.html#2.3">Error Handling (Lua manual)</a> and
    ///     <a href="https://www.lua.org/manual/5.4/manual.html#pdf-warn"><c>warn (msg1, ···) (Lua Manual)</c></a> for more information about warnings.
    /// </summary>
    /// <param name="message"> The warning message. </param>
    public void EmitWarning(ReadOnlySpan<char> message)
    {
        lua_warning(State.L, message, false);
    }
}
