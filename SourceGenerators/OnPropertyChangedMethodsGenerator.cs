using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Text;
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

        context.RegisterSourceOutput(classes, GenerateCode);
    }

    private static bool Predicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return syntaxNode is ClassDeclarationSyntax;
    }

    private static ITypeSymbol? Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var type = context.SemanticModel.GetDeclaredSymbol(classDeclaration) as ITypeSymbol;
        return type?.BaseType?.Name == "ViewModelBase" ? type : null;
    }

    private static void GenerateCode(SourceProductionContext context, ImmutableArray<ITypeSymbol?> classes)
    {
        foreach (var @class in classes)
        {
            if (@class != null && GenerateCodeForClass(@class) is string code)
            {
                var @namespace = @class.ContainingNamespace.IsGlobalNamespace ? null : @class.ContainingNamespace + ".";
                context.AddSource($"{@namespace}{@class.Name}.g.cs", code);
            }
        }
    }

    private static string? GenerateCodeForClass(ITypeSymbol @class)
    {
        var @namespace = @class.ContainingNamespace.IsGlobalNamespace ? null : @class.ContainingNamespace;

        var propertyNames = @class.GetMembers().OfType<IPropertySymbol>().Select(prop => prop.Name).ToList();

        if (propertyNames.Any())
        {
            var code = $$"""
                using System;
                using System.ComponentModel;
                
                {{(@namespace != null ? $"namespace {@namespace};" : "")}}

                partial class {{@class.Name}} 
                {
                   protected override void __EnableOnPropertyChangedMethods()
                   {
                        PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
                        {
                            switch(e.PropertyName)
                            {
                                {{GenerateCaseStatements(propertyNames)}}
                            }
                        };
                    }

                    {{GenerateMethodDeclarations(propertyNames)}}
                }
                """;
            return code;
        }
        return null;
    }

    private static string GenerateCaseStatements(List<string> propertyNames)
    {
        var stringBuilder = new StringBuilder();
        foreach (string propertyName in propertyNames)
        {
            stringBuilder.AppendLine($"""case "{propertyName}": On{propertyName}Changed(); break;""");
        }
        return stringBuilder.ToString();
    }

    private static string GenerateMethodDeclarations(List<string> propertyNames)
    {
        var stringBuilder = new StringBuilder();
        foreach (string propertyName in propertyNames)
        {
            stringBuilder.AppendLine($"partial void On{propertyName}Changed();");
        }
        return stringBuilder.ToString();
    }
}
