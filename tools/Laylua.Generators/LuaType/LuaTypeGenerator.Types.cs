using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Laylua.Generators;

public partial class LuaTypeGenerator
{
    private enum LuaValueKind
    {
        Integer,
        Number,
        String,
        Boolean,
        Marshaled,
        Void,
    }

    private class LuaTypeInfo
    {
        public string Name = "";
        public string SafeName = "";
        public string FullName = "";
        public string? Namespace;
        public Location? TypeLocation;

        public string NameWithinNamespace => Namespace != null
            ? FullName.Substring(Namespace.Length + 1)
            : FullName;

        public bool IsAbstract;
        public bool IsValueType;
        public bool HasParameterlessConstructor;
        public int Members;
        public int Features;
        public int NamingPolicy;
        public bool EmitToString;
        public bool EmitDispose;
        public bool ImplementsDisposable;
        public bool EmitOperators;
        public bool EmitPairsFeature;
        public int PairsBehavior;
        public int UnknownPropertyBehavior;
        public int OverloadResolution;
        public int IndexMembers;
        public int IndexPriority;
        public int Optimizations;
        public int Construction;
        public string? FactoryMethodName;
        public int ArgumentValidation;
        public int ObjectMembers;
        public int ObsoleteMembers;
        public int AwaitableReturnHandling;
        public int BindingConversions;
        public bool EmitLength;
        public string? LengthPropertyName;
        public bool ImplementsEnumerable;
        public bool BaseHasLuaType;

        /// <summary>
        ///     Raw unresolved type config, stored for deferred resolution against assembly defaults.
        /// </summary>
        public LuaTypeConfig RawConfig;

        public readonly List<LuaIndexerInfo> Indexers = [];
        public readonly List<LuaMemberInfo> Properties = [];
        public readonly List<LuaMemberInfo> Fields = [];
        public readonly List<LuaMethodInfo> Methods = [];
        public readonly List<LuaMemberInfo> StaticProperties = [];
        public readonly List<LuaMemberInfo> StaticFields = [];
        public readonly List<LuaMethodInfo> StaticMethods = [];
        public readonly List<LuaOperatorInfo> Operators = [];
        public readonly List<LuaConstructorInfo> Constructors = [];
        public readonly List<LuaMethodInfo> TypeCallMethods = [];
        public readonly List<LuaEventInfo> Events = [];
        public readonly List<LuaEventInfo> StaticEvents = [];
        public readonly List<(string Keyword, string Name)> ContainingTypes = [];
        public readonly List<LuaDiagnosticInfo> Diagnostics = [];

        public bool HasStaticMembers => StaticProperties.Count > 0 || StaticFields.Count > 0 || StaticMethods.Count > 0 || StaticEvents.Count > 0;

        public bool EmitDirectCall => (Members & MembersConstructors) != 0 && Construction == ConstructionDirect && Constructors.Count > 0;

        public bool EmitFactory => (Members & MembersConstructors) != 0 && Construction == ConstructionFactory && Constructors.Count > 0;

        public bool HasDefinition => HasStaticMembers || EmitDirectCall || EmitFactory;

        public bool EmitPairs => EmitPairsFeature && (PairsBehavior == PairsProperties
            || (PairsBehavior == PairsIfEnumerable && ImplementsEnumerable));

        /// <summary>
        ///     All readable instance members (properties and fields) for __pairs enumeration.
        /// </summary>
        public List<LuaMemberInfo> ReadableMembers
        {
            get
            {
                var members = new List<LuaMemberInfo>();
                members.AddRange(Properties.Where(static property => property.CanRead));
                members.AddRange(Fields.Where(static f => f.CanRead));
                return members;
            }
        }
    }

    private class LuaMemberInfo
    {
        public string ClrName = "";
        public string LuaName = "";
        public string TypeName = "";
        public Location? DeclarationLocation;
        public bool CanRead;
        public bool CanWrite;
        public bool IsConstant;
        public bool HasExplicitLuaName;
        public LuaValueKind ValueKind;
        public bool IsLuaString; // [LuaString] on a span/memory property/field
        public bool IsObsolete;
    }

