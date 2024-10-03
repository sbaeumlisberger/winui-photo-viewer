using Microsoft.CodeAnalysis;
using SourceGenerators;
using System;
using System.Collections.Immutable;

namespace PhotoViewer.SourceGenerators;

[Generator]
public class CompileTimeConstantsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classes = context.SyntaxProvider
          .CreateSyntaxProvider<ITypeSymbol?>((_, _) => false, (_, _) => null)
          .Collect();

        context.RegisterSourceOutput(classes, GenerateCode);
    }

    private static void GenerateCode(SourceProductionContext context, ImmutableArray<ITypeSymbol?> _)
    {
        string source = $$"""
           internal static class CompileTimeConstants 
           {
               public static string GMailPassword { get; } = "{{GetEnvironmentVariable(context, "PhotoViewerEMailPassword")}}";
               public static string HereApiKey { get; } = "{{GetEnvironmentVariable(context, "PhotoViewerHereApiKey")}}";
           }
           """;

        context.AddSource("CompileTimeConstants.g.cs", source);
    }

    private static string GetEnvironmentVariable(SourceProductionContext context, string name)
    {
#pragma warning disable RS1035 // Do not use APIs banned for analyzers
        string? value = Environment.GetEnvironmentVariable(name);
#pragma warning restore RS1035 // Do not use APIs banned for analyzers

        if (string.IsNullOrEmpty(value))
        {
            context.ReportWarning(Constants.WarningEnvironmentNotSet, $"Environment variable {name} not set");
        }
        return value ?? "";
    }
}
