using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Linq;

namespace SourceGenerators;

[Generator]
public class OnPropertyChangedMethodsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classes = context.SyntaxProvider
            .CreateSyntaxProvider(Predicate, Transform)
            .Where(type => type is not null)
            .Collect();

        context.RegisterSourceOutput(classes, GenerateCode!);
    }

    private static bool Predicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return syntaxNode is ClassDeclarationSyntax classDeclaration
            && classDeclaration.BaseList?.Types.Any() is true;
    }

    private static ITypeSymbol? Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
        if (classSymbol is null) return null;
        bool inheritsViewModelBase = classSymbol.Inherits(Constants.ViewModelBaseClassName);
        return inheritsViewModelBase && !classSymbol.IsAbstract ? classSymbol : null;
    }

    private static void GenerateCode(SourceProductionContext context, ImmutableArray<ITypeSymbol> classes)
    {
        foreach (var classSymbol in classes)
        {
            if (GenerateCodeForClass(classSymbol) is string code)
            {
                context.AddSource($"{Utils.GetFullName(classSymbol)}.g.cs", code);
            }
        }
    }

    private static string? GenerateCodeForClass(ITypeSymbol classSymbol)
    {
        var @namespace = Utils.GetNamespace(classSymbol);

        var propertyNames = classSymbol.GetMembers().OfType<IPropertySymbol>().Select(prop => prop.Name).ToList();

        if (propertyNames.Any())
        {
            var code = $$"""
                #nullable enable

                using System;
                using System.ComponentModel;
                
                {{(@namespace != null ? $"namespace {@namespace};" : "")}}

                partial class {{classSymbol.Name}} 
                {
                   protected override void __EnableOnPropertyChangedMethods()
                   {
                        PropertyChanged += (object? sender, PropertyChangedEventArgs e) =>
                        {
                            switch(e.PropertyName)
                            {
                                {{Utils.Indent(4, GenerateCaseStatements(propertyNames))}}
                            }
                        };
                    }

                    {{Utils.Indent(1, GenerateMethodDeclarations(propertyNames))}}
                }
                """;
            return code;
        }
        return null;
    }

    private static IEnumerable<string> GenerateCaseStatements(List<string> propertyNames)
    {
        return propertyNames.Select(propertyName => $"""
            case "{propertyName}":
                On{propertyName}Changed();
                break;
            """);
    }

    private static IEnumerable<string> GenerateMethodDeclarations(List<string> propertyNames)
    {
        return propertyNames.Select(propertyName =>
            $"partial void On{propertyName}Changed();");
    }
}
