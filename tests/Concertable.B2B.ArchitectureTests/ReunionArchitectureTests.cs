using System.Xml.Linq;
using Xunit;

namespace Concertable.B2B.ArchitectureTests;

public sealed class ReunionArchitectureTests
{
    private static readonly string[] ReunionPackages =
        ["Reunion", "Reunion.AspNetCore", "Reunion.Errors"];

    [Fact]
    public void B2BSource_LegacyResultIdentities_AreAbsent()
    {
        var oldFunctionalNamespace = "Concertable.Kernel." + "Functional";
        var oldApiResultsNamespace = "Concertable.Shared.Api." + "Results";
        var oldPackage = "Fluent" + "Results";
        var violations = Directory
            .EnumerateFiles(FindB2BRoot(), "*.*", SearchOption.AllDirectories)
            .Where(path => Path.GetExtension(path) is ".cs" or ".csproj" or ".props")
            .Where(path => !IsGeneratedPath(path))
            .Where(path =>
            {
                var source = File.ReadAllText(path);
                return source.Contains(oldFunctionalNamespace, StringComparison.Ordinal)
                    || source.Contains(oldApiResultsNamespace, StringComparison.Ordinal)
                    || source.Contains(oldPackage, StringComparison.Ordinal);
            })
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ReunionPackages_AreOwnedDirectlyByTheirSourceConsumers()
    {
        foreach (var projectPath in Directory.EnumerateFiles(FindB2BRoot(), "*.csproj", SearchOption.AllDirectories)
                     .Where(path => !IsGeneratedPath(path)))
        {
            var projectDirectory = Path.GetDirectoryName(projectPath)!;
            var source = string.Join(
                '\n',
                Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories)
                    .Where(path => !IsGeneratedPath(path))
                    .Where(path => !path.EndsWith(nameof(ReunionArchitectureTests) + ".cs", StringComparison.Ordinal))
                    .Select(File.ReadAllText));
            var expected = ReunionPackages
                .Where(package => SourceUses(source, package))
                .Order()
                .ToArray();
            var actual = XDocument
                .Load(projectPath)
                .Descendants("PackageReference")
                .Select(reference => (string?)reference.Attribute("Include"))
                .Where(package => package is not null && ReunionPackages.Contains(package))
                .Order()
                .ToArray();

            Assert.Equal(expected, actual);
        }
    }

    private static bool SourceUses(string source, string package) => package switch
    {
        "Reunion" => source.Contains("using Reunion;", StringComparison.Ordinal)
            || source.Contains("Reunion.Option`1", StringComparison.Ordinal),
        "Reunion.Errors" => source.Contains("using Reunion.Errors;", StringComparison.Ordinal),
        "Reunion.AspNetCore" => source.Contains("using Reunion.AspNetCore", StringComparison.Ordinal),
        _ => false
    };

    private static bool IsGeneratedPath(string path)
    {
        var separator = Path.DirectorySeparatorChar;
        return path.Contains($"{separator}bin{separator}", StringComparison.OrdinalIgnoreCase)
            || path.Contains($"{separator}obj{separator}", StringComparison.OrdinalIgnoreCase);
    }

    private static string FindB2BRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Concertable.B2B.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate api/Concertable.B2B.");
    }
}
