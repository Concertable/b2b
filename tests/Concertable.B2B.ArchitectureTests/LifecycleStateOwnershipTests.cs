using System.Reflection;
using System.Text.RegularExpressions;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Testing;
using Xunit;

namespace Concertable.B2B.ArchitectureTests;

public sealed partial class LifecycleStateOwnershipTests
{
    private static readonly (Type Aggregate, string SourcePath)[] Aggregates =
    [
        (typeof(ApplicationEntity),
            "src/Modules/Application/Concertable.B2B.Application.Domain/Entities/ApplicationEntity.cs"),
        (typeof(BookingEntity),
            "src/Modules/Booking/Concertable.B2B.Booking.Domain/Entities/BookingEntity.cs"),
        (typeof(ConcertEntity),
            "src/Modules/Concert/Concertable.B2B.Concert.Domain/Entities/ConcertEntity.cs")
    ];

    [Fact]
    public void LifecycleState_IsAssignedOnlyByOwningAggregateTransition()
    {
        foreach (var (aggregate, sourcePath) in Aggregates)
        {
            var property = aggregate.GetProperty(
                "State",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(property);
            Assert.True(property.SetMethod!.IsPrivate, $"{aggregate.FullName}.State must have a private setter.");

            var source = File.ReadAllText(Path.Combine(FindB2BRoot().FullName, sourcePath));
            Assert.Single(StateAssignment().Matches(source).Cast<Match>());
        }
    }

    [Fact]
    public void LifecycleModules_DoNotBypassAggregateStateTransitionsWithBulkUpdates()
    {
        var violations = new[] { "Application", "Booking", "Concert" }
            .Select(module => new DirectoryInfo(Path.Combine(
                FindB2BRoot().FullName,
                "src",
                "Modules",
                module)))
            .SelectMany(directory => directory
                .EnumerateFiles("*.cs", SearchOption.AllDirectories)
                .Where(file => !file.Directory!.AncestorsAndSelf().Any(ancestor => ancestor.Name is "bin" or "obj")))
            .Where(file => BulkStateAssignment().IsMatch(File.ReadAllText(file.FullName)))
            .Select(file => file.FullName)
            .ToArray();

        Assert.Empty(violations);
    }

    private static DirectoryInfo FindB2BRoot() =>
        typeof(LifecycleStateOwnershipTests).Assembly.SolutionDirectory;

    [GeneratedRegex(@"(?m)^\s*State\s*=\s*next\s*;")]
    private static partial Regex StateAssignment();

    [GeneratedRegex(@"SetProperty\s*\([^;]*?=>\s*\w+\.State\b", RegexOptions.Singleline)]
    private static partial Regex BulkStateAssignment();
}
