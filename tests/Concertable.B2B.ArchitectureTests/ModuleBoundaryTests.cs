using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Concertable.B2B.ArchitectureTests;

/// <summary>
/// Enforces the modular-monolith rules (api/docs/MODULAR_MONOLITH_RULES.md) that the compiler alone
/// can't: cross-module isolation once a type is <c>public</c>, plus the layer reference graph as
/// defense-in-depth. ArchUnitNET reads compiled IL, so it sees <c>internal</c> types too.
/// </summary>
public sealed class ModuleBoundaryTests
{
    private static readonly string[] Modules =
        ["Artist", "Concert", "Conversations", "Deal", "Tenant", "User", "Venue"];

    private static readonly string ModsAlt = string.Join("|", Modules);

    private static readonly Architecture Architecture = Load();

    private static Architecture Load()
    {
        var dir = Path.GetDirectoryName(typeof(ModuleBoundaryTests).Assembly.Location)!;
        var assemblies = Directory.GetFiles(dir, "Concertable.B2B.*.dll")
            .Where(p => !Path.GetFileNameWithoutExtension(p).Contains("Test", StringComparison.Ordinal))
            .Select(System.Reflection.Assembly.LoadFrom)
            .ToArray();
        return new ArchLoader().LoadAssemblies(assemblies).Build();
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

    // Cross-module isolation — a module talks to another only via its Contracts / integration events,
    // never reaching into its Infrastructure. (Domain is intentionally allowed: public read-model
    // types are shared cross-module as projection targets — MODULAR_MONOLITH_RULES.md.)

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

    private static void Forbid(string layer, params string[] forbiddenLayers)
    {
        var source = $@"^Concertable\.B2B\.({ModsAlt})\.{layer}($|\.)";
        var forbidden = $@"^Concertable\.B2B\.({ModsAlt})\.({string.Join("|", forbiddenLayers)})($|\.)";

        Types().That().ResideInNamespace(source, useRegularExpressions: true)
            .Should().NotDependOnAny(Types().That().ResideInNamespace(forbidden, useRegularExpressions: true))
            .Because($"the {layer} layer must not depend on {string.Join("/", forbiddenLayers)} (MODULAR_MONOLITH_RULES.md reference graph).")
            .Check(Architecture);
    }
}
