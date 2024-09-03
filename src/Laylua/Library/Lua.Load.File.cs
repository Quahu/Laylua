using System;
using System.Diagnostics;
using System.IO;

namespace Laylua;

public unsafe partial class Lua
{
    private static ReadOnlySpan<byte> Utf8Preamble => [0xEF, 0xBB, 0xBF];

    /// <summary>
    ///     Loads a Lua chunk from the specified file.
    ///     <para> See <a href="https://www.lua.org/manual/5.4/manual.html#3.3.2">Lua manual</a> for more information about chunks. </para>
    /// </summary>
    /// <param name="filePath"> The path to the file to load the Lua chunk from.  </param>
    /// <param name="chunkName"> The name of the chunk. If not specified, will default to <paramref name="filePath"/>. </param>
    /// <returns>
    ///     A <see cref="LuaFunction"/> representing the loaded chunk.
    /// </returns>
    public LuaFunction LoadFile(string filePath, ReadOnlySpan<char> chunkName = default)
    {
        using (Stack.SnapshotCount())
        {
            LoadFilePath(filePath, chunkName);
            return Stack[-1].GetValue<LuaFunction>()!;
        }
    }

    private void LoadFilePath(string filePath, ReadOnlySpan<char> chunkName)
    {
        using (var fileStream = File.OpenRead(filePath))
        {
            SkipPreamble(fileStream);

            LoadStream(fileStream, chunkName.Length == 0 ? $"@{fileStream.Name}" : chunkName);
        }

        static void SkipPreamble(FileStream fileStream)
        {
            if (fileStream.Length >= Utf8Preamble.Length)
            {
                var preambleBuffer = (stackalloc byte[4])[..Utf8Preamble.Length];
                var bytesRead = fileStream.Read(preambleBuffer);
                Debug.Assert(bytesRead == Utf8Preamble.Length);

                if (preambleBuffer.SequenceEqual(Utf8Preamble))
                {
                    fileStream.Seek(Utf8Preamble.Length, SeekOrigin.Begin);
                }
            }
        }
    }
}
