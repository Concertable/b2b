using System.Collections.Immutable;
using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Authorization.UnitTests;

public sealed class MembershipSnapshotTests
{
    private readonly MembershipSnapshot actor = new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Guid.Parse("22222222-2222-2222-2222-222222222222"),
        Guid.Parse("33333333-3333-3333-3333-333333333333"), 3, 5,
        ImmutableDictionary<TenantPermission, ResourceAudience>.Empty.Add(
            TenantPermission.OperationsView, ResourceAudience.TenantResources));

    [Theory]
    [InlineData(ResourceAudience.AssignedResources)]
    [InlineData(ResourceAudience.TenantResources)]
    public void HasPermission_ResourceReadPermission_AllowsEitherGrantedAudience(ResourceAudience audience)
    {
        var reader = actor with { Permissions = actor.Permissions.Add(TenantPermission.TermsRead, audience) };

        Assert.True(reader.HasPermission(TenantPermission.TermsRead));
    }

    [Theory]
    [InlineData(ResourceAudience.AssignedResources, false)]
    [InlineData(ResourceAudience.TenantResources, true)]
    [InlineData(ResourceAudience.None, false)]
    public void AudienceFor_CreationPermission_RequiresTenantAudience(ResourceAudience audience, bool expected)
    {
        var creator = actor with
        {
            Permissions = actor.Permissions.Add(TenantPermission.ApplicationsSubmit, audience),
        };

        Assert.Equal(expected, creator.AudienceFor(TenantPermission.ApplicationsSubmit)
            .Covers(ResourceAudience.TenantResources));
    }

    [Theory]
    [InlineData(ResourceAudience.None)]
    [InlineData((ResourceAudience)99)]
    public void Covers_InvalidRequiredAudience_IsNeverSatisfied(ResourceAudience required)
    {
        Assert.False(actor.AudienceFor(TenantPermission.OperationsView).Covers(required));
    }

    [Fact]
    public void HasSameAuthorityAs_PermissionAddedUnderANewVersion_IsAnotherAuthority()
    {
        var changed = actor with
        {
            PermissionVersion = actor.PermissionVersion + 1,
            Permissions = actor.Permissions.Add(
                TenantPermission.ApplicationsSubmit, ResourceAudience.TenantResources),
        };

        Assert.False(actor.HasSameAuthorityAs(changed));
        Assert.True(actor.HasSameAuthorityAs(actor with { }));
    }
}
