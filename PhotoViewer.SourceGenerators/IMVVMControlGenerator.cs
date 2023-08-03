﻿using Microsoft.CodeAnalysis;
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
                using PhotoViewer.App.Utils.Logging;

                {{(@namespace != null ? $"namespace {@namespace};" : "")}}

                partial class {{classSymbol.Name}} 
                {
                    private {{viewModelType}}? ViewModel => _viewModel;
            
                    private {{viewModelType}}? _viewModel;

                    private void InitializeComponentMVVM()
                    {
                        Unloaded += (s, e) =>
                        {
                            if (_viewModel is {{viewModelType}} currentViewModel)
                            {
                                disconnect();
                            }
                        };

                        DataContextChanged += (s, e) =>
                        {
                            if (e.NewValue != _viewModel)
                            {
                                if (_viewModel is {{viewModelType}} currentViewModel)
                                {
                                    disconnect();
                                }
                                if (e.NewValue is {{viewModelType}} newViewModel)
                                {
                                    connect(newViewModel);
                                }
                            }
                        };

                        if(DataContext is {{viewModelType}} newViewModel) 
                        {
                            connect(newViewModel);
                        }

                        InitializeComponent();

                        void connect({{viewModelType}} newViewModel)
                        {                  
                            //Log.Debug($"Connect {this} to {newViewModel}");
                            _viewModel = newViewModel;
                            ConnectToViewModel(_viewModel);     
                            if (IsLoaded)
                            {
                                Bindings.Update();
                            }                     
                        }
            
                        void disconnect()
                        { 
                            //Log.Debug($"Disconnect {this} from {_viewModel}");
                            DisconnectFromViewModel(_viewModel);  
                            Bindings.StopTracking();                        
                            _viewModel = null;
                            DataContext = null;
                        }
                    }                                   

                    partial void ConnectToViewModel({{viewModelType}} viewModel);     
                    partial void DisconnectFromViewModel({{viewModelType}} viewModel);    
                }
            """;
        return code;
    }

}

