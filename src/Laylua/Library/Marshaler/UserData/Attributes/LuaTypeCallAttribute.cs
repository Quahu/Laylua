using System;

namespace Laylua.Marshaling;

/// <summary>
///     When applied to a method, allows the userdata object to be invoked like a function from Lua.
/// </summary>
/// <example>
///     <code>
///     [LuaType]
///     public partial class Adder
///     {
///         [LuaTypeCall]
///         public int Add(int a, int b) => a + b;
///     }
///     
///     Lua.Execute("""
///         local adder = Adder()
///         print(adder(1, 2))
///     """);
///     </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public class LuaTypeCallAttribute : Attribute;
