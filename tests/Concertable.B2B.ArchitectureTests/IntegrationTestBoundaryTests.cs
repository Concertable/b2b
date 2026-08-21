using System.Xml.Linq;
using Xunit;

namespace Concertable.B2B.ArchitectureTests;

public sealed class IntegrationTestBoundaryTests
{
    [Fact]
    public void ModuleIntegrationProjects_DoNotReferenceAnotherModulesDomainOrInfrastructure()
    {
        var violations = Directory
            .EnumerateFiles(FindB2BRoot(), "*.IntegrationTests.csproj", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}Modules{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .SelectMany(FindCrossModuleReferences)
            .Order()
            .ToArray();

        Assert.Empty(violations);
    }

    private static IEnumerable<string> FindCrossModuleReferences(string projectPath)
    {
        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        var owner = projectName.Split('.')[2];
        var projectDirectory = Path.GetDirectoryName(projectPath)!;

        foreach (var reference in XDocument.Load(projectPath).Descendants("ProjectReference"))
        {
            var include = (string?)reference.Attribute("Include");
            if (include is null)
                continue;

            var referencedName = Path.GetFileNameWithoutExtension(Path.GetFullPath(include, projectDirectory));
            var parts = referencedName.Split('.');
            if (parts.Length < 4 || parts[0] != "Concertable" || parts[1] != "B2B")
                continue;

            var referencedModule = parts[2];
            var referencedLayer = parts[3];
            if (referencedModule != owner && referencedLayer is "Domain" or "Infrastructure")
                yield return $"{projectName} -> {referencedName}";
        }
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
