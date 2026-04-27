using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Laylua.Analyzers;

internal static class LuaTypeAnalyzerHelpers
{
    private const string LuaTypeAttributeFullName = "Laylua.Marshaling.LuaTypeAttribute";

    public static bool HasLuaTypeAttribute(SyntaxNodeAnalysisContext context, TypeDeclarationSyntax typeDeclaration)
    {
        var luaTypeAttribute = context.SemanticModel.Compilation.GetTypeByMetadataName(LuaTypeAttributeFullName);
        if (luaTypeAttribute == null)
        {
            return false;
        }

        foreach (var attribute in typeDeclaration.AttributeLists.SelectMany(static list => list.Attributes))
        {
            var symbolInfo = context.SemanticModel.GetSymbolInfo(attribute, context.CancellationToken);
            if (IsLuaTypeAttribute(symbolInfo.Symbol, luaTypeAttribute))
            {
                return true;
            }

            foreach (var candidateSymbol in symbolInfo.CandidateSymbols)
            {
                if (IsLuaTypeAttribute(candidateSymbol, luaTypeAttribute))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool IsOpenGeneric(INamedTypeSymbol symbol)
    {
        if (symbol.TypeParameters.Length > 0)
        {
            return true;
        }

        var containingType = symbol.ContainingType;
        while (containingType != null)
        {
            if (containingType.TypeParameters.Length > 0)
            {
                return true;
            }

            containingType = containingType.ContainingType;
        }

        return false;
    }

    private static bool IsLuaTypeAttribute(ISymbol? symbol, INamedTypeSymbol luaTypeAttribute)
    {
        var attributeType = symbol switch
        {
            IMethodSymbol methodSymbol => methodSymbol.ContainingType,
            INamedTypeSymbol namedTypeSymbol => namedTypeSymbol,
            _ => null,
        };

        return attributeType != null
            && SymbolEqualityComparer.Default.Equals(attributeType, luaTypeAttribute);
    }
}
