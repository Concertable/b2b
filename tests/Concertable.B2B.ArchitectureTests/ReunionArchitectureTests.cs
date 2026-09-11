using System.Xml.Linq;
using Concertable.Testing;
using Xunit;

namespace Concertable.B2B.ArchitectureTests;

public sealed class ReunionArchitectureTests
{
    private static readonly string[] ReunionPackages =
        ["Reunion", "Reunion.AspNetCore", "Reunion.Errors", "Reunion.Validation"];

    [Fact]
    public void B2BSource_LegacyResultIdentities_AreAbsent()
    {
        var oldFunctionalNamespace = "Concertable.Kernel." + "Functional";
        var oldApiResultsNamespace = "Concertable.Shared.Api." + "Results";
        var oldPackage = "Fluent" + "Results";
        var violations = FindB2BRoot()
            .EnumerateFiles("*.*", SearchOption.AllDirectories)
            .Where(file => file.Extension is ".cs" or ".csproj" or ".props")
            .Where(file => !IsGeneratedPath(file))
            .Where(file =>
            {
                var source = File.ReadAllText(file.FullName);
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
        foreach (var projectFile in FindB2BRoot()
                     .EnumerateFiles("*.csproj", SearchOption.AllDirectories)
                     .Where(file => !IsGeneratedPath(file)))
        {
            var projectDirectory = projectFile.Directory!;
            var source = string.Join(
                '\n',
                projectDirectory.EnumerateFiles("*.cs", SearchOption.AllDirectories)
                    .Where(file => !IsGeneratedPath(file))
                    .Where(file => file.Name != nameof(ReunionArchitectureTests) + ".cs")
                    .Select(file => File.ReadAllText(file.FullName)));
            var expected = ReunionPackages
                .Where(package => SourceUses(source, package))
                .Order()
                .ToArray();
            var actual = XDocument
                .Load(projectFile.FullName)
                .Descendants("PackageReference")
                .Select(reference => (string?)reference.Attribute("Include"))
                .Where(package => package is not null && ReunionPackages.Contains(package))
                .Order()
                .ToArray();

            Assert.True(
                expected.SequenceEqual(actual),
                $"{projectFile.FullName}: expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}]");
        }
    }

    private static bool SourceUses(string source, string package) => package switch
    {
        "Reunion" => source.Contains("using Reunion;", StringComparison.Ordinal)
            || source.Contains("Reunion.Option`1", StringComparison.Ordinal),
        "Reunion.Errors" => source.Contains("using Reunion.Errors;", StringComparison.Ordinal),
        "Reunion.Validation" => source.Contains("using Reunion.Validation;", StringComparison.Ordinal),
        "Reunion.AspNetCore" => source.Contains("using Reunion.AspNetCore", StringComparison.Ordinal),
        _ => false
    };

    private static bool IsGeneratedPath(FileInfo file) =>
        file.Directory!.AncestorsAndSelf().Any(ancestor => ancestor.Name is "bin" or "obj");

    private static DirectoryInfo FindB2BRoot() =>
        typeof(ReunionArchitectureTests).Assembly.SolutionDirectory;
}
