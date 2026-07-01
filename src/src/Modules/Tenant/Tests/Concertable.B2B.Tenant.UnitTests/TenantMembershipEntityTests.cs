using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class TenantMembershipEntityTests
{
    [Fact]
    public void Create_FoundingOwner_HasNoInviter()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var membership = TenantMembershipEntity.Create(tenantId, userId, TenantRole.Owner, invitedBy: null, now);

        Assert.NotEqual(Guid.Empty, membership.Id);
        Assert.Equal(tenantId, membership.TenantId);
        Assert.Equal(userId, membership.UserId);
        Assert.Equal(TenantRole.Owner, membership.Role);
        Assert.Null(membership.InvitedByUserId);
        Assert.Equal(now, membership.CreatedAt);
    }

    [Fact]
    public void Create_Invited_RecordsTheInviter()
    {
        var invitedBy = Guid.NewGuid();

        var membership = TenantMembershipEntity.Create(
            Guid.NewGuid(), Guid.NewGuid(), TenantRole.Manager, invitedBy, DateTime.UtcNow);

        Assert.Equal(invitedBy, membership.InvitedByUserId);
    }

    [Fact]
    public void ChangeRole_UpdatesTheRole()
    {
        var membership = TenantMembershipEntity.Create(
            Guid.NewGuid(), Guid.NewGuid(), TenantRole.Staff, invitedBy: null, DateTime.UtcNow);

        membership.ChangeRole(TenantRole.Finance);

        Assert.Equal(TenantRole.Finance, membership.Role);
    }
}
