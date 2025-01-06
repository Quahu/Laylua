using System;
using System.Collections.Generic;

namespace Laylua;

public sealed partial class Lua
{
    private readonly List<LuaLibrary> _openLibraries;

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
