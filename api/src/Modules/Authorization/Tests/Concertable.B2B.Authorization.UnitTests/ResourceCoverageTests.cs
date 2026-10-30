namespace Concertable.B2B.Authorization.UnitTests;

public sealed class ResourceCoverageTests
{
    public static TheoryData<ResourceCoverage, ResourceCoverage, bool> Coverage => new()
    {
        { ResourceCoverage.TenantResources, ResourceCoverage.TenantResources, true },
        { ResourceCoverage.TenantResources, ResourceCoverage.AssignedResources, true },
        { ResourceCoverage.AssignedResources, ResourceCoverage.AssignedResources, true },
        { ResourceCoverage.AssignedResources, ResourceCoverage.TenantResources, false },
        { ResourceCoverage.None, ResourceCoverage.AssignedResources, false },
        { ResourceCoverage.TenantResources, ResourceCoverage.None, false },
        { (ResourceCoverage)99, ResourceCoverage.AssignedResources, false },
    };

    [Theory]
    [MemberData(nameof(Coverage))]
    public void Covers_RequiredCoverage_MatchesBreadth(ResourceCoverage held, ResourceCoverage required, bool expected)
    {
        Assert.Equal(expected, held.Covers(required));
    }
}
