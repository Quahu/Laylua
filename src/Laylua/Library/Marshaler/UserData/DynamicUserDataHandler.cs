using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Laylua.Moon;

namespace Laylua.Marshaling;

/// <summary>
///     A dynamic (reflection-based) <see cref="ILuaUserDataHandler"/> for types that are not annotated with <see cref="LuaTypeAttribute"/>.
///     Uses reflection to discover and expose members at runtime.
///     For pre-defined types where performance matters, prefer <see cref="LuaTypeAttribute"/> for source-generated handlers.
///     Respects <see cref="LuaIgnoreAttribute"/>, <see cref="LuaIncludeAttribute"/>,
///     <see cref="LuaNameAttribute"/>, and <see cref="DynamicUserDataOptions.MemberFilter"/>.
/// </summary>
public sealed unsafe class DynamicUserDataHandler : ILuaUserDataHandler
{
    private static readonly Dictionary<string, string> OperatorMetamethods = new(StringComparer.Ordinal)
    {
        ["op_Addition"] = "__add",
        ["op_Subtraction"] = "__sub",
        ["op_Multiply"] = "__mul",
        ["op_Division"] = "__div",
        ["op_Modulus"] = "__mod",
        ["op_UnaryNegation"] = "__unm",
        ["op_Equality"] = "__eq",
        ["op_LessThan"] = "__lt",
        ["op_LessThanOrEqual"] = "__le",
        ["op_BitwiseAnd"] = "__band",
        ["op_BitwiseOr"] = "__bor",
        ["op_ExclusiveOr"] = "__bxor",
        ["op_OnesComplement"] = "__bnot",
        ["op_LeftShift"] = "__shl",
        ["op_RightShift"] = "__shr",
    };

    private static readonly Dictionary<string, LuaTypeFeatures> OperatorFlags = new(StringComparer.Ordinal)
    {
        ["op_Addition"] = LuaTypeFeatures.Add,
        ["op_Subtraction"] = LuaTypeFeatures.Subtract,
        ["op_Multiply"] = LuaTypeFeatures.Multiply,
        ["op_Division"] = LuaTypeFeatures.Divide,
        ["op_Modulus"] = LuaTypeFeatures.Modulus,
        ["op_UnaryNegation"] = LuaTypeFeatures.UnaryNegation,
        ["op_Equality"] = LuaTypeFeatures.Equality,
        ["op_LessThan"] = LuaTypeFeatures.LessThan,
        ["op_LessThanOrEqual"] = LuaTypeFeatures.LessThanOrEqual,
        ["op_BitwiseAnd"] = LuaTypeFeatures.BitwiseAnd,
        ["op_BitwiseOr"] = LuaTypeFeatures.BitwiseOr,
        ["op_ExclusiveOr"] = LuaTypeFeatures.ExclusiveOr,
        ["op_OnesComplement"] = LuaTypeFeatures.OnesComplement,
        ["op_LeftShift"] = LuaTypeFeatures.LeftShift,
        ["op_RightShift"] = LuaTypeFeatures.RightShift,
    };

    private static long _nextHandlerId;
    private const string MetatablePrefix = "Laylua:";
    private const string DefinitionMetatableSuffix = "__definition";
    private const string PairsEnumeratorMetatableName = "__Laylua:PairsEnumerator";
    private static readonly ConcurrentDictionary<Type, TryGetObjectDelegate> MarshalerReaders = new();
    private static readonly ConcurrentDictionary<Type, GetObjectDelegate> MarshalerGetters = new();
    private static readonly ConcurrentDictionary<ParameterInfo, bool> NonNullableReferenceParameters = new();
    private static readonly ConcurrentDictionary<Type, object> DefaultValueTypeInstances = new();

    [ThreadStatic]
    private static NullabilityInfoContext? _nullabilityContext;

    private readonly string _metatableName;
    private readonly long _handlerId;
    private readonly bool _implementsDisposable;
    private readonly bool _implementsEnumerable;

    private readonly Dictionary<string, PropertyInfo> _readableProperties;
    private readonly Dictionary<string, Func<object, object?>> _propertyGetters;
    private readonly Dictionary<string, PropertyInfo> _writableProperties;
    private readonly Dictionary<string, Action<object, object?>> _propertySetters;
    private readonly Dictionary<string, FieldInfo> _fields;
    private readonly Dictionary<string, FieldInfo> _writableFields;
    private readonly Dictionary<string, List<MethodInfo>> _methods;
    private readonly List<MethodInfo> _typeCallMethods;
    private readonly Dictionary<string, List<MethodInfo>> _operators;
    private readonly PropertyInfo? _lengthProperty;
    private readonly List<(PropertyInfo Property, Type KeyType, Type ValueType)> _indexers;

    private readonly Dictionary<string, Func<object?, object?>> _staticPropertyGetters;
    private readonly Dictionary<string, Action<object?, object?>> _staticPropertySetters;
    private readonly Dictionary<string, PropertyInfo> _staticWritableProperties;
    private readonly Dictionary<string, FieldInfo> _staticFields;
    private readonly Dictionary<string, FieldInfo> _staticWritableFields;
    private readonly Dictionary<string, List<MethodInfo>> _staticMethods;
    private readonly (ConstructorInfo? Constructor, ParameterInfo[] Parameters, bool HasParams, int RequiredCount)[] _constructors;

    private readonly Dictionary<string, EventInfo> _events;
    private readonly Dictionary<string, EventInfo> _staticEvents;
    private readonly LuaEventSubscriptionTracker? _eventTracker;

    private readonly Dictionary<string, nint> _methodFunctionPtrs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LuaCFunction> _methodFunctions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, nint> _operatorFunctionPtrs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LuaCFunction> _operatorFunctions = new(StringComparer.Ordinal);
    private nint _lenFunctionPtr;
    private LuaCFunction? _lenFunction;

    private nint _indexFunctionPtr;
    private nint _newIndexFunctionPtr;
    private nint _gcFunctionPtr;
    private nint _toStringFunctionPtr;
    private nint _pairsFunctionPtr;
    private nint _closeFunctionPtr;
    private nint _pairsIteratorFunctionPtr;
    private nint _pairsEnumeratorGcFunctionPtr;

    private nint _definitionIndexFunctionPtr;
    private nint _definitionNewIndexFunctionPtr;
    private nint _definitionCallFunctionPtr;
    private nint _definitionFactoryFunctionPtr;
    private nint _definitionGcFunctionPtr;
    private nint _typeCallFunctionPtr;

    private LuaCFunction? _indexFunction;
    private LuaCFunction? _newIndexFunction;
    private LuaCFunction? _gcFunction;
    private LuaCFunction? _toStringFunction;
    private LuaCFunction? _pairsFunction;
    private LuaCFunction? _closeFunction;
    private LuaCFunction? _pairsIteratorFunction;
    private LuaCFunction? _pairsEnumeratorGcFunction;

    private LuaCFunction? _definitionIndexFunction;
    private LuaCFunction? _definitionNewIndexFunction;
    private LuaCFunction? _definitionCallFunction;
    private LuaCFunction? _definitionFactoryFunction;
    private LuaCFunction? _definitionGcFunction;
    private LuaCFunction? _typeCallFunction;

    /// <inheritdoc/>
    public Type Type { get; }

    private readonly DynamicUserDataOptions _options;

    private delegate bool TryGetObjectDelegate(LuaMarshaler marshaler, LuaThread thread, int stackIndex, out object? value);

    private delegate object? GetObjectDelegate(LuaThread thread, int stackIndex, LuaAllowedValueConversions allowedValueConversions);

    public DynamicUserDataHandler(Type type)
        : this(type, new DynamicUserDataOptions())
    { }

    public DynamicUserDataHandler(Type type, DynamicUserDataOptions options)
    {
        _options = options;
        Type = type;
        _handlerId = Interlocked.Increment(ref _nextHandlerId);
        _metatableName = $"{MetatablePrefix}{BuildMetatableTypeName(type)}:{_handlerId}";
        _implementsDisposable = typeof(IDisposable).IsAssignableFrom(type);
        _implementsEnumerable = typeof(IEnumerable).IsAssignableFrom(type);

        var instanceNameOrigins = new Dictionary<string, string>(StringComparer.Ordinal);
        var staticNameOrigins = new Dictionary<string, string>(StringComparer.Ordinal);

        _readableProperties = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
        _propertyGetters = new Dictionary<string, Func<object, object?>>(StringComparer.Ordinal);
        _writableProperties = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
        _propertySetters = new Dictionary<string, Action<object, object?>>(StringComparer.Ordinal);
        _fields = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        _writableFields = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        _methods = new Dictionary<string, List<MethodInfo>>(StringComparer.Ordinal);
        _typeCallMethods = new List<MethodInfo>();

        var ignoreObsoleteMembers = _options.ObsoleteMembers != LuaObsoleteMemberHandling.Include;
        var members = _options.Members;
        var features = _options.Features;

        if (members.HasFlag(LuaTypeMembers.Properties))
        {
            foreach (var property in EnumerateReadableProperties(type, isTypeDefinition: false))
            {
                if (!ShouldExposeCandidateMember(property, ignoreObsoleteMembers))
                {
                    continue;
                }

                if (HasUnsupportedPropertySignature(property))
                {
                    continue;
                }

                var luaName = ResolveLuaName(property);
                RegisterMemberOrigin(instanceNameOrigins, luaName, $"property '{property.Name}'");
                _readableProperties[luaName] = property;
                _propertyGetters[luaName] = PropertyAccessorFactory.CreateGetter(property);
            }

            foreach (var property in EnumerateWritableProperties(type, isTypeDefinition: false))
            {
                if (!ShouldExposeCandidateMember(property, ignoreObsoleteMembers))
                {
                    continue;
                }

                if (HasUnsupportedPropertySignature(property))
                {
                    continue;
                }

                var luaName = ResolveLuaName(property);
                RegisterMemberOrigin(instanceNameOrigins, luaName, $"property '{property.Name}'");
                _writableProperties[luaName] = property;
                _propertySetters[luaName] = PropertyAccessorFactory.CreateSetter(property);
            }
        }

        if (members.HasFlag(LuaTypeMembers.Fields))
        {
            foreach (var field in EnumerateFields(type, isTypeDefinition: false))
            {
                if (!ShouldExposeCandidateMember(field, ignoreObsoleteMembers))
                {
                    continue;
                }

                if (HasUnsupportedFieldType(field))
                {
                    continue;
                }

                var luaName = ResolveLuaName(field);
                RegisterMemberOrigin(instanceNameOrigins, luaName, $"field '{field.Name}'");
                _fields[luaName] = field;

                if (!field.IsInitOnly && !field.IsLiteral)
                {
                    _writableFields[luaName] = field;
                }
            }
        }

        if (members.HasFlag(LuaTypeMembers.Methods))
        {
            foreach (var method in EnumerateMethods(type, isTypeDefinition: false))
            {
                if (method.Name is "GetType" or "Dispose")
                {
                    continue;
                }

                if (method.Name == "ToString" && (_options.ObjectMembers & LuaSystemObjectMembers.ToString) == 0)
                {
                    continue;
                }

                if (method.Name == "Equals" && (_options.ObjectMembers & LuaSystemObjectMembers.Equals) == 0)
                {
                    continue;
                }

                if (method.Name == "GetHashCode" && (_options.ObjectMembers & LuaSystemObjectMembers.GetHashCode) == 0)
                {
                    continue;
                }

                if (!ShouldExposeCandidateMember(method, ignoreObsoleteMembers))
                {
                    continue;
                }

                if (HasUnsupportedMethodSignature(method))
                {
                    continue;
                }

                if (method.GetCustomAttribute<LuaTypeCallAttribute>() != null)
                {
                    _typeCallMethods.Add(method);
                    continue;
                }

                var luaName = ResolveLuaName(method);
                RegisterMemberOrigin(instanceNameOrigins, luaName, $"method '{method.Name}'");
                if (!_methods.TryGetValue(luaName, out var list))
                {
                    list = [];
                    _methods[luaName] = list;
                }

                list.Add(method);
            }
        }

        _operators = new Dictionary<string, List<MethodInfo>>(StringComparer.Ordinal);
        if ((features & LuaTypeFeatures.Operators) != 0)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            {
                if (!method.IsSpecialName)
                {
                    continue;
                }

                if (!ShouldExposeCandidateMember(method, ignoreObsoleteMembers))
                {
                    continue;
                }

                if (OperatorMetamethods.TryGetValue(method.Name, out var metamethodName))
                {
                    if (OperatorFlags.TryGetValue(method.Name, out var flag) && (features & flag) == 0)
                    {
                        continue;
                    }

                    if (!_operators.TryGetValue(metamethodName, out var list))
                    {
                        list = [];
                        _operators[metamethodName] = list;
                    }

                    list.Add(method);
                }
            }
        }

        _lengthProperty = FindLengthProperty(type, ignoreObsoleteMembers);

        _indexers = [];
        if (members.HasFlag(LuaTypeMembers.Indexers))
        {
            foreach (var property in EnumerateIndexers(type))
            {
                if (!ShouldExposeCandidateMember(property, ignoreObsoleteMembers))
                {
                    continue;
                }

                if (HasUnsupportedPropertySignature(property))
                {
                    continue;
                }

                var indexParameters = property.GetIndexParameters();
                _indexers.Add((property, indexParameters[0].ParameterType, property.PropertyType));
            }
        }

        _events = new Dictionary<string, EventInfo>(StringComparer.Ordinal);
        _staticEvents = new Dictionary<string, EventInfo>(StringComparer.Ordinal);

