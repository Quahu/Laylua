using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Laylua.Analyzers;

/// <summary>
///     Reports LAYLUA005 when <c>[LuaType]</c> is applied to an open generic type.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LuaTypeOpenGenericAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "LAYLUA005";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "[LuaType] cannot be applied to open generic types",
        "Type '{0}' has [LuaType] but is open generic, which is not supported",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration,
            SyntaxKind.ClassDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration);
    }

    private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
    {
        var typeDeclaration = (TypeDeclarationSyntax) context.Node;
        if (!LuaTypeAnalyzerHelpers.HasLuaTypeAttribute(context, typeDeclaration))
        {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(typeDeclaration, context.CancellationToken) is not INamedTypeSymbol symbol)
        {
            return;
        }

        if (!LuaTypeAnalyzerHelpers.IsOpenGeneric(symbol))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            typeDeclaration.Identifier.GetLocation(),
            typeDeclaration.Identifier.Text));
    }
}
