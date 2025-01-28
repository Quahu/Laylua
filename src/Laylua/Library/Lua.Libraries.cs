using System;
using System.Collections.Generic;

namespace Laylua;

public partial class Lua
{
    /// <summary>
    ///     Gets the currently open libraries.
    /// </summary>
    public IReadOnlyList<LuaLibrary> OpenLibraries => _openLibraries;

    private readonly List<LuaLibrary> _openLibraries;

    /// <summary>
    ///     Opens all standard libraries using <see cref="LuaLibraries.Standard.EnumerateAll"/>.
    /// </summary>
    public void OpenStandardLibraries()
    {
        foreach (var library in LuaLibraries.Standard.EnumerateAll())
        {
            OpenLibrary(library);
        }
    }

    public bool OpenLibrary(LuaLibrary library)
    {
        foreach (var openLibrary in _openLibraries)
        {
            if (string.Equals(openLibrary.Name, library.Name, StringComparison.Ordinal))
                return false;
        }

        library.Open(this, false);
        _openLibraries.Add(library);
        return true;
    }

    /// <summary>
    ///     Closes all standard libraries using <see cref="LuaLibraries.Standard.EnumerateAll"/>.
    /// </summary>
    public void CloseStandardLibraries()
    {
        foreach (var library in LuaLibraries.Standard.EnumerateAll())
        {
            CloseLibrary(library.Name);
        }
    }

    public bool CloseLibrary(LuaLibrary library)
    {
        return CloseLibrary(library.Name);
    }

    public bool CloseLibrary(string libraryName)
    {
        for (var i = 0; i < _openLibraries.Count; i++)
        {
            var openLibrary = _openLibraries[i];
            if (string.Equals(openLibrary.Name, libraryName, StringComparison.Ordinal))
            {
                openLibrary.Close(this);
                _openLibraries.RemoveAt(i);
                return true;
            }
        }

        return false;
    }
}
