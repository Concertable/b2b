using System.Xml.Linq;
using Concertable.Testing;
using Xunit;

namespace Concertable.B2B.ArchitectureTests;

public sealed class LayeringArchitectureTests
{
    private static readonly string[] ModulesPendingVocabularyMigration =
    [
        "Concertable.B2B.Admin.Domain",
        "Concertable.B2B.Application.Domain",
        "Concertable.B2B.Artist.Domain",
        "Concertable.B2B.Booking.Domain",
        "Concertable.B2B.Concert.Domain",
        "Concertable.B2B.Conversations.Domain",
        "Concertable.B2B.Opportunity.Domain",
        "Concertable.B2B.Tenant.Domain",
        "Concertable.B2B.User.Domain",
        "Concertable.B2B.Venue.Domain"
    ];

    [Fact]
    public void DomainProjects_ReferenceNoContractsProject_ExceptThosePendingMigration()
    {
        var violations = DomainProjects()
            .Where(project => ReferencedContractsProjects(project).Length > 0)
            .Select(project => Path.GetFileNameWithoutExtension(project.Name))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ModulesPendingVocabularyMigration, violations);
    }

    private static FileInfo[] DomainProjects() =>
        FindB2BRoot()
            .EnumerateFiles("*.Domain.csproj", SearchOption.AllDirectories)
            .Where(file => !IsGeneratedPath(file))
            .ToArray();

    private static string[] ReferencedContractsProjects(FileInfo project) =>
        XDocument
            .Load(project.FullName)
            .Descendants("ProjectReference")
            .Select(reference => (string?)reference.Attribute("Include"))
            .Where(path => path is not null)
            .Select(path => Path.GetFileNameWithoutExtension(path!))
            .Where(name => name.EndsWith(".Contracts", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static bool IsGeneratedPath(FileInfo file) =>
        file.Directory!.AncestorsAndSelf().Any(ancestor => ancestor.Name is "bin" or "obj");

    private static DirectoryInfo FindB2BRoot() =>
        typeof(LayeringArchitectureTests).Assembly.SolutionDirectory;
}
