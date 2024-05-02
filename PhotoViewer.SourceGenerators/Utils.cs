using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace SourceGenerators;

internal static class Utils
{
    public static string GetFullName(ISymbol symbol)
    {
        var symbolDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
        return symbol.ToDisplayString(symbolDisplayFormat);
    }

    public static string? GetNamespace(ISymbol symbol)
    {
        if (symbol.ContainingNamespace is { } containingNamespace
            && !containingNamespace.IsGlobalNamespace)
        {
            if (GetNamespace(containingNamespace) is string @namespace)
            {
                return @namespace + "." + containingNamespace.Name;
            }
            return containingNamespace.Name;
        }
        return null;
    }

    public static AttributeData? GetAttribute(this ISymbol symbol, string attributeName)
    {
        return symbol.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.Name == attributeName);
    }

    public static KeyValuePair<string, TypedConstant>? GetArgument(this AttributeData attribute, string argumentName)
    {
        return attribute.NamedArguments.FirstOrDefault(argument => argument.Key == argumentName);
    }

    public static INamedTypeSymbol? GetInterface(this INamedTypeSymbol namedTypeSymbol, string interfaceName)
    {
        return namedTypeSymbol.AllInterfaces.FirstOrDefault(interfaceSymbol => interfaceSymbol.Name == interfaceName);
    }

    public static bool Inherits(this INamedTypeSymbol namedTypeSymbol, string typeName)
    {
        var baseType = namedTypeSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.Name == typeName)
            {
                return true;
            }
            baseType = baseType.BaseType;
        }
        return false;
    }

    public static string Indent(int indent, IEnumerable<string> text)
    {
        return IndentLines(indent, text.SelectMany(part => part.Split('\n')));
    }

    public static string Indent(int indent, string text)
    {
        return IndentLines(indent, text.Split('\n'));
    }

    private static string IndentLines(int indent, IEnumerable<string> lines)
    {
        string indentString = new string('\t', indent);
        return string.Join("\n" + indentString, lines);
    }

    public static void ReportError(this SourceProductionContext context, string id, string message, Location? location = null)
    {
        Report(context, id, message, DiagnosticSeverity.Error, location);
    }

    public static void ReportWarning(this SourceProductionContext context, string id, string message, Location? location = null)
    {
        Report(context, id, message, DiagnosticSeverity.Warning, location);
    }

    private static void Report(this SourceProductionContext context, string id, string message, DiagnosticSeverity diagnosticSeverity, Location? location = null)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(id, message, message, "", diagnosticSeverity, true), location));
    }

}
