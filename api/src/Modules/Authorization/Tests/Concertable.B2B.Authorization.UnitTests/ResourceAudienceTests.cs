namespace Concertable.B2B.Authorization.UnitTests;

public sealed class ResourceAudienceTests
{
    public static TheoryData<ResourceAudience, ResourceAudience, bool> Coverage => new()
    {
        { ResourceAudience.TenantResources, ResourceAudience.TenantResources, true },
        { ResourceAudience.TenantResources, ResourceAudience.AssignedResources, true },
        { ResourceAudience.AssignedResources, ResourceAudience.AssignedResources, true },
        { ResourceAudience.AssignedResources, ResourceAudience.TenantResources, false },
        { ResourceAudience.None, ResourceAudience.AssignedResources, false },
        { ResourceAudience.TenantResources, ResourceAudience.None, false },
        { (ResourceAudience)99, ResourceAudience.AssignedResources, false },
    };

    [Theory]
    [MemberData(nameof(Coverage))]
    public void Covers_RequiredAudience_MatchesBreadth(ResourceAudience held, ResourceAudience required, bool expected)
    {
        Assert.Equal(expected, held.Covers(required));
    }
}
