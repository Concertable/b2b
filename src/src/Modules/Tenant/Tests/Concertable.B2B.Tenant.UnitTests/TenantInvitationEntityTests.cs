using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.B2B.Tenant.Domain.Events;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class TenantInvitationEntityTests
{
    [Fact]
    public void Create_ReturnsPendingInvitation_WithExpectedValues()
    {
        var tenantId = Guid.NewGuid();
        var inviter = Guid.NewGuid();
        var at = DateTime.UtcNow;

        var invitation = TenantInvitationEntity.Create(
            tenantId, TenantType.Venue, "invitee@example.com", TenantRole.Manager, inviter, at, TimeSpan.FromDays(7));

        Assert.NotEqual(Guid.Empty, invitation.Id);
        Assert.Equal(tenantId, invitation.TenantId);
        Assert.Equal("invitee@example.com", invitation.Email);
        Assert.Equal(TenantRole.Manager, invitation.Role);
        Assert.Equal(InvitationStatus.Pending, invitation.Status);
        Assert.Equal(inviter, invitation.CreatedByUserId);
        Assert.Equal(at, invitation.CreatedAt);
        Assert.Equal(at.AddDays(7), invitation.ExpiresAt);
    }

    [Fact]
    public void Create_RaisesInvitationCreatedDomainEvent_CarryingInviteeAndPortalType()
    {
        var invitation = TenantInvitationEntity.Create(
            Guid.NewGuid(), TenantType.Artist, "invitee@example.com", TenantRole.Staff, Guid.NewGuid(), DateTime.UtcNow, TimeSpan.FromDays(7));

        var raised = Assert.IsType<TenantInvitationCreatedDomainEvent>(Assert.Single(invitation.DomainEvents));
        Assert.Equal(invitation.Id, raised.InvitationId);
        Assert.Equal("invitee@example.com", raised.Email);
        Assert.Equal(TenantRole.Staff, raised.Role);
        Assert.Equal(TenantType.Artist, raised.TenantType);
    }

    [Fact]
    public void ClearDomainEvents_RemovesTheRaisedEvent()
    {
        var invitation = TenantInvitationEntity.Create(
            Guid.NewGuid(), TenantType.Venue, "invitee@example.com", TenantRole.Manager, Guid.NewGuid(), DateTime.UtcNow, TimeSpan.FromDays(7));

        invitation.ClearDomainEvents();

        Assert.Empty(invitation.DomainEvents);
    }
}
