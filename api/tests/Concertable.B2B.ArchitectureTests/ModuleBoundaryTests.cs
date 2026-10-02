using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.Testing.Architecture;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Concertable.B2B.ArchitectureTests;

/// <summary>
/// Enforces the modular-monolith rules (the `dotnet-standards:module-structure` skill) that the compiler alone
/// can't: cross-module isolation once a type is <c>public</c>, plus the layer reference graph as
/// defense-in-depth. ArchUnitNET reads compiled IL, so it sees <c>internal</c> types too.
///
/// The module set comes from <see cref="ServiceArchitecture"/> — read off the loaded assembly graph, never a
/// hand-maintained list — so a new, renamed or moved module is covered without touching this file.
/// </summary>
public sealed class ModuleBoundaryTests
{
    private static readonly ServiceArchitecture Topology =
        ServiceArchitecture.Create(typeof(ModuleBoundaryTests).Assembly);

    private static readonly Architecture Graph = new ArchLoader()
        .LoadAssemblies([.. Topology.Assemblies])
        .Build();

    // Layering — the reference graph only points inward (toward Contracts/Kernel).

    [Fact]
    public void Domain_does_not_depend_on_Application_Infrastructure_or_Api() =>
        Forbid(ArchitectureLayer.Domain, ArchitectureLayer.Application, ArchitectureLayer.Infrastructure, ArchitectureLayer.Api);

    [Fact]
    public void Application_does_not_depend_on_Infrastructure_or_Api() =>
        Forbid(ArchitectureLayer.Application, ArchitectureLayer.Infrastructure, ArchitectureLayer.Api);

    [Fact]
    public void Contracts_do_not_depend_on_inner_layers() =>
        Forbid(ArchitectureLayer.Contracts, ArchitectureLayer.Domain, ArchitectureLayer.Application, ArchitectureLayer.Infrastructure, ArchitectureLayer.Api);

    [Fact]
    public void Api_does_not_depend_on_Option() =>
        Types().That().ResideInNamespace(Topology.NamespacePattern(ArchitectureLayer.Api), useRegularExpressions: true)
            .Should().NotDependOnAny(Types().That().AreAssignableTo("Reunion.Option`1", useRegularExpressions: false))
            .Because("controllers receive application-owned Results rather than deciding what absence means")
            .Check(Graph);

    // Cross-module isolation — a module talks to another only via its Contracts / integration events,
    // never reaching into its Infrastructure. (Domain is intentionally allowed: public read-model
    // types are shared cross-module as projection targets — MODULES.md.)

    [Fact]
    public void Modules_do_not_reach_into_another_modules_Infrastructure()
    {
        foreach (var from in Topology.Modules)
        foreach (var into in Topology.Modules)
        {
            if (from == into)
                continue;

            Types().That().ResideInNamespace(Topology.NamespacePattern(from), useRegularExpressions: true)
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespace(
                        Topology.NamespacePattern(into, ArchitectureLayer.Infrastructure), useRegularExpressions: true))
                .Because($"{from} must reach {into} only via {into}.Contracts or integration events, never its Infrastructure.")
                .Check(Graph);
        }
    }

    [Fact]
    public void Module_facades_do_not_depend_on_persistence_or_mapping_components()
    {
        var violations = Topology.ModuleAssemblies
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

    // Lifecycle direction — Opportunity -> Application -> Booking -> Concert is one-way. A later stage may
    // read an earlier stage's published facts, but never command an earlier stage. See
    // plans/launch/DEAL_LIFECYCLE_OWNERSHIP_PLAN.md.

    private static readonly (string Module, System.Type Contract)[] LifecycleStages =
        [
            ("Opportunity", typeof(IOpportunityModule)),
            ("Application", typeof(IApplicationModule)),
            ("Booking", typeof(IBookingModule)),
            ("Concert", typeof(IConcertModule))
        ];

    [Fact]
    public void LifecycleModuleFacades_ExposeQueryMembersOnly()
    {
        foreach (var (_, contract) in LifecycleStages)
            MethodMembers().That().AreDeclaredIn(contract)
                .Should().HaveNameStartingWith("Get")
                .Because($"{contract.Name} is a lifecycle-stage facade: it may publish facts for a later " +
                          "stage to read, never accept a command (MM_BOUNDARY_HARDENING_PROMPT.md Part A3).")
                .Check(Graph);
    }

    [Fact]
    public void LaterLifecycleStages_DoNotCommandAnEarlierStage()
    {
        for (var earlier = 0; earlier < LifecycleStages.Length; earlier++)
        for (var later = earlier + 1; later < LifecycleStages.Length; later++)
        {
            var (_, earlierContract) = LifecycleStages[earlier];
            var laterModule = LifecycleStages[later].Module;

            MethodMembers().That()
                .AreDeclaredIn(earlierContract).And()
                .DoNotHaveNameStartingWith("Get")
                .Should().NotBeCalledBy(Topology.NamespacePattern(laterModule), useRegularExpressions: true)
                .Because($"{laterModule} is downstream of {earlierContract.Name} in the deal lifecycle; a " +
                          "downstream stage may read an upstream stage's contract but never command it.")
                .WithoutRequiringPositiveResults()
                .Check(Graph);
        }
    }

    private static void Forbid(ArchitectureLayer layer, params ArchitectureLayer[] forbiddenLayers)
    {
        Types().That().ResideInNamespace(Topology.NamespacePattern(layer), useRegularExpressions: true)
            .Should().NotDependOnAny(
                Types().That().ResideInNamespace(Topology.NamespacePattern(forbiddenLayers), useRegularExpressions: true))
            .Because($"the {layer} layer must not depend on {string.Join("/", forbiddenLayers)} (MODULES.md reference graph).")
            .Check(Graph);
    }
}
