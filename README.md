# Laylua
Laylua allows you to easily embed [Lua 5.4](https://www.lua.org/manual/5.4/manual.html) in your .NET application.

Highlights:
- plug and play
- Roslyn source generators and analyzers
- automatic type marshaling
- proper handling of exceptions and Lua errors
- built with sandboxing in mind; limit memory, instructions, and what scripts can access
- optimized and type-safe, with no value type boxing

## Example
```cs
using Laylua;
using Laylua.Marshaling;

using var lua = new Lua();

lua.OpenLibrary(LuaLibraries.Standard.Base);

lua["player"] = new Player("Steve");

lua.Execute("""
    player:TakeDamage(25)
    print(player.Name, player.Health)
    """);

[LuaType]
public partial class Player(string name)
{
    public string Name { get; } = name;

    public int Health { get; private set; } = 100;

    public void TakeDamage(int amount)
    {
        Health -= amount;
    }
}
```

## Documentation
[Check out the wiki](https://github.com/Quahu/Laylua/wiki).
