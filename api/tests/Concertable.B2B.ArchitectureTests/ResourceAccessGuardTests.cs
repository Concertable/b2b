using Concertable.Testing;
using Xunit;

namespace Concertable.B2B.ArchitectureTests;

public sealed class ResourceAccessGuardTests
{
    [Fact]
    public void EveryResourceScopedContext_DeclaresAtLeastOneFilter()
    {
        var unfiltered = ResourceScopedContexts()
            .Where(file => !File.ReadAllText(file.FullName).Contains(
                "HasQueryFilter(TenantFilters.Key",
                StringComparison.Ordinal))
            .Select(file => file.Name)
            .ToArray();

        Assert.Empty(unfiltered);
    }

    [Fact]
    public void ResourceScopedContexts_AreDiscoverable() => Assert.NotEmpty(ResourceScopedContexts());

    private static FileInfo[] ResourceScopedContexts() =>
        new DirectoryInfo(Path.Combine(
                typeof(ResourceAccessGuardTests).Assembly.SolutionDirectory.FullName,
                "api",
                "src",
                "Modules"))
            .EnumerateFiles("*DbContext.cs", SearchOption.AllDirectories)
            .Where(file => !file.Directory!.AncestorsAndSelf().Any(ancestor => ancestor.Name is "bin" or "obj"))
            .Where(file => File.ReadAllText(file.FullName)
                .Contains(": ResourceScopedDbContext(", StringComparison.Ordinal))
            .ToArray();
}
