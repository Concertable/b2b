using System.Reflection;
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
        var assemblies = FindModuleIntegrationAssemblies();
        var violations = assemblies
            .SelectMany(assembly => assembly.CrossModuleDomainOrInfrastructureReferences(assemblies)
                .Select(reference => $"{assembly.GetName().Name} -> {reference}"))
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
}
