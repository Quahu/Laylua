using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Laylua.Analyzers;

/// <summary>
///     Reports LAYLUA001 when an invocation returns <c>LuaFunctionResults</c>
///     and the result is not disposed.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UndisposedCallResultAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "LAYLUA001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Undisposed LuaFunctionResults value",
        "The LuaFunctionResults returned by '{0}' should be disposed",
        "Reliability",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private const string LuaFunctionResultsTypeName = "Laylua.LuaFunctionResults";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation) context.Operation;
        if (invocation.Type == null)
        {
            return;
        }

        if (invocation.Type.ToDisplayString() != LuaFunctionResultsTypeName)
        {
            return;
        }

        var parent = invocation.Parent;
        if (parent is IExpressionStatementOperation)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                invocation.Syntax.GetLocation(),
                GetInvocationDisplayName(invocation)));

            return;
        }

        if (parent is IVariableInitializerOperation variableInit)
        {
            var declarator = variableInit.Parent;
            if (declarator?.Syntax is VariableDeclaratorSyntax varDeclarator)
            {
                var localDecl = varDeclarator.Parent?.Parent as LocalDeclarationStatementSyntax;
                if (localDecl != null && localDecl.UsingKeyword == default)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rule,
                        invocation.Syntax.GetLocation(),
                        GetInvocationDisplayName(invocation)));
                }
            }
        }

        if (parent is IReturnOperation)
        {
            if (context.ContainingSymbol is IMethodSymbol method
                && method.ReturnType.ToDisplayString() == LuaFunctionResultsTypeName)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                invocation.Syntax.GetLocation(),
                GetInvocationDisplayName(invocation)));

            return;
        }

        if (parent is IArgumentOperation)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                invocation.Syntax.GetLocation(),
                GetInvocationDisplayName(invocation)));
        }
    }

    private static string GetInvocationDisplayName(IInvocationOperation invocation)
    {
        return $"{invocation.TargetMethod.ContainingType.Name}.{invocation.TargetMethod.Name}()";
    }
}
