using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Laylua.Generators;

[Generator]
public partial class LuaTypeGenerator : IIncrementalGenerator
{
    private const string LuaTypeAttributeName = "Laylua.Marshaling.LuaTypeAttribute";
    private const string LuaTypeDefaultsAttributeName = "Laylua.Marshaling.LuaTypeDefaultsAttribute";
    private const string LuaIgnoreAttributeName = "Laylua.Marshaling.LuaIgnoreAttribute";
    private const string LuaIncludeAttributeName = "Laylua.Marshaling.LuaIncludeAttribute";
    private const string LuaNameAttributeName = "Laylua.Marshaling.LuaNameAttribute";
    private const string LuaCallingThreadAttributeName = "Laylua.Marshaling.LuaCallingThreadAttribute";
    private const string LuaReturnCountAttributeName = "Laylua.Marshaling.LuaReturnCountAttribute";
    private const string LuaTypeCallAttributeName = "Laylua.Marshaling.LuaTypeCallAttribute";
    private const string LuaStringAttributeName = "Laylua.Marshaling.LuaStringAttribute";

    private static readonly DiagnosticDescriptor UnsupportedParameterDiagnostic = new("LAYLUA100",
        "Method skipped due to unsupported Lua boundary type",
        "Method '{0}' on type '{1}' was skipped because {2} has unsupported type '{3}' which cannot be marshaled. Use [LuaIgnore] to suppress this warning.",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedParameterInheritedDiagnostic = new("LAYLUA101",
        "Inherited method skipped due to unsupported Lua boundary type",
        "Inherited method '{0}' on type '{1}' (declared in '{4}') was skipped because {2} has unsupported type '{3}' which cannot be marshaled",
        "Usage",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedEventDiagnostic = new("LAYLUA102",
        "Event skipped due to unsupported delegate signature",
        "Event '{0}' on type '{1}' was skipped because its delegate {2} has unsupported type '{3}' which cannot be marshaled across the Lua boundary",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedConstructorDiagnostic = new("LAYLUA103",
        "Constructor skipped due to unsupported Lua boundary type",
        "Constructor on type '{0}' was skipped because parameter '{1}' has unsupported type '{2}' which cannot be marshaled across the Lua boundary. Use [LuaIgnore] to suppress this warning.",
        "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidEnumConfigDiagnostic = new("LAYLUA107",
        "Invalid LuaType enum configuration value",
        "{0} on type '{1}' has invalid value '{2}' for {3}",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidFlagsConfigDiagnostic = new("LAYLUA108",
        "Invalid LuaType flags configuration value",
        "{0} on type '{1}' has invalid value '{2}' for {3}; allowed mask is '{4}'",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor LuaNameCollisionDiagnostic = new("LAYLUA109",
        "Lua-visible name collision",
        "Type '{0}' exposes Lua name '{1}' multiple times in {2}: {3}",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedFromTableConversionDiagnostic = new("LAYLUA110",
        "Generated table conversion is unsupported",
        "From-table conversion is enabled for type '{0}', but generated table conversion cannot be emitted because {1}",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // Naming policy enum values (must match LuaNamingPolicy)
    private const int PolicyOriginal = 1;
    private const int PolicyLowerCase = 2;
    private const int PolicyUpperCase = 3;
    private const int PolicyCamelCase = 4;
    private const int PolicyLowerSnakeCase = 5;
    private const int PolicyUpperSnakeCase = 6;
    private const int PolicyPascalCase = 7;

    // LuaTypeMembers flag values (must match LuaTypeMembers)
    private const int MembersProperties = 1 << 0;
    private const int MembersFields = 1 << 1;
    private const int MembersMethods = 1 << 2;
    private const int MembersEvents = 1 << 3;
    private const int MembersIndexers = 1 << 4;
    private const int MembersConstructors = 1 << 5;
    private const int MembersStaticProperties = 1 << 6;
    private const int MembersStaticFields = 1 << 7;
    private const int MembersStaticMethods = 1 << 8;
    private const int MembersStaticEvents = 1 << 9;
    private const int MembersConstants = 1 << 10;
    private const int MembersInstanceMembers = MembersProperties | MembersFields | MembersMethods | MembersEvents | MembersIndexers | MembersConstructors;
    private const int MembersStaticMembers = MembersStaticProperties | MembersStaticFields | MembersStaticMethods | MembersStaticEvents | MembersConstants;
    private const int MembersAll = MembersInstanceMembers | MembersStaticMembers;

    // LuaTypeFeatures flag values (must match LuaTypeFeatures)
    private const int FeatureToString = 1 << 0;
    private const int FeatureDispose = 1 << 1;
    private const int FeatureLength = 1 << 2;
    private const int FeaturePairs = 1 << 3;
    private const int FeatureAdd = 1 << 4;
    private const int FeatureSubtract = 1 << 5;
    private const int FeatureMultiply = 1 << 6;
    private const int FeatureDivide = 1 << 7;
    private const int FeatureModulus = 1 << 8;
    private const int FeatureUnaryNegation = 1 << 9;
    private const int FeatureEquality = 1 << 10;
    private const int FeatureLessThan = 1 << 11;
    private const int FeatureLessThanOrEqual = 1 << 12;
    private const int FeatureBitwiseAnd = 1 << 13;
    private const int FeatureBitwiseOr = 1 << 14;
    private const int FeatureExclusiveOr = 1 << 15;
    private const int FeatureOnesComplement = 1 << 16;
    private const int FeatureLeftShift = 1 << 17;
    private const int FeatureRightShift = 1 << 18;
    private const int FeatureArithmeticOperators = FeatureAdd | FeatureSubtract | FeatureMultiply | FeatureDivide | FeatureModulus | FeatureUnaryNegation;
    private const int FeatureComparisonOperators = FeatureEquality | FeatureLessThan | FeatureLessThanOrEqual;
    private const int FeatureBitwiseOperators = FeatureBitwiseAnd | FeatureBitwiseOr | FeatureExclusiveOr | FeatureOnesComplement | FeatureLeftShift | FeatureRightShift;
    private const int FeatureOperators = FeatureArithmeticOperators | FeatureComparisonOperators | FeatureBitwiseOperators;
    private const int FeatureBehaviors = FeatureToString | FeatureDispose | FeatureLength | FeaturePairs;
    private const int FeatureAll = FeatureBehaviors | FeatureOperators;

    // LuaPairsBehavior enum values (must match LuaPairsBehavior)
    private const int PairsIfEnumerable = 1;
    private const int PairsProperties = 2;

    // LuaUnknownPropertyBehavior enum values (must match LuaUnknownPropertyBehavior)
    private const int UnknownPropSilent = 1;
    private const int UnknownPropError = 2;

    // LuaOverloadResolution enum values (must match LuaOverloadResolution)
    private const int OverloadBestMatch = 1;
    private const int OverloadError = 2;

    // LuaIndexMembers flag values (must match LuaIndexMembers)
    private const int IndexMembersProperties = 1 << 0;
    private const int IndexMembersMethods = 1 << 1;
    private const int IndexMembersIndexer = 1 << 2;
    private const int IndexMembersEvents = 1 << 3;
    private const int IndexMembersAll = IndexMembersProperties | IndexMembersMethods | IndexMembersIndexer | IndexMembersEvents;

    // LuaIndexPriority enum values (must match LuaIndexPriority)
    private const int IndexPriorityMembersFirst = 1;
    private const int IndexPriorityIndexerFirst = 2;

    // LuaConstructionMode enum values (must match LuaConstructionMode)
    private const int ConstructionDirect = 1;
    private const int ConstructionFactory = 2;

    // LuaTypeOptimizations flag values (must match LuaTypeOptimizations)
    private const int OptimizationNone = 0;
    private const int OptimizationPrimitiveMarshalerBypass = 1 << 0;
    private const int OptimizationAll = OptimizationPrimitiveMarshalerBypass;

    // RefKind values (matches Microsoft.CodeAnalysis.RefKind)
    private const int RefKindNone = 0;
    private const int RefKindRef = 1;
    private const int RefKindOut = 2;
    private const int RefKindIn = 3;

    // LuaArgumentValidation flag values (must match LuaArgumentValidation)
    private const int ArgValidationNone = 0;
    private const int ArgValidationNullability = 1 << 0;
    private const int ArgValidationType = 1 << 1;
    private const int ArgValidationCount = 1 << 2;
    private const int ArgValidationRange = 1 << 3;
    private const int ArgValidationStrict = ArgValidationNullability | ArgValidationType | ArgValidationCount | ArgValidationRange;

    // LuaSystemObjectMembers flag values (must match LuaSystemObjectMembers)
    private const int ObjectMembersNone = 0;
    private const int ObjectMembersToString = 1 << 0;
    private const int ObjectMembersEquals = 1 << 1;
    private const int ObjectMembersGetHashCode = 1 << 2;
    private const int ObjectMembersReferenceEquals = 1 << 3;

    // LuaObsoleteMemberHandling values (must match LuaObsoleteMemberHandling)
    private const int ObsoleteMembersIgnore = 1;
    private const int ObsoleteMembersInclude = 2;

    // LuaAwaitableReturnHandling values (must match LuaAwaitableReturnHandling)
    private const int AwaitableReturnImplicitAwait = 1;
    private const int AwaitableReturnReturnAwaitable = 2;

    // LuaFromTableConversion values (must match LuaFromTableConversion)
    private const int FromTableConversionDisabled = 1;
    private const int FromTableConversionWhenTypeHasParameterlessConstructor = 2;
    private const int FromTableConversionEnabled = 3;

    // LuaAllowedValueConversions flag values (must match LuaAllowedValueConversions)
    private const int AllowedConversionsNone = 0;
    private const int AllowedConversionsStringToPrimitive = 1 << 0;
    private const int AllowedConversionsTableToObject = 1 << 1;
    private const int AllowedConversionsFunctionToCallableTarget = 1 << 2;
    private const int AllowedConversionsObjectFallback = 1 << 3;
    private const int AllowedConversionsPrimitiveToString = 1 << 4;
    private const int AllowedConversionsStringToChar = 1 << 5;
    private const int AllowedConversionsStringToEnum = 1 << 6;
    private const int AllowedConversionsNumberToEnum = 1 << 7;
    private const int AllowedConversionsTruncateFraction = 1 << 8;
    private const int AllowedConversionsAllowOverflow = 1 << 9;
    private const int AllowedConversionsDefault = AllowedConversionsAll;
    private const int AllowedConversionsAll = AllowedConversionsStringToPrimitive | AllowedConversionsTableToObject | AllowedConversionsFunctionToCallableTarget | AllowedConversionsObjectFallback | AllowedConversionsPrimitiveToString | AllowedConversionsStringToChar | AllowedConversionsStringToEnum | AllowedConversionsNumberToEnum | AllowedConversionsTruncateFraction | AllowedConversionsAllowOverflow;

    // Maps C# operator method names to Lua metamethods and LuaTypeFeatures flag values
    private static readonly Dictionary<string, (string Metamethod, int Flag)> OperatorMetamethodsAndFlags = new(StringComparer.Ordinal)
    {
        ["op_Addition"] = ("__add", FeatureAdd),
        ["op_Subtraction"] = ("__sub", FeatureSubtract),
        ["op_Multiply"] = ("__mul", FeatureMultiply),
        ["op_Division"] = ("__div", FeatureDivide),
        ["op_Modulus"] = ("__mod", FeatureModulus),
        ["op_UnaryNegation"] = ("__unm", FeatureUnaryNegation),
        ["op_Equality"] = ("__eq", FeatureEquality),
        ["op_LessThan"] = ("__lt", FeatureLessThan),
        ["op_LessThanOrEqual"] = ("__le", FeatureLessThanOrEqual),
        ["op_BitwiseAnd"] = ("__band", FeatureBitwiseAnd),
        ["op_BitwiseOr"] = ("__bor", FeatureBitwiseOr),
        ["op_ExclusiveOr"] = ("__bxor", FeatureExclusiveOr),
        ["op_OnesComplement"] = ("__bnot", FeatureOnesComplement),
        ["op_LeftShift"] = ("__shl", FeatureLeftShift),
        ["op_RightShift"] = ("__shr", FeatureRightShift),
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var assemblyDefaults = context.CompilationProvider
            .Select(static (compilation, _) => ReadAssemblyDefaults(compilation));

        var types = context.SyntaxProvider
            .ForAttributeWithMetadataName(LuaTypeAttributeName,
                static (node, _) => node is TypeDeclarationSyntax typeDecl
                    && typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword),
                static (ctx, _) => ExtractTypeInfo(ctx))
            .Where(static info => info != null)
            .Select(static (info, _) => info!);

        var combined = types.Combine(assemblyDefaults);

        context.RegisterSourceOutput(combined, static (ctx, pair) =>
        {
            var (typeInfo, defaults) = pair;

            ResolveConfig(typeInfo, defaults);
            FilterMembers(typeInfo);
            AssignGeneratedMethodIdentifiers(typeInfo);
            ValidateResolvedLuaNames(typeInfo);
            ResolveFromTableConversionEmission(typeInfo);

            foreach (var diag in typeInfo.Diagnostics)
            {
                if (diag.Id is "LAYLUA100" or "LAYLUA101")
                {
                    var parts = diag.Message.Split('|');
                    var descriptor = diag.Id == "LAYLUA100"
                        ? UnsupportedParameterDiagnostic
                        : UnsupportedParameterInheritedDiagnostic;

                    ctx.ReportDiagnostic(Diagnostic.Create(descriptor,
                        diag.Location,
                        diag.Properties,
                        parts[0], parts[1], parts[2], parts[3], parts[4]));
                }
                else if (diag.Id is "LAYLUA102")
                {
                    var parts = diag.Message.Split('|');
                    ctx.ReportDiagnostic(Diagnostic.Create(UnsupportedEventDiagnostic,
                        diag.Location,
                        diag.Properties,
                        parts[0], parts[1], parts[2], parts[3]));
                }
                else if (diag.Id is "LAYLUA103")
                {
                    var parts = diag.Message.Split('|');
                    ctx.ReportDiagnostic(Diagnostic.Create(UnsupportedConstructorDiagnostic,
                        diag.Location,
                        diag.Properties,
                        parts[0], parts[1], parts[2]));
                }
                else if (diag.Id is "LAYLUA107")
                {
                    var parts = diag.Message.Split('|');
                    ctx.ReportDiagnostic(Diagnostic.Create(InvalidEnumConfigDiagnostic,
                        diag.Location,
                        diag.Properties,
                        parts[0], parts[1], parts[2], parts[3]));
                }
                else if (diag.Id is "LAYLUA108")
                {
                    var parts = diag.Message.Split('|');
                    ctx.ReportDiagnostic(Diagnostic.Create(InvalidFlagsConfigDiagnostic,
                        diag.Location,
                        diag.Properties,
                        parts[0], parts[1], parts[2], parts[3], parts[4]));
                }
                else if (diag.Id is "LAYLUA109")
                {
                    var parts = diag.Message.Split('|');
                    ctx.ReportDiagnostic(Diagnostic.Create(LuaNameCollisionDiagnostic,
                        diag.Location,
                        diag.Properties,
                        parts[0], parts[1], parts[2], parts[3]));
                }
                else if (diag.Id is "LAYLUA110")
                {
                    var parts = diag.Message.Split('|');
                    ctx.ReportDiagnostic(Diagnostic.Create(UnsupportedFromTableConversionDiagnostic,
                        diag.Location,
                        diag.Properties,
                        parts[0], parts[1]));
                }
            }

            if (HasFatalDiagnostics(typeInfo))
            {
                return;
            }

            var source = GenerateSource(typeInfo);
            ctx.AddSource($"{typeInfo.SafeName}.LuaType.g.cs", source);
        });
    }

    private static LuaTypeInfo? ExtractTypeInfo(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol symbol)
        {
            return null;
        }

        if (symbol.IsGenericType)
        {
            return null;
        }

        if (symbol.IsRefLikeType)
        {
            return null;
        }

        {
            var containing = symbol.ContainingType;
            while (containing != null)
            {
                if (containing.IsGenericType)
                {
                    return null;
                }

                containing = containing.ContainingType;
            }
        }

        var config = ReadTypeConfig(symbol);
        var fullName = symbol.ToDisplayString();

        var nameBuilder = new List<string>();
        var containingType = symbol.ContainingType;
        while (containingType != null)
        {
            nameBuilder.Insert(0, containingType.Name);
            containingType = containingType.ContainingType;
        }

        nameBuilder.Add(symbol.Name);
        var typePath = string.Join(".", nameBuilder);

        var info = new LuaTypeInfo
        {
            Name = symbol.Name,
            SafeName = fullName.Replace('.', '_').Replace('<', '_').Replace('>', '_'),
            FullName = symbol.ContainingNamespace?.IsGlobalNamespace == false
                ? $"{symbol.ContainingNamespace.ToDisplayString()}.{typePath}"
                : typePath,
            Namespace = symbol.ContainingNamespace?.IsGlobalNamespace == false
                ? symbol.ContainingNamespace.ToDisplayString()
                : null,
            IsAbstract = symbol.IsAbstract,
            IsValueType = symbol.IsValueType,
            HasParameterlessConstructor = symbol.IsValueType || symbol.InstanceConstructors.Any(static c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public),
            ImplementsDisposable = ImplementsInterface(symbol, "System.IDisposable"),
            ImplementsEnumerable = ImplementsInterface(symbol, "System.Collections.IEnumerable"),
            ImplementsLuaUserDataFromTable = symbol.AllInterfaces.Any(@interface =>
                @interface.Name == "ILuaUserDataFromTable"
                && @interface.ContainingNamespace.ToDisplayString() == "Laylua.Marshaling"
                && @interface.TypeArguments.Length == 1
                && SymbolEqualityComparer.Default.Equals(@interface.TypeArguments[0], symbol)),
            BaseHasLuaType = HasBaseWithAttribute(symbol, LuaTypeAttributeName),
            TypeLocation = symbol.Locations.FirstOrDefault(),
            RawConfig = config,
        };

        // Detect __len property: Count -> Length -> ICollection.Count
        var lengthProp = FindLengthProperty(symbol);
        if (lengthProp != null)
        {
            info.LengthPropertyName = lengthProp;
        }

        containingType = symbol.ContainingType;
        while (containingType != null)
        {
            var keyword = containingType.IsValueType ? "struct" : "class";
            info.ContainingTypes.Insert(0, (keyword, containingType.Name));
            containingType = containingType.ContainingType;
        }

        CollectRequiredMembers(symbol, info);
        CollectMembers(symbol, info, GetInitialNamingPolicy(config));

        return info;
    }

    private static void CollectRequiredMembers(INamedTypeSymbol symbol, LuaTypeInfo info)
    {
        var seenProperties = new HashSet<string>(StringComparer.Ordinal);
        var seenFields = new HashSet<string>(StringComparer.Ordinal);
        var currentType = symbol;

        while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
        {
            foreach (var member in currentType.GetMembers())
            {
                switch (member)
                {
                    case IPropertySymbol property when property is
                    {
                        IsIndexer: false,
                        IsRequired: true,
                        IsStatic: false
                    }:
                    {
                        if (seenProperties.Add(property.Name))
                        {
                            info.RequiredMembers.Add(new LuaRequiredMemberInfo
                            {
                                ClrName = property.Name,
                                DeclarationLocation = property.Locations.FirstOrDefault(static location => location.IsInSource) ?? info.TypeLocation,
                            });
                        }

                        break;
                    }
                    case IFieldSymbol field when field is
                    {
                        IsImplicitlyDeclared: false,
                        IsRequired: true,
                        IsStatic: false
                    }:
                    {
                        if (seenFields.Add(field.Name))
                        {
                            info.RequiredMembers.Add(new LuaRequiredMemberInfo
                            {
                                ClrName = field.Name,
                                DeclarationLocation = field.Locations.FirstOrDefault(static location => location.IsInSource) ?? info.TypeLocation,
                            });
                        }

                        break;
                    }
                }
            }

            currentType = currentType.BaseType;
        }
    }

    private static void CollectMembers(
        INamedTypeSymbol symbol,
        LuaTypeInfo info,
        int namingPolicy)
    {
        var seenProperties = new HashSet<string>(StringComparer.Ordinal);
        var seenFields = new HashSet<string>(StringComparer.Ordinal);
        var seenInstanceMethodNames = new HashSet<string>(StringComparer.Ordinal);
        var seenStaticMethodNames = new HashSet<string>(StringComparer.Ordinal);
        var seenEvents = new HashSet<string>(StringComparer.Ordinal);
        var currentType = symbol;

        while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
        {
            foreach (var member in currentType.GetMembers())
            {
                if (HasAttribute(member, LuaIgnoreAttributeName))
                {
                    continue;
                }

                var isBaseType = !SymbolEqualityComparer.Default.Equals(currentType, symbol);
                if (member.DeclaredAccessibility != Accessibility.Public
                    && !HasAttribute(member, LuaIncludeAttributeName))
                {
                    continue;
                }

                // [LuaInclude] on private base members would produce uncompilable code - skip
                if (isBaseType && member.DeclaredAccessibility == Accessibility.Private)
                {
                    continue;
                }

                var isObsolete = HasAttribute(member, "System.ObsoleteAttribute");

                var explicitLuaName = GetLuaName(member);
                var luaName = explicitLuaName ?? ApplyNamingPolicy(member.Name, namingPolicy);
                var hasExplicitLuaName = explicitLuaName != null;

                switch (member)
                {
                    case IPropertySymbol property when property.IsIndexer:
                    {
                        if (property.Parameters.Length != 1)
                        {
                            break;
                        }

                        var keyType = property.Parameters[0].Type;
                        if (IsUnsupportedType(keyType) || IsUnsupportedType(property.Type))
                        {
                            break;
                        }

                        var keyKind = ClassifyType(keyType);

                        // Skip duplicate key types (by full type name for precision)
                        var keyTypeFqn = keyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        if (info.Indexers.Any(ix => ix.KeyTypeName == keyTypeFqn))
                        {
                            break;
                        }

                        var hasLuaInclude = HasAttribute(member, LuaIncludeAttributeName);
                        info.Indexers.Add(new LuaIndexerInfo
                        {
                            KeyTypeName = keyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            KeyValueKind = keyKind,
                            ValueTypeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            ValueKind = ClassifyType(property.Type),
                            HasGetter = property.GetMethod != null
                                && (property.GetMethod.DeclaredAccessibility == Accessibility.Public || hasLuaInclude),
                            HasSetter = property.SetMethod != null
                                && !property.SetMethod.IsInitOnly
                                && (property.SetMethod.DeclaredAccessibility == Accessibility.Public || hasLuaInclude),
                        });

                        break;
                    }

                    case IPropertySymbol property when !property.IsIndexer:
                    {
                        if (!seenProperties.Add(property.Name))
                        {
                            break;
                        }

                        var isLuaStringProperty = HasAttribute(member, LuaStringAttributeName) && IsLuaStringCandidate(property.Type);
                        if (IsUnsupportedType(property.Type) && !isLuaStringProperty)
                        {
                            break;
                        }

                        var memberInfo = new LuaMemberInfo
                        {
                            ClrName = property.Name,
                            LuaName = luaName,
                            HasExplicitLuaName = hasExplicitLuaName,
                            TypeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            DeclaringTypeName = property.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            DeclarationLocation = property.Locations.FirstOrDefault(static location => location.IsInSource) ?? info.TypeLocation,
                            CanRead = property.GetMethod != null
                                && (property.GetMethod.DeclaredAccessibility == Accessibility.Public
                                    || HasAttribute(member, LuaIncludeAttributeName)),
                            CanWrite = !isLuaStringProperty && property.SetMethod != null
                                && !property.SetMethod.IsInitOnly
                                && (property.SetMethod.DeclaredAccessibility == Accessibility.Public
                                    || HasAttribute(member, LuaIncludeAttributeName)),
                            CanTableWrite = !isLuaStringProperty && property.SetMethod != null
                                && (property.SetMethod.DeclaredAccessibility == Accessibility.Public
                                    || HasAttribute(member, LuaIncludeAttributeName)),
                            IsInitOnly = property.SetMethod?.IsInitOnly == true,
                            IsRequired = property.IsRequired,
                            ValueKind = isLuaStringProperty ? LuaValueKind.String : ClassifyType(property.Type),
                            IsLuaString = isLuaStringProperty,
                            IsObsolete = isObsolete,
                        };

                        if (property.IsStatic)
                        {
                            info.StaticProperties.Add(memberInfo);
                        }
                        else
                        {
                            info.Properties.Add(memberInfo);
                        }

                        break;
                    }

                    case IFieldSymbol field when !field.IsImplicitlyDeclared:
                    {
                        if (!seenFields.Add(field.Name))
                        {
                            break;
                        }

                        var isLuaStringField = HasAttribute(member, LuaStringAttributeName) && IsLuaStringCandidate(field.Type);
                        if (IsUnsupportedType(field.Type) && !isLuaStringField)
                        {
                            break;
                        }

                        var memberInfo = new LuaMemberInfo
                        {
                            ClrName = field.Name,
                            LuaName = luaName,
                            HasExplicitLuaName = hasExplicitLuaName,
                            TypeName = field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            DeclaringTypeName = field.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            DeclarationLocation = field.Locations.FirstOrDefault(static location => location.IsInSource) ?? info.TypeLocation,
                            CanRead = true,
                            CanWrite = !isLuaStringField && !field.IsReadOnly && !field.IsConst,
                            CanTableWrite = !isLuaStringField && !field.IsReadOnly && !field.IsConst,
                            IsInitOnly = false,
                            IsRequired = field.IsRequired,
                            IsConstant = field.IsConst,
                            ValueKind = isLuaStringField ? LuaValueKind.String : ClassifyType(field.Type),
                            IsLuaString = isLuaStringField,
                            IsObsolete = isObsolete,
                        };

                        if (field.IsStatic || field.IsConst)
                        {
                            info.StaticFields.Add(memberInfo);
                        }
                        else
                        {
                            info.Fields.Add(memberInfo);
                        }

                        break;
                    }

                    case IMethodSymbol method when method.MethodKind == MethodKind.Ordinary:
                    {
                        if (info.ImplementsLuaUserDataFromTable && IsLuaUserDataFromTableImplementationMethod(method, symbol))
                        {
                            break;
                        }

                        if (method.Name is "GetType" or "Dispose")
                        {
                            break;
                        }

                        // Tag object-inherited methods with their flag value for deferred filtering
                        var objectMethodFlag = method.Name switch
                        {
                            "ToString" when method.ContainingType.SpecialType == SpecialType.System_Object
                                || method.IsOverride => ObjectMembersToString,
                            "Equals" when method.ContainingType.SpecialType == SpecialType.System_Object
                                || method.IsOverride || method.IsStatic => ObjectMembersEquals,
                            "GetHashCode" when method.ContainingType.SpecialType == SpecialType.System_Object
                                || method.IsOverride => ObjectMembersGetHashCode,
                            "ReferenceEquals" when method.IsStatic => ObjectMembersReferenceEquals,
                            _ => 0,
                        };

                        // Skip methods with unsupported parameter types (ref struct, pointer, etc.)
                        // Parameters with [LuaString] on span/memory types are exempt
                        var unsupportedParam = method.Parameters.FirstOrDefault(static parameter => IsUnsupportedType(parameter.Type)
                            && !(HasAttribute(parameter, LuaStringAttributeName) && IsLuaStringCandidate(parameter.Type)));

                        if (unsupportedParam != null)
                        {
                            AddUnsupportedMethodDiagnostic(info,
                                symbol,
                                currentType,
                                method,
                                $"parameter '{unsupportedParam.Name}'",
                                unsupportedParam.Type);

                            break;
                        }

                        // Skip methods with unsupported return types
                        if (!method.ReturnsVoid && (method.ReturnsByRef || method.ReturnsByRefReadonly || IsUnsupportedType(method.ReturnType)))
                        {
                            AddUnsupportedMethodDiagnostic(info,
                                symbol,
                                currentType,
                                method,
                                "return type",
                                method.ReturnType);

                            break;
                        }

                        var hasCallingThread = method.Parameters.Any(p => HasAttribute(p, LuaCallingThreadAttributeName));
                        var hasReturnCountAttribute = HasReturnAttribute(method, LuaReturnCountAttributeName);

                        var returnsRawCount = hasCallingThread
                            && hasReturnCountAttribute
                            && IsIntegerType(method.ReturnType);

                        var parameters = method.Parameters
                            .Select(static parameter =>
                            {
                                var isParams = parameter.IsParams;
                                var elementType = isParams && parameter.Type is IArrayTypeSymbol arrayType
                                    ? arrayType.ElementType
                                    : null;

                                var isLuaString = HasAttribute(parameter, LuaStringAttributeName) && IsLuaStringCandidate(parameter.Type);

                                return new LuaParameterInfo
                                {
                                    Name = parameter.Name,
                                    TypeName = parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                    ValueKind = isLuaString
                                        ? LuaValueKind.String
                                        : ClassifyType(parameter.Type),
                                    IsOptional = parameter.HasExplicitDefaultValue,
                                    DefaultValue = parameter.HasExplicitDefaultValue
                                        ? FormatDefaultValue(parameter)
                                        : null,
                                    IsCallingThread = HasAttribute(parameter, LuaCallingThreadAttributeName),
                                    IsLuaReference = InheritsFromLuaReference(parameter.Type)
                                        || (parameter.Type is IArrayTypeSymbol parameterArrayType
                                            && InheritsFromLuaReference(parameterArrayType.ElementType))
                                        || (elementType != null && InheritsFromLuaReference(elementType)),
                                    IsParams = isParams,
                                    ParamsElementTypeName = elementType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                    ParamsElementKind = elementType != null ? ClassifyType(elementType) : LuaValueKind.Marshaled,
                                    IsParamsElementNonNullableReference = elementType != null
                                        && !elementType.IsValueType
                                        && elementType.NullableAnnotation == NullableAnnotation.NotAnnotated,
                                    RefKind = (int) parameter.RefKind,
                                    IsNonNullableReference = !parameter.Type.IsValueType
                                        && parameter.NullableAnnotation == NullableAnnotation.NotAnnotated,
                                    IsValueType = parameter.Type.IsValueType,
                                    IsNullableValueType = parameter.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T,
                                    IsLuaString = isLuaString,
                                };
                            }).ToList();

                        var methodInfo = new LuaMethodInfo
                        {
                            ClrName = method.Name,
                            LuaName = luaName,
                            HasExplicitLuaName = hasExplicitLuaName,
                            ReturnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            DeclarationLocation = method.Locations.FirstOrDefault(static location => location.IsInSource) ?? info.TypeLocation,
                            ReturnsVoid = method.ReturnsVoid,
                            ReturnsRawCount = returnsRawCount,
                            ReturnValueKind = method.ReturnsVoid || returnsRawCount
                                ? LuaValueKind.Void
                                : ClassifyType(method.ReturnType),
                            Parameters = parameters,
                            IsObsolete = isObsolete,
                            ObjectMethodFlag = objectMethodFlag,
                        };

                        if (IsAwaitableTypeName(methodInfo.ReturnType)
                            && (methodInfo.ReturnsRawCount
                                || methodInfo.Parameters.Any(static parameter =>
                                    parameter.RefKind == RefKindOut
                                    || parameter.RefKind == RefKindRef
                                    || IsLuaStackValueRangeTypeName(parameter.TypeName))))
                        {
                            AddUnsupportedMethodDiagnostic(info,
                                symbol,
                                currentType,
                                method,
                                "awaitable return type with out/ref Lua returns or stack-bound arguments",
                                method.ReturnType);

                            break;
                        }

                        var seenSet = method.IsStatic ? seenStaticMethodNames : seenInstanceMethodNames;

                        // [LuaTypeCall] methods go to TypeCallMethods, not regular method dispatch
                        if (!method.IsStatic && HasAttribute(method, LuaTypeCallAttributeName))
                        {
                            if (info.TypeCallMethods.Count > 0)
                            {
                                var existing = info.TypeCallMethods[0];
                                existing.Overloads ??= [];
                                existing.Overloads.Add(methodInfo);
                            }
                            else
                            {
                                // Use __call as the Lua-facing name for error messages
                                methodInfo.LuaName = "__call";
                                info.TypeCallMethods.Add(methodInfo);
                            }

                            break;
                        }

                        if (!seenSet.Add(luaName))
                        {
                            var targetList = method.IsStatic ? info.StaticMethods : info.Methods;
                            var existing = targetList.Find(m => m.LuaName == luaName);
                            if (existing != null)
                            {
                                existing.Overloads ??= [];
                                existing.Overloads.Add(methodInfo);
                            }

                            break;
                        }

                        if (method.IsStatic)
                        {
                            info.StaticMethods.Add(methodInfo);
                        }
                        else
                        {
                            info.Methods.Add(methodInfo);
                        }

                        break;
                    }

                    case IMethodSymbol operatorMethod when operatorMethod.MethodKind == MethodKind.UserDefinedOperator:
                    {
                        if (!OperatorMetamethodsAndFlags.TryGetValue(operatorMethod.Name, out var metamethodAndFlag))
                        {
                            break;
                        }

                        info.Operators.Add(new LuaOperatorInfo
                        {
                            MethodName = operatorMethod.Name,
                            MetamethodName = metamethodAndFlag.Metamethod,
                            ReturnType = operatorMethod.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            ReturnValueKind = ClassifyType(operatorMethod.ReturnType),
                            ReturnsAnnotatedType = SymbolEqualityComparer.Default.Equals(operatorMethod.ReturnType, symbol),
                            Parameters = operatorMethod.Parameters.Select(parameter =>
                                new LuaOperatorParameterInfo
                                {
                                    TypeName = parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                    IsAnnotatedType = SymbolEqualityComparer.Default.Equals(parameter.Type, symbol),
                                    ValueKind = ClassifyType(parameter.Type),
                                }).ToList(),
                        });

                        break;
                    }

                    case IEventSymbol eventSymbol:
                    {
                        if (!seenEvents.Add(eventSymbol.Name))
                        {
                            break;
                        }

                        if (eventSymbol.Type is not INamedTypeSymbol delegateType || delegateType.DelegateInvokeMethod == null)
                        {
                            break;
                        }

                        var delegateInvokeMethod = delegateType.DelegateInvokeMethod;
                        var unsupportedEventParam = delegateInvokeMethod.Parameters.FirstOrDefault(static parameter => parameter.RefKind != RefKind.None || IsUnsupportedType(parameter.Type));
                        if (unsupportedEventParam != null)
                        {
                            info.Diagnostics.Add(new LuaDiagnosticInfo
                            {
                                Id = "LAYLUA102",
                                Message = $"{eventSymbol.Name}|{info.Name}|parameter '{unsupportedEventParam.Name}'|{unsupportedEventParam.Type.ToDisplayString()}",
                                Location = eventSymbol.Locations.FirstOrDefault(static location => location.IsInSource) ?? info.TypeLocation,
                                Properties = ImmutableDictionary<string, string?>.Empty.Add("eventName", eventSymbol.Name),
                            });

                            break;
                        }

                        if (delegateInvokeMethod.ReturnsByRef
                            || delegateInvokeMethod.ReturnsByRefReadonly
                            || IsUnsupportedType(delegateInvokeMethod.ReturnType))
                        {
                            info.Diagnostics.Add(new LuaDiagnosticInfo
                            {
                                Id = "LAYLUA102",
                                Message = $"{eventSymbol.Name}|{info.Name}|return value|{delegateInvokeMethod.ReturnType.ToDisplayString()}",
                                Location = eventSymbol.Locations.FirstOrDefault(static location => location.IsInSource) ?? info.TypeLocation,
                                Properties = ImmutableDictionary<string, string?>.Empty
                                    .Add("eventName", eventSymbol.Name),
                            });

                            break;
                        }

                        var hasLuaInclude = HasAttribute(member, LuaIncludeAttributeName);
                        var eventInfo = new LuaEventInfo
                        {
                            ClrName = eventSymbol.Name,
                            LuaName = luaName,
                            HasExplicitLuaName = hasExplicitLuaName,
                            DelegateTypeName = delegateType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            DeclarationLocation = eventSymbol.Locations.FirstOrDefault(static location => location.IsInSource) ?? info.TypeLocation,
                            CanAdd = eventSymbol.AddMethod != null
                                && (eventSymbol.AddMethod.DeclaredAccessibility == Accessibility.Public || hasLuaInclude),
                            CanRemove = eventSymbol.RemoveMethod != null
                                && (eventSymbol.RemoveMethod.DeclaredAccessibility == Accessibility.Public || hasLuaInclude),
                            IsObsolete = isObsolete,
                            IsStatic = eventSymbol.IsStatic,
                        };

                        if (eventSymbol.IsStatic)
                        {
                            info.StaticEvents.Add(eventInfo);
                        }
                        else
                        {
                            info.Events.Add(eventInfo);
                        }

                        break;
                    }
                }
            }

            currentType = currentType.BaseType;
        }

        // Collect constructors from the declared type only (not base types)
        if (!symbol.IsAbstract)
        {
            foreach (var constructor in symbol.Constructors)
            {
                if (constructor.IsStatic || (constructor.IsImplicitlyDeclared && constructor.Parameters.Length == 0))
                {
                    continue;
                }

                if (HasAttribute(constructor, LuaIgnoreAttributeName))
                {
                    continue;
                }

                if (constructor.DeclaredAccessibility != Accessibility.Public
                    && !HasAttribute(constructor, LuaIncludeAttributeName))
                {
                    continue;
                }

                var unsupportedConstructorParam = constructor.Parameters.FirstOrDefault(static parameter => parameter.RefKind != RefKind.None
                    || (IsUnsupportedType(parameter.Type) && !(HasAttribute(parameter, LuaStringAttributeName) && IsLuaStringCandidate(parameter.Type))));

                if (unsupportedConstructorParam != null)
                {
                    info.Diagnostics.Add(new LuaDiagnosticInfo
                    {
                        Id = "LAYLUA103",
                        Message = $"{info.Name}|{unsupportedConstructorParam.Name}|{unsupportedConstructorParam.Type.ToDisplayString()}",
                        Location = constructor.Locations.FirstOrDefault(static location => location.IsInSource) ?? info.TypeLocation,
                        Properties = ImmutableDictionary<string, string?>.Empty.Add("typeName", info.Name),
                    });

                    continue;
                }

                info.Constructors.Add(new LuaConstructorInfo
                {
                    Parameters = constructor.Parameters.Select(parameter =>
                    {
                        var isParams = parameter.IsParams;
                        var elementType = isParams && parameter.Type is IArrayTypeSymbol arrayType
                            ? arrayType.ElementType
                            : null;

                        var isLuaString = HasAttribute(parameter, LuaStringAttributeName) && IsLuaStringCandidate(parameter.Type);

                        return new LuaParameterInfo
                        {
                            Name = parameter.Name,
                            TypeName = parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            ValueKind = isLuaString ? LuaValueKind.String : ClassifyType(parameter.Type),
                            IsOptional = parameter.HasExplicitDefaultValue,
                            DefaultValue = parameter.HasExplicitDefaultValue
                                ? FormatDefaultValue(parameter)
                                : null,
                            IsLuaReference = InheritsFromLuaReference(parameter.Type)
                                || (parameter.Type is IArrayTypeSymbol parameterArrayType
                                    && InheritsFromLuaReference(parameterArrayType.ElementType))
                                || (elementType != null && InheritsFromLuaReference(elementType)),
                            IsParams = isParams,
                            ParamsElementTypeName = elementType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            ParamsElementKind = elementType != null ? ClassifyType(elementType) : LuaValueKind.Marshaled,
                            IsParamsElementNonNullableReference = elementType != null
                                && !elementType.IsValueType
                                && elementType.NullableAnnotation == NullableAnnotation.NotAnnotated,
                            IsNonNullableReference = !parameter.Type.IsValueType
                                && parameter.NullableAnnotation == NullableAnnotation.NotAnnotated,
                            IsValueType = parameter.Type.IsValueType,
                            IsNullableValueType = parameter.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T,
                            IsLuaString = isLuaString,
                        };
                    }).ToList(),
                });
            }
        }
    }

    private static int GetInitialNamingPolicy(LuaTypeConfig config)
    {
        if (config.NamingPolicy is PolicyOriginal
            or PolicyLowerCase
            or PolicyUpperCase
            or PolicyCamelCase
            or PolicyLowerSnakeCase
            or PolicyUpperSnakeCase
            or PolicyPascalCase)
        {
            return config.NamingPolicy.Value;
        }

        return PolicyOriginal;
    }

    private static LuaTypeConfig ReadTypeConfig(INamedTypeSymbol symbol)
    {
        var config = new LuaTypeConfig();

        var attribute = symbol.GetAttributes().FirstOrDefault(attr =>
            attr.AttributeClass?.ToDisplayString() == LuaTypeAttributeName);

        if (attribute == null)
        {
            return config;
        }

        foreach (var namedArgument in attribute.NamedArguments)
        {
            switch (namedArgument.Key)
            {
                case "NamingPolicy" when namedArgument.Value.Value is int policy:
                {
                    config.NamingPolicy = policy;
                    break;
                }
                case "Members" when namedArgument.Value.Value is int members:
                {
                    config.Members = members;
                    break;
                }
                case "Features" when namedArgument.Value.Value is int feat:
                {
                    config.Features = feat;
                    break;
                }
                case "PairsBehavior" when namedArgument.Value.Value is int pairs:
                {
                    config.PairsBehavior = pairs;
                    break;
                }
                case "UnknownPropertyBehavior" when namedArgument.Value.Value is int unknownProp:
                {
                    config.UnknownPropertyBehavior = unknownProp;
                    break;
                }
                case "OverloadResolution" when namedArgument.Value.Value is int overload:
                {
                    config.OverloadResolution = overload;
                    break;
                }
                case "IndexMembers" when namedArgument.Value.Value is int indexMembers:
                {
                    config.IndexMembers = indexMembers;
                    break;
                }
                case "IndexPriority" when namedArgument.Value.Value is int indexPriority:
                {
                    config.IndexPriority = indexPriority;
                    break;
                }
                case "Optimizations" when namedArgument.Value.Value is int optimizations:
                {
                    config.Optimizations = optimizations;
                    break;
                }
                case "Construction" when namedArgument.Value.Value is int construction:
                {
                    config.Construction = construction;
                    break;
                }
                case "FactoryMethodName" when namedArgument.Value.Value is string factoryName:
                {
                    config.FactoryMethodName = factoryName;
                    break;
                }
                case "ArgumentValidation" when namedArgument.Value.Value is int argValidation:
                {
                    config.ArgumentValidation = argValidation;
                    break;
                }
                case "ObjectMembers" when namedArgument.Value.Value is int objectMembers:
                {
                    config.ObjectMembers = objectMembers;
                    break;
                }
                case "ObsoleteMembers" when namedArgument.Value.Value is int obsoleteMembers:
                {
                    config.ObsoleteMembers = obsoleteMembers;
                    break;
                }
                case "AwaitableReturnHandling" when namedArgument.Value.Value is int awaitableReturnHandling:
                {
                    config.AwaitableReturnHandling = awaitableReturnHandling;
                    break;
                }
                case "FromTableConversion" when namedArgument.Value.Value is int fromTableConversion:
                {
                    config.FromTableConversion = fromTableConversion;
                    config.FromTableConversionLocation = GetNamedArgumentLocation(attribute, "FromTableConversion");
                    break;
                }
                case "BindingConversions" when namedArgument.Value.Value is int bindingConversions:
                {
                    config.BindingConversions = bindingConversions;
                    break;
                }
            }
        }

        return config;
    }

    private static LuaTypeConfig ReadAssemblyDefaults(Compilation compilation)
    {
        var defaults = new LuaTypeConfig
        {
            NamingPolicy = PolicyOriginal,
            Members = MembersAll,
            Features = FeatureAll,
            PairsBehavior = PairsIfEnumerable,
            UnknownPropertyBehavior = UnknownPropSilent,
            OverloadResolution = OverloadBestMatch,
            IndexMembers = IndexMembersAll,
            IndexPriority = IndexPriorityMembersFirst,
            Optimizations = OptimizationAll,
            Construction = ConstructionDirect,
            FactoryMethodName = null,
            ArgumentValidation = ArgValidationStrict,
            ObjectMembers = ObjectMembersNone,
            ObsoleteMembers = ObsoleteMembersIgnore,
            AwaitableReturnHandling = AwaitableReturnImplicitAwait,
            FromTableConversion = FromTableConversionWhenTypeHasParameterlessConstructor,
            BindingConversions = AllowedConversionsDefault,
        };

        var attribute = compilation.Assembly.GetAttributes().FirstOrDefault(attr =>
            attr.AttributeClass?.ToDisplayString() == LuaTypeDefaultsAttributeName);

        if (attribute == null)
        {
            return defaults;
        }

        foreach (var namedArgument in attribute.NamedArguments)
        {
            switch (namedArgument.Key)
            {
                case "NamingPolicy" when namedArgument.Value.Value is int policy:
                {
                    defaults.NamingPolicy = policy;
                    break;
                }
                case "Members" when namedArgument.Value.Value is int members:
                {
                    defaults.Members = members;
                    break;
                }
                case "Features" when namedArgument.Value.Value is int features:
                {
                    defaults.Features = features;
                    break;
                }
                case "PairsBehavior" when namedArgument.Value.Value is int pairs:
                {
                    defaults.PairsBehavior = pairs;
                    break;
                }
                case "UnknownPropertyBehavior" when namedArgument.Value.Value is int unknownProp:
                {
                    defaults.UnknownPropertyBehavior = unknownProp;
                    break;
                }
                case "OverloadResolution" when namedArgument.Value.Value is int overload:
                {
                    defaults.OverloadResolution = overload;
                    break;
                }
                case "IndexMembers" when namedArgument.Value.Value is int indexMembers:
                {
                    defaults.IndexMembers = indexMembers;
                    break;
                }
                case "IndexPriority" when namedArgument.Value.Value is int indexPriority:
                {
                    defaults.IndexPriority = indexPriority;
                    break;
                }
                case "Optimizations" when namedArgument.Value.Value is int optimizations:
                {
                    defaults.Optimizations = optimizations;
                    break;
                }
                case "Construction" when namedArgument.Value.Value is int construction:
                {
                    defaults.Construction = construction;
                    break;
                }
                case "FactoryMethodName" when namedArgument.Value.Value is string factoryName:
                {
                    defaults.FactoryMethodName = factoryName;
                    break;
                }
                case "ArgumentValidation" when namedArgument.Value.Value is int argValidation:
                {
                    defaults.ArgumentValidation = argValidation;
                    break;
                }
                case "ObjectMembers" when namedArgument.Value.Value is int objectMembers:
                {
                    defaults.ObjectMembers = objectMembers;
                    break;
                }
                case "ObsoleteMembers" when namedArgument.Value.Value is int obsoleteMembers:
                {
                    defaults.ObsoleteMembers = obsoleteMembers;
                    break;
                }
                case "AwaitableReturnHandling" when namedArgument.Value.Value is int awaitableReturnHandling:
                {
                    defaults.AwaitableReturnHandling = awaitableReturnHandling;
                    break;
                }
                case "FromTableConversion" when namedArgument.Value.Value is int fromTableConversion:
                {
                    defaults.FromTableConversion = fromTableConversion;
                    defaults.FromTableConversionLocation = GetNamedArgumentLocation(attribute, "FromTableConversion");
                    break;
                }
                case "BindingConversions" when namedArgument.Value.Value is int bindingConversions:
                {
                    defaults.BindingConversions = bindingConversions;
                    break;
                }
            }
        }

        return defaults;
    }

    private static void ResolveConfig(LuaTypeInfo info, LuaTypeConfig assemblyDefaults)
    {
        var config = info.RawConfig;

        info.NamingPolicy = ResolveEnumConfig(info,
            config.NamingPolicy,
            assemblyDefaults.NamingPolicy,
            PolicyOriginal,
            "NamingPolicy",
            "LuaNamingPolicy",
            PolicyOriginal,
            PolicyLowerCase,
            PolicyUpperCase,
            PolicyCamelCase,
            PolicyLowerSnakeCase,
            PolicyUpperSnakeCase,
            PolicyPascalCase);

        info.Members = ResolveFlagsConfig(info, config.Members, assemblyDefaults.Members, MembersAll, "Members", "LuaTypeMembers", MembersAll);
        info.PairsBehavior = ResolveEnumConfig(info,
            config.PairsBehavior,
            assemblyDefaults.PairsBehavior,
            PairsIfEnumerable,
            "PairsBehavior",
            "LuaPairsBehavior",
            PairsIfEnumerable,
            PairsProperties);

        info.UnknownPropertyBehavior = ResolveEnumConfig(info,
            config.UnknownPropertyBehavior,
            assemblyDefaults.UnknownPropertyBehavior,
            UnknownPropSilent,
            "UnknownPropertyBehavior",
            "LuaUnknownPropertyBehavior",
            UnknownPropSilent,
            UnknownPropError);

        info.OverloadResolution = ResolveEnumConfig(info,
            config.OverloadResolution,
            assemblyDefaults.OverloadResolution,
            OverloadBestMatch,
            "OverloadResolution",
            "LuaOverloadResolution",
            OverloadBestMatch,
            OverloadError);

        info.IndexMembers = ResolveFlagsConfig(info,
            config.IndexMembers,
            assemblyDefaults.IndexMembers,
            IndexMembersAll,
            "IndexMembers",
            "LuaIndexMembers",
            IndexMembersAll);

        info.IndexPriority = ResolveEnumConfig(info,
            config.IndexPriority,
            assemblyDefaults.IndexPriority,
            IndexPriorityMembersFirst,
            "IndexPriority",
            "LuaIndexPriority",
            IndexPriorityMembersFirst,
            IndexPriorityIndexerFirst);

        info.Optimizations = ResolveFlagsConfig(info,
            config.Optimizations,
            assemblyDefaults.Optimizations,
            OptimizationAll,
            "Optimizations",
            "LuaTypeOptimizations",
            OptimizationAll);

        info.Construction = ResolveEnumConfig(info,
            config.Construction,
            assemblyDefaults.Construction,
            ConstructionDirect,
            "Construction",
            "LuaConstructionMode",
            ConstructionDirect,
            ConstructionFactory);

        info.FactoryMethodName = config.FactoryMethodName ?? assemblyDefaults.FactoryMethodName ?? "new";
        info.ArgumentValidation = ResolveFlagsConfig(info,
            config.ArgumentValidation,
            assemblyDefaults.ArgumentValidation,
            ArgValidationStrict,
            "ArgumentValidation",
            "LuaArgumentValidation",
            ArgValidationStrict);

        info.ObjectMembers = ResolveFlagsConfig(info,
            config.ObjectMembers,
            assemblyDefaults.ObjectMembers,
            ObjectMembersReferenceEquals | ObjectMembersGetHashCode | ObjectMembersEquals | ObjectMembersToString,
            "ObjectMembers",
            "LuaSystemObjectMembers",
            ObjectMembersReferenceEquals | ObjectMembersGetHashCode | ObjectMembersEquals | ObjectMembersToString);

        info.ObsoleteMembers = ResolveEnumConfig(info,
            config.ObsoleteMembers,
            assemblyDefaults.ObsoleteMembers,
            ObsoleteMembersIgnore,
            "ObsoleteMembers",
            "LuaObsoleteMemberHandling",
            ObsoleteMembersIgnore,
            ObsoleteMembersInclude);

        info.AwaitableReturnHandling = ResolveEnumConfig(info,
            config.AwaitableReturnHandling,
            assemblyDefaults.AwaitableReturnHandling,
            AwaitableReturnImplicitAwait,
            "AwaitableReturnHandling",
            "LuaAwaitableReturnHandling",
            AwaitableReturnImplicitAwait,
            AwaitableReturnReturnAwaitable);

        info.FromTableConversion = ResolveEnumConfig(info,
            config.FromTableConversion,
            assemblyDefaults.FromTableConversion,
            FromTableConversionWhenTypeHasParameterlessConstructor,
            "FromTableConversion",
            "LuaFromTableConversion",
            FromTableConversionDisabled,
            FromTableConversionWhenTypeHasParameterlessConstructor,
            FromTableConversionEnabled);

        info.FromTableConversionLocation = config.FromTableConversionLocation
            ?? assemblyDefaults.FromTableConversionLocation
            ?? info.TypeLocation;

        info.BindingConversions = ResolveFlagsConfig(info,
            config.BindingConversions,
            assemblyDefaults.BindingConversions,
            AllowedConversionsDefault,
            "BindingConversions",
            "LuaAllowedValueConversions",
            AllowedConversionsAll);

        var features = ResolveFlagsConfig(info,
            config.Features,
            assemblyDefaults.Features,
            FeatureAll,
            "Features",
            "LuaTypeFeatures",
            FeatureAll);

        info.Features = features;
        info.EmitToString = (features & FeatureToString) != 0;
        info.EmitDispose = (features & FeatureDispose) != 0 && info.ImplementsDisposable;
        info.EmitOperators = (features & FeatureOperators) != 0;
        info.EmitPairsFeature = (features & FeaturePairs) != 0;
        info.EmitLength = (features & FeatureLength) != 0 && info.LengthPropertyName != null;

        if (config.NamingPolicy == null)
        {
            ReapplyNamingPolicy(info);
        }
    }

    private static int ResolveEnumConfig(
        LuaTypeInfo info,
        int? typeValue,
        int? defaultsValue,
        int fallbackValue,
        string propertyName,
        string enumTypeName,
        params int[] validValues)
    {
        var resolvedValue = typeValue ?? defaultsValue ?? fallbackValue;
        if (validValues.Contains(resolvedValue))
        {
            return resolvedValue;
        }

        AddInvalidConfigDiagnostic(info,
            typeValue.HasValue ? "LuaTypeAttribute" : "LuaTypeDefaultsAttribute",
            propertyName,
            resolvedValue,
            enumTypeName,
            isFlags: false,
            allowedMask: null);

        return fallbackValue;
    }

    private static int ResolveFlagsConfig(
        LuaTypeInfo info,
        int? typeValue,
        int? defaultsValue,
        int fallbackValue,
        string propertyName,
        string enumTypeName,
        int allowedMask)
    {
        var resolvedValue = typeValue ?? defaultsValue ?? fallbackValue;
        if (resolvedValue >= 0 && (resolvedValue & ~allowedMask) == 0)
        {
            return resolvedValue;
        }

        AddInvalidConfigDiagnostic(info,
            typeValue.HasValue ? "LuaTypeAttribute" : "LuaTypeDefaultsAttribute",
            propertyName,
            resolvedValue,
            enumTypeName,
            isFlags: true,
            allowedMask: allowedMask);

        return fallbackValue;
    }

    private static void AddInvalidConfigDiagnostic(
        LuaTypeInfo info,
        string attributeName,
        string propertyName,
        int invalidValue,
        string enumTypeName,
        bool isFlags,
        int? allowedMask)
    {
        info.Diagnostics.Add(new LuaDiagnosticInfo
        {
            Id = isFlags ? "LAYLUA108" : "LAYLUA107",
            Message = isFlags
                ? $"{attributeName}.{propertyName}|{info.Name}|{invalidValue}|{enumTypeName}|{allowedMask}"
                : $"{attributeName}.{propertyName}|{info.Name}|{invalidValue}|{enumTypeName}",
            Location = info.TypeLocation,
            Properties = ImmutableDictionary<string, string?>.Empty,
        });
    }

    private static bool HasAccessibleParameterlessConstructor(LuaTypeInfo info)
    {
        return info.HasParameterlessConstructor || info.Constructors.Any(static constructor => constructor.Parameters.Count == 0);
    }

    private static bool IsLuaUserDataFromTableImplementationMethod(IMethodSymbol method, INamedTypeSymbol containingType)
    {
        return method.Name == "TryGetFromTable"
            && method.IsStatic
            && method.Parameters.Length == 3
            && method.Parameters[0].Type.ToDisplayString() == "Laylua.LuaThread"
            && method.Parameters[1].Type.ToDisplayString() == "Laylua.LuaTable"
            && method.Parameters[2].RefKind == RefKind.Out
            && SymbolEqualityComparer.Default.Equals(method.Parameters[2].Type, containingType);
    }

    private static void ResolveFromTableConversionEmission(LuaTypeInfo info)
    {
        var tableMembers = info.Properties.Concat(info.Fields).ToList();
        var requiredMembers = tableMembers.Where(static member => member.IsRequired).ToList();
        var tableRequiredMemberNames = new HashSet<string>(requiredMembers.Select(static member => member.ClrName),
            StringComparer.Ordinal);

        var unavailableRequiredMembers = info.RequiredMembers
            .Where(member => !tableRequiredMemberNames.Contains(member.ClrName))
            .ToList();

        var optionalInitOnlyMembers = tableMembers
            .Where(static member => member is
            {
                IsInitOnly: true,
                IsRequired: false
            })
            .ToList();

        var missingParameterlessConstructor = !HasAccessibleParameterlessConstructor(info);
        var nonWritableRequiredMembers = requiredMembers
            .Where(static member => !member.CanTableWrite)
            .ToList();

        var canEmitGeneratedDefault = !missingParameterlessConstructor
            && unavailableRequiredMembers.Count == 0
            && nonWritableRequiredMembers.Count == 0
            && optionalInitOnlyMembers.Count == 0;

        if (info.ImplementsLuaUserDataFromTable)
        {
            info.EmitGeneratedFromTableDefault = canEmitGeneratedDefault;
            return;
        }

        if (info.FromTableConversion == FromTableConversionDisabled)
        {
            return;
        }

        if (unavailableRequiredMembers.Count > 0)
        {
            info.Diagnostics.Add(new LuaDiagnosticInfo
            {
                Id = "LAYLUA110",
                Message = $"{info.Name}|required member '{unavailableRequiredMembers[0].ClrName}' is not available for generated table conversion",
                Location = unavailableRequiredMembers[0].DeclarationLocation ?? info.FromTableConversionLocation ?? info.TypeLocation,
                Properties = ImmutableDictionary<string, string?>.Empty,
            });

            return;
        }

        if (optionalInitOnlyMembers.Count > 0)
        {
            info.Diagnostics.Add(new LuaDiagnosticInfo
            {
                Id = "LAYLUA110",
                Message = $"{info.Name}|optional init-only member '{optionalInitOnlyMembers[0].ClrName}' is not supported by generated table conversion",
                Location = optionalInitOnlyMembers[0].DeclarationLocation ?? info.FromTableConversionLocation ?? info.TypeLocation,
                Properties = ImmutableDictionary<string, string?>.Empty,
            });

            return;
        }

        if (canEmitGeneratedDefault)
        {
            info.EmitGeneratedFromTableConversion = true;
            info.EmitGeneratedFromTableDefault = true;
            return;
        }

        if (info.FromTableConversion != FromTableConversionEnabled)
        {
            return;
        }

        var reasons = new List<string>();
        if (missingParameterlessConstructor)
        {
            reasons.Add("the type has no accessible parameterless constructor");
        }

        foreach (var member in nonWritableRequiredMembers)
        {
            reasons.Add($"required member '{member.ClrName}' is not writable from table conversion");
        }

        info.Diagnostics.Add(new LuaDiagnosticInfo
        {
            Id = "LAYLUA110",
            Message = $"{info.Name}|{string.Join("; ", reasons)}",
            Location = info.FromTableConversionLocation ?? info.TypeLocation,
            Properties = ImmutableDictionary<string, string?>.Empty,
        });
    }

    private static Location? GetNamedArgumentLocation(AttributeData attribute, string name)
    {
        if (attribute.ApplicationSyntaxReference?.GetSyntax() is not AttributeSyntax attributeSyntax)
        {
            return null;
        }

        foreach (var argument in attributeSyntax.ArgumentList?.Arguments ?? default(SeparatedSyntaxList<AttributeArgumentSyntax>))
        {
            if (argument.NameEquals?.Name.Identifier.ValueText == name)
            {
                return argument.GetLocation();
            }
        }

        return attributeSyntax.GetLocation();
    }

    /// <summary>
    ///     Filters collected members based on resolved config (ObjectMembers, ObsoleteMembers).
    ///     Must be called after ResolveConfig so all values have been resolved to actual defaults.
    /// </summary>
    private static void FilterMembers(LuaTypeInfo info)
    {
        var includeObsolete = info.ObsoleteMembers == ObsoleteMembersInclude;
        var objectMembers = info.ObjectMembers;
        var members = info.Members;

        if ((members & MembersProperties) == 0)
        {
            info.Properties.Clear();
        }
        else
        {
            info.Properties.RemoveAll(m => !includeObsolete && m.IsObsolete);
        }

        if ((members & MembersStaticProperties) == 0)
        {
            info.StaticProperties.Clear();
        }
        else
        {
            info.StaticProperties.RemoveAll(m => !includeObsolete && m.IsObsolete);
        }

        if ((members & MembersFields) == 0)
        {
            info.Fields.Clear();
        }
        else
        {
            info.Fields.RemoveAll(m => !includeObsolete && m.IsObsolete);
        }

        if ((members & (MembersStaticFields | MembersConstants)) == 0)
        {
            info.StaticFields.Clear();
        }
        else
        {
            info.StaticFields.RemoveAll(member =>
                (!includeObsolete && member.IsObsolete)
                || (member.IsConstant
                    ? (members & MembersConstants) == 0
                    : (members & MembersStaticFields) == 0));
        }

        if ((members & MembersMethods) == 0)
        {
            info.Methods.Clear();
        }
        else
        {
            info.Methods.RemoveAll(m =>
                (!includeObsolete && m.IsObsolete)
                || (m.ObjectMethodFlag != 0 && (objectMembers & m.ObjectMethodFlag) == 0));
        }

        if ((members & MembersStaticMethods) == 0)
        {
            info.StaticMethods.Clear();
        }
        else
        {
            info.StaticMethods.RemoveAll(m =>
                (!includeObsolete && m.IsObsolete)
                || (m.ObjectMethodFlag != 0 && (objectMembers & m.ObjectMethodFlag) == 0));
        }

        if ((members & MembersIndexers) == 0)
        {
            info.Indexers.Clear();
        }

        if ((members & MembersConstructors) == 0)
        {
            info.Constructors.Clear();
        }

        if ((members & MembersEvents) == 0)
        {
            info.Events.Clear();
        }
        else
        {
            info.Events.RemoveAll(m => !includeObsolete && m.IsObsolete);
        }

        if ((members & MembersStaticEvents) == 0)
        {
            info.StaticEvents.Clear();
        }
        else
        {
            info.StaticEvents.RemoveAll(m => !includeObsolete && m.IsObsolete);
        }

        if (!info.EmitOperators)
        {
            info.Operators.Clear();
        }
        else
        {
            info.Operators.RemoveAll(op =>
                OperatorMetamethodsAndFlags.TryGetValue(op.MethodName, out var metamethodAndFlag)
                && (info.Features & metamethodAndFlag.Flag) == 0);
        }
    }

    private static void AssignGeneratedMethodIdentifiers(LuaTypeInfo info)
    {
        AssignGeneratedMethodIdentifiers(info.Methods);
        AssignGeneratedMethodIdentifiers(info.StaticMethods);
    }

    private static void AssignGeneratedMethodIdentifiers(List<LuaMethodInfo> methods)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var method in methods)
        {
            if (!counts.TryGetValue(method.ClrName, out var count))
            {
                counts[method.ClrName] = 1;
                method.GeneratedIdentifier = method.ClrName;
                continue;
            }

            count++;
            counts[method.ClrName] = count;
            method.GeneratedIdentifier = $"{method.ClrName}_{count}";
        }
    }

    private static void ValidateResolvedLuaNames(LuaTypeInfo info)
    {
        var instanceEntries = new List<(string LuaName, string Description)>();
        if ((info.IndexMembers & IndexMembersProperties) != 0)
        {
            instanceEntries.AddRange(info.Properties
                .Where(static member => member.CanRead || member.CanWrite)
                .Select(static member => (member.LuaName, $"property '{member.ClrName}'")));

            instanceEntries.AddRange(info.Fields
                .Where(static member => member.CanRead || member.CanWrite)
                .Select(static member => (member.LuaName, $"field '{member.ClrName}'")));
        }

        if ((info.IndexMembers & IndexMembersMethods) != 0)
        {
            instanceEntries.AddRange(info.Methods.Select(static method => (method.LuaName, $"method '{method.ClrName}'")));
        }

        if ((info.IndexMembers & IndexMembersEvents) != 0)
        {
            instanceEntries.AddRange(info.Events.Select(static eventInfo => (eventInfo.LuaName, $"event '{eventInfo.ClrName}'")));
        }

        AddLuaNameCollisionDiagnostics(info, instanceEntries, "instance members");

        var definitionEntries = new List<(string LuaName, string Description)>();
        definitionEntries.AddRange(info.StaticProperties
            .Where(static member => member.CanRead || member.CanWrite)
            .Select(static member => (member.LuaName, $"static property '{member.ClrName}'")));

        definitionEntries.AddRange(info.StaticFields
            .Where(static member => member.CanRead || member.CanWrite)
            .Select(static member => (member.LuaName, $"static field '{member.ClrName}'")));

        definitionEntries.AddRange(info.StaticMethods.Select(static method => (method.LuaName, $"static method '{method.ClrName}'")));
        definitionEntries.AddRange(info.StaticEvents.Select(static eventInfo => (eventInfo.LuaName, $"static event '{eventInfo.ClrName}'")));

        if (info.EmitFactory)
        {
            definitionEntries.Add((info.FactoryMethodName!, $"factory method '{info.FactoryMethodName}'"));
        }

        AddLuaNameCollisionDiagnostics(info, definitionEntries, "definition members");
    }

    private static void AddLuaNameCollisionDiagnostics(
        LuaTypeInfo info,
        List<(string LuaName, string Description)> entries,
        string scope)
    {
        foreach (var group in entries
            .Where(static entry => !string.IsNullOrEmpty(entry.LuaName))
            .GroupBy(static entry => entry.LuaName, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1))
        {
            info.Diagnostics.Add(new LuaDiagnosticInfo
            {
                Id = "LAYLUA109",
                Message = $"{info.Name}|{group.Key}|{scope}|{string.Join(", ", group.Select(static entry => entry.Description))}",
                Location = info.TypeLocation,
                Properties = ImmutableDictionary<string, string?>.Empty,
            });
        }
    }

    private static bool HasFatalDiagnostics(LuaTypeInfo info)
    {
        return info.Diagnostics.Any(static diagnostic => diagnostic.Id is "LAYLUA107" or "LAYLUA108" or "LAYLUA109" or "LAYLUA110");
    }

    private static void ReapplyNamingPolicy(LuaTypeInfo info)
    {
        var policy = info.NamingPolicy;

        foreach (var m in info.Properties)
        {
            RenameMember(m, policy);
        }

        foreach (var m in info.Fields)
        {
            RenameMember(m, policy);
        }

        foreach (var m in info.StaticProperties)
        {
            RenameMember(m, policy);
        }

        foreach (var m in info.StaticFields)
        {
            RenameMember(m, policy);
        }

        foreach (var m in info.Methods)
        {
            RenameMethod(m, policy);
        }

        foreach (var m in info.StaticMethods)
        {
            RenameMethod(m, policy);
        }

        foreach (var e in info.Events)
        {
            RenameEvent(e, policy);
        }

        foreach (var e in info.StaticEvents)
        {
            RenameEvent(e, policy);
        }

        return;

        static void RenameMember(LuaMemberInfo m, int policy)
        {
            if (!m.HasExplicitLuaName)
            {
                m.LuaName = ApplyNamingPolicy(m.ClrName, policy);
            }
        }

        static void RenameMethod(LuaMethodInfo m, int policy)
        {
            if (!m.HasExplicitLuaName)
            {
                m.LuaName = ApplyNamingPolicy(m.ClrName, policy);
            }

            if (m.Overloads != null)
            {
                foreach (var overload in m.Overloads)
                {
                    RenameMethod(overload, policy);
                }
            }
        }

        static void RenameEvent(LuaEventInfo e, int policy)
        {
            if (!e.HasExplicitLuaName)
            {
                e.LuaName = ApplyNamingPolicy(e.ClrName, policy);
            }
        }
    }

    private static string? FormatDefaultValue(IParameterSymbol parameter)
    {
        if (parameter.ExplicitDefaultValue == null)
        {
            return "default";
        }

        var value = parameter.ExplicitDefaultValue;
        var type = parameter.Type;
        var enumType = GetEnumType(type);
        if (enumType != null)
        {
            return $"({enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})({FormatEnumUnderlyingLiteral(value)})";
        }

        if (value is string stringValue)
        {
            var escaped = stringValue
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t")
                .Replace("\0", "\\0");

            return $"\"{escaped}\"";
        }

        if (value is bool boolValue)
        {
            return boolValue ? "true" : "false";
        }

        if (value is char charValue)
        {
            return $"'\\u{(int) charValue:X4}'";
        }

        if (value is float floatValue)
        {
            if (float.IsNaN(floatValue))
            {
                return "float.NaN";
            }

            if (float.IsPositiveInfinity(floatValue))
            {
                return "float.PositiveInfinity";
            }

            if (float.IsNegativeInfinity(floatValue))
            {
                return "float.NegativeInfinity";
            }

            return $"{floatValue.ToString(CultureInfo.InvariantCulture)}f";
        }

        if (value is double doubleValue)
        {
            if (double.IsNaN(doubleValue))
            {
                return "double.NaN";
            }

            if (double.IsPositiveInfinity(doubleValue))
            {
                return "double.PositiveInfinity";
            }

            if (double.IsNegativeInfinity(doubleValue))
            {
                return "double.NegativeInfinity";
            }

            return $"{doubleValue.ToString(CultureInfo.InvariantCulture)}d";
        }

        if (value is decimal decimalValue)
        {
            return $"{decimalValue.ToString(CultureInfo.InvariantCulture)}m";
        }

        if (value is long longValue)
        {
            return $"{longValue}L";
        }

        if (value is ulong ulongValue)
        {
            return $"{ulongValue}UL";
        }

        if (type.TypeKind == TypeKind.Enum)
        {
            return $"({type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})({value})";
        }

        return value.ToString();
    }

    private static INamedTypeSymbol? GetEnumType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType)
        {
            if (namedType.TypeKind == TypeKind.Enum)
            {
                return namedType;
            }

            if (namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                && namedType.TypeArguments[0] is INamedTypeSymbol nullableEnumType
                && nullableEnumType.TypeKind == TypeKind.Enum)
            {
                return nullableEnumType;
            }
        }

        return null;
    }

    private static string FormatEnumUnderlyingLiteral(object value)
    {
        return value switch
        {
            sbyte sbyteValue => sbyteValue.ToString(CultureInfo.InvariantCulture),
            byte byteValue => byteValue.ToString(CultureInfo.InvariantCulture),
            short shortValue => shortValue.ToString(CultureInfo.InvariantCulture),
            ushort ushortValue => ushortValue.ToString(CultureInfo.InvariantCulture),
            int intValue => intValue.ToString(CultureInfo.InvariantCulture),
            uint uintValue => $"{uintValue.ToString(CultureInfo.InvariantCulture)}U",
            long longValue => longValue == long.MinValue
                ? "long.MinValue"
                : $"{longValue.ToString(CultureInfo.InvariantCulture)}L",
            ulong ulongValue => ulongValue == ulong.MaxValue
                ? "ulong.MaxValue"
                : $"{ulongValue.ToString(CultureInfo.InvariantCulture)}UL",
            _ => value.ToString() ?? throw new InvalidOperationException("Enum default value formatting failed.")
        };
    }

    private static string? GetLuaName(ISymbol member)
    {
        var attribute = member.GetAttributes().FirstOrDefault(attribute =>
            attribute.AttributeClass?.ToDisplayString() == LuaNameAttributeName);

        if (attribute == null || attribute.ConstructorArguments.Length == 0)
        {
            return null;
        }

        return attribute.ConstructorArguments[0].Value as string;
    }

    /// <summary>
    ///     Finds the best property to use for <c>__len</c>.
    ///     Priority: "Count" property -> "Length" property.
    ///     Must be public, non-static, return an integer type.
    /// </summary>
    private static string? FindLengthProperty(INamedTypeSymbol symbol)
    {
        string? found = null;
        ITypeSymbol? current = symbol;
        while (current != null && current.SpecialType != SpecialType.System_Object)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is IPropertySymbol property
                    && !property.IsStatic
                    && property.GetMethod != null
                    && property.DeclaredAccessibility == Accessibility.Public
                    && property.Name is "Count" or "Length"
                    && ClassifyType(property.Type) == LuaValueKind.Integer)
                {
                    if (property.Name == "Count")
                    {
                        return "self.Count";
                    }

                    found ??= "self.Length";
                }
            }

