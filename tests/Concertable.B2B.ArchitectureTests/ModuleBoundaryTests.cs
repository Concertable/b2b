using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Concertable.B2B.ArchitectureTests;

/// <summary>
/// Enforces the modular-monolith rules (api/agents/CONVENTIONS.md) that the compiler alone
/// can't: cross-module isolation once a type is <c>public</c>, plus the layer reference graph as
/// defense-in-depth. ArchUnitNET reads compiled IL, so it sees <c>internal</c> types too.
/// </summary>
public sealed class ModuleBoundaryTests
{
    private static readonly string[] Modules =
        ["Application", "Artist", "Booking", "Concert", "Conversations", "Deal", "Opportunity", "Tenant", "User", "Venue"];

    private static readonly string ModsAlt = string.Join("|", Modules);

    private static readonly System.Reflection.Assembly[] Assemblies = LoadAssemblies();

    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(Assemblies)
        .Build();

    private static System.Reflection.Assembly[] LoadAssemblies()
    {
        var dir = Path.GetDirectoryName(typeof(ModuleBoundaryTests).Assembly.Location)!;
        return Directory.GetFiles(dir, "Concertable.B2B.*.dll")
            .Where(p => !Path.GetFileNameWithoutExtension(p).Contains("Test", StringComparison.Ordinal))
            .Select(System.Reflection.Assembly.LoadFrom)
            .Append(System.Reflection.Assembly.LoadFrom(Path.Combine(dir, "Concertable.Kernel.dll")))
            .ToArray();
    }

    // Layering — the reference graph only points inward (toward Contracts/Kernel).

    [Fact]
    public void Domain_does_not_depend_on_Application_Infrastructure_or_Api() =>
        Forbid("Domain", "Application", "Infrastructure", "Api");

    [Fact]
    public void Application_does_not_depend_on_Infrastructure_or_Api() =>
        Forbid("Application", "Infrastructure", "Api");

    [Fact]
    public void Contracts_do_not_depend_on_inner_layers() =>
        Forbid("Contracts", "Domain", "Application", "Infrastructure", "Api");

    [Fact]
    public void Api_does_not_depend_on_Option() =>
        Types().That().ResideInNamespace($@"^Concertable\.B2B\.({ModsAlt})\.Api($|\.)", useRegularExpressions: true)
            .Should().NotDependOnAny(Types().That().AreAssignableTo("Reunion.Option`1", useRegularExpressions: false))
            .Because("controllers receive application-owned Results rather than deciding what absence means")
            .Check(Architecture);

    // Cross-module isolation — a module talks to another only via its Contracts / integration events,
    // never reaching into its Infrastructure. (Domain is intentionally allowed: public read-model
    // types are shared cross-module as projection targets — CONVENTIONS.md.)

    [Fact]
    public void Modules_do_not_reach_into_another_modules_Infrastructure()
    {
        foreach (var from in Modules)
        foreach (var into in Modules)
        {
            if (from == into)
                continue;

            Types().That().ResideInNamespace($@"^Concertable\.B2B\.{from}\.", useRegularExpressions: true)
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespace($@"^Concertable\.B2B\.{into}\.Infrastructure($|\.)", useRegularExpressions: true))
                .Because($"{from} must reach {into} only via {into}.Contracts or integration events, never its Infrastructure.")
                .Check(Architecture);
        }
    }

    [Fact]
    public void Module_facades_do_not_depend_on_persistence_or_mapping_components()
    {
        var violations = Assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass && type.Name.EndsWith("Module", StringComparison.Ordinal))
            .Where(type => type.GetInterfaces().Any(contract => contract.Name.EndsWith("Module", StringComparison.Ordinal)))
            .SelectMany(type => type.GetConstructors(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic))
            .SelectMany(constructor => constructor.GetParameters(), (constructor, parameter) => new
            {
                Facade = constructor.DeclaringType!,
                Dependency = parameter.ParameterType
            })
            .Where(pair =>
                pair.Dependency.Name.EndsWith("Repository", StringComparison.Ordinal) ||
                pair.Dependency.Name.EndsWith("Mapper", StringComparison.Ordinal) ||
                pair.Dependency.Name.EndsWith("DbContext", StringComparison.Ordinal))
            .Select(pair => $"{pair.Facade.FullName} -> {pair.Dependency.FullName}")
            .Order()
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Module facades must delegate to application use cases:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    private static void Forbid(string layer, params string[] forbiddenLayers)
    {
        var source = $@"^Concertable\.B2B\.({ModsAlt})\.{layer}($|\.)";
        var forbidden = $@"^Concertable\.B2B\.({ModsAlt})\.({string.Join("|", forbiddenLayers)})($|\.)";

        Types().That().ResideInNamespace(source, useRegularExpressions: true)
            .Should().NotDependOnAny(Types().That().ResideInNamespace(forbidden, useRegularExpressions: true))
            .Because($"the {layer} layer must not depend on {string.Join("/", forbiddenLayers)} (CONVENTIONS.md reference graph).")
            .Check(Architecture);
    }
}
