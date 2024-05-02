using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace SourceGenerators;

[Generator]
public class NotifyCanExecuteChangedGenerator : IIncrementalGenerator
{
    private record struct RelayCommandInfo(string CommandName, string CanExecutePropertyName);

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
        foreach (var classSymbol in classes.OfType<ITypeSymbol>())
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

        List<RelayCommandInfo> list = new();

        foreach (var method in classSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            if (method.GetAttribute("RelayCommandAttribute") is { } attribute
                && attribute.GetArgument("CanExecute") is { } canExecuteArgument)
            {
                if (canExecuteArgument.Value.Value is string propertyName)
                {
                    string commandName = method.Name.Replace("Async", "") + "Command";
                    list.Add(new RelayCommandInfo(commandName, propertyName));
                }
            }
        }

        if (list.Any())
        {
            var code = $$"""
                #nullable enable

                using System;
                using System.ComponentModel;
                
                {{(@namespace != null ? $"namespace {@namespace};" : "")}}

                partial class {{classSymbol.Name}} 
                {
                    protected override void _NotifyCanExecuteChanged(string? propertyName)
                    {
                        switch(propertyName)
                        {
                            {{Utils.Indent(3, GenerateCaseStatements(list))}}
                            default:
                                break;
                        }                        
                    }
                }
                """;
            return code;
        }
        return null;
    }

    private static IEnumerable<string> GenerateCaseStatements(List<RelayCommandInfo> commands)
    {
        return commands.GroupBy(commandInfo => commandInfo.CanExecutePropertyName).Select(group =>
        {
            string propertyName = group.Key;
            return $"""
                case "{propertyName}": 
                    {Utils.Indent(1, group.Select(commandInfo => commandInfo.CommandName + ".NotifyCanExecuteChanged();"))}
                    break;
                """;
        });
    }
}


