using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Laylua.Analyzers;

/// <summary>
///     Provides a code fix for LAYLUA100:
///     adds <c>[LuaIgnore]</c> to methods with unsupported Lua boundary types.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddLuaIgnoreCodeFixProvider))]
[Shared]
public sealed class AddLuaIgnoreCodeFixProvider : CodeFixProvider
{
    private const string DiagnosticId = "LAYLUA100";

    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticId];

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            diagnostic.Properties.TryGetValue("methodSignature", out var methodSignature);
            if (!diagnostic.Properties.TryGetValue("methodName", out var methodName) || methodName == null)
            {
                // Fall back to parsing the message: first quoted token.
                var msg = diagnostic.GetMessage();
                var start = msg.IndexOf('\'');
                var end = start >= 0 ? msg.IndexOf('\'', start + 1) : -1;
                if (start < 0 || end < 0)
                {
                    continue;
                }

                methodSignature = msg.Substring(start + 1, end - start - 1);
                var parameterListIndex = methodSignature.IndexOf('(');
                methodName = parameterListIndex >= 0
                    ? methodSignature.Substring(0, parameterListIndex)
                    : methodSignature;

                var lastDotIndex = methodName.LastIndexOf('.');
                if (lastDotIndex >= 0)
                {
                    methodName = methodName.Substring(lastDotIndex + 1);
                }
            }

            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(diagnosticSpan);
            var methodDeclaration = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();

            if (methodDeclaration == null)
            {
                // Fall back to the containing type and match by method name for older diagnostics.
                var typeDecl = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
                if (typeDecl == null)
                {
                    continue;
                }

                methodDeclaration = typeDecl.Members
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(m => m.Identifier.Text == methodName);
            }

            if (methodDeclaration != null)
            {
                var displayName = methodSignature ?? methodName;
                context.RegisterCodeFix(
                    CodeAction.Create(
                        $"Add [LuaIgnore] to '{displayName}'",
                        ct => AddLuaIgnoreAttribute(context.Document, methodDeclaration, ct),
                        equivalenceKey: "AddLuaIgnore"),
                    diagnostic);
            }
        }
    }

    private static async Task<Document> AddLuaIgnoreAttribute(
        Document document,
        MethodDeclarationSyntax methodDecl,
        CancellationToken cancellationToken)
    {
        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var methodStart = methodDecl.Modifiers.Count > 0
            ? methodDecl.Modifiers[0].SpanStart
            : methodDecl.ReturnType.SpanStart;

        var methodLine = sourceText.Lines.GetLineFromPosition(methodStart);
        var insertionPoint = methodLine.Start;
        var indentation = sourceText.ToString(TextSpan.FromBounds(methodLine.Start, methodStart));
        var endOfLine = GetEndOfLineText(sourceText, methodLine);

        var newText = sourceText.WithChanges(new TextChange(
            new TextSpan(insertionPoint, 0),
            $"{indentation}[global::Laylua.Marshaling.LuaIgnore]{endOfLine}"));

        return document.WithText(newText);
    }

    private static string GetEndOfLineText(SourceText sourceText, TextLine line)
    {
        if (line.EndIncludingLineBreak > line.End)
        {
            return sourceText.ToString(TextSpan.FromBounds(line.End, line.EndIncludingLineBreak));
        }

        return "\r\n";
    }
}
