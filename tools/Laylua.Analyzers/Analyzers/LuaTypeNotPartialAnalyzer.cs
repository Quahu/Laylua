using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Laylua.Analyzers;

/// <summary>
///     Reports LAYLUA003 when <c>[LuaType]</c> is applied to a type that is not <c>partial</c>.
///     The source generator silently skips non-partial types, which can be confusing.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LuaTypeNotPartialAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "LAYLUA003";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "[LuaType] requires partial type",
        "Type '{0}' has [LuaType] but is not declared as partial",
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
        var typeDecl = (TypeDeclarationSyntax) context.Node;

        if (typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            return;
        }

        if (LuaTypeAnalyzerHelpers.HasLuaTypeAttribute(context, typeDecl))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                typeDecl.Identifier.GetLocation(),
                typeDecl.Identifier.Text));
        }
    }
}
