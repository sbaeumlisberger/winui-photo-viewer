using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace SourceGenerators;

[Generator]
public class IMVVMControlGenerator : IIncrementalGenerator
{

    private static readonly string InterfaceName = "IMVVMControl";

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
        return classSymbol?.GetInterface(InterfaceName) != null ? classSymbol : null;
    }

    public void GenerateCode(SourceProductionContext context, ImmutableArray<ITypeSymbol> classes)
    {
        foreach (var classSymbol in classes.Distinct(SymbolEqualityComparer.Default).OfType<ITypeSymbol>())
        {
            string code = GenerateCodeForClass(classSymbol);
            context.AddSource($"{Utils.GetFullName(classSymbol)}.g.cs", code);
        }
    }

    private static string GenerateCodeForClass(ITypeSymbol classSymbol)
    {
        var @namespace = Utils.GetNamespace(classSymbol);

        var interfaceSymbol = classSymbol.Interfaces.First(i => i.Name == InterfaceName);
        string viewModelType = Utils.GetFullName(interfaceSymbol.TypeArguments.First());


        var code = $$"""
                #nullable enable

                using System;
                using Microsoft.UI.Xaml;
                using PhotoViewer.App.Utils;

                {{(@namespace != null ? $"namespace {@namespace};" : "")}}

                partial class {{classSymbol.Name}} 
                {
                    void IMVVMControl<{{viewModelType}}>.InitializeComponent() => InitializeComponent();
                    void IMVVMControl<{{viewModelType}}>.UpdateBindings() => Bindings.Update();
                    void IMVVMControl<{{viewModelType}}>.StopBindings() => Bindings.StopTracking();

                    partial void ConnectToViewModel({{viewModelType}} viewModel);     
                    partial void DisconnectFromViewModel({{viewModelType}} viewModel);    

                    void IMVVMControl<{{viewModelType}}>.ConnectToViewModel({{viewModelType}} viewModel) => ConnectToViewModel(viewModel);     
                    void IMVVMControl<{{viewModelType}}>.DisconnectFromViewModel({{viewModelType}} viewModel) => DisconnectFromViewModel(viewModel);   
                }
            """;
        return code;
    }

}

