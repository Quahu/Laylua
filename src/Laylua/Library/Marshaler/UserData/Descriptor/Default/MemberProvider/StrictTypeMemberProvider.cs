using System;
using System.Collections.Generic;
using System.Reflection;

namespace Laylua.Marshaling;

/// <summary>
///     Represents a <see cref="TypeMemberProvider"/> which only allows members
///     declared by types decorated with <see cref="LuaUserDataAttribute"/>.
/// </summary>
public class StrictTypeMemberProvider : TypeMemberProvider
{
    /// <inheritdoc/>
    public override IEnumerable<PropertyInfo> EnumerateReadableProperties(Type type, bool isTypeDefinition)
    {
        foreach (var property in base.EnumerateReadableProperties(type, isTypeDefinition))
        {
            var declaringType = property.GetMethod?.GetBaseDefinition().DeclaringType;
            if (declaringType?.GetCustomAttribute<LuaUserDataAttribute>() == null)
                continue;

            yield return property;
        }
    }

    /// <inheritdoc/>
    public override IEnumerable<PropertyInfo> EnumerateWritableProperties(Type type, bool isTypeDefinition)
    {
        foreach (var property in base.EnumerateWritableProperties(type, isTypeDefinition))
        {
            var declaringType = property.SetMethod?.GetBaseDefinition().DeclaringType;
            if (declaringType?.GetCustomAttribute<LuaUserDataAttribute>() == null)
                continue;

            yield return property;
        }
    }

    /// <inheritdoc/>
    public override IEnumerable<FieldInfo> EnumerateFields(Type type, bool isTypeDefinition)
    {
        foreach (var field in base.EnumerateFields(type, isTypeDefinition))
        {
            var declaringType = field.DeclaringType;
            if (declaringType?.GetCustomAttribute<LuaUserDataAttribute>() == null)
                continue;

            yield return field;
        }
    }

    /// <inheritdoc/>
    public override IEnumerable<MethodInfo> EnumerateMethods(Type type, bool isTypeDefinition)
    {
        foreach (var method in base.EnumerateMethods(type, isTypeDefinition))
        {
            var declaringType = method.GetBaseDefinition().DeclaringType;
            if (declaringType?.GetCustomAttribute<LuaUserDataAttribute>() == null)
                continue;

            yield return method;
        }
    }
}