        var hasAnyEvents = false;
        if (members.HasFlag(LuaTypeMembers.Events))
        {
            foreach (var eventInfo in EnumerateEvents(type, isTypeDefinition: false))
            {
                if (!ShouldExposeCandidateMember(eventInfo, ignoreObsoleteMembers))
                {
                    continue;
                }

                if (eventInfo.EventHandlerType == null)
                {
                    continue;
                }

                if (HasUnsupportedEventHandlerSignature(eventInfo))
                {
                    continue;
                }

                var luaName = ResolveLuaName(eventInfo);
                RegisterMemberOrigin(instanceNameOrigins, luaName, $"event '{eventInfo.Name}'");
                _events[luaName] = eventInfo;
                hasAnyEvents = true;
            }
        }

        if (members.HasFlag(LuaTypeMembers.StaticEvents))
        {
            foreach (var eventInfo in EnumerateEvents(type, isTypeDefinition: true))
            {
                if (!ShouldExposeCandidateMember(eventInfo, ignoreObsoleteMembers))
                {
                    continue;
                }

                if (eventInfo.EventHandlerType == null)
                {
                    continue;
                }

                if (HasUnsupportedEventHandlerSignature(eventInfo))
                {
                    continue;
                }

                var luaName = ResolveLuaName(eventInfo);
                RegisterMemberOrigin(staticNameOrigins, luaName, $"static event '{eventInfo.Name}'");
                _staticEvents[luaName] = eventInfo;
                hasAnyEvents = true;
            }
        }

        _eventTracker = hasAnyEvents ? new LuaEventSubscriptionTracker() : null;

        _staticPropertyGetters = new Dictionary<string, Func<object?, object?>>(StringComparer.Ordinal);
        _staticPropertySetters = new Dictionary<string, Action<object?, object?>>(StringComparer.Ordinal);
        _staticWritableProperties = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
        _staticFields = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        _staticWritableFields = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        _staticMethods = new Dictionary<string, List<MethodInfo>>(StringComparer.Ordinal);

        if (members.HasFlag(LuaTypeMembers.StaticProperties))
        {
            foreach (var property in EnumerateReadableProperties(type, isTypeDefinition: true))
            {
                if (!ShouldExposeCandidateMember(property, ignoreObsoleteMembers))
                {
                    continue;
                }

                if (HasUnsupportedPropertySignature(property))
                {
                    continue;
                }

                var luaName = ResolveLuaName(property);
                RegisterMemberOrigin(staticNameOrigins, luaName, $"static property '{property.Name}'");
                _staticPropertyGetters[luaName] = PropertyAccessorFactory.CreateStaticGetter(property);
            }

            foreach (var property in EnumerateWritableProperties(type, isTypeDefinition: true))
            {
                if (!ShouldExposeCandidateMember(property, ignoreObsoleteMembers))
                {
                    continue;
                }

                if (HasUnsupportedPropertySignature(property))
                {
                    continue;
                }

                var luaName = ResolveLuaName(property);
                RegisterMemberOrigin(staticNameOrigins, luaName, $"static property '{property.Name}'");
                _staticWritableProperties[luaName] = property;
                _staticPropertySetters[luaName] = PropertyAccessorFactory.CreateStaticSetter(property);
            }
        }

        if (members.HasFlag(LuaTypeMembers.StaticFields) || members.HasFlag(LuaTypeMembers.Constants))
        {
            foreach (var field in EnumerateFields(type, isTypeDefinition: true))
            {
                if (!ShouldExposeCandidateMember(field, ignoreObsoleteMembers))
                {
                    continue;
                }

                if (HasUnsupportedFieldType(field))
                {
                    continue;
                }

                var luaName = ResolveLuaName(field);
                var exposeLiteral = field.IsLiteral && members.HasFlag(LuaTypeMembers.Constants);
                var exposeStaticField = !field.IsLiteral && members.HasFlag(LuaTypeMembers.StaticFields);
                if (!exposeLiteral && !exposeStaticField)
                {
                    continue;
                }

                RegisterMemberOrigin(staticNameOrigins, luaName, $"static field '{field.Name}'");
                _staticFields[luaName] = field;

                if (!field.IsInitOnly && !field.IsLiteral)
                {
                    _staticWritableFields[luaName] = field;
                }
            }
        }

        if (members.HasFlag(LuaTypeMembers.StaticMethods))
        {
            foreach (var method in EnumerateMethods(type, isTypeDefinition: true))
            {
                if (method.Name is "GetType")
                {
                    continue;
                }

                if (method.Name == "ReferenceEquals" && (_options.ObjectMembers & LuaSystemObjectMembers.ReferenceEquals) == 0)
                {
                    continue;
                }

                // Static Equals(object, object) is also an Object member
                if (method.Name == "Equals" && method.IsStatic && (_options.ObjectMembers & LuaSystemObjectMembers.Equals) == 0)
                {
                    continue;
                }

                if (!ShouldExposeCandidateMember(method, ignoreObsoleteMembers))
                {
                    continue;
                }

                if (HasUnsupportedMethodSignature(method))
                {
                    continue;
                }

                var luaName = ResolveLuaName(method);
                RegisterMemberOrigin(staticNameOrigins, luaName, $"static method '{method.Name}'");
                if (!_staticMethods.TryGetValue(luaName, out var list))
                {
                    list = [];
                    _staticMethods[luaName] = list;
                }

                list.Add(method);
            }
        }

        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var ctorList = new List<(ConstructorInfo? Constructor, ParameterInfo[] Parameters, bool HasParams, int RequiredCount)>(constructors.Length + 1);
        foreach (var ctor in EnumerateConstructors(type))
        {
            if (!ShouldExposeCandidateMember(ctor, ignoreObsoleteMembers))
            {
                continue;
            }

            var parameters = ctor.GetParameters();
            if (HasUnsupportedConstructorParameterTypes(parameters))
            {
                continue;
            }

            var hasParams = parameters.Length > 0 && parameters[^1].IsDefined(typeof(ParamArrayAttribute));
            var requiredCount = 0;
            for (var i = 0; i < parameters.Length; i++)
            {
                if (!parameters[i].HasDefaultValue && !(i == parameters.Length - 1 && hasParams))
                {
                    requiredCount = i + 1;
                }
            }

            ctorList.Add((ctor, parameters, hasParams, requiredCount));
        }

        ctorList.Sort((a, b) => a.Parameters.Length.CompareTo(b.Parameters.Length));
        _constructors = ctorList.ToArray();

