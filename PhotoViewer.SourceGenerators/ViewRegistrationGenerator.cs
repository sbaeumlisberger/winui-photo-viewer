using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace SourceGenerators;

[Generator]
public class ViewRegistrationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classesProvider = context.SyntaxProvider
            .CreateSyntaxProvider(Predicate, Transform)
            .Where(type => type is not null)
            .Collect();

        context.RegisterSourceOutput(classesProvider, GenerateCode!);
    }

    private static bool Predicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return syntaxNode is ClassDeclarationSyntax classDeclaration
            && classDeclaration.AttributeLists.Any();
    }

    private static INamedTypeSymbol? Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
        return classSymbol?.GetAttribute("ViewRegistrationAttribute") is { } ? classSymbol : null;
    }

    private static void GenerateCode(SourceProductionContext context, ImmutableArray<INamedTypeSymbol> classes)
    {
        string source = $$"""
            #nullable enable

            using System;
            using System.Collections.Generic;
       
            internal class ViewRegistrations
            {
                public static ViewRegistrations Instance { get; } = new ViewRegistrations();

                private static IReadOnlyDictionary<Type, Func<object>> ViewFactoriesByViewModelType { get; } = new Dictionary<Type, Func<object>>()
                {
                    {{Utils.Indent(2, CreateViewFactoriesByViewModelTypeDictionaryEntries(context, classes))}}
                };
            
                private static IReadOnlyDictionary<Type, Type> ViewTypeByViewModelType { get; } = new Dictionary<Type, Type>()
                {
                    {{Utils.Indent(2, CreateViewTypeByViewModelTypeDictionaryEntries(classes))}}
                };

                public object CreateViewForViewModelType(Type viewModelType) 
                {
                    if(ViewFactoriesByViewModelType.TryGetValue(viewModelType, out var factory))
                    {
                        return factory.Invoke();
                    }
                    throw new Exception($"No view found for model type {viewModelType.FullName}");           
                }

                public Type? GetViewTypeForViewModelType(Type viewModelType) 
                {
                    if(ViewTypeByViewModelType.TryGetValue(viewModelType, out var viewType))
                    {
                        return viewType;
                    }
                    throw new Exception($"No view found for model type {viewModelType.FullName}");   
                }
            }
            """;

        context.AddSource("ViewRegistrations.g.cs", source);
    }

    private static IEnumerable<string> CreateViewFactoriesByViewModelTypeDictionaryEntries(
        SourceProductionContext context, ImmutableArray<INamedTypeSymbol> classes)
    {
        foreach (var classSymbol in classes)
        {
            var attribute = classSymbol.GetAttribute("ViewRegistrationAttribute")!;
            var viewModelTypeArg = attribute.ConstructorArguments.FirstOrDefault();
            var constructor = classSymbol.InstanceConstructors[0];
            string className = Utils.GetFullName(classSymbol);

            if (constructor.Parameters.Length != 0)
            {
                context.ReportError("PVSG", "Constructor must not have any arguments.", constructor.Locations[0]);
            }

            yield return $$"""{ typeof({{viewModelTypeArg.Value}}), () => new {{className}}() },""";
        }
    }

    private static IEnumerable<string> CreateViewTypeByViewModelTypeDictionaryEntries(ImmutableArray<INamedTypeSymbol> classes)
    {
        foreach (var classSymbol in classes)
        {
            var attribute = classSymbol.GetAttribute("ViewRegistrationAttribute")!;
            var viewModelTypeArg = attribute.ConstructorArguments.FirstOrDefault();
            string className = Utils.GetFullName(classSymbol);
            yield return $$"""{ typeof({{viewModelTypeArg.Value}}), typeof({{className}}) },""";
        }
    }
}