    private class LuaMethodInfo
    {
        public string ClrName = "";
        public string LuaName = "";
        public string ReturnType = "";
        public string GeneratedIdentifier = "";
        public Location? DeclarationLocation;
        public bool ReturnsVoid;
        public bool ReturnsRawCount;
        public bool HasExplicitLuaName;
        public LuaValueKind ReturnValueKind;
        public List<LuaParameterInfo> Parameters = [];
        public List<LuaMethodInfo>? Overloads;
        public bool IsObsolete;
        public int ObjectMethodFlag; // 0 if not an object method, else the flag value (e.g., ObjectMembersToString)
    }

    private class LuaParameterInfo
    {
        public string Name = "";
        public string TypeName = "";
        public LuaValueKind ValueKind;
        public bool IsOptional;
        public string? DefaultValue;
        public bool IsCallingThread;
        public bool IsLuaReference;
        public bool IsParams;
        public string? ParamsElementTypeName;
        public LuaValueKind ParamsElementKind;
        public bool IsParamsElementNonNullableReference;
        public int RefKind;                 // 0 = None, 1 = Ref, 2 = Out, 3 = In
        public bool IsNonNullableReference; // NRT: parameter is a non-nullable reference type
        public bool IsValueType;
        public bool IsNullableValueType;
        public bool IsLuaString; // [LuaString] on a span/memory parameter
    }

    private class LuaOperatorInfo
    {
        public string MethodName = "";
        public string MetamethodName = "";
        public string ReturnType = "";
        public LuaValueKind ReturnValueKind;
        public bool ReturnsAnnotatedType;
        public List<LuaOperatorParameterInfo> Parameters = [];
    }

    private class LuaOperatorParameterInfo
    {
        public string TypeName = "";
        public bool IsAnnotatedType;
        public LuaValueKind ValueKind;
    }

    private class LuaIndexerInfo
    {
        public string KeyTypeName = "";
        public LuaValueKind KeyValueKind;
        public string ValueTypeName = "";
        public LuaValueKind ValueKind;
        public bool HasGetter;
        public bool HasSetter;
    }

    /// <summary>
    ///     Entry for the __index switch statement. Has a Member, Method, Event, or IsFactory set.
    /// </summary>
    private class IndexSwitchEntry(string luaName, LuaMemberInfo? member, LuaMethodInfo? method, LuaEventInfo? eventInfo = null)
    {
        public readonly string LuaName = luaName;
        public readonly LuaMemberInfo? Member = member;
        public readonly LuaMethodInfo? Method = method;
        public readonly LuaEventInfo? Event = eventInfo;
        public bool IsFactory;
    }

    private class LuaConstructorInfo
    {
        public List<LuaParameterInfo> Parameters = [];
    }

    private class LuaDiagnosticInfo
    {
        public string Id = "";
        public string Message = "";
        public Location? Location;
        public ImmutableDictionary<string, string?> Properties = ImmutableDictionary<string, string?>.Empty;
    }

    private class LuaEventInfo
    {
        public string ClrName = "";
        public string LuaName = "";
        public string DelegateTypeName = "";
        public Location? DeclarationLocation;
        public bool HasExplicitLuaName;
        public bool CanAdd;
        public bool CanRemove;
        public bool IsObsolete;
        public bool IsStatic;
    }

    private struct LuaTypeConfig
    {
        public int? NamingPolicy;
        public int? Members;
        public int? Features;
        public int? PairsBehavior;
        public int? UnknownPropertyBehavior;
        public int? OverloadResolution;
        public int? IndexMembers;
        public int? IndexPriority;
        public int? Optimizations;
        public int? Construction;
        public string? FactoryMethodName;
        public int? ArgumentValidation;
        public int? ObjectMembers;
        public int? ObsoleteMembers;
        public int? AwaitableReturnHandling;
        public int? BindingConversions;
    }
}