        EnsureInitialized();
    }

    private static string BuildMetatableTypeName(Type type)
    {
        var typeName = type.FullName ?? type.Name;
        return typeName.Replace('+', '.');
    }

    private bool ShouldExposeCandidateMember(MemberInfo member, bool ignoreObsoleteMembers)
    {
        if (!_options.ShouldExposeMember(member))
        {
            return false;
        }

        if (ignoreObsoleteMembers && member.IsDefined(typeof(ObsoleteAttribute)))
        {
            return false;
        }

        return true;
    }

    private static BindingFlags GetMemberBindingFlags(bool isTypeDefinition)
    {
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic;
        if (isTypeDefinition)
        {
            bindingFlags |= BindingFlags.Static | BindingFlags.FlattenHierarchy;
        }
        else
        {
            bindingFlags |= BindingFlags.Instance;
        }

        return bindingFlags;
    }

    private static IEnumerable<PropertyInfo> EnumerateReadableProperties(Type type, bool isTypeDefinition)
    {
        foreach (var property in type.GetProperties(GetMemberBindingFlags(isTypeDefinition)))
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            if (!IsPropertyAccessorExposed(property, property.GetMethod))
            {
                continue;
            }

            yield return property;
        }
    }

    private static IEnumerable<PropertyInfo> EnumerateWritableProperties(Type type, bool isTypeDefinition)
    {
        foreach (var property in type.GetProperties(GetMemberBindingFlags(isTypeDefinition)))
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            if (!IsPropertyAccessorExposed(property, property.SetMethod))
            {
                continue;
            }

            yield return property;
        }
    }

    private static IEnumerable<FieldInfo> EnumerateFields(Type type, bool isTypeDefinition)
    {
        foreach (var field in type.GetFields(GetMemberBindingFlags(isTypeDefinition)))
        {
            if (field.IsDefined(typeof(LuaIgnoreAttribute)))
            {
                continue;
            }

            if (!field.IsPublic && !field.IsDefined(typeof(LuaIncludeAttribute)))
            {
                continue;
            }

            yield return field;
        }
    }

    private static IEnumerable<MethodInfo> EnumerateMethods(Type type, bool isTypeDefinition)
    {
        foreach (var method in type.GetMethods(GetMemberBindingFlags(isTypeDefinition)))
        {
            if (method.IsSpecialName)
            {
                continue;
            }

            if (method.IsDefined(typeof(LuaIgnoreAttribute)))
            {
                continue;
            }

            if (!method.IsPublic && !method.IsDefined(typeof(LuaIncludeAttribute)))
            {
                continue;
            }

            yield return method;
        }
    }

    private static IEnumerable<PropertyInfo> EnumerateIndexers(Type type)
    {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length != 1)
            {
                continue;
            }

            if (property.IsDefined(typeof(LuaIgnoreAttribute)))
            {
                continue;
            }

            yield return property;
        }
    }

    private static IEnumerable<EventInfo> EnumerateEvents(Type type, bool isTypeDefinition)
    {
        foreach (var eventInfo in type.GetEvents(GetMemberBindingFlags(isTypeDefinition)))
        {
            if (!IsEventExposed(eventInfo))
            {
                continue;
            }

            yield return eventInfo;
        }
    }

    private static IEnumerable<ConstructorInfo> EnumerateConstructors(Type type)
    {
        foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (constructor.IsDefined(typeof(LuaIgnoreAttribute)))
            {
                continue;
            }

            if (!constructor.IsPublic && !constructor.IsDefined(typeof(LuaIncludeAttribute)))
            {
                continue;
            }

            yield return constructor;
        }
    }

    private static bool IsPropertyAccessorExposed(PropertyInfo property, MethodInfo? accessor)
    {
        if (property.IsDefined(typeof(LuaIgnoreAttribute)))
        {
            return false;
        }

        if (accessor == null)
        {
            return false;
        }

        if (!accessor.IsPublic &&
            !property.IsDefined(typeof(LuaIncludeAttribute)) &&
            !accessor.IsDefined(typeof(LuaIncludeAttribute)))
        {
            return false;
        }

        return true;
    }

    private static bool IsEventExposed(EventInfo eventInfo)
    {
        if (eventInfo.IsDefined(typeof(LuaIgnoreAttribute)))
        {
            return false;
        }

        var addMethod = eventInfo.AddMethod;
        if (addMethod != null && addMethod.IsPublic)
        {
            return true;
        }

        var removeMethod = eventInfo.RemoveMethod;
        if (removeMethod != null && removeMethod.IsPublic)
        {
            return true;
        }

        if (eventInfo.IsDefined(typeof(LuaIncludeAttribute)))
        {
            return true;
        }

        return false;
    }

    private string ResolveLuaName(MemberInfo member)
    {
        var attribute = member.GetCustomAttribute<LuaNameAttribute>();
        if (attribute != null)
        {
            return attribute.Name;
        }

        return DynamicUserDataOptions.ApplyNamingPolicy(member.Name, _options.NamingPolicy);
    }

    private void RegisterMemberOrigin(Dictionary<string, string> origins, string luaName, string origin)
    {
        if (origins.TryGetValue(luaName, out var existing))
        {
            if (existing == origin)
            {
                return;
            }

            throw new InvalidOperationException(
                $"'{Type.Name}' has multiple members mapped to Lua name '{luaName}': {existing} and {origin}.");
        }

        origins[luaName] = origin;
    }

    private static bool HasUnsupportedMethodSignature(MethodInfo method)
    {
        if (IsUnsupportedExposedType(method.ReturnType))
        {
            return true;
        }

        if (AwaitableUserData.IsAwaitableType(method.ReturnType))
        {
            foreach (var parameter in method.GetParameters())
            {
                if (ProducesLuaReturnValue(parameter) || UsesLuaStackValueRange(parameter))
                {
                    return true;
                }
            }
        }

        return HasUnsupportedParameterTypes(method.GetParameters());
    }

    private static bool HasUnsupportedParameterTypes(ParameterInfo[] parameters)
    {
        foreach (var param in parameters)
        {
            var paramType = GetLuaBoundaryType(param);
            if (paramType.IsByRefLike || paramType.IsPointer)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasUnsupportedEventParameterTypes(ParameterInfo[] parameters)
    {
        foreach (var param in parameters)
        {
            if (param.ParameterType.IsByRef || IsUnsupportedExposedType(GetLuaBoundaryType(param)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasUnsupportedConstructorParameterTypes(ParameterInfo[] parameters)
    {
        foreach (var param in parameters)
        {
            if (param.ParameterType.IsByRef || IsUnsupportedExposedType(GetLuaBoundaryType(param)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasUnsupportedPropertySignature(PropertyInfo property)
    {
        return IsUnsupportedExposedType(property.PropertyType)
            || HasUnsupportedParameterTypes(property.GetIndexParameters());
    }

    private static bool HasUnsupportedFieldType(FieldInfo field)
    {
        return IsUnsupportedExposedType(field.FieldType);
    }

    private static bool IsUnsupportedExposedType(Type type)
    {
        return type.IsByRef || type.IsByRefLike || type.IsPointer;
    }

    private static bool HasUnsupportedEventHandlerSignature(EventInfo eventInfo)
    {
        var handlerType = eventInfo.EventHandlerType;
        if (handlerType == null)
        {
            return true;
        }

        var invokeMethod = handlerType.GetMethod("Invoke");
        if (invokeMethod == null)
        {
            return true;
        }

        return IsUnsupportedExposedType(invokeMethod.ReturnType)
            || HasUnsupportedEventParameterTypes(invokeMethod.GetParameters());
    }

    private static bool ConsumesLuaArgument(ParameterInfo parameter)
    {
        return !parameter.IsOut;
    }

    private static ParameterInfo[] ComputeFixedInputParameters(ParameterInfo[] parameters, int fixedCount)
    {
        var count = 0;
        for (var i = 0; i < fixedCount; i++)
        {
            if (ConsumesLuaArgument(parameters[i]))
            {
                count++;
            }
        }

        if (count == fixedCount)
        {
            return fixedCount == parameters.Length
                ? parameters
                : parameters[..fixedCount];
        }

        var result = new ParameterInfo[count];
        var index = 0;
        for (var i = 0; i < fixedCount; i++)
        {
            if (ConsumesLuaArgument(parameters[i]))
            {
                result[index++] = parameters[i];
            }
        }

        return result;
    }

    private static bool ProducesLuaReturnValue(ParameterInfo parameter)
    {
        return parameter.IsOut || (parameter.ParameterType.IsByRef && !parameter.IsIn);
    }

    private static bool UsesLuaStackValueRange(ParameterInfo parameter)
    {
        return GetLuaBoundaryType(parameter) == typeof(LuaStackValueRange);
    }

    private static Type GetLuaBoundaryType(ParameterInfo parameter)
    {
        return GetLuaBoundaryType(parameter.ParameterType);
    }

    private static Type GetLuaBoundaryType(Type type)
    {
        return LuaTypeServices.NormalizeBoundaryType(type);
    }

    private void EnsureInitialized()
    {
        if (_indexFunction != null)
        {
            return;
        }

        _indexFunction = IndexCallback;
        _indexFunctionPtr = CreateSafeCFunction(_indexFunction);

        _newIndexFunction = NewIndexCallback;
        _newIndexFunctionPtr = CreateSafeCFunction(_newIndexFunction);

        _gcFunction = GcCallback;
        _gcFunctionPtr = CreateSafeCFunction(_gcFunction);

        if (_options.Features.HasFlag(LuaTypeFeatures.ToString))
        {
            _toStringFunction = ToStringCallback;
            _toStringFunctionPtr = CreateSafeCFunction(_toStringFunction);
        }

        var pairsBehavior = _options.PairsBehavior;

        var hasPairs = _options.Features.HasFlag(LuaTypeFeatures.Pairs)
            && (pairsBehavior == LuaPairsBehavior.Properties
                || (pairsBehavior == LuaPairsBehavior.Enumerable && _implementsEnumerable));

        if (hasPairs)
        {
            _pairsFunction = PairsCallback;
            _pairsFunctionPtr = CreateSafeCFunction(_pairsFunction);

            if (pairsBehavior == LuaPairsBehavior.Enumerable && _implementsEnumerable)
            {
                _pairsIteratorFunction = PairsIteratorCallback;
                _pairsIteratorFunctionPtr = CreateSafeCFunction(_pairsIteratorFunction);
                _pairsEnumeratorGcFunction = PairsEnumeratorGcCallback;
                _pairsEnumeratorGcFunctionPtr = CreateSafeCFunction(_pairsEnumeratorGcFunction);
            }
        }

        if (_implementsDisposable && _options.Features.HasFlag(LuaTypeFeatures.Dispose))
        {
            _closeFunction = CloseCallback;
            _closeFunctionPtr = CreateSafeCFunction(_closeFunction);
        }

        _definitionIndexFunction = DefinitionIndexCallback;
        _definitionIndexFunctionPtr = CreateSafeCFunction(_definitionIndexFunction);

        _definitionNewIndexFunction = DefinitionNewIndexCallback;
        _definitionNewIndexFunctionPtr = CreateSafeCFunction(_definitionNewIndexFunction);

        if (_options.Members.HasFlag(LuaTypeMembers.Constructors) && _options.ConstructionMode == LuaConstructionMode.Direct && _constructors.Length > 0)
        {
            _definitionCallFunction = DefinitionCallCallback;
            _definitionCallFunctionPtr = CreateSafeCFunction(_definitionCallFunction);
        }

        if (_options.Members.HasFlag(LuaTypeMembers.Constructors) && _options.ConstructionMode == LuaConstructionMode.Factory && _constructors.Length > 0)
        {
            _definitionFactoryFunction = FactoryConstructCallback;
            _definitionFactoryFunctionPtr = CreateSafeCFunction(_definitionFactoryFunction);
        }

        _definitionGcFunction = DefinitionGcCallback;
        _definitionGcFunctionPtr = CreateSafeCFunction(_definitionGcFunction);

        if ((_options.Features & LuaTypeFeatures.Operators) != 0)
        {
            foreach (var (metamethod, overloads) in _operators)
            {
                var callback = overloads.Count == 1
                    ? CreateOperatorCallback(overloads[0])
                    : CreateOperatorDispatchCallback(overloads);

                var ptr = CreateSafeCFunction(callback);
                _operatorFunctions[metamethod] = callback;
                _operatorFunctionPtrs[metamethod] = ptr;
            }
        }

        if (_lengthProperty != null && _options.Features.HasFlag(LuaTypeFeatures.Length))
        {
            _lenFunction = CreateLenCallback(_lengthProperty);
            _lenFunctionPtr = CreateSafeCFunction(_lenFunction);
        }

        if (_typeCallMethods.Count > 0)
        {
            _typeCallFunction = _typeCallMethods.Count == 1
                ? CreateMethodWrapper(_typeCallMethods[0], _typeCallMethods[0].Name)
                : CreateOverloadDispatchWrapper(_typeCallMethods[0].Name, _typeCallMethods);

            _typeCallFunctionPtr = CreateSafeCFunction(_typeCallFunction);
        }
    }

    /// <inheritdoc/>
    public void Push(lua_State* L, object value)
    {
        EnsureFreeStackCapacity(L, 4);
        LayluaState.FromExtraSpace(L).KeepAlive(this);
        var ud = LuaTypeServices.NewUserData(L, value);

        if (luaL_newmetatable(L, _metatableName))
        {
            try
            {
                SetMetamethod(L, "__index", _indexFunctionPtr);
                SetMetamethod(L, "__newindex", _newIndexFunctionPtr);
                SetMetamethod(L, "__gc", _gcFunctionPtr);

                lua_pushstring(L, "__metatable");
                lua_pushboolean(L, false);
                lua_rawset(L, -3);

                if (_toStringFunction != null)
                {
                    SetMetamethod(L, "__tostring", _toStringFunctionPtr);
                }

                if (_closeFunction != null)
                {
                    SetMetamethod(L, "__close", _closeFunctionPtr);
                }

                if (_pairsFunction != null)
                {
                    SetMetamethod(L, "__pairs", _pairsFunctionPtr);
                }

                foreach (var (metamethod, ptr) in _operatorFunctionPtrs)
                {
                    SetMetamethod(L, metamethod, ptr);
                }

                if (_lenFunction != null)
                {
                    SetMetamethod(L, "__len", _lenFunctionPtr);
                }

                if (_typeCallFunction != null)
                {
                    SetMetamethod(L, "__call", _typeCallFunctionPtr);
                }

                if (_options.OnMetatableCreated != null)
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    _options.OnMetatableCreated(thread, thread.Stack[-1]);
                }
            }
            catch
            {
                lua_pushnil(L);
                lua_setfield(L, LuaRegistry.Index, _metatableName);
                LuaTypeServices.TryFreeUserData(ud);
                lua_pop(L, 2);
                throw;
            }
        }

        lua_setmetatable(L, -2);
    }

    /// <inheritdoc/>
    public bool TryGet(lua_State* L, int stackIndex, out object? value)
    {
        var ud = (nint*) luaL_testudata(L, stackIndex, _metatableName);
        if (ud != null && LuaTypeServices.TryGetUserDataTarget(ud, out var exactTarget))
        {
            value = exactTarget;
            return true;
        }

        ud = (nint*) lua_touserdata(L, stackIndex);
        if (LuaTypeServices.TryGetUserDataTarget(ud, out var target) && Type.IsInstanceOfType(target))
        {
            value = target;
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc/>
    public bool TryGetFromTable(lua_State* L, int stackIndex, out object? value)
    {
        if (lua_type(L, stackIndex) != LuaType.Table || !TryCreateParameterlessInstance(out var instance))
        {
            value = default;
            return false;
        }

        EnsureFreeStackCapacity(L, 1);

        using var thread = LuaThread.FromExtraSpace(L);

        foreach (var (luaName, setter) in _propertySetters)
        {
            lua_getfield(L, stackIndex, luaName);
            try
            {
                if (lua_type(L, -1) != LuaType.Nil)
                {
                    var propertyType = _writableProperties[luaName].PropertyType;
                    if (!TryReadWithMarshaler(thread, -1, propertyType, out var fieldValue))
                    {
                        value = default;
                        return false;
                    }

                    setter(instance, fieldValue);
                }
            }
            finally
            {
                lua_pop(L);
            }
        }

        foreach (var (luaName, field) in _writableFields)
        {
            lua_getfield(L, stackIndex, luaName);
            try
            {
                if (lua_type(L, -1) != LuaType.Nil)
                {
                    if (!TryReadWithMarshaler(thread, -1, field.FieldType, out var fieldValue))
                    {
                        value = default;
                        return false;
                    }

                    field.SetValue(instance, fieldValue);
                }
            }
            finally
            {
                lua_pop(L);
            }
        }

        value = instance;
        return true;
    }

    /// <summary>
    ///     Validates that the userdata at the given stack index is a Laylua-managed userdata
    ///     containing an instance of the expected type, and returns the instance.
    ///     Returns luaL_error result if validation fails.
    /// </summary>
    private int ValidateSelf(lua_State* L, int stackIndex, out object self)
    {
        var ud = (nint*) lua_touserdata(L, stackIndex);
        if (!LuaTypeServices.TryGetUserDataTarget(ud, out var target))
        {
            self = null!;
            return luaL_error(L, $"{Type.Name}: 'self' is not a valid userdata");
        }

        if (!Type.IsInstanceOfType(target))
        {
            self = null!;
            return luaL_error(L, $"{Type.Name}: 'self' is not a valid {Type.Name} instance");
        }

        self = target;
        return -1;
    }

    /// <inheritdoc/>
    public void PushDefinition(lua_State* L)
    {
        var definitionMetatableName = _metatableName + DefinitionMetatableSuffix;
        EnsureFreeStackCapacity(L, 4);
        LayluaState.FromExtraSpace(L).KeepAlive(this);
        var ud = LuaTypeServices.NewUserData(L, Type);

        if (luaL_newmetatable(L, definitionMetatableName))
        {
            try
            {
                SetMetamethod(L, "__index", _definitionIndexFunctionPtr);
                SetMetamethod(L, "__newindex", _definitionNewIndexFunctionPtr);
                SetMetamethod(L, "__gc", _definitionGcFunctionPtr);

                if (_definitionCallFunctionPtr != 0)
                {
                    SetMetamethod(L, "__call", _definitionCallFunctionPtr);
                }

                lua_pushstring(L, "__metatable");
                lua_pushboolean(L, false);
                lua_rawset(L, -3);

                if (_options.OnDefinitionMetatableCreated != null)
                {
                    using var thread = LuaThread.FromExtraSpace(L);
                    _options.OnDefinitionMetatableCreated(thread, thread.Stack[-1]);
                }
            }
            catch
            {
                lua_pushnil(L);
                lua_setfield(L, LuaRegistry.Index, definitionMetatableName);
                LuaTypeServices.TryFreeUserData(ud);
                lua_pop(L, 2);
                throw;
            }
        }

        lua_setmetatable(L, -2);
    }

    /// <inheritdoc/>
    public void CleanUpState(lua_State* L)
    {
        EnsureFreeStackCapacity(L, 1);

        lua_pushnil(L);
        lua_setfield(L, LuaRegistry.Index, _metatableName);

        lua_pushnil(L);
        lua_setfield(L, LuaRegistry.Index, _metatableName + DefinitionMetatableSuffix);
    }

    private static void SetMetamethod(lua_State* L, string name, nint functionPtr)
    {
        lua_pushstring(L, name);
        lua_pushcfunction(L, functionPtr);
        lua_rawset(L, -3);
    }

    private static void EnsureFreeStackCapacity(lua_State* L, int count)
    {
        if (!lua_checkstack(L, count))
        {
            throw new InvalidOperationException($"The stack could not be resized to fit {count} extra values.");
        }
    }

    private int IndexCallback(lua_State* L)
    {
        var validationResult = ValidateSelf(L, 1, out var self);
        if (validationResult >= 0)
        {
            return validationResult;
        }

        var indexMembers = _options.IndexMembers;
        var indexPriority = _options.IndexPriority;

        if (indexPriority == LuaIndexPriority.IndexerFirst && indexMembers.HasFlag(LuaIndexMembers.Indexer) && _indexers.Count > 0)
        {
            var result = IndexerGet(L, self);
            if (result >= 0)
            {
                return result;
            }
        }

        if (lua_type(L, 2) == LuaType.String)
        {
            ReadOnlySpan<byte> keyBytes = lua_tostring(L, 2);
            var key = Encoding.UTF8.GetString(keyBytes);

            if (indexMembers.HasFlag(LuaIndexMembers.Properties))
            {
                if (_propertyGetters.TryGetValue(key, out var getter))
                {
                    var value = getter(self);
                    PushObject(L, value);
                    return 1;
                }

                if (_fields.TryGetValue(key, out var field))
                {
                    var value = field.GetValue(self);
                    PushObject(L, value);
                    return 1;
                }
            }

            if (indexMembers.HasFlag(LuaIndexMembers.Methods) && _methods.TryGetValue(key, out var methods))
            {
                PushMethodClosure(L, key, methods);
                return 1;
            }

            if (indexMembers.HasFlag(LuaIndexMembers.Events) && _eventTracker != null && _events.TryGetValue(key, out var eventInfo))
            {
                LuaEventAccessor.Push(L,
                    self,
                    eventInfo,
                    _eventTracker,
                    1,
                    null,
                    eventInfo.GetAddMethod(nonPublic: true) != null,
                    eventInfo.GetRemoveMethod(nonPublic: true) != null);

                return 1;
            }
        }

        if (indexPriority == LuaIndexPriority.MembersFirst && indexMembers.HasFlag(LuaIndexMembers.Indexer) && _indexers.Count > 0)
        {
            var result = IndexerGet(L, self);
            if (result >= 0)
            {
                return result;
            }
        }

        if (_options.UnknownPropertyBehavior == LuaUnknownPropertyBehavior.Error)
        {
            if (lua_type(L, 2) == LuaType.String)
            {
                ReadOnlySpan<byte> keyBytes = lua_tostring(L, 2);
                var key = Encoding.UTF8.GetString(keyBytes);
                luaL_error(L, $"'{key}' is not a member of '{Type.Name}'");
            }
            else
            {
                luaL_error(L, $"unknown member access on '{Type.Name}'");
            }
        }

        return 0;
    }

    private int NewIndexCallback(lua_State* L)
    {
        var validationResult = ValidateSelf(L, 1, out var self);
        if (validationResult >= 0)
        {
            return validationResult;
        }

        var indexMembers = _options.IndexMembers;
        var indexPriority = _options.IndexPriority;

        if (indexPriority == LuaIndexPriority.IndexerFirst && indexMembers.HasFlag(LuaIndexMembers.Indexer) && _indexers.Count > 0)
        {
            var result = IndexerSet(L, self);
            if (result >= 0)
            {
                return result;
            }
        }

        if (lua_type(L, 2) == LuaType.String)
        {
            ReadOnlySpan<byte> keyBytes = lua_tostring(L, 2);
            var key = Encoding.UTF8.GetString(keyBytes);

            if (indexMembers.HasFlag(LuaIndexMembers.Properties))
            {
                if (_propertySetters.TryGetValue(key, out var setter))
                {
                    var valueType = _writableProperties[key].PropertyType;
                    var value = ReadObject(L, 3, valueType);
                    var shouldDisposeValue = true;
                    try
                    {
                        setter(self, value);
                        shouldDisposeValue = false;
                    }
                    finally
                    {
                        if (shouldDisposeValue)
                        {
                            DisposeLuaReferenceValue(value, valueType);
                        }
                    }

                    return 0;
                }

                if (_writableFields.TryGetValue(key, out var field))
                {
                    var fieldType = field.FieldType;
                    var value = ReadObject(L, 3, field.FieldType);
                    var shouldDisposeValue = true;
                    try
                    {
                        field.SetValue(self, value);
                        shouldDisposeValue = false;
                    }
                    finally
                    {
                        if (shouldDisposeValue)
                        {
                            DisposeLuaReferenceValue(value, fieldType);
                        }
                    }

                    return 0;
                }

                if (_readableProperties.ContainsKey(key))
                {
                    luaL_error(L, $"'{key}' is a read-only property of '{Type.Name}'");
                    return 0;
                }

                if (_fields.ContainsKey(key) && !_writableFields.ContainsKey(key))
                {
                    luaL_error(L, $"'{key}' is a read-only field of '{Type.Name}'");
                    return 0;
                }
            }
        }

        if (indexPriority == LuaIndexPriority.MembersFirst && indexMembers.HasFlag(LuaIndexMembers.Indexer) && _indexers.Count > 0)
        {
            var result = IndexerSet(L, self);
            if (result >= 0)
            {
                return result;
            }
        }

        if (_options.UnknownPropertyBehavior == LuaUnknownPropertyBehavior.Error)
        {
            if (lua_type(L, 2) == LuaType.String)
            {
                ReadOnlySpan<byte> keyBytes = lua_tostring(L, 2);
                var key = Encoding.UTF8.GetString(keyBytes);
                luaL_error(L, $"'{key}' is not a member of '{Type.Name}'");
            }
            else
            {
                luaL_error(L, $"unknown member access on '{Type.Name}'");
            }
        }

        return 0;
    }

    private int IndexerGet(lua_State* L, object self)
    {
        foreach (var (property, keyType, _) in _indexers)
        {
            if (!IsLuaTypeCompatible(L, 2, keyType, allowedConversions: _options.BindingConversions))
            {
                continue;
            }

            var key = ReadObject(L, 2, keyType);
            try
            {
                var value = property.GetValue(self, [key]);
                PushObject(L, value);
                return 1;
            }
            catch (TargetInvocationException ex)
            {
                ThrowUnwrapped(ex);
                return 0;
            }
            finally
            {
                DisposeLuaReferenceValue(key, keyType);
            }
        }

        return -1;
    }

    private int IndexerSet(lua_State* L, object self)
    {
        foreach (var (property, keyType, valueType) in _indexers)
        {
            if (!property.CanWrite)
            {
                continue;
            }

            if (!IsLuaTypeCompatible(L, 2, keyType, allowedConversions: _options.BindingConversions))
            {
                continue;
            }

            var key = ReadObject(L, 2, keyType);
            var value = ReadObject(L, 3, valueType);
            var shouldDisposeValue = true;
            try
            {
                property.SetValue(self, value, [key]);
                shouldDisposeValue = false;
                return 0;
            }
            catch (TargetInvocationException ex)
            {
                ThrowUnwrapped(ex);
                return 0;
            }
            finally
            {
                if (shouldDisposeValue)
                {
                    DisposeLuaReferenceValue(value, valueType);
                }

                DisposeLuaReferenceValue(key, keyType);
            }
        }

        return -1;
    }

    private int GcCallback(lua_State* L)
    {
        var ud = (nint*) lua_touserdata(L, 1);
        var dispose = _implementsDisposable && _options.Features.HasFlag(LuaTypeFeatures.Dispose);
        CleanupUserData(L, ud, dispose);
        return 0;
    }

    private int CloseCallback(lua_State* L)
    {
        var ud = (nint*) lua_touserdata(L, 1);
        CleanupUserData(L, ud, dispose: true);
        return 0;
    }

    private void CleanupUserData(lua_State* L, nint* ud, bool dispose)
    {
        Exception? cleanupException = null;

        if (_eventTracker != null && ud != null && ud[0] != 0)
        {
            try
            {
                _eventTracker.CleanupAll(L, (nint) ud);
            }
            catch (Exception ex)
            {
                cleanupException = LuaTypeServices.CombineCleanupException(cleanupException, ex);
            }
        }

        if (!dispose || ud == null || !LuaTypeServices.TryGetUserDataTarget(ud, out var target))
        {
            LuaTypeServices.TryFreeUserData(ud);
            LuaTypeServices.ThrowCleanupException(cleanupException);
            return;
        }

        try
        {
            if (target is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        catch (Exception ex)
        {
            cleanupException = LuaTypeServices.CombineCleanupException(cleanupException, ex);
        }
        finally
        {
            LuaTypeServices.TryFreeUserData(ud);
        }

        LuaTypeServices.ThrowCleanupException(cleanupException);
    }

    private int ToStringCallback(lua_State* L)
    {
        var rawUd = (nint*) lua_touserdata(L, 1);
        if (rawUd == null || rawUd[0] == 0)
        {
            lua_pushstring(L, Type.Name);
            return 1;
        }

        if (!LuaTypeServices.TryGetUserDataTarget(rawUd, out var target) || !Type.IsInstanceOfType(target))
        {
            return luaL_error(L, $"{Type.Name}: 'self' is not a valid {Type.Name} instance");
        }

        lua_pushstring(L, target.ToString());
        return 1;
    }

    private int PairsCallback(lua_State* L)
    {
        var validationResult = ValidateSelf(L, 1, out var self);
        if (validationResult >= 0)
        {
            return validationResult;
        }

        if (_pairsIteratorFunction != null && self is IEnumerable enumerable)
        {
            var enumerator = enumerable.GetEnumerator();
            GCHandle handle = default;
            nint* userdataPtr = null;
            var isUserdataOnStack = false;
            var isMetatableOnStack = false;
            try
            {
                var state = new object[]
                {
                    enumerator, 0
                };

                handle = GCHandle.Alloc(state);

                userdataPtr = (nint*) lua_newuserdatauv(L, (nuint) sizeof(nint), 0);
                isUserdataOnStack = true;
                *userdataPtr = GCHandle.ToIntPtr(handle);

                if (luaL_newmetatable(L, PairsEnumeratorMetatableName))
                {
                    isMetatableOnStack = true;
                    try
                    {
                        lua_pushcfunction(L, _pairsEnumeratorGcFunctionPtr);
                        lua_setfield(L, -2, "__gc");

                        lua_pushboolean(L, false);
                        lua_setfield(L, -2, "__metatable");
                    }
                    catch
                    {
                        lua_pushnil(L);
                        lua_setfield(L, LuaRegistry.Index, PairsEnumeratorMetatableName);

                        var popCount = (isMetatableOnStack ? 1 : 0) + (isUserdataOnStack ? 1 : 0);
                        if (popCount > 0)
                        {
                            lua_pop(L, popCount);
                            isMetatableOnStack = false;
                            isUserdataOnStack = false;
                        }

                        throw;
                    }
                }
                else
                {
                    isMetatableOnStack = true;
                }

                lua_setmetatable(L, -2);
                isMetatableOnStack = false;

                lua_pushcclosure(L, _pairsIteratorFunctionPtr, 1);
                handle = default;
                isUserdataOnStack = false;
                lua_pushnil(L);
                lua_pushnil(L);
                return 3;
            }
            catch (Exception ex)
            {
                var capturedException = ex;
                var popCount = (isMetatableOnStack ? 1 : 0) + (isUserdataOnStack ? 1 : 0);
                if (popCount > 0)
                {
                    lua_pop(L, popCount);
                }

                if (handle.IsAllocated)
                {
                    handle.Free();
                    if (userdataPtr != null)
                    {
                        *userdataPtr = 0;
                    }
                }

                var dispatchException = capturedException;
                if (enumerator is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception disposeException)
                    {
                        dispatchException = new AggregateException("One or more errors occurred while preparing userdata pairs enumeration.",
                            capturedException,
                            disposeException);
                    }
                }

                ExceptionDispatchInfo.Capture(dispatchException).Throw();
                return 0;
            }
        }

        var memberCount = _readableProperties.Count + _fields.Count;
        lua_createtable(L, 0, memberCount);

        foreach (var kvp in _propertyGetters)
        {
            lua_pushstring(L, kvp.Key);
            PushObject(L, kvp.Value(self));
            lua_rawset(L, -3);
        }

        foreach (var kvp in _fields)
        {
            lua_pushstring(L, kvp.Key);
            PushObject(L, kvp.Value.GetValue(self));
            lua_rawset(L, -3);
        }

        lua_getglobal(L, "next");
        lua_pushvalue(L, -2);
        lua_remove(L, -3);
        lua_pushnil(L);
        return 3;
    }

    private static int PairsIteratorCallback(lua_State* L)
    {
        var userdataPtr = (nint*) lua_touserdata(L, lua_upvalueindex(1));
        var handle = GCHandle.FromIntPtr(*userdataPtr);
        var state = (object[]) handle.Target!;
        var enumerator = (IEnumerator) state[0];
        var index = (int) state[1];

        if (enumerator.MoveNext())
        {
            index++;
            state[1] = index;
            lua_pushinteger(L, index);
            PushObject(L, enumerator.Current);
            return 2;
        }

        LuaTypeServices.DisposePairsEnumeratorState(userdataPtr);
        return 0;
    }

    private static int PairsEnumeratorGcCallback(lua_State* L)
    {
        var userdataPtr = (nint*) lua_touserdata(L, 1);
        var ptr = *userdataPtr;
        if (ptr != 0)
        {
            var handle = GCHandle.FromIntPtr(ptr);
            try
            {
                var state = (object[]) handle.Target!;
                if (state[0] is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            finally
            {
                handle.Free();
                *userdataPtr = 0;
            }
        }

        return 0;
    }

    private readonly Dictionary<string, nint> _staticMethodFunctionPtrs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LuaCFunction> _staticMethodFunctions = new(StringComparer.Ordinal);

    private int DefinitionIndexCallback(lua_State* L)
    {
        if (lua_type(L, 2) != LuaType.String)
        {
            return 0;
        }

        ReadOnlySpan<byte> keyBytes = lua_tostring(L, 2);
        var key = Encoding.UTF8.GetString(keyBytes);

        if (_staticPropertyGetters.TryGetValue(key, out var getter))
        {
            PushObject(L, getter(null));
            return 1;
        }

        if (_staticFields.TryGetValue(key, out var field))
        {
            PushObject(L, field.GetValue(null));
            return 1;
        }

        if (_staticMethods.TryGetValue(key, out var methods))
        {
            PushStaticMethodClosure(L, key, methods);
            return 1;
        }

        if (_definitionFactoryFunctionPtr != 0 && _options.ConstructionMode == LuaConstructionMode.Factory && key == _options.FactoryMethodName)
        {
            lua_pushcfunction(L, _definitionFactoryFunctionPtr);
            return 1;
        }

        if (_eventTracker != null && _staticEvents.TryGetValue(key, out var eventInfo))
        {
            LuaEventAccessor.Push(L,
                null,
                eventInfo,
                _eventTracker,
                1,
                _handlerId,
                eventInfo.GetAddMethod(nonPublic: true) != null,
                eventInfo.GetRemoveMethod(nonPublic: true) != null);

            return 1;
        }

        return 0;
    }

    private int DefinitionNewIndexCallback(lua_State* L)
    {
        if (lua_type(L, 2) != LuaType.String)
        {
            return 0;
        }

        ReadOnlySpan<byte> keyBytes = lua_tostring(L, 2);
        var key = Encoding.UTF8.GetString(keyBytes);

        if (_staticPropertySetters.TryGetValue(key, out var setter))
        {
            var valueType = _staticWritableProperties[key].PropertyType;
            var value = ReadObject(L, 3, valueType);
            var shouldDisposeValue = true;
            try
            {
                setter(null, value);
                shouldDisposeValue = false;
            }
            finally
            {
                if (shouldDisposeValue)
                {
                    DisposeLuaReferenceValue(value, valueType);
                }
            }

            return 0;
        }

        if (_staticWritableFields.TryGetValue(key, out var field))
        {
            var fieldType = field.FieldType;
            var value = ReadObject(L, 3, fieldType);
            var shouldDisposeValue = true;
            try
            {
                field.SetValue(null, value);
                shouldDisposeValue = false;
            }
            finally
            {
                if (shouldDisposeValue)
                {
                    DisposeLuaReferenceValue(value, fieldType);
                }
            }

            return 0;
        }

        luaL_error(L, $"'{key}' is not a writable static member of '{Type.Name}'");
        return 0;
    }

    private int DefinitionCallCallback(lua_State* L)
    {
        return ConstructInstance(L, argBase: 2);
    }

    private int FactoryConstructCallback(lua_State* L)
    {
        return ConstructInstance(L, argBase: 1);
    }

    private int ConstructInstance(lua_State* L, int argBase)
    {
        var argCount = lua_gettop(L) - (argBase - 1);
        var allowedConversions = _options.BindingConversions;
        using var thread = LuaThread.FromExtraSpace(L);

        (ConstructorInfo? Constructor, ParameterInfo[] Parameters, bool HasParams, int RequiredCount)? bestMatch = null;
        (ConstructorInfo? Constructor, ParameterInfo[] Parameters, bool HasParams, int RequiredCount)? soleArityMatch = null;
        var arityMatchCount = 0;
        var bestTotalCost = int.MaxValue;

        for (var i = 0; i < _constructors.Length; i++)
        {
            var candidate = _constructors[i];
            var fixedCount = candidate.HasParams ? candidate.Parameters.Length - 1 : candidate.Parameters.Length;

            if (argCount < candidate.RequiredCount)
            {
                continue;
            }

            if (!candidate.HasParams && argCount > candidate.Parameters.Length)
            {
                continue;
            }

            arityMatchCount++;
            if (arityMatchCount == 1)
            {
                soleArityMatch = candidate;
            }

            var compatible = true;
            var totalCost = 0;
            for (var j = 0; j < Math.Min(argCount, fixedCount); j++)
            {
                var cost = GetLuaTypeCompatibilityCost(thread, j + argBase, candidate.Parameters[j].ParameterType, candidate.Parameters[j], _options.ArgumentValidation, allowedConversions: allowedConversions);
                if (cost < 0)
                {
                    compatible = false;
                    break;
                }

                totalCost += cost;
            }

            if (compatible && candidate.HasParams && argCount > fixedCount)
            {
                var paramsElementType = candidate.Parameters[^1].ParameterType.GetElementType()!;
                for (var j = fixedCount; j < argCount; j++)
                {
                    var cost = GetLuaTypeCompatibilityCost(thread, j + argBase, paramsElementType, candidate.Parameters[^1], _options.ArgumentValidation, isParamsElement: true, allowedConversions: allowedConversions);
                    if (cost < 0)
                    {
                        compatible = false;
                        break;
                    }

                    totalCost += cost;
                }
            }

            if (!compatible)
            {
                continue;
            }

            if (totalCost < bestTotalCost || (totalCost == bestTotalCost && bestMatch != null && IsMoreSpecificConstructor(candidate, bestMatch.Value, argCount)))
            {
                bestMatch = candidate;
                bestTotalCost = totalCost;
            }
        }

        // When exactly one constructor matched arity but compatibility scoring rejected it, defer to the
        // invocation-time validator so the user gets a precise error (e.g. "cannot be nil") instead of a
        // generic "no constructor accepts N arguments" message.
        if (bestMatch == null && arityMatchCount == 1)
        {
            bestMatch = soleArityMatch;
        }

        if (bestMatch == null)
        {
            if (_constructors.Length == 0)
            {
                luaL_error(L, $"'{Type.Name}' has no public constructors");
            }
            else
            {
                luaL_error(L, $"no constructor of '{Type.Name}' accepts {argCount} argument(s)");
            }

            return 0;
        }

        var match = bestMatch.Value;
        var ctor = match.Constructor;
        var parameters = match.Parameters;
        var hasParams = match.HasParams;
        var validationResult = ValidateInvocationArguments(L, Type.Name, parameters, hasParams, argCount, argBase, _options.ArgumentValidation, _options.BindingConversions);
        if (validationResult >= 0)
        {
            return validationResult;
        }

        var args = parameters.Length == 0 ? null : new object?[parameters.Length];

        if (args != null)
        {
            if (hasParams)
            {
                var fixedParamCount = parameters.Length - 1;
                for (var j = 0; j < fixedParamCount; j++)
                {
                    if (j < argCount)
                    {
                        args[j] = ReadInvocationArgument(L, j + argBase, parameters[j], _options.ArgumentValidation, _options.BindingConversions);
                    }
                    else if (parameters[j].HasDefaultValue)
                    {
                        args[j] = parameters[j].DefaultValue;
                    }
                }

                var paramsElementType = parameters[^1].ParameterType.GetElementType()!;
                var paramsCount = Math.Max(0, argCount - fixedParamCount);
                var paramsArray = Array.CreateInstance(paramsElementType, paramsCount);
                for (var k = 0; k < paramsCount; k++)
                {
                    paramsArray.SetValue(ReadInvocationArgument(L, fixedParamCount + k + argBase, paramsElementType, parameters[^1], _options.ArgumentValidation, _options.BindingConversions), k);
                }

                args[fixedParamCount] = paramsArray;
            }
            else
            {
                for (var j = 0; j < parameters.Length; j++)
                {
                    if (j < argCount)
                    {
                        args[j] = ReadInvocationArgument(L, j + argBase, parameters[j], _options.ArgumentValidation, _options.BindingConversions);
                    }
                    else if (parameters[j].HasDefaultValue)
                    {
                        args[j] = parameters[j].DefaultValue;
                    }
                }
            }
        }

        try
        {
            var instance = CreateInstance(ctor, args);
            Push(L, instance);
            return 1;
        }
        finally
        {
            DisposeLuaReferenceArguments(args, parameters);
        }
    }

    private int DefinitionGcCallback(lua_State* L)
    {
        var ud = (nint*) lua_touserdata(L, 1);
        CleanupUserData(L, ud, dispose: false);
        return 0;
    }

    private void PushStaticMethodClosure(lua_State* L, string name, List<MethodInfo> methods)
    {
        lock (_staticMethodFunctionPtrs)
        {
            if (!_staticMethodFunctionPtrs.TryGetValue(name, out var functionPtr))
            {
                var wrapper = methods.Count == 1
                    ? CreateStaticMethodWrapper(methods[0], name)
                    : CreateStaticOverloadDispatchWrapper(name, methods);

                functionPtr = CreateSafeCFunction(wrapper);
                _staticMethodFunctions[name] = wrapper;
                _staticMethodFunctionPtrs[name] = functionPtr;

                var clrNames = new string[methods.Count];
                for (var i = 0; i < methods.Count; i++)
                {
                    clrNames[i] = methods[i].Name;
                }

                LuaTypeRegistry.RegisterMethodPointer(functionPtr, Type, clrNames, isStatic: true);
            }

            lua_pushcfunction(L, functionPtr);
        }
    }

    private LuaCFunction CreateStaticMethodWrapper(MethodInfo method, string callableName)
    {
        var parameters = method.GetParameters();
        var hasParams = parameters.Length > 0 && parameters[^1].IsDefined(typeof(ParamArrayAttribute));

        return L =>
        {
            var argCount = lua_gettop(L);
            return InvokeStaticMethod(
                L,
                callableName,
                method,
                parameters,
                hasParams,
                argCount,
                _options.ArgumentValidation,
                _options.BindingConversions,
                _options.AwaitableReturnHandling);
        };
    }

    private LuaCFunction CreateStaticOverloadDispatchWrapper(string callableName, List<MethodInfo> methods)
    {
        var overloads = new (MethodInfo Method, ParameterInfo[] Parameters, bool HasParams, int RequiredCount, ParameterInfo[] FixedInputParameters)[methods.Count];
        for (var i = 0; i < methods.Count; i++)
        {
            var parameters = methods[i].GetParameters();
            var hasParams = parameters.Length > 0 && parameters[^1].IsDefined(typeof(ParamArrayAttribute));
            var requiredCount = 0;
            for (var j = 0; j < parameters.Length; j++)
            {
                if (hasParams && j == parameters.Length - 1)
                {
                    break;
                }

                if (ConsumesLuaArgument(parameters[j]) && !parameters[j].HasDefaultValue)
                {
                    requiredCount++;
                }
            }

            var fixedCount = hasParams ? parameters.Length - 1 : parameters.Length;
            var fixedInputParameters = ComputeFixedInputParameters(parameters, fixedCount);

            overloads[i] = (methods[i], parameters, hasParams, requiredCount, fixedInputParameters);
        }

        return L =>
        {
            var argCount = lua_gettop(L);
            var hasMatch = TryResolveOverload(L, overloads, argCount, stackOffset: 1, _options.ArgumentValidation, out var bestMatch, out var compatibleCount);
            if (hasMatch)
            {
                if (_options.OverloadResolution == LuaOverloadResolution.Error && compatibleCount > 1)
                {
                    return luaL_error(L, $"ambiguous overload accepts {argCount} argument(s)");
                }

                return InvokeStaticMethod(
                    L,
                    callableName,
                    bestMatch!.Value.Method,
                    bestMatch.Value.Parameters,
                    bestMatch.Value.HasParams,
                    argCount,
                    _options.ArgumentValidation,
                    _options.BindingConversions,
                    _options.AwaitableReturnHandling);
            }

            return luaL_error(L, $"no overload accepts {argCount} argument(s)");
        };
    }

    private bool TryResolveOverload(
        lua_State* L,
        (MethodInfo Method, ParameterInfo[] Parameters, bool HasParams, int RequiredCount, ParameterInfo[] FixedInputParameters)[] overloads,
        int argCount,
        int stackOffset,
        LuaArgumentValidation argumentValidation,
        out (MethodInfo Method, ParameterInfo[] Parameters, bool HasParams, int RequiredCount, ParameterInfo[] FixedInputParameters)? bestMatch,
        out int compatibleCount)
    {
        bestMatch = null;
        compatibleCount = 0;
        (MethodInfo Method, ParameterInfo[] Parameters, bool HasParams, int RequiredCount, ParameterInfo[] FixedInputParameters)? soleArityMatch = null;
        var arityMatchCount = 0;
        var bestTotalCost = int.MaxValue;
        var allowedConversions = _options.BindingConversions;
        using var thread = LuaThread.FromExtraSpace(L);

        for (var i = 0; i < overloads.Length; i++)
        {
            var overload = overloads[i];
            var fixedInputParameters = overload.FixedInputParameters;
            var fixedInputCount = fixedInputParameters.Length;

            if (argCount < overload.RequiredCount)
            {
                continue;
            }

            if (!overload.HasParams && argCount > fixedInputCount)
            {
                continue;
            }

            arityMatchCount++;
            if (arityMatchCount == 1)
            {
                soleArityMatch = overload;
            }

            var compatible = true;
            var totalCost = 0;
            for (var j = 0; j < Math.Min(argCount, fixedInputCount); j++)
            {
                var cost = GetLuaTypeCompatibilityCost(thread, j + stackOffset, fixedInputParameters[j].ParameterType, fixedInputParameters[j], argumentValidation, allowedConversions: allowedConversions);
                if (cost < 0)
                {
                    compatible = false;
                    break;
                }

                totalCost += cost;
            }

            if (compatible && overload.HasParams && argCount > fixedInputCount)
            {
                var paramsElementType = overload.Parameters[^1].ParameterType.GetElementType()!;
                for (var j = fixedInputCount; j < argCount; j++)
                {
                    var cost = GetLuaTypeCompatibilityCost(thread, j + stackOffset, paramsElementType, overload.Parameters[^1], argumentValidation, isParamsElement: true, allowedConversions: allowedConversions);
                    if (cost < 0)
                    {
                        compatible = false;
                        break;
                    }

                    totalCost += cost;
                }
            }

            if (!compatible)
            {
                continue;
            }

            compatibleCount++;
            if (totalCost < bestTotalCost || (totalCost == bestTotalCost && bestMatch != null && IsMoreSpecificOverload(overload, bestMatch.Value, argCount)))
            {
                bestMatch = overload;
                bestTotalCost = totalCost;
            }
        }

        // See ConstructInstance for rationale: defer to invocation-time validation when exactly one
        // overload matched arity but compatibility scoring rejected it.
        if (bestMatch == null && arityMatchCount == 1)
        {
            bestMatch = soleArityMatch;
        }

        return bestMatch != null;
    }

    private static int InvokeStaticMethod(
        lua_State* L,
        string callableName,
        MethodInfo method,
        ParameterInfo[] parameters,
        bool hasParams,
        int argCount,
        LuaArgumentValidation argValidation = LuaArgumentValidation.None,
        LuaAllowedValueConversions allowedConversions = LuaAllowedValueConversions.Default,
        LuaAwaitableReturnHandling awaitableReturnHandling = LuaAwaitableReturnHandling.ImplicitAwait)
    {
        var validationResult = ValidateInvocationArguments(L, callableName, parameters, hasParams, argCount, stackOffset: 1, argValidation, allowedConversions);
        if (validationResult >= 0)
        {
            return validationResult;
        }

        object?[] args;

        if (hasParams)
        {
            var fixedCount = parameters.Length - 1;
            var fixedInputCount = 0;
            args = new object?[parameters.Length];

            for (var i = 0; i < fixedCount; i++)
            {
                if (!ConsumesLuaArgument(parameters[i]))
                {
                    continue;
                }

                var stackIndex = fixedInputCount + 1;
                if (fixedInputCount < argCount)
                {
                    args[i] = ReadInvocationArgument(L, stackIndex, parameters[i], argValidation, allowedConversions);
                }
                else if (parameters[i].HasDefaultValue)
                {
                    args[i] = parameters[i].DefaultValue;
                }

                fixedInputCount++;
            }

            var paramsElementType = parameters[^1].ParameterType.GetElementType()!;
            var paramsCount = Math.Max(0, argCount - fixedInputCount);
            var paramsArray = Array.CreateInstance(paramsElementType, paramsCount);
            for (var i = 0; i < paramsCount; i++)
            {
                var stackIndex = fixedInputCount + i + 1;
                paramsArray.SetValue(ReadInvocationArgument(L, stackIndex, paramsElementType, parameters[^1], argValidation, allowedConversions), i);
            }

            args[fixedCount] = paramsArray;
        }
        else
        {
            args = new object?[parameters.Length];
            var inputCount = 0;
            for (var i = 0; i < parameters.Length; i++)
            {
                if (!ConsumesLuaArgument(parameters[i]))
                {
                    continue;
                }

                var stackIndex = inputCount + 1;
                if (inputCount < argCount)
                {
                    args[i] = ReadInvocationArgument(L, stackIndex, parameters[i], argValidation, allowedConversions);
                }
                else if (parameters[i].HasDefaultValue)
                {
                    args[i] = parameters[i].DefaultValue;
                }

                inputCount++;
            }
        }

        var returnCount = 0;
        var shouldYieldAwaitable = false;

        try
        {
            var result = method.Invoke(null, args);

            if (method.ReturnType != typeof(void))
            {
                if (AwaitableUserData.IsAwaitableType(method.ReturnType))
                {
                    if (awaitableReturnHandling == LuaAwaitableReturnHandling.ImplicitAwait)
                    {
                        LuaTypeServices.PushImplicitAwaitable(L, result);
                        shouldYieldAwaitable = true;
                    }
                    else
                    {
                        LuaTypeServices.PushAwaitable(L, result);
                    }
                }
                else
                {
                    PushObject(L, result);
                }
                returnCount++;
            }

            for (var i = 0; i < parameters.Length; i++)
            {
                if (ProducesLuaReturnValue(parameters[i]))
                {
                    PushObject(L, args[i]);
                    returnCount++;
                }
            }

        }
        catch (TargetInvocationException ex)
        {
            ThrowUnwrapped(ex);
            return 0;
        }
        finally
        {
            DisposeLuaReferenceArguments(args, parameters);
        }

        if (shouldYieldAwaitable)
        {
            return LuaAsyncLibrary.YieldAwaitable(L);
        }

        return returnCount;
    }

    private static void PushObject(lua_State* L, object? value)
    {
        using var thread = LuaThread.FromExtraSpace(L);
        thread.Stack.Push(value);
    }

    private static bool TryReadWithMarshaler(LuaThread thread, int index, Type targetType, out object? value)
    {
        targetType = GetLuaBoundaryType(targetType);
        var reader = MarshalerReaders.GetOrAdd(targetType, static type =>
        {
            var method = typeof(DynamicUserDataHandler)
                .GetMethod(nameof(TryReadWithMarshalerGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(type);

            return (TryGetObjectDelegate) method.CreateDelegate(typeof(TryGetObjectDelegate));
        });

        return reader(thread.Marshaler, thread, index, out value);
    }

    private static bool TryReadWithMarshalerGeneric<T>(LuaMarshaler marshaler, LuaThread thread, int stackIndex, out object? value)
    {
        if (marshaler.TryGetValue<T>(thread, stackIndex, out var typed))
        {
            value = typed;
            return true;
        }

        value = typed;
        return false;
    }

    private static object? GetValueWithMarshaler(
        LuaThread thread,
        int index,
        Type targetType,
        LuaAllowedValueConversions allowedValueConversions)
    {
        targetType = GetLuaBoundaryType(targetType);
        var reader = MarshalerGetters.GetOrAdd(targetType, static type =>
        {
            var method = typeof(DynamicUserDataHandler)
                .GetMethod(nameof(GetValueWithMarshalerGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(type);

            return (GetObjectDelegate) method.CreateDelegate(typeof(GetObjectDelegate));
        });

        return reader(thread, index, allowedValueConversions);
    }

    private static object? GetValueWithMarshalerGeneric<T>(
        LuaThread thread,
        int stackIndex,
        LuaAllowedValueConversions allowedValueConversions)
    {
        return thread.Stack[stackIndex].GetValue<T>(allowedValueConversions);
    }

    private static object? ReadObject(lua_State* L, int stackIndex, Type targetType)
    {
        using var thread = LuaThread.FromExtraSpace(L);
        return TryReadWithMarshaler(thread, stackIndex, targetType, out var value) ? value : null;
    }

    private static int ValidateInvocationArguments(
        lua_State* L,
        string callableName,
        ParameterInfo[] parameters,
        bool hasParams,
        int argCount,
        int stackOffset,
        LuaArgumentValidation argValidation,
        LuaAllowedValueConversions allowedConversions = LuaAllowedValueConversions.Default)
    {
        var requiredCount = GetRequiredArgumentCount(parameters, hasParams);
        if (argCount < requiredCount)
        {
            return luaL_error(L, $"{callableName}: expected at least {requiredCount} argument(s)");
        }

        var validateNullability = (argValidation & LuaArgumentValidation.Nullability) != 0;
        var validateRange = (argValidation & LuaArgumentValidation.Range) != 0;
        var validateType = (argValidation & LuaArgumentValidation.Type) != 0;
        var fixedCount = hasParams ? parameters.Length - 1 : parameters.Length;
        var inputCount = 0;
        using var thread = LuaThread.FromExtraSpace(L);

        for (var i = 0; i < fixedCount; i++)
        {
            var parameter = parameters[i];
            if (!ConsumesLuaArgument(parameter))
            {
                continue;
            }

            if (inputCount >= argCount)
            {
                break;
            }

            var stackIndex = stackOffset + inputCount;
            var validationResult = ValidateArgumentValue(thread, L, stackIndex, parameter.ParameterType, parameter, callableName, validateNullability, validateRange, validateType, allowedConversions);
            if (validationResult >= 0)
            {
                return validationResult;
            }

            inputCount++;
        }

        if (hasParams)
        {
            var paramsParameter = parameters[^1];
            var paramsElementType = paramsParameter.ParameterType.GetElementType()!;
            for (var i = inputCount; i < argCount; i++)
            {
                var stackIndex = stackOffset + i;
                var validationResult = ValidateArgumentValue(thread, L, stackIndex, paramsElementType, paramsParameter, callableName, validateNullability, validateRange, validateType, allowedConversions, isParamsElement: true);
                if (validationResult >= 0)
                {
                    return validationResult;
                }
            }
        }

        return -1;
    }

    private static int GetRequiredArgumentCount(ParameterInfo[] parameters, bool hasParams)
    {
        var fixedCount = hasParams ? parameters.Length - 1 : parameters.Length;
        var requiredCount = 0;
        for (var i = 0; i < fixedCount; i++)
        {
            if (ConsumesLuaArgument(parameters[i]) && !parameters[i].HasDefaultValue)
            {
                requiredCount++;
            }
        }

        return requiredCount;
    }

    private static int ValidateArgumentValue(
        LuaThread thread,
        lua_State* L,
        int stackIndex,
        Type parameterType,
        ParameterInfo parameter,
        string callableName,
        bool validateNullability,
        bool validateRange,
        bool validateType = false,
        LuaAllowedValueConversions allowedConversions = LuaAllowedValueConversions.Default,
        bool isParamsElement = false)
    {
        var originalParameterType = parameterType;
        parameterType = GetLuaBoundaryType(parameterType);
        if (validateNullability && lua_type(L, stackIndex) == LuaType.Nil)
        {
            var rejectNil = isParamsElement
                ? IsNilRejectedValueType(originalParameterType)
                : RejectsNil(parameter, originalParameterType);

            if (rejectNil)
            {
                return luaL_error(L, $"{callableName}: argument '{parameter.Name}' cannot be nil");
            }
        }

        if (validateType && lua_type(L, stackIndex) != LuaType.Nil)
        {
            if (!LuaTypeServices.IsImplicitlyCompatible(thread, stackIndex, parameterType, allowedConversions))
            {
                var expectedKind = GetExpectedKindName(parameterType);
                var actualKind = lua_type(L, stackIndex);
                return luaL_error(L, $"{callableName}: argument '{parameter.Name}' expected {expectedKind}, got {actualKind}");
            }
        }

        if (validateRange)
        {
            ValidateNumericRange(L, stackIndex, parameterType, callableName, parameter.Name!);
        }

        return -1;
    }

    private static bool RejectsNil(ParameterInfo parameter, Type parameterType)
    {
        if (parameterType.IsValueType)
        {
            return IsNilRejectedValueType(parameterType);
        }

        return NonNullableReferenceParameters.GetOrAdd(parameter, static value =>
        {
            _nullabilityContext ??= new NullabilityInfoContext();
            return _nullabilityContext.Create(value).ReadState == NullabilityState.NotNull;
        });
    }

    private static bool IsNilRejectedValueType(Type parameterType)
    {
        return Nullable.GetUnderlyingType(parameterType) == null;
    }

    private static object? ReadInvocationArgument(lua_State* L, int stackIndex, ParameterInfo parameter, LuaArgumentValidation argValidation, LuaAllowedValueConversions allowedConversions)
    {
        return ReadInvocationArgument(L, stackIndex, parameter.ParameterType, parameter, argValidation, allowedConversions);
    }

    private static object? ReadInvocationArgument(lua_State* L, int stackIndex, Type parameterType, ParameterInfo parameter, LuaArgumentValidation argValidation, LuaAllowedValueConversions allowedConversions)
    {
        var originalParameterType = parameterType;
        parameterType = GetLuaBoundaryType(parameterType);
        var validateNullability = (argValidation & LuaArgumentValidation.Nullability) != 0;
        var luaType = lua_type(L, stackIndex);
        var allowNilDefault = !validateNullability
            && originalParameterType.IsValueType
            && Nullable.GetUnderlyingType(originalParameterType) == null;
        var allowNilReference = validateNullability && !RejectsNil(parameter, originalParameterType);
        var allowNilNullableValueType = Nullable.GetUnderlyingType(originalParameterType) != null;

        if (luaType == LuaType.Nil)
        {
            if (allowNilDefault)
            {
                return DefaultValueTypeInstances.GetOrAdd(parameterType, static type => Activator.CreateInstance(type)!);
            }

            if (allowNilReference || allowNilNullableValueType || !RequiresDirectLuaRead(parameterType))
            {
                return null;
            }
        }

        if (parameterType == typeof(bool))
        {
            return lua_toboolean(L, stackIndex);
        }

        if (parameterType == typeof(string))
        {
            if (luaType == LuaType.Nil)
            {
                return luaL_checkstring(L, stackIndex)!;
            }

            return LuaTypeServices.GetValue<string>(L, stackIndex, allowedConversions)!;
        }

        if (IsIntegerType(parameterType))
        {
            var value = luaL_checkinteger(L, stackIndex);
            if (parameterType == typeof(byte))
            {
                return (byte) value;
            }

            if (parameterType == typeof(sbyte))
            {
                return (sbyte) value;
            }

            if (parameterType == typeof(short))
            {
                return (short) value;
            }

            if (parameterType == typeof(ushort))
            {
                return (ushort) value;
            }

            if (parameterType == typeof(int))
            {
                return (int) value;
            }

            if (parameterType == typeof(uint))
            {
                return (uint) value;
            }

            if (parameterType == typeof(ulong))
            {
                return (ulong) value;
            }

            return value;
        }

        if (parameterType == typeof(float))
        {
            return (float) luaL_checknumber(L, stackIndex);
        }

        if (parameterType == typeof(decimal))
        {
            return (decimal) luaL_checknumber(L, stackIndex);
        }

        if (parameterType == typeof(double))
        {
            return luaL_checknumber(L, stackIndex);
        }

        using var thread = LuaThread.FromExtraSpace(L);
        return GetValueWithMarshaler(thread, stackIndex, parameterType, allowedConversions);
    }

    private static bool RequiresDirectLuaRead(Type parameterType)
    {
        parameterType = GetLuaBoundaryType(parameterType);
        return parameterType == typeof(bool)
            || parameterType == typeof(string)
            || IsIntegerType(parameterType)
            || parameterType == typeof(float)
            || parameterType == typeof(double)
            || parameterType == typeof(decimal);
    }

    /// <summary>
    ///     Validates that the Lua numeric value at the given stack index fits within the target CLR type's range.
    ///     Calls <c>luaL_error</c> if out of range.
    /// </summary>
    private static void ValidateNumericRange(lua_State* L, int stackIndex, Type targetType, string methodName, string paramName)
    {
        targetType = GetLuaBoundaryType(targetType);
        if (IsIntegerType(targetType))
        {
            var longValue = lua_tointegerx(L, stackIndex, out var isNumber);
            if (!isNumber)
            {
                return;
            }

            if (targetType == typeof(byte))
            {
                if (longValue is < byte.MinValue or > byte.MaxValue)
                {
                    luaL_error(L, $"{methodName}: argument '{paramName}' is out of range for byte");
                }
            }
            else if (targetType == typeof(sbyte))
            {
                if (longValue is < sbyte.MinValue or > sbyte.MaxValue)
                {
                    luaL_error(L, $"{methodName}: argument '{paramName}' is out of range for sbyte");
                }
            }
            else if (targetType == typeof(short))
            {
                if (longValue is < short.MinValue or > short.MaxValue)
                {
                    luaL_error(L, $"{methodName}: argument '{paramName}' is out of range for short");
                }
            }
            else if (targetType == typeof(ushort))
            {
                if (longValue is < ushort.MinValue or > ushort.MaxValue)
                {
                    luaL_error(L, $"{methodName}: argument '{paramName}' is out of range for ushort");
                }
            }
            else if (targetType == typeof(int))
            {
                if (longValue is < int.MinValue or > int.MaxValue)
                {
                    luaL_error(L, $"{methodName}: argument '{paramName}' is out of range for int");
                }
            }
            else if (targetType == typeof(uint))
            {
                if (longValue is < uint.MinValue or > uint.MaxValue)
                {
                    luaL_error(L, $"{methodName}: argument '{paramName}' is out of range for uint");
                }
            }
            else if (targetType == typeof(ulong))
            {
                if (longValue < 0)
                {
                    luaL_error(L, $"{methodName}: argument '{paramName}' is out of range for ulong");
                }
            }

            return;
        }

        if (targetType != typeof(float) && targetType != typeof(decimal))
        {
            return;
        }

        var doubleValue = lua_tonumberx(L, stackIndex, out var isNumeric);
        if (!isNumeric)
        {
            return;
        }

        if (targetType == typeof(float))
        {
            if (doubleValue is < -float.MaxValue or > float.MaxValue)
            {
                luaL_error(L, $"{methodName}: argument '{paramName}' is out of range for float");
            }
        }
        else if (targetType == typeof(decimal))
        {
            if (double.IsNaN(doubleValue)
                || double.IsInfinity(doubleValue)
                || doubleValue < (double) decimal.MinValue
                || doubleValue > (double) decimal.MaxValue)
            {
                luaL_error(L, $"{methodName}: argument '{paramName}' is out of range for decimal");
            }
        }
    }

    private static void DisposeLuaReferenceArguments(object?[]? args, ParameterInfo[] parameters)
    {
        if (args == null || args.Length == 0)
        {
            return;
        }

        Exception? disposeException = null;
        var count = Math.Min(args.Length, parameters.Length);
        for (var i = 0; i < count; i++)
        {
            try
            {
                DisposeLuaReferenceValue(args[i], parameters[i].ParameterType);
            }
            catch (Exception ex)
            {
                disposeException = LuaTypeServices.CombineCleanupException(disposeException, ex);
            }
        }

        if (disposeException != null)
        {
            ExceptionDispatchInfo.Capture(disposeException).Throw();
        }
    }

    private static void DisposeLuaReferenceValue(object? value, Type targetType)
    {
        if (value == null)
        {
            return;
        }

        if (targetType.IsByRef)
        {
            targetType = targetType.GetElementType()!;
        }

        if (typeof(LuaReference).IsAssignableFrom(targetType))
        {
            ((LuaReference) value).Dispose();
            return;
        }

        if (!targetType.IsArray)
        {
            return;
        }

        var elementType = targetType.GetElementType();
        if (elementType == null || !typeof(LuaReference).IsAssignableFrom(elementType))
        {
            return;
        }

        Exception? disposeException = null;
        foreach (var element in (Array) value)
        {
            if (element is not LuaReference reference)
            {
                continue;
            }

            try
            {
                reference.Dispose();
            }
            catch (Exception ex)
            {
                disposeException = LuaTypeServices.CombineCleanupException(disposeException, ex);
            }
        }

        if (disposeException != null)
        {
            ExceptionDispatchInfo.Capture(disposeException).Throw();
        }
    }

    private void PushMethodClosure(lua_State* L, string name, List<MethodInfo> methods)
    {
        lock (_methodFunctionPtrs)
        {
            if (!_methodFunctionPtrs.TryGetValue(name, out var functionPtr))
            {
                var wrapper = methods.Count == 1
                    ? CreateMethodWrapper(methods[0], name)
                    : CreateOverloadDispatchWrapper(name, methods);

                functionPtr = CreateSafeCFunction(wrapper);
                _methodFunctions[name] = wrapper;
                _methodFunctionPtrs[name] = functionPtr;

                var clrNames = new string[methods.Count];
                for (var i = 0; i < methods.Count; i++)
                {
                    clrNames[i] = methods[i].Name;
                }

                LuaTypeRegistry.RegisterMethodPointer(functionPtr, Type, clrNames, isStatic: false);
            }

            lua_pushcfunction(L, functionPtr);
        }
    }

    private LuaCFunction CreateOverloadDispatchWrapper(string callableName, List<MethodInfo> methods)
    {
        var overloads = new (MethodInfo Method, ParameterInfo[] Parameters, bool HasParams, int RequiredCount, ParameterInfo[] FixedInputParameters)[methods.Count];
        for (var i = 0; i < methods.Count; i++)
        {
            var parameters = methods[i].GetParameters();
            var hasParams = parameters.Length > 0 && parameters[^1].IsDefined(typeof(ParamArrayAttribute));
            var requiredCount = 0;
            for (var j = 0; j < parameters.Length; j++)
            {
                if (hasParams && j == parameters.Length - 1)
                {
                    break;
                }

                if (ConsumesLuaArgument(parameters[j]) && !parameters[j].HasDefaultValue)
                {
                    requiredCount++;
                }
            }

            var fixedCount = hasParams ? parameters.Length - 1 : parameters.Length;
            var fixedInputParameters = ComputeFixedInputParameters(parameters, fixedCount);

            overloads[i] = (methods[i], parameters, hasParams, requiredCount, fixedInputParameters);
        }

        return L =>
        {
            var argCount = lua_gettop(L) - 1;
            var validationResult = ValidateSelf(L, 1, out var self);
            if (validationResult >= 0)
            {
                return validationResult;
            }

            var hasMatch = TryResolveOverload(L, overloads, argCount, stackOffset: 2, _options.ArgumentValidation, out var bestMatch, out var compatibleCount);
            if (hasMatch)
            {
                if (_options.OverloadResolution == LuaOverloadResolution.Error && compatibleCount > 1)
                {
                    return luaL_error(L, $"ambiguous overload accepts {argCount} argument(s)");
                }

                return InvokeMethod(
                    L,
                    self,
                    callableName,
                    bestMatch!.Value.Method,
                    bestMatch.Value.Parameters,
                    bestMatch.Value.HasParams,
                    argCount,
                    _options.ArgumentValidation,
                    _options.BindingConversions,
                    _options.AwaitableReturnHandling);
            }

            return luaL_error(L, $"no overload accepts {argCount} argument(s)");
        };
    }

    private LuaCFunction CreateMethodWrapper(MethodInfo method, string callableName)
    {
        var parameters = method.GetParameters();
        var hasParams = parameters.Length > 0 && parameters[^1].IsDefined(typeof(ParamArrayAttribute));

        return L =>
        {
            var argCount = lua_gettop(L) - 1;
            var validationResult = ValidateSelf(L, 1, out var self);
            if (validationResult >= 0)
            {
                return validationResult;
            }

            return InvokeMethod(
                L,
                self,
                callableName,
                method,
                parameters,
                hasParams,
                argCount,
                _options.ArgumentValidation,
                _options.BindingConversions,
                _options.AwaitableReturnHandling);
        };
    }

    private static int InvokeMethod(
        lua_State* L,
        object self,
        string callableName,
        MethodInfo method,
        ParameterInfo[] parameters,
        bool hasParams,
        int argCount,
        LuaArgumentValidation argValidation = LuaArgumentValidation.None,
        LuaAllowedValueConversions allowedConversions = LuaAllowedValueConversions.Default,
        LuaAwaitableReturnHandling awaitableReturnHandling = LuaAwaitableReturnHandling.ImplicitAwait)
    {
        var validationResult = ValidateInvocationArguments(L, callableName, parameters, hasParams, argCount, stackOffset: 2, argValidation, allowedConversions);
        if (validationResult >= 0)
        {
            return validationResult;
        }

        object?[] args;

        if (hasParams)
        {
            var fixedCount = parameters.Length - 1;
            var fixedInputCount = 0;
            args = new object?[parameters.Length];

            for (var i = 0; i < fixedCount; i++)
            {
                if (!ConsumesLuaArgument(parameters[i]))
                {
                    continue;
                }

                var stackIndex = fixedInputCount + 2;
                if (fixedInputCount < argCount)
                {
                    args[i] = ReadInvocationArgument(L, stackIndex, parameters[i], argValidation, allowedConversions);
                }
                else if (parameters[i].HasDefaultValue)
                {
                    args[i] = parameters[i].DefaultValue;
                }

                fixedInputCount++;
            }

            var paramsElementType = parameters[^1].ParameterType.GetElementType()!;
            var paramsCount = Math.Max(0, argCount - fixedInputCount);
            var paramsArray = Array.CreateInstance(paramsElementType, paramsCount);
            for (var i = 0; i < paramsCount; i++)
            {
                var stackIndex = fixedInputCount + i + 2;
                paramsArray.SetValue(ReadInvocationArgument(L, stackIndex, paramsElementType, parameters[^1], argValidation, allowedConversions), i);
            }

            args[fixedCount] = paramsArray;
        }
        else
        {
            args = new object?[parameters.Length];
            var inputCount = 0;
            for (var i = 0; i < parameters.Length; i++)
            {
                if (!ConsumesLuaArgument(parameters[i]))
                {
                    continue;
                }

                var stackIndex = inputCount + 2;
                if (inputCount < argCount)
                {
                    args[i] = ReadInvocationArgument(L, stackIndex, parameters[i], argValidation, allowedConversions);
                }
                else if (parameters[i].HasDefaultValue)
                {
                    args[i] = parameters[i].DefaultValue;
                }

                inputCount++;
            }
        }

        var returnCount = 0;
        var shouldYieldAwaitable = false;

        try
        {
            var result = method.Invoke(self, args);

            if (method.ReturnType != typeof(void))
            {
                if (AwaitableUserData.IsAwaitableType(method.ReturnType))
                {
                    if (awaitableReturnHandling == LuaAwaitableReturnHandling.ImplicitAwait)
                    {
                        LuaTypeServices.PushImplicitAwaitable(L, result);
                        shouldYieldAwaitable = true;
                    }
                    else
                    {
                        LuaTypeServices.PushAwaitable(L, result);
                    }
                }
                else
                {
                    PushObject(L, result);
                }
                returnCount++;
            }

            for (var i = 0; i < parameters.Length; i++)
            {
                if (ProducesLuaReturnValue(parameters[i]))
                {
                    PushObject(L, args[i]);
                    returnCount++;
                }
            }

        }
        catch (TargetInvocationException ex)
        {
            ThrowUnwrapped(ex);
            return 0;
        }
        finally
        {
            DisposeLuaReferenceArguments(args, parameters);
        }

        if (shouldYieldAwaitable)
        {
            return LuaAsyncLibrary.YieldAwaitable(L);
        }

        return returnCount;
    }

    private bool TryCreateParameterlessInstance([NotNullWhen(true)] out object? instance)
    {
        foreach (var (constructor, parameters, _, _) in _constructors)
        {
            if (parameters.Length != 0)
            {
                continue;
            }

            instance = CreateInstance(constructor, null);
            return true;
        }

        if (Type.IsValueType)
        {
            instance = CreateInstance(null, null);
            return true;
        }

        instance = default;
        return false;
    }

    private object CreateInstance(ConstructorInfo? constructor, object?[]? args)
    {
        try
        {
            return (constructor != null ? constructor.Invoke(args) : Activator.CreateInstance(Type))
                ?? throw new InvalidOperationException($"'{Type.Name}' constructor produced a null instance");
        }
        catch (TargetInvocationException ex)
        {
            ThrowUnwrapped(ex);
            return null!;
        }
    }

    private static LuaCFunction CreateOperatorCallback(MethodInfo method)
    {
        var parameters = method.GetParameters();

        if (parameters.Length == 1)
        {
            var paramType = parameters[0].ParameterType;
            return L =>
            {
                var operand = ReadObject(L, 1, paramType);
                try
                {
                    var result = method.Invoke(null, [operand]);
                    PushObject(L, result);
                    return 1;
                }
                catch (TargetInvocationException ex)
                {
                    ThrowUnwrapped(ex);
                    return 0;
                }
                finally
                {
                    DisposeLuaReferenceValue(operand, paramType);
                }
            };
        }

        var leftType = parameters[0].ParameterType;
        var rightType = parameters[1].ParameterType;
        return L =>
        {
            var left = ReadObject(L, 1, leftType);
            var right = ReadObject(L, 2, rightType);
            try
            {
                var result = method.Invoke(null, [left, right]);
                PushObject(L, result);
                return 1;
            }
            catch (TargetInvocationException ex)
            {
                ThrowUnwrapped(ex);
                return 0;
            }
            finally
            {
                DisposeLuaReferenceValue(right, rightType);
                DisposeLuaReferenceValue(left, leftType);
            }
        };
    }

    private LuaCFunction CreateOperatorDispatchCallback(List<MethodInfo> overloads)
    {
        var overloadInfo = new (MethodInfo Method, ParameterInfo[] Parameters)[overloads.Count];
        for (var i = 0; i < overloads.Count; i++)
        {
            overloadInfo[i] = (overloads[i], overloads[i].GetParameters());
        }

        return L =>
        {
            foreach (var (method, parameters) in overloadInfo)
            {
                var compatible = true;
                for (var i = 0; i < parameters.Length; i++)
                {
                    if (!IsLuaTypeCompatible(L, i + 1, parameters[i].ParameterType, allowedConversions: _options.BindingConversions))
                    {
                        compatible = false;
                        break;
                    }
                }

                if (!compatible)
                {
                    continue;
                }

                var args = new object?[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                {
                    args[i] = ReadObject(L, i + 1, parameters[i].ParameterType);
                }

                try
                {
                    var result = method.Invoke(null, args);
                    PushObject(L, result);
                    return 1;
                }
                catch (TargetInvocationException ex)
                {
                    ThrowUnwrapped(ex);
                    return 0;
                }
                finally
                {
                    DisposeLuaReferenceArguments(args, parameters);
                }
            }

            luaL_error(L, "no matching operator overload");
            return 0;
        };
    }

    private static bool IsLuaTypeCompatible(
        lua_State* L,
        int stackIndex,
        Type paramType,
        ParameterInfo? parameter = null,
        LuaArgumentValidation argumentValidation = LuaArgumentValidation.None,
        bool isParamsElement = false,
        LuaAllowedValueConversions allowedConversions = LuaAllowedValueConversions.Default)
    {
        using var thread = LuaThread.FromExtraSpace(L);
        return GetLuaTypeCompatibilityCost(thread, stackIndex, paramType, parameter, argumentValidation, isParamsElement, allowedConversions) >= 0;
    }

    private static int GetLuaTypeCompatibilityCost(
        LuaThread thread,
        int stackIndex,
        Type paramType,
        ParameterInfo? parameter = null,
        LuaArgumentValidation argumentValidation = LuaArgumentValidation.None,
        bool isParamsElement = false,
        LuaAllowedValueConversions allowedConversions = LuaAllowedValueConversions.Default)
    {
        var L = thread.State.L;
        paramType = GetLuaBoundaryType(paramType);
        var luaType = lua_type(L, stackIndex);

        if (luaType == LuaType.Nil)
        {
            return IsNilCompatible(paramType, parameter, argumentValidation, isParamsElement)
                ? 0
                : LuaTypeServices.CostIncompatible;
        }

        var validateNumericRange = (argumentValidation & LuaArgumentValidation.Range) != 0;
        return LuaTypeServices.GetImplicitCompatibilityCost(thread, stackIndex, paramType, allowedConversions, validateNumericRange);
    }

    private static bool IsNilCompatible(Type parameterType, ParameterInfo? parameter, LuaArgumentValidation argumentValidation, bool isParamsElement)
    {
        if ((argumentValidation & LuaArgumentValidation.Nullability) != 0)
        {
            if (parameter == null)
            {
                return !IsNilRejectedValueType(parameterType);
            }

            return isParamsElement
                ? !IsNilRejectedValueType(parameterType)
                : !RejectsNil(parameter, parameterType);
        }

        if (parameterType.IsValueType)
        {
            return true;
        }

        return !RequiresDirectLuaRead(parameterType);
    }

    private LuaCFunction CreateLenCallback(PropertyInfo property)
    {
        var getter = PropertyAccessorFactory.CreateGetter(property);
        return L =>
        {
            var validationResult = ValidateSelf(L, 1, out var self);
            if (validationResult >= 0)
            {
                return validationResult;
            }

            var length = getter(self);
            lua_pushinteger(L, Convert.ToInt64(length));
            return 1;
        };
    }

    private PropertyInfo? FindLengthProperty(Type type, bool ignoreObsoleteMembers)
    {
        var countProperty = type.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance);
        if (IsLengthPropertyExposed(countProperty, ignoreObsoleteMembers))
        {
            return countProperty;
        }

        var lengthProperty = type.GetProperty("Length", BindingFlags.Public | BindingFlags.Instance);
        if (IsLengthPropertyExposed(lengthProperty, ignoreObsoleteMembers))
        {
            return lengthProperty;
        }

        if (typeof(ICollection).IsAssignableFrom(type))
        {
            var collectionCountProperty = typeof(ICollection).GetProperty("Count");
            if (IsLengthPropertyExposed(collectionCountProperty, ignoreObsoleteMembers))
            {
                return collectionCountProperty;
            }
        }

        return null;
    }

    private bool IsLengthPropertyExposed([NotNullWhen(true)] PropertyInfo? property, bool ignoreObsoleteMembers)
    {
        if (property == null)
        {
            return false;
        }

        if (!IsIntegerType(property.PropertyType))
        {
            return false;
        }

        if (!IsPropertyAccessorExposed(property, property.GetMethod))
        {
            return false;
        }

        if (!ShouldExposeCandidateMember(property, ignoreObsoleteMembers))
        {
            return false;
        }

        return true;
    }

    private static bool IsIntegerType(Type type)
    {
        return LuaTypeServices.IsIntegerType(type);
    }

    private static bool IsMoreSpecificConstructor(
        (ConstructorInfo? Constructor, ParameterInfo[] Parameters, bool HasParams, int RequiredCount) candidate,
        (ConstructorInfo? Constructor, ParameterInfo[] Parameters, bool HasParams, int RequiredCount) currentBest,
        int argCount)
    {
        for (var argumentIndex = 0; argumentIndex < argCount; argumentIndex++)
        {
            var candidateRank = GetDispatchSpecificityRank(candidate.Parameters, candidate.HasParams, argumentIndex);
            var currentRank = GetDispatchSpecificityRank(currentBest.Parameters, currentBest.HasParams, argumentIndex);
            if (candidateRank != currentRank)
            {
                return candidateRank < currentRank;
            }
        }

        if (candidate.HasParams != currentBest.HasParams)
        {
            return !candidate.HasParams;
        }

        return false;
    }

    private static bool IsMoreSpecificOverload(
        (MethodInfo Method, ParameterInfo[] Parameters, bool HasParams, int RequiredCount, ParameterInfo[] FixedInputParameters) candidate,
        (MethodInfo Method, ParameterInfo[] Parameters, bool HasParams, int RequiredCount, ParameterInfo[] FixedInputParameters) currentBest,
        int argCount)
    {
        for (var argumentIndex = 0; argumentIndex < argCount; argumentIndex++)
        {
            var candidateRank = GetDispatchSpecificityRank(candidate.Parameters, candidate.HasParams, candidate.FixedInputParameters, argumentIndex);
            var currentRank = GetDispatchSpecificityRank(currentBest.Parameters, currentBest.HasParams, currentBest.FixedInputParameters, argumentIndex);
            if (candidateRank != currentRank)
            {
                return candidateRank < currentRank;
            }
        }

        if (candidate.HasParams != currentBest.HasParams)
        {
            return !candidate.HasParams;
        }

        return false;
    }

    private static int GetDispatchSpecificityRank(ParameterInfo[] parameters, bool hasParams, int argumentIndex)
    {
        var fixedCount = hasParams ? parameters.Length - 1 : parameters.Length;
        if (argumentIndex < fixedCount)
        {
            return GetSpecificityRank(parameters[argumentIndex].ParameterType);
        }

        if (hasParams)
        {
            return GetSpecificityRank(parameters[^1].ParameterType.GetElementType()!);
        }

        return int.MaxValue;
    }

    private static int GetDispatchSpecificityRank(ParameterInfo[] parameters, bool hasParams, ParameterInfo[] fixedInputParameters, int argumentIndex)
    {
        if (argumentIndex < fixedInputParameters.Length)
        {
            return GetSpecificityRank(fixedInputParameters[argumentIndex].ParameterType);
        }

        if (hasParams)
        {
            return GetSpecificityRank(parameters[^1].ParameterType.GetElementType()!);
        }

        return int.MaxValue;
    }

    private static int GetSpecificityRank(Type type)
    {
        type = GetLuaBoundaryType(type);
        if (type == typeof(bool))
        {
            return 0;
        }

        if (IsIntegerType(type))
        {
            return 1;
        }

        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
        {
            return 2;
        }

        if (type == typeof(string))
        {
            return 3;
        }

        return 4;
    }

    [DoesNotReturn]
    private static void ThrowUnwrapped(TargetInvocationException ex)
    {
        ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
        throw ex;
    }

    private static string GetExpectedKindName(Type type)
    {
        type = GetLuaBoundaryType(type);
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
        {
            type = underlying;
        }

        if (type == typeof(bool))
        {
            return "boolean";
        }

        if (IsIntegerType(type) || type == typeof(float) || type == typeof(double) || type == typeof(decimal))
        {
            return "number";
        }

        if (type == typeof(string))
        {
            return "string";
        }

        if (typeof(LuaTable).IsAssignableFrom(type))
        {
            return "table";
        }

        if (typeof(LuaFunction).IsAssignableFrom(type))
        {
            return "function";
        }

        return type.Name;
    }

    private static class PropertyAccessorFactory
    {
        private static readonly MethodInfo CreateTypedGetterMethod =
            typeof(PropertyAccessorFactory).GetMethod(nameof(CreateTypedGetter), BindingFlags.Static | BindingFlags.NonPublic)!;

        private static readonly MethodInfo CreateTypedSetterMethod =
            typeof(PropertyAccessorFactory).GetMethod(nameof(CreateTypedSetter), BindingFlags.Static | BindingFlags.NonPublic)!;

        private static readonly MethodInfo CreateTypedStaticGetterMethod =
            typeof(PropertyAccessorFactory).GetMethod(nameof(CreateTypedStaticGetter), BindingFlags.Static | BindingFlags.NonPublic)!;

        private static readonly MethodInfo CreateTypedStaticSetterMethod =
            typeof(PropertyAccessorFactory).GetMethod(nameof(CreateTypedStaticSetter), BindingFlags.Static | BindingFlags.NonPublic)!;

        public static Func<object, object?> CreateGetter(PropertyInfo property)
        {
            if (property.DeclaringType!.IsValueType)
            {
                return obj =>
                {
                    try
                    {
                        return property.GetValue(obj);
                    }
                    catch (TargetInvocationException ex)
                    {
                        ThrowUnwrapped(ex);
                        return null;
                    }
                };
            }

            var method = property.GetGetMethod(true)!;
            var factory = CreateTypedGetterMethod.MakeGenericMethod(property.DeclaringType!, property.PropertyType);
            return (Func<object, object?>) factory.Invoke(null, [method])!;
        }

        public static Action<object, object?> CreateSetter(PropertyInfo property)
        {
            if (property.DeclaringType!.IsValueType)
            {
                return (obj, value) =>
                {
                    try
                    {
                        property.SetValue(obj, value);
                    }
                    catch (TargetInvocationException ex)
                    {
                        ThrowUnwrapped(ex);
                    }
                };
            }

            var method = property.GetSetMethod(true)!;
            var factory = CreateTypedSetterMethod.MakeGenericMethod(property.DeclaringType!, property.PropertyType);
            return (Action<object, object?>) factory.Invoke(null, [method])!;
        }

        public static Func<object?, object?> CreateStaticGetter(PropertyInfo property)
        {
            var method = property.GetGetMethod(true)!;
            var factory = CreateTypedStaticGetterMethod.MakeGenericMethod(property.PropertyType);
            return (Func<object?, object?>) factory.Invoke(null, [method])!;
        }

        public static Action<object?, object?> CreateStaticSetter(PropertyInfo property)
        {
            var method = property.GetSetMethod(true)!;
            var factory = CreateTypedStaticSetterMethod.MakeGenericMethod(property.PropertyType);
            return (Action<object?, object?>) factory.Invoke(null, [method])!;
        }

        private static Func<object, object?> CreateTypedGetter<TOwner, TProp>(MethodInfo method)
        {
            var getter = method.CreateDelegate<Func<TOwner, TProp>>();
            return obj => getter((TOwner) obj);
        }

        private static Action<object, object?> CreateTypedSetter<TOwner, TProp>(MethodInfo method)
        {
            var setter = method.CreateDelegate<Action<TOwner, TProp>>();
            return (obj, value) => setter((TOwner) obj, (TProp) value!);
        }

        private static Func<object?, object?> CreateTypedStaticGetter<TProp>(MethodInfo method)
        {
            var getter = method.CreateDelegate<Func<TProp>>();
            return _ => getter();
        }

        private static Action<object?, object?> CreateTypedStaticSetter<TProp>(MethodInfo method)
        {
            var setter = method.CreateDelegate<Action<TProp>>();
            return (_, value) => setter((TProp) value!);
        }
    }
}
