using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Laylua.Analyzers;

/// <summary>
///     Reports LAYLUA004 when <c>[LuaType]</c> is applied to a ref struct.
///     Ref struct types cannot cross the Lua boundary.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LuaTypeRefStructAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "LAYLUA004";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "[LuaType] cannot be applied to ref struct types",
        "Type '{0}' has [LuaType] but is a ref struct, which cannot cross the Lua boundary",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration,
            SyntaxKind.StructDeclaration,
            SyntaxKind.RecordStructDeclaration);
    }

    private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
    {
        var typeDecl = (TypeDeclarationSyntax) context.Node;

        if (!typeDecl.Modifiers.Any(SyntaxKind.RefKeyword))
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
