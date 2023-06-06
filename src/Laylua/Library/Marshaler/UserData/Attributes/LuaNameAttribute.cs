using System;

namespace Laylua.Marshaling;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method)]
public class LuaNameAttribute : Attribute
{
    public string Name { get; }

    public LuaNameAttribute(string name)
    {
        Name = name;
    }
}
