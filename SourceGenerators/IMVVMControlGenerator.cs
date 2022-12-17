using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SourceGenerators;

[Generator]
public class IMVVMControlGenerator : ISourceGenerator
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

            if (!type.Interfaces.Any(inf => inf.Name == "IMVVMControl"))
            {
                continue;
            }

            var source = new StringBuilder();
            source.AppendLine("using System;");
            source.AppendLine("");
            source.AppendLine("namespace " + fullName.Replace("." + type.Name, "") + ";");
            source.AppendLine("");
            source.AppendLine("partial class " + type.Name);
            source.AppendLine("{");
            source.AppendLine("  public void LoadComponent() => InitializeComponent();");
            source.AppendLine("  public void InitializeBindings() => Bindings.Initialize();");
            source.AppendLine("  public void StopBindings() => Bindings.StopTracking();");
            source.AppendLine("}");
            context.AddSource(fullName + ".g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
        }

    }

}

