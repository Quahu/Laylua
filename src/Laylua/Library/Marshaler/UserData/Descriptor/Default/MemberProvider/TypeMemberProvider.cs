using System;
using System.Collections.Generic;
using System.Reflection;

namespace Laylua.Marshaling;

public class TypeMemberProvider
{
    public static TypeMemberProvider Default { get; } = new();

    public static StrictTypeMemberProvider Strict { get; } = new();

    public virtual IEnumerable<PropertyInfo> EnumerateReadableProperties(Type type, bool isTypeDefinition)
    {
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | (isTypeDefinition ? BindingFlags.Static : BindingFlags.Instance);
        var properties = type.GetProperties(bindingFlags);
        foreach (var property in properties)
        {
            var indexParameters = property.GetIndexParameters();
            if (indexParameters.Length > 0)
                continue;

            if (property.GetCustomAttribute<LuaIgnoreAttribute>() != null)
                continue;

            var getMethod = property.GetMethod;
            if (getMethod == null)
                continue;

            if (!getMethod.IsPublic)
            {
                if (property.GetCustomAttribute<LuaIncludeAttribute>() == null && getMethod.GetCustomAttribute<LuaIncludeAttribute>() == null)
                    continue;
            }

            yield return property;
        }
    }

    public virtual IEnumerable<PropertyInfo> EnumerateWritableProperties(Type type, bool isTypeDefinition)
    {
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | (isTypeDefinition ? BindingFlags.Static : BindingFlags.Instance);
        var properties = type.GetProperties(bindingFlags);
        foreach (var property in properties)
        {
            var indexParameters = property.GetIndexParameters();
            if (indexParameters.Length > 0)
                continue;

            if (property.GetCustomAttribute<LuaIgnoreAttribute>() != null)
                continue;

            var setMethod = property.SetMethod;
            if (setMethod == null)
                continue;

            if (!setMethod.IsPublic)
            {
                if (property.GetCustomAttribute<LuaIncludeAttribute>() == null && setMethod.GetCustomAttribute<LuaIncludeAttribute>() == null)
                    continue;
            }

            yield return property;
        }
    }

    public virtual IEnumerable<FieldInfo> EnumerateFields(Type type, bool isTypeDefinition)
    {
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | (isTypeDefinition ? BindingFlags.Static : BindingFlags.Instance);
        var fields = type.GetFields(bindingFlags);
        foreach (var field in fields)
        {
            if (field.GetCustomAttribute<LuaIgnoreAttribute>() != null)
                continue;

            if (!field.IsPublic)
            {
                if (field.GetCustomAttribute<LuaIncludeAttribute>() == null)
                    continue;
            }

            yield return field;
        }
    }

    public virtual IEnumerable<MethodInfo> EnumerateMethods(Type type, bool isTypeDefinition)
    {
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | (isTypeDefinition ? BindingFlags.Static : BindingFlags.Instance);
        var methods = type.GetMethods(bindingFlags);
        foreach (var method in methods)
        {
            if (method.IsSpecialName)
                continue;

            if (method.GetCustomAttribute<LuaIgnoreAttribute>() != null)
                continue;

            if (!method.IsPublic)
            {
                if (method.GetCustomAttribute<LuaIncludeAttribute>() == null)
                    continue;
            }

            yield return method;
        }
    }
}
