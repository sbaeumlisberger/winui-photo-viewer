using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SourceGenerators;

[Generator]
public class ViewRegistrationGenerator : ISourceGenerator
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

        var source = new StringBuilder();
        source.AppendLine("using System;");
        source.AppendLine("using System.Collections.Generic;");
        source.AppendLine("");
        source.AppendLine("public class ViewRegistrations");
        source.AppendLine("{");
        source.AppendLine("    public Dictionary<Type, Func<object, object>> ViewFactoriesByViewModelType { get; } = new Dictionary<Type, Func<object, object>>()");
        source.AppendLine("    {");

        HashSet<string> registeredView = new HashSet<string>();

        foreach (var type in types)
        {
            var attribute = type.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "ViewRegistrationAttribute");
            if (attribute == null)
                continue;

            var viewModelTypeArg = attribute.ConstructorArguments.FirstOrDefault();

            var constructor = type.InstanceConstructors[0];

            string className = Utils.GetFullName(type);

            if (registeredView.Contains(className))
            {
                continue;
            }
            registeredView.Add(className);

            if (constructor.Parameters.Length == 1)
            {
                source.AppendLine($"        {{ typeof({viewModelTypeArg.Value}), (vm) => new {className}(({viewModelTypeArg.Value})vm) }},");
            }
            else
            {
                source.AppendLine($"        {{ typeof({viewModelTypeArg.Value}), (vm) => new {className}() }},");
            }
        }

        source.AppendLine("    };");


        source.AppendLine("    public Dictionary<Type, Type> ViewTypeByViewModelType { get; } = new Dictionary<Type, Type>()");
        source.AppendLine("    {");

        registeredView.Clear();

        foreach (var type in types)
        {
            var attribute = type.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "ViewRegistrationAttribute");
            if (attribute == null)
                continue;

            var viewModelTypeArg = attribute.ConstructorArguments.FirstOrDefault();

            string className = Utils.GetFullName(type);

            if (registeredView.Contains(className))
            {
                continue;
            }
            registeredView.Add(className);

            source.AppendLine($"        {{ typeof({viewModelTypeArg.Value}), typeof({className}) }},");
        }

        source.AppendLine("    };");

        source.AppendLine("}");

        context.AddSource("ViewRegistrations.Generated.cs", SourceText.From(source.ToString(), Encoding.UTF8));
    }

}

