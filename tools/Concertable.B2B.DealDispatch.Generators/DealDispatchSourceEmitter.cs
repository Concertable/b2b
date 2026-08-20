using System.Text;
using Microsoft.CodeAnalysis;

namespace Concertable.B2B.DealDispatch.Generators;

public sealed partial class DealDispatchGenerator
{
    private static string EmitStrategy(StrategyModel model)
    {
        var source = new StringBuilder();
        Header(source, model.Request.Anchor.ContainingNamespace);
        var className = "Generated" + TrimLeadingI(model.Request.Factory.Name);
        var genericNames = new[] { "TStrategy" }
            .Concat(model.Slots.Select(slot => "T" + slot.Stem))
            .ToArray();

        source.Append("internal sealed class ")
            .Append(className)
            .Append('<')
            .Append(string.Join(", ", genericNames))
            .Append("> : ")
            .Append(NamedTypeDefinitionName(model.Request.Factory.ConstructedFrom))
            .AppendLine("<TStrategy>");
        source.Append("    where TStrategy : class, ")
            .AppendLine(TypeName(model.Request.Marker!));
        foreach (var slot in model.Slots)
        {
            source.Append("    where T")
                .Append(slot.Stem)
                .AppendLine(" : class, TStrategy");
        }

        source.AppendLine("{");
        foreach (var slot in model.Slots)
        {
            source.Append("    private readonly T")
                .Append(slot.Stem)
                .Append(' ')
                .Append(Camel(slot.Stem))
                .AppendLine(";");
        }

        source.AppendLine();
        source.Append("    public ")
            .Append(className)
            .AppendLine("(");
        for (var index = 0; index < model.Slots.Length; index++)
        {
            var slot = model.Slots[index];
            source.Append("        T")
                .Append(slot.Stem)
                .Append(' ')
                .Append(Camel(slot.Stem))
                .AppendLine(index == model.Slots.Length - 1 ? ")" : ",");
        }
        source.AppendLine("    {");
        foreach (var slot in model.Slots)
        {
            source.Append("        this.")
                .Append(Camel(slot.Stem))
                .Append(" = ")
                .Append(Camel(slot.Stem))
                .AppendLine(";");
        }
        source.AppendLine("    }");

        foreach (var selector in model.Selectors)
        {
            source.AppendLine();
            source.Append("    public TStrategy Create(")
                .Append(TypeName(selector.Input))
                .AppendLine(" value) => value switch");
            source.AppendLine("    {");
            foreach (var item in selector.Cases)
            {
                source.Append("        ")
                    .Append(TypeName(item.Type))
                    .Append(" => ")
                    .Append(Camel(item.Stem))
                    .AppendLine(",");
            }
            source.AppendLine("        null => throw new global::System.ArgumentNullException(nameof(value)),");
            source.AppendLine("        _ => throw new global::System.ArgumentOutOfRangeException(nameof(value), value, null)");
            source.AppendLine("    };");
        }
        source.AppendLine("}");
        source.AppendLine();

        EmitAnchorStart(source, model.Request.Anchor);
        source.Append("    internal static global::Microsoft.Extensions.DependencyInjection.IServiceCollection ")
            .Append(model.RegistrationMethod)
            .AppendLine("(");
        source.AppendLine("        this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)");
        source.AppendLine("    {");

        foreach (var implementation in model.Families
                     .SelectMany(family => family.Implementations)
                     .GroupBy(type => type.ToDisplayString(), StringComparer.Ordinal)
                     .Select(group => group.First()))
        {
            source.Append("        services.AddScoped<")
                .Append(TypeName(implementation))
                .AppendLine(">();");
        }

        foreach (var family in model.Families)
        {
            source.Append("        services.AddScoped<")
                .Append(NamedTypeDefinitionName(model.Request.Factory.ConstructedFrom))
                .Append('<')
                .Append(TypeName(family.Family))
                .Append(">, ")
                .Append(className)
                .Append('<')
                .Append(TypeName(family.Family));
            foreach (var implementation in family.Implementations)
            {
                source.Append(", ")
                    .Append(TypeName(implementation));
            }
            source.AppendLine(">>();");
        }

        source.AppendLine();
        source.AppendLine("        return services;");
        source.AppendLine("    }");
        source.AppendLine("}");
        return source.ToString();
    }

