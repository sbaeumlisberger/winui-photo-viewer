using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace SourceGenerators;

[Generator]
public class NotifyComputedPropertyChangedGenerator : IIncrementalGenerator
{
    private record PropertyInfo(IPropertySymbol PropertySymbol, List<string> DependsOnPropertyNames);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var properties = context.SyntaxProvider
            .CreateSyntaxProvider(Predicate, Transform)
            .Where(propertyInfo => propertyInfo is not null)
            .Collect();

        context.RegisterSourceOutput(properties, GenerateCode!);
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
        // The node must be a property declaration with a expression body (e.g. "string MyProperty => SomeOtherProperty;")
        if (property.ExpressionBody is null)
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

    private static PropertyInfo? Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
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

        if (!classSymbol.Inherits("ObservableObjectBase"))
        {
            return null;
        }

        var dependsOnPropertyNames = GetDependsOnPropertyNames(context, propertyDeclaration, classSymbol);

        if (dependsOnPropertyNames.Count == 0)
        {
            return null;
        }

        var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclaration)!;

        return new PropertyInfo(propertySymbol, dependsOnPropertyNames);
    }

    private static List<string> GetDependsOnPropertyNames(GeneratorSyntaxContext context, PropertyDeclarationSyntax propertyDeclarationSyntax, ITypeSymbol classSymbol)
    {
        return GetChildsRecursive(propertyDeclarationSyntax.ExpressionBody!.Expression)
             .Where(child => child is IdentifierNameSyntax identifierNameSyntax)
             .Select(identifierNameSyntax => context.SemanticModel.GetSymbolInfo(identifierNameSyntax).Symbol)
             .OfType<IPropertySymbol>()
             .Where(propertySymbol => SymbolEqualityComparer.Default.Equals(propertySymbol.ContainingSymbol, classSymbol))
             .Select(propertySymbol => propertySymbol.Name)
             .ToList();
    }

    private static IEnumerable<SyntaxNode> GetChildsRecursive(SyntaxNode syntaxNode)
    {
        var childs = syntaxNode.ChildNodes();
        return childs.Concat(childs.SelectMany(GetChildsRecursive));
    }

    private static void GenerateCode(SourceProductionContext context, ImmutableArray<PropertyInfo> properties)
    {
        foreach (var group in properties
            .GroupBy(propertyInfo => propertyInfo.PropertySymbol.ContainingSymbol, SymbolEqualityComparer.Default))
        {
            ITypeSymbol classSymbol = (ITypeSymbol)group.Key;
            IEnumerable<PropertyInfo> propertyInfos = group;
            string sourceCode = GenerateCodeForClass(classSymbol, propertyInfos);
            context.AddSource($"{Utils.GetFullName(classSymbol)}.NotifyComputedPropertyChanged.g.cs", sourceCode);
        }
    }

    private static string GenerateCodeForClass(ITypeSymbol classSymbol, IEnumerable<PropertyInfo> propertyInfos)
    {
        return $$"""
            #nullable enable

            using System;
            using System.ComponentModel;

            {{Utils.GetNamespaceDeclaration(classSymbol)}}

            {{Utils.GeneratePartialClass(classSymbol, GenerateCodeForProperties(propertyInfos))}}  
            """;
    }

    private static string GenerateCodeForProperties(IEnumerable<PropertyInfo> propertyInfos)
    {
        return $$"""
            protected override void _NotifyComputedPropertyChanged(string ? propertyName)
            {
                {{Utils.Indent(2, propertyInfos.Select(GeneratePropertyChangedCheck))}}
            }            
            
            {{Utils.Indent(1, propertyInfos.Select(GenerateOnPropertyChangedMethod))}} 
            """;
    }

    private static string GeneratePropertyChangedCheck(PropertyInfo propertyInfo)
    {
        string propertyName = propertyInfo.PropertySymbol.Name;
        string propertyType = Utils.ToFullyQualifiedTypeString(propertyInfo.PropertySymbol.Type);

        var propertyNameEqualChecks = propertyInfo.DependsOnPropertyNames.Select(propertyName => $"propertyName == \"{propertyName}\"");

        return $$"""
            if(propertyName is null || {{string.Join(" || ", propertyNameEqualChecks)}}) 
            {
                OnPropertyChanged(new PropertyChangedEventArgs("{{propertyName}}"));
                On{{propertyName}}Changed();
            }
            """;
    }

    private static string GenerateOnPropertyChangedMethod(PropertyInfo propertyInfo)
    {
        return $"partial void On{propertyInfo.PropertySymbol.Name}Changed();";
    }
}
