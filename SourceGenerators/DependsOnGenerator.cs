using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SourceGenerators;

[Generator]
public class DependsOnGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
#if DEBUG
        if (!Debugger.IsAttached)
        {
            //Debugger.Launch();
        }
#endif
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var types = Utils.GetAllTypeSymbols(context);

        foreach (var type in types)
        {
            string fullName = Utils.GetFullName(type);

            if (type.BaseType?.Name != "ViewModelBase")
            {
                continue;
            }

            var properties = type.GetMembers().OfType<IPropertySymbol>()
                .Where(prop => prop.GetAttributes().Any(attr => attr.AttributeClass?.Name == "DependsOnAttribute"))
                .ToList();

            if (properties.Any())
            {
                var source = new StringBuilder();
                source.AppendLine("using System;");
                source.AppendLine("using System.ComponentModel;");
                source.AppendLine("");
                source.AppendLine("namespace " + fullName.Replace("." + type.Name, "") + ";");
                source.AppendLine("");
                source.AppendLine("partial class " + type.Name);
                source.AppendLine("{");
                source.AppendLine("  protected override void __EnableDependsOn()");
                source.AppendLine("  {");
                source.AppendLine("    PropertyChanged += (object sender, PropertyChangedEventArgs e) =>");
                source.AppendLine("    {");
                source.AppendLine("      switch(e.PropertyName)");
                source.AppendLine("      {");
                foreach (var group in properties
                    .GroupBy(prop => prop.GetAttributes().First(attr => attr.AttributeClass?.Name == "DependsOnAttribute").ConstructorArguments.First().Value))
                {
                    source.AppendLine("        case \"" + group.Key + "\": OnPropertyChanged(nameof(" + group.First().Name + "));");
                    foreach (var prop in group)
                    {
                        source.AppendLine("          OnPropertyChanged(nameof(" + prop.Name + "));");
                    }
                    source.AppendLine("          break;");
                }
                source.AppendLine("      }");
                source.AppendLine("    };");
                source.AppendLine("  }");
                source.AppendLine("}");
                context.AddSource(fullName + ".g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
            }
        }


    }

}

