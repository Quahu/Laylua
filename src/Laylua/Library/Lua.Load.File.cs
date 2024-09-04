using System;
using System.Diagnostics;
using System.IO;

namespace Laylua;

public unsafe partial class Lua
{
    private static ReadOnlySpan<byte> Utf8Preamble => [0xEF, 0xBB, 0xBF];

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
}
