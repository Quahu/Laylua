# Laylua
Laylua is a .NET 7 Lua wrapper that allows you to easily embed and sandbox Lua in your application.  
It offers several advantages that make it stand out:
- **Performance**  
  Lua operations can be performed in a "zero-alloc" manner, i.e. without any needless allocations, and utilizing entity pooling for referenced Lua entities. The lifetime of the referenced entities is controlled by your code. Values can be passed to and from Lua without being boxed.
- **Flexible API Access**  
  Both low and high-level API is available. With a simple `global using static Laylua.Moon.LuaNative;` you essentially get 1:1 Lua C API experience. 
- **Preventing Panic Aborts**  
  Unlike other similar libraries, Laylua prevents the Lua panic handler from aborting the application even on Linux, making Lua interactions completely safe and far more error-forgiving.
- **Proper Sandbox Capabilities**  
  Laylua does not impose any restrictions on you, allowing you to initialize a clean Lua state with no preloaded libraries. You can then load individual libraries of your choice.

## Example
```cs
using (var lua = new Lua())
{
    lua.SetGlobal("text", "Hello, World!");

    lua.OpenLibrary(LuaLibraries.Standard.Base);

    lua.Execute("print(text)");
}
```

## Documentation
Documentation is available on [Laylua's wiki](https://github.com/Quahu/Laylua/wiki).