using Concertable.Testing;
using Xunit;

namespace Concertable.B2B.ArchitectureTests;

public sealed class ResourceAccessGuardTests
{
    [Fact]
    public void EveryGrantFamily_HasItsConfigurationRegistered()
    {
        var unregistered = GrantFamilies()
            .Where(family => !ConfigurationProviderOf(family.Module).Contains(
                $"new {family.Name}Configuration()",
                StringComparison.Ordinal))
            .Select(family => family.Name)
            .ToArray();

        Assert.Empty(unregistered);
    }

    [Fact]
    public void GrantFamilies_AreDiscoverable()
    {
        Assert.NotEmpty(GrantFamilies());
    }

    [Fact]
    public void EveryGrantScopedContext_DeclaresAtLeastOneFilter()
    {
        var unfiltered = GrantScopedContexts()
            .Where(file => !File.ReadAllText(file.FullName).Contains(
                "HasQueryFilter(TenantFilters.Key",
                StringComparison.Ordinal))
            .Select(file => file.Name)
            .ToArray();

        Assert.Empty(unfiltered);
    }

    [Fact]
    public void GrantScopedContexts_AreDiscoverable() => Assert.NotEmpty(GrantScopedContexts());

    private static (string Name, DirectoryInfo Module)[] GrantFamilies() =>
        ModuleSourceFiles()
            .Where(file => File.ReadAllText(file.FullName)
                .Contains(": ResourceAccessGrant<", StringComparison.Ordinal))
            .Select(file => (Name: Path.GetFileNameWithoutExtension(file.Name), Module: ModuleRootOf(file)))
            .ToArray();

    private static FileInfo[] GrantScopedContexts() =>
        ModuleSourceFiles()
            .Where(file => file.Name.EndsWith("DbContext.cs", StringComparison.Ordinal))
            .Where(file => File.ReadAllText(file.FullName)
                .Contains(": ResourceScopedDbContext(", StringComparison.Ordinal))
            .ToArray();

    private static string ConfigurationProviderOf(DirectoryInfo module) =>
        string.Concat(module
            .EnumerateFiles("*ConfigurationProvider.cs", SearchOption.AllDirectories)
            .Where(NotBuildOutput)
            .Select(file => File.ReadAllText(file.FullName)));

    private static IEnumerable<FileInfo> ModuleSourceFiles() =>
        new DirectoryInfo(Path.Combine(
                typeof(ResourceAccessGuardTests).Assembly.SolutionDirectory.FullName,
                "api",
                "src",
                "Modules"))
            .EnumerateFiles("*.cs", SearchOption.AllDirectories)
            .Where(NotBuildOutput);

    private static DirectoryInfo ModuleRootOf(FileInfo file)
    {
        for (var directory = file.Directory; directory is not null; directory = directory.Parent)
            if (directory.Parent?.Name == "Modules")
                return directory;

        throw new InvalidOperationException($"{file.FullName} is not inside a module.");
    }

    private static bool NotBuildOutput(FileInfo file) =>
        !file.Directory!.AncestorsAndSelf().Any(ancestor => ancestor.Name is "bin" or "obj");
}
