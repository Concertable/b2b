using Concertable.Testing;
using Xunit;

namespace Concertable.B2B.ArchitectureTests;

/// <summary>
/// One tenant-filtered DbContext base now serves both the single-owner and the two-party stance, so nothing
/// in the type system says a two-party context must also carry the write guard. These tests are what says it.
/// </summary>
public sealed class TenantWriteGuardTests
{
    [Fact]
    public void TwoPartyContexts_RegisterTheVenueArtistWriteGuard()
    {
        var unguarded = TwoPartyModules()
            .Where(module => !CompositionRootOf(module).Contains(
                "VenueArtistTenantInterceptor",
                StringComparison.Ordinal))
            .Select(module => module.Name)
            .ToArray();

        Assert.Empty(unguarded);
    }

    [Fact]
    public void TwoPartyModules_AreDiscoverable()
    {
        // Guards the guard: a rename of ApplyVenueArtist would otherwise leave the test above scanning
        // nothing and passing.
        Assert.NotEmpty(TwoPartyModules());
    }

    private static DirectoryInfo[] TwoPartyModules() =>
        ModuleSourceFiles()
            .Where(file => file.Name.EndsWith("DbContext.cs", StringComparison.Ordinal))
            .Where(file => File.ReadAllText(file.FullName)
                .Contains("ApplyVenueArtist<", StringComparison.Ordinal))
            .Select(ModuleRootOf)
            .DistinctBy(module => module.FullName)
            .ToArray();

    private static string CompositionRootOf(DirectoryInfo module) =>
        string.Concat(module
            .EnumerateFiles("ServiceCollectionExtensions.cs", SearchOption.AllDirectories)
            .Where(NotBuildOutput)
            .Select(file => File.ReadAllText(file.FullName)));

    private static IEnumerable<FileInfo> ModuleSourceFiles() =>
        new DirectoryInfo(Path.Combine(
                typeof(TenantWriteGuardTests).Assembly.SolutionDirectory.FullName,
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
