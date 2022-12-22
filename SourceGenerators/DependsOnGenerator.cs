using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;

namespace SourceGenerators;

[Generator]
public class DependsOnGenerator : IIncrementalGenerator
{
    private record struct Dependency(string PropertyName, string DependsOnPropertyName);

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
        return classSymbol?.BaseType?.Name == Constants.ViewModelBaseClassName ? classSymbol : null;
    }

    public void GenerateCode(SourceProductionContext context, ImmutableArray<ITypeSymbol> classes)
    {
        foreach (var classSymbol in classes.Distinct(SymbolEqualityComparer.Default).OfType<ITypeSymbol>())
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

        var dependencies = classSymbol.GetMembers().OfType<IPropertySymbol>()
              .Select(property => (property, attribute: property.GetAttribute("DependsOnAttribute")))
              .Where(tuple => tuple.attribute != null)
              .SelectMany(tuple => tuple.attribute!.ConstructorArguments
                  .Select(argument => new Dependency(tuple.property.Name, (string)argument.Value!)))
              .ToList();

        if (dependencies.Any())
        {
            var code = $$"""
                #nullable enable

                using System;
                using System.ComponentModel;

                {{(@namespace != null ? $"namespace {@namespace};" : "")}}

                partial class {{classSymbol.Name}} 
                {
                    protected override void __EnableDependsOn()
                    {
                        PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
                        {
                            switch(e.PropertyName)
                            {
                                {{Utils.Indent(4, GenerateCaseStatements(dependencies))}}
                            }
                        };
                    }
                }
                """;
            return code;
        }
        return null;
    }

    private static IEnumerable<string> GenerateCaseStatements(List<Dependency> dependencies)
    {
        return dependencies.GroupBy(dependency => dependency.DependsOnPropertyName).Select(group => $"""
            case "{group.Key}":
                {Utils.Indent(1, GenerateOnPropertyChangedInvocations(group))}                
                break;
            """);
    }

    private static IEnumerable<string> GenerateOnPropertyChangedInvocations(IEnumerable<Dependency> dependencies) 
    {
        return dependencies.Select(dependency =>
            $"OnPropertyChanged(new PropertyChangedEventArgs(nameof({dependency.PropertyName})));");
    }

}