    private static string EmitVariant(VariantModel model)
    {
        var source = new StringBuilder();
        Header(source, model.Request.Anchor.ContainingNamespace);
        var className = "Generated" + TrimLeadingI(model.Request.Factory.Name);

        source.Append("internal sealed class ")
            .Append(className)
            .Append(" : ")
            .AppendLine(TypeName(model.Request.Factory));
        source.AppendLine("{");
        foreach (var implementation in model.Implementations)
        {
            source.Append("    private readonly ")
                .Append(TypeName(implementation.Implementation))
                .Append(' ')
                .Append(Camel(implementation.Implementation.Name))
                .AppendLine(";");
        }

        source.AppendLine();
        source.Append("    public ")
            .Append(className)
            .AppendLine("(");
        for (var index = 0; index < model.Implementations.Length; index++)
        {
            var implementation = model.Implementations[index].Implementation;
            source.Append("        ")
                .Append(TypeName(implementation))
                .Append(' ')
                .Append(Camel(implementation.Name))
                .AppendLine(index == model.Implementations.Length - 1 ? ")" : ",");
        }
        source.AppendLine("    {");
        foreach (var implementation in model.Implementations)
        {
            source.Append("        this.")
                .Append(Camel(implementation.Implementation.Name))
                .Append(" = ")
                .Append(Camel(implementation.Implementation.Name))
                .AppendLine(";");
        }
        source.AppendLine("    }");
        source.AppendLine();
        source.Append("    public ")
            .Append(TypeName(model.Wrapper))
            .Append(" Create(")
            .Append(TypeName(model.Input))
            .AppendLine(" value) => value switch");
        source.AppendLine("    {");

        foreach (var item in model.Cases)
        {
            var implementation = model.Implementations.Single(candidate =>
                candidate.Cases.Any(type =>
                    SymbolEqualityComparer.Default.Equals(type, item.Type)));
            source.Append("        ")
                .Append(TypeName(item.Type))
                .Append(" => new ")
                .Append(TypeName(model.Wrapper))
                .Append('(')
                .Append(Camel(implementation.Implementation.Name))
                .AppendLine("),");
        }
        source.AppendLine("        null => throw new global::System.ArgumentNullException(nameof(value)),");
        source.AppendLine("        _ => throw new global::System.ArgumentOutOfRangeException(nameof(value), value, null)");
        source.AppendLine("    };");
        source.AppendLine("}");
        source.AppendLine();

        EmitAnchorStart(source, model.Request.Anchor);
        source.Append("    internal static global::Microsoft.Extensions.DependencyInjection.IServiceCollection ")
            .Append(model.RegistrationMethod)
            .AppendLine("(");
        source.AppendLine("        this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)");
        source.AppendLine("    {");
        foreach (var implementation in model.Implementations)
        {
            source.Append("        services.AddScoped<")
                .Append(TypeName(implementation.Implementation))
                .AppendLine(">();");
        }
        source.Append("        services.AddScoped<")
            .Append(TypeName(model.Request.Factory))
            .Append(", ")
            .Append(className)
            .AppendLine(">();");
        source.AppendLine();
        source.AppendLine("        return services;");
        source.AppendLine("    }");
        source.AppendLine("}");
        return source.ToString();
    }

    private static void Header(StringBuilder source, INamespaceSymbol namespaceSymbol)
    {
        source.AppendLine("// <auto-generated />");
        source.AppendLine("#nullable enable");
        source.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        source.AppendLine();
        source.Append("namespace ")
            .Append(namespaceSymbol.ToDisplayString())
            .AppendLine(";");
        source.AppendLine();
    }

    private static void EmitAnchorStart(StringBuilder source, INamedTypeSymbol anchor) =>
        source.Append("internal static partial class ")
            .Append(anchor.Name)
            .AppendLine()
            .AppendLine("{");

    private static string TypeName(ITypeSymbol symbol) =>
        symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static string NamedTypeDefinitionName(INamedTypeSymbol symbol) =>
        "global::" +
        (symbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : symbol.ContainingNamespace.ToDisplayString() + ".") +
        symbol.Name;

    private static string TrimLeadingI(string name) =>
        name.StartsWith("I", StringComparison.Ordinal) && name.Length > 1 && char.IsUpper(name[1])
            ? name.Substring(1)
            : name;

    private static string Camel(string value) =>
        char.ToLowerInvariant(value[0]) + value.Substring(1);
}
