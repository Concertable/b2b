using System.Reflection;
using System.Xml.Linq;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.Testing;
using Concertable.Testing.Architecture;
using Xunit;

namespace Concertable.B2B.ArchitectureTests;

public sealed class IntegrationTestBoundaryTests
{
    private const BindingFlags DeclaredMembers =
        BindingFlags.Public |
        BindingFlags.NonPublic |
        BindingFlags.Instance |
        BindingFlags.Static |
        BindingFlags.DeclaredOnly;

    private static readonly ServiceArchitecture Topology =
        ServiceArchitecture.Create(typeof(IntegrationTestBoundaryTests).Assembly);

    // The fixture project every module integration suite (and the cross-module Lifecycle suite) references to
    // boot its module. Selecting suites by this reference — not a maintained path or name list — means a new
    // suite is covered the moment it opts into the fixture. Whether a suite *declares* a reference to another
    // module's Domain or Infrastructure is a project-file question — a transitive assembly reference is not a
    // declared one — so the check itself reads the .csproj.
    private static readonly string ModuleFixtureProject = typeof(ApiFixture).Assembly.GetName().Name!;

    [Fact]
    public void ModuleIntegrationProjects_DoNotReferenceAnotherModulesDomainOrInfrastructure()
    {
        var violations = SolutionDirectory
            .EnumerateFiles($"{Topology.Company}.{Topology.Service}.*.IntegrationTests.csproj", SearchOption.AllDirectories)
            .Where(ReferencesModuleFixture)
            .SelectMany(FindCrossModuleProjectReferences)
            .Order()
            .ToArray();

        Assert.True(violations.Length == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void ModuleIntegrationTests_UseOwningFixture()
    {
        var violations = typeof(IntegrationTestBoundaryTests).Assembly
            .LoadSiblingModuleIntegrationTestAssemblies()
            .SelectMany(FindSharedFixtureConsumers)
            .Order()
            .ToArray();

        Assert.Empty(violations);
    }

    private static bool ReferencesModuleFixture(FileInfo project) =>
        XDocument.Load(project.FullName).Descendants("ProjectReference")
            .Select(reference => (string?)reference.Attribute("Include"))
            .Any(include => include is not null &&
                Path.GetFileNameWithoutExtension(include) == ModuleFixtureProject);

    private static IEnumerable<string> FindCrossModuleProjectReferences(FileInfo project)
    {
        // The module the suite owns — its third name segment; a nested-module suite (Dashboard) owns the
        // whole `Dashboard.*` family, so compare on the first module segment.
        var owner = Path.GetFileNameWithoutExtension(project.Name).Split('.')[2];
        foreach (var reference in XDocument.Load(project.FullName).Descendants("ProjectReference"))
        {
            var include = (string?)reference.Attribute("Include");
            if (include is null)
                continue;

            if (Topology.Parse(Path.GetFileNameWithoutExtension(include)) is { Module: var module, Layer: var layer } &&
                module.Split('.')[0] != owner &&
                layer is ArchitectureLayer.Domain or ArchitectureLayer.Infrastructure)
                yield return $"{project.Name} -> {Path.GetFileNameWithoutExtension(include)}";
        }
    }

    private static IEnumerable<string> FindSharedFixtureConsumers(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            var consumesSharedFixture = type.GetFields(DeclaredMembers)
                    .Any(field => field.FieldType == typeof(ApiFixture)) ||
                type.GetProperties(DeclaredMembers)
                    .Any(property => property.PropertyType == typeof(ApiFixture)) ||
                type.GetConstructors(DeclaredMembers)
                    .SelectMany(constructor => constructor.GetParameters())
                    .Any(parameter => parameter.ParameterType == typeof(ApiFixture)) ||
                type.GetMethods(DeclaredMembers)
                    .Any(method => method.ReturnType == typeof(ApiFixture) ||
                        method.GetParameters().Any(parameter => parameter.ParameterType == typeof(ApiFixture)));

            if (consumesSharedFixture)
                yield return $"{assembly.GetName().Name}: {type.FullName}";
        }
    }

    private static DirectoryInfo SolutionDirectory =>
        typeof(IntegrationTestBoundaryTests).Assembly.SolutionDirectory;
}
