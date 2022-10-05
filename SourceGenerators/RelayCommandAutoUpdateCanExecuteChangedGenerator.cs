using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SourceGenerators;

[Generator]
public class RelayCommandAutoUpdateCanExecuteChangedGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
#if DEBUG
        if (!Debugger.IsAttached)
        {
            //Debugger.Launch();
        }
#endif

        context.RegisterForSyntaxNotifications(() => new ClassSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var types = Utils.GetAllTypeSymbols(context);

        foreach (var clazz in types)
        {
            string className = Utils.GetFullName(clazz);

            if (clazz.BaseType?.Name != "ViewModelBase")
            {
                continue;
            }

            List<(string Property, string Command)> list = new();

            foreach (var member in clazz.GetMembers())
            {
                if (member.GetAttributes().FirstOrDefault(attr => attr.AttributeClass!.Name == "RelayCommandAttribute") is { } attribute &&
                    attribute.NamedArguments.FirstOrDefault(arg => arg.Key == "CanExecute") is { } canExecuteArg)
                {
                    string? propertyName = canExecuteArg.Value.Value as string;
                    if (!string.IsNullOrEmpty(propertyName))
                    {
                        list.Add((propertyName!, member.Name));
                    }
                }
            }

            if (list.Any())
            {
                var source = new StringBuilder();
                source.AppendLine("using System;");
                source.AppendLine("using System.ComponentModel;");
                source.AppendLine("");
                source.AppendLine("namespace " + className.Replace("." + clazz.Name, "") + ";");
                source.AppendLine("");
                source.AppendLine("partial class " + clazz.Name);
                source.AppendLine("{");
                source.AppendLine("  protected override void __EnableAutoNotifyCanExecuteChanged()");
                source.AppendLine("  {");
                source.AppendLine("    PropertyChanged += (object? sender, PropertyChangedEventArgs e) =>");
                source.AppendLine("    {");
                source.AppendLine("      switch(e.PropertyName)");
                source.AppendLine("      {");
                foreach (var (property, command) in list)
                {
                    source.AppendLine("        case \"" + property + "\": " + command.Replace("Async", "") + "Command.NotifyCanExecuteChanged(); break;");
                }
                source.AppendLine("      }");
                source.AppendLine("    };");
                source.AppendLine("  }");
                source.AppendLine("}");
                context.AddSource(className + ".g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
            }
        }


    }

}

