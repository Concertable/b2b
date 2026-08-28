using System.Reflection;
using System.Xml.Linq;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.Testing;
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

    [Fact]
    public void ModuleIntegrationProjects_DoNotReferenceAnotherModulesDomainOrInfrastructure()
    {
        var violations = FindB2BRoot()
            .EnumerateFiles("Concertable.B2B.*.IntegrationTests.csproj", SearchOption.AllDirectories)
            .Where(project => project.FullName.Contains(
                    $"{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}Modules{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal) ||
                project.Name == "Concertable.B2B.Process.IntegrationTests.csproj")
            .SelectMany(FindCrossModuleProjectReferences)
            .Order()
            .ToArray();

        Assert.True(violations.Length == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void ModuleIntegrationTests_UseOwningFixture()
    {
        var violations = FindModuleIntegrationAssemblies()
            .SelectMany(FindSharedFixtureConsumers)
            .Order()
            .ToArray();

        Assert.Empty(violations);
    }

    private static IReadOnlyCollection<Assembly> FindModuleIntegrationAssemblies() =>
        typeof(IntegrationTestBoundaryTests).Assembly.LoadSiblingModuleIntegrationTestAssemblies();

    private static IEnumerable<string> FindCrossModuleProjectReferences(FileInfo project)
    {
        var owner = Path.GetFileNameWithoutExtension(project.Name).Split('.')[2];
        foreach (var reference in XDocument.Load(project.FullName).Descendants("ProjectReference"))
        {
            var include = (string?)reference.Attribute("Include");
            if (include is null)
                continue;

            var referenceName = Path.GetFileNameWithoutExtension(include).Split('.');
            if (referenceName is ["Concertable", "B2B", var module, "Domain" or "Infrastructure"] &&
                module != owner)
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

    private static DirectoryInfo FindB2BRoot() =>
        typeof(IntegrationTestBoundaryTests).Assembly.SolutionDirectory;
}