            current = current.BaseType;
        }

        // Also check interfaces (e.g., ICollection<T>.Count, IReadOnlyCollection<T>.Count)
        if (found == null)
        {
            foreach (var @interface in symbol.AllInterfaces)
            {
                foreach (var member in @interface.GetMembers())
                {
                    if (member is IPropertySymbol property
                        && property.Name == "Count"
                        && ClassifyType(property.Type) == LuaValueKind.Integer)
                    {
                        var interfaceName = @interface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        return $"(({interfaceName}) self).Count";
                    }
                }
            }
        }

        return found;
    }

    private static string GenerateSource(LuaTypeInfo typeInfo)
    {
        var writer = new CodeWriter();

        writer.Code("""
            // <auto-generated />
            #nullable enable

            using System;
            using System.Diagnostics.CodeAnalysis;
            using System.Collections;
            using System.Reflection;
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices;
            using Laylua;
            using Laylua.Marshaling;
            using Laylua.Moon;
            using static Laylua.Moon.LuaNative;
            """);

        writer.Line();

        if (typeInfo.Namespace != null)
        {
            writer.Line($"namespace {typeInfo.Namespace};");
            writer.Line();
        }

        foreach (var (keyword, name) in typeInfo.ContainingTypes)
        {
            writer.Line($"partial {keyword} {name}");
            writer.OpenBrace();
        }

        var typeKeyword = typeInfo.IsValueType ? "struct" : "class";
        writer.Line($"unsafe partial {typeKeyword} {typeInfo.Name}");
        writer.OpenBrace();

        EmitPartialHookDeclarations(writer, typeInfo);
        writer.Line();
        writer.Line($"private const int __LayluaBindingConversions = {typeInfo.BindingConversions};");

        var tableMembers = typeInfo.Properties.Concat(typeInfo.Fields).ToList();
        if (typeInfo.EmitGeneratedFromTableDefault)
        {
            EmitOuterTryGetFromTable(writer, typeInfo, tableMembers);
            writer.Line();
        }

        writer.Code($"""
            /// <summary>
            ///     Source-generated Lua userdata handler for <see cref="{typeInfo.Name}"/>.
            /// </summary>
            public {(typeInfo.BaseHasLuaType ? "new " : "")}sealed unsafe class LuaUserDataHandler : ILuaUserDataHandler, ILuaUserDataHandler<{typeInfo.Name}>
            """);

        writer.OpenBrace();

        writer.Line("public static readonly LuaUserDataHandler Instance = new();");
        writer.Line();
        var metatableName = "Laylua:" + typeInfo.FullName;
        writer.Line($"private const string MetatableName = \"{metatableName}\";");
        if (typeInfo.HasDefinition)
        {
            writer.Line($"private const string DefinitionMetatableName = \"{metatableName}__definition\";");
        }

        writer.Code($"""
            private const int NamingPolicy = {typeInfo.NamingPolicy};
            private const int Members = {typeInfo.Members};
            private const int UnknownPropertyBehavior = {typeInfo.UnknownPropertyBehavior};
            private const int ObsoleteMembers = {typeInfo.ObsoleteMembers};
            private const int BindingConversions = {typeInfo.BindingConversions};
            private const int AwaitableReturnHandling = {typeInfo.AwaitableReturnHandling};
            """);

        writer.Line();

        EmitCallbackFields(writer, typeInfo);
        writer.Line();
        EmitEnsureInitialized(writer, typeInfo);
        writer.Line();
        EmitPush(writer, typeInfo);
        writer.Line();

        if (typeInfo.HasDefinition)
        {
            EmitPushDefinition(writer, typeInfo);
            writer.Line();
        }

        EmitTryGet(writer, typeInfo);

        writer.Line();
        EmitTryGetFromTable(writer, typeInfo);

        writer.Line();
        EmitIndexCallback(writer, typeInfo);
        writer.Line();
        EmitNewIndexCallback(writer, typeInfo);

        if (typeInfo.EmitToString)
        {
            writer.Line();
            EmitToStringCallback(writer, typeInfo);
        }

        if (typeInfo.HasDefinition)
        {
            writer.Line();
            EmitDefinitionToStringCallback(writer, typeInfo);
        }

        foreach (var method in typeInfo.Methods)
        {
            writer.Line();
            EmitMethodCallback(writer, typeInfo, method, isStatic: false);
        }

        if (typeInfo.TypeCallMethods.Count > 0)
        {
            writer.Line();
            EmitTypeCallCallback(writer, typeInfo);
        }

        var emittedOperatorGroups = typeInfo.Operators
            .GroupBy(op => op.MetamethodName)
            .ToList();

        foreach (var group in emittedOperatorGroups)
        {
            writer.Line();
            EmitOperatorCallback(writer, typeInfo, group.ToList());
        }

        writer.Line();
        EmitGcCallback(writer, typeInfo);

        if (typeInfo.ImplementsDisposable)
        {
            writer.Line();
            EmitCloseCallback(writer, typeInfo);
        }

        if (typeInfo.EmitPairs)
        {
            writer.Line();
            EmitPairsCallback(writer, typeInfo);
        }

        if (typeInfo.EmitPairs && typeInfo.PairsBehavior == PairsIfEnumerable && typeInfo.ImplementsEnumerable)
        {
            writer.Line();
            EmitPairsIteratorCallback(writer);
            writer.Line();
            EmitPairsEnumeratorGcCallback(writer);
        }

        if (typeInfo.EmitLength)
        {
            writer.Line();
            EmitLenCallback(writer, typeInfo);
        }

        if (typeInfo.HasDefinition)
        {
            writer.Line();
            EmitDefinitionGcCallback(writer, typeInfo);
            writer.Line();
            EmitDefinitionIndexCallback(writer, typeInfo);
            writer.Line();
            EmitDefinitionNewIndexCallback(writer, typeInfo);

            foreach (var method in typeInfo.StaticMethods)
            {
                writer.Line();
                EmitMethodCallback(writer, typeInfo, method, isStatic: true);
            }

            if (typeInfo.EmitDirectCall)
            {
                writer.Line();
                EmitConstructorCallback(writer, typeInfo, "__DefinitionCall", 1);
            }
            else if (typeInfo.EmitFactory)
            {
                writer.Line();
                EmitConstructorCallback(writer, typeInfo, "__DefinitionFactoryConstruct", 0);
            }
        }

        writer.Line();
        EmitInterfaceImplementation(writer, typeInfo);

        writer.CloseBrace(); // LuaUserDataHandler
        writer.CloseBrace(); // partial type

        foreach (var _ in typeInfo.ContainingTypes)
        {
            writer.CloseBrace();
        }

        writer.Line();
        EmitRegistration(writer, typeInfo);

        return writer.ToString();
    }
}
