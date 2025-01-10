# Laylua
Laylua allows you to easily embed [Lua 5.4](https://www.lua.org/manual/5.4/manual.html) in your .NET application.

Highlights:
- plug and play
- proper handling of exceptions and Lua errors
- built-in memory allocation and instruction count limiting
- control over what gets exposed to Lua code
- optimized, zero-alloc, poolable Lua entities, no value type boxing

## Examples
```cs
using (var lua = new Lua())
{
    lua.SetGlobal("text", "Hello, World!");

    lua.OpenLibrary(LuaLibraries.Standard.Base);

    lua.Execute("print(text)");
}
```

## Documentation
[Check out the wiki](https://github.com/Quahu/Laylua/wiki).