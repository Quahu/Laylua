using System;
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
    ///     Control messages ignore this property. <br/>
    ///     Defaults to <see langword="true"/>. <br/>
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
    public event LuaWarningEventHandler? WarningEmitted;

    private MemoryStream? _warningBuffer;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void WarningHandler(void* ud, byte* msg, int tocont)
    {
        var lua = FromExtraSpace((lua_State*) ud);
        var msgSpan = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(msg);
        bool isControl;
        if (tocont == 0)
        {
            if ((isControl = msgSpan.StartsWith("@"u8)))
            {
                ProcessControlWarningMessage(lua, msgSpan[1..]);
            }
        }
        else
        {
            (lua._warningBuffer ??= new(capacity: 128)).Write(msgSpan);
            return;
        }

        if (!isControl && (!lua.EmitsWarnings || lua.WarningEmitted == null))
        {
            return;
        }

        InvokeWarningEmitted(lua, msgSpan);
        return;

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

        static void InvokeWarningEmitted(Lua lua, ReadOnlySpan<byte> msgSpan)
        {
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
                try
                {
                    lua.WarningEmitted?.Invoke(lua, new LuaWarningEmittedEventArgs(message));
                }
                catch (Exception ex)
                {
                    LuaException.RaiseErrorInfo(lua.GetStatePointer(), "An exception occurred while invoking the warning event.", ex);
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

/// <summary>
///     Represents a method that will handle the <see cref="Lua.WarningEmitted"/> <see langword="event"/>.
/// </summary>
public delegate void LuaWarningEventHandler(object? sender, LuaWarningEmittedEventArgs e);

/// <summary>
///     Represents event data for <see cref="Lua.WarningEmitted"/>.
/// </summary>
public readonly ref struct LuaWarningEmittedEventArgs
{
    /// <summary>
    ///     Gets the warning message Lua emitted.
    /// </summary>
    public ReadOnlySpan<char> Message { get; }

    internal LuaWarningEmittedEventArgs(ReadOnlySpan<char> message)
    {
        Message = message;
    }
}
