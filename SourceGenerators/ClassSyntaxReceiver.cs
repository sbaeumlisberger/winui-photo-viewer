using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceGenerators;

internal class ClassSyntaxReceiver : ISyntaxReceiver
{
    public List<ClassDeclarationSyntax> Classes { get; } = new();

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is ClassDeclarationSyntax cds)
        {
            Classes.Add(cds);
        }
    }
}
