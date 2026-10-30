using System.Collections.Immutable;
using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Authorization.UnitTests;

public sealed class MembershipSnapshotTests
{
    private readonly MembershipSnapshot actor = new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Guid.Parse("22222222-2222-2222-2222-222222222222"),
        Guid.Parse("33333333-3333-3333-3333-333333333333"), 3, 5,
        ImmutableDictionary<TenantPermission, ResourceCoverage>.Empty.Add(
            TenantPermission.OperationsView, ResourceCoverage.TenantResources));

    [Theory]
    [InlineData(ResourceCoverage.AssignedResources)]
    [InlineData(ResourceCoverage.TenantResources)]
    public void HasPermission_ResourceReadPermission_AllowsEitherGrantedCoverage(ResourceCoverage coverage)
    {
        var reader = actor with { Permissions = actor.Permissions.Add(TenantPermission.TermsRead, coverage) };

        Assert.True(reader.HasPermission(TenantPermission.TermsRead));
    }

    [Theory]
    [InlineData(ResourceCoverage.AssignedResources, false)]
    [InlineData(ResourceCoverage.TenantResources, true)]
    [InlineData(ResourceCoverage.None, false)]
    public void CoverageFor_CreationPermission_RequiresTenantCoverage(ResourceCoverage coverage, bool expected)
    {
        var creator = actor with
        {
            Permissions = actor.Permissions.Add(TenantPermission.ApplicationsSubmit, coverage),
        };

        Assert.Equal(expected, creator.CoverageFor(TenantPermission.ApplicationsSubmit)
            .Covers(ResourceCoverage.TenantResources));
    }

    [Theory]
    [InlineData(ResourceCoverage.None)]
    [InlineData((ResourceCoverage)99)]
    public void Covers_InvalidRequiredCoverage_IsNeverSatisfied(ResourceCoverage required)
    {
        Assert.False(actor.CoverageFor(TenantPermission.OperationsView).Covers(required));
    }

    [Fact]
    public void HasSameAuthorityAs_PermissionAddedUnderANewVersion_IsAnotherAuthority()
    {
        var changed = actor with
        {
            PermissionVersion = actor.PermissionVersion + 1,
            Permissions = actor.Permissions.Add(
                TenantPermission.ApplicationsSubmit, ResourceCoverage.TenantResources),
        };

        Assert.False(actor.HasSameAuthorityAs(changed));
        Assert.True(actor.HasSameAuthorityAs(actor with { }));
    }
}
