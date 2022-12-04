using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SourceGenerators;

[Generator]
public class OnPropertyChangedMethodsGenerator : ISourceGenerator
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

            List<string> properties = type.GetMembers().OfType<IPropertySymbol>().Select(prop => prop.Name).ToList();

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
                source.AppendLine("  protected override void __EnableOnPropertyChangedMethods()");
                source.AppendLine("  {");
                source.AppendLine("    PropertyChanged += (object sender, PropertyChangedEventArgs e) =>");
                source.AppendLine("    {");
                source.AppendLine("      switch(e.PropertyName)");
                source.AppendLine("      {");
                foreach (string property in properties)
                {
                    source.AppendLine("        case \"" + property + "\": On" + property + "Changed(); break;");
                }
                source.AppendLine("      }");
                source.AppendLine("    };");
                source.AppendLine("  }");
                source.AppendLine("");
                foreach (string property in properties)
                {
                    source.AppendLine("  partial void On" + property + "Changed();");
                }
                source.AppendLine("}");
                context.AddSource(fullName + ".g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
            }
        }


    }

}

