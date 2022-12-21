using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace SourceGenerators;

internal static class Utils
{
    internal static List<INamedTypeSymbol> GetAllTypeSymbols(GeneratorExecutionContext context)
    {
        var types = new List<INamedTypeSymbol>();

        GetAllTypeSymbolsRecursive(context.Compilation.Assembly.GlobalNamespace, types);

        return types;
    }

    private static void GetAllTypeSymbolsRecursive(INamespaceSymbol namespaceSymbol, List<INamedTypeSymbol> types)
    {
        foreach (var namespaze in namespaceSymbol.GetNamespaceMembers())
        {
            types.AddRange(namespaze.GetTypeMembers());
            foreach (var type in namespaze.GetTypeMembers())
            {
                types.AddRange(type.GetTypeMembers());
            }
            GetAllTypeSymbolsRecursive(namespaze, types);
        }
    }


    internal static string GetFullName(ISymbol symbol)
    {
        var nss = new List<string>();
        INamespaceSymbol ns;

        if (symbol.ContainingType != null)
        {
            nss.Add(symbol.ContainingType.Name);
            ns = symbol.ContainingType.ContainingNamespace;
        }
        else
        {
            ns = symbol.ContainingNamespace;
        }

        while (ns != null)
        {
            if (string.IsNullOrWhiteSpace(ns.Name))
            {
                break;
            }
            nss.Add(ns.Name);
            ns = ns.ContainingNamespace;
        }
        nss.Reverse();
        if (nss.Any())
        {
            return $"{string.Join(".", nss)}.{symbol.Name}";
        }
        return symbol.Name;
    }

    public static void ReportDiag(this SourceProductionContext context, string id, string message, Location? location = null) 
    {
        context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(id, message, message, "", DiagnosticSeverity.Warning, true), location));
    }

}
