using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Laylua.Analyzers;

/// <summary>
///     Reports LAYLUA002 when an invocation returning <c>LuaFunctionResults</c>
///     occurs inside a loop without disposing the result.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UndisposedCallResultInLoopAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "LAYLUA002";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Undisposed LuaFunctionResults value in loop",
        "The LuaFunctionResults returned by '{0}' inside a loop should be disposed",
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
        if (invocation.Type == null || invocation.Type.ToDisplayString() != LuaFunctionResultsTypeName)
        {
            return;
        }

        if (!IsInsideLoop(invocation.Syntax))
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

    private static bool IsInsideLoop(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is ForStatementSyntax
                or ForEachStatementSyntax
                or WhileStatementSyntax
                or DoStatementSyntax)
            {
                return true;
            }

            if (current is MethodDeclarationSyntax
                or LocalFunctionStatementSyntax
                or LambdaExpressionSyntax)
            {
                return false;
            }

            current = current.Parent;
        }

        return false;
    }

    private static string GetInvocationDisplayName(IInvocationOperation invocation)
    {
        return $"{invocation.TargetMethod.ContainingType.Name}.{invocation.TargetMethod.Name}()";
    }
}
