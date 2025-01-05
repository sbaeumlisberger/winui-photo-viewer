using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

namespace SourceGenerators;

[Generator]
public class NotifyPropertyChangedGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classes = context.SyntaxProvider
            .CreateSyntaxProvider(Predicate, Transform)
            .Where(symbol => symbol is not null)
            .Collect();

        context.RegisterSourceOutput(classes, GenerateCode!);
    }

    private static bool Predicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return syntaxNode is PropertyDeclarationSyntax propertyDeclaration
            && IsValidCandidateProperty(propertyDeclaration);
    }

    /// <summary>
    /// Checks whether a given property declaration has valid syntax.
    /// </summary>
    private static bool IsValidCandidateProperty(PropertyDeclarationSyntax property)
    {
        // The node must be a property declaration with two accessors
        if (property.AccessorList?.Accessors is not { Count: 2 } accessors)
        {
            return false;
        }

        // The accessors must be a get and a set (with any accessibility)
        if (accessors[0].Kind() is not (SyntaxKind.GetAccessorDeclaration or SyntaxKind.SetAccessorDeclaration) ||
            accessors[1].Kind() is not (SyntaxKind.GetAccessorDeclaration or SyntaxKind.SetAccessorDeclaration))
        {
            return false;
        }

        // The property must be partial
        if (!property.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            return false;
        }

        // Static properties are not supported
        if (property.Modifiers.Any(SyntaxKind.StaticKeyword))
        {
            return false;
        }

        return true;
    }

    private static IPropertySymbol? Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;

        if (propertyDeclaration.Parent is not ClassDeclarationSyntax classDeclarationSyntax)
        {
            return null;
        }

        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

        if (classSymbol is null)
        {
            return null;
        }

        bool implementsINotifyPropertyChanged = classSymbol.AllInterfaces
            .Any(interfaceSymbol => interfaceSymbol.Name == "INotifyPropertyChanged");

        if (!implementsINotifyPropertyChanged)
        {
            return null;
        }

        return context.SemanticModel.GetDeclaredSymbol(propertyDeclaration);
    }

    private static void GenerateCode(SourceProductionContext context, ImmutableArray<IPropertySymbol> properties)
    {
        foreach (var group in properties.GroupBy(property => property.ContainingSymbol, SymbolEqualityComparer.Default))
        {
            ITypeSymbol classSymbol = (ITypeSymbol)group.Key;
            IEnumerable<IPropertySymbol> propertySymbols = group;
            string sourceCode = GenerateCodeForClass(classSymbol, propertySymbols);
            context.AddSource($"{Utils.GetFullName(classSymbol)}.NotifyPropertyChanged.g.cs", sourceCode);
        }
    }

    private static string GenerateCodeForClass(ITypeSymbol classSymbol, IEnumerable<IPropertySymbol> propertySymbols)
    {
        return $$"""
            #nullable enable

            using System;
            using System.ComponentModel;

            {{Utils.GetNamespaceDeclaration(classSymbol)}}

            {{Utils.GeneratePartialClass(classSymbol, propertySymbols.Select(GenerateCodeForProperty))}}
            """;
    }

    private static string GenerateCodeForProperty(IPropertySymbol propertySymbol)
    {
        string propertyName = propertySymbol.Name;
        string propertyType = Utils.ToFullyQualifiedTypeString(propertySymbol.Type);

        string propertyAccessibility = ToKeyword(propertySymbol.DeclaredAccessibility) + " ";
        string getterAccessibility = ToKeyword(propertySymbol.GetMethod!.DeclaredAccessibility) + " ";
        string setterAccessibility = ToKeyword(propertySymbol.SetMethod!.DeclaredAccessibility) + " ";

        if (getterAccessibility == propertyAccessibility)
        {
            getterAccessibility = "";
        }

        if (setterAccessibility == propertyAccessibility)
        {
            setterAccessibility = "";
        }

        string required = propertySymbol.IsRequired ? "required " : "";

        return $$"""
            {{propertyAccessibility}}{{required}}partial {{propertyType}} {{propertyName}}
            {
                {{getterAccessibility}}get => field;
                {{setterAccessibility}}set
                {
                    if (!EqualityComparer<{{propertyType}}>.Default.Equals(value, field))
                    {
                        var oldValue = field;
                        field = value;
                        OnPropertyChanged(new PropertyChangedEventArgs("{{propertyName}}"));
                        On{{propertyName}}Changed();
                        On{{propertyName}}Changed(oldValue, value);
                    }
                }
            }

            partial void On{{propertyName}}Changed();

            partial void On{{propertyName}}Changed({{propertyType}} oldValue, {{propertyType}} newValue);

            """;
    }

    private static string ToKeyword(Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.Private => "private",
            Accessibility.ProtectedAndInternal => "protected internal",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedOrInternal => "private protected",
            Accessibility.Public => "public",
            _ => ""
        };
    }
}
