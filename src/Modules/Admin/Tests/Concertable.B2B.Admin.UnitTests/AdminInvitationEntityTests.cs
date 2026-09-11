using Concertable.B2B.Admin.Domain.Entities;
using Concertable.B2B.Admin.Domain.Errors;
using Concertable.B2B.Admin.Domain.Events;
using Concertable.Kernel;

namespace Concertable.B2B.Admin.UnitTests;

public sealed class AdminInvitationEntityTests
{
    private static readonly DateTime CreatedAt = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ReturnsPendingInvitation_WithExpectedValues()
    {
        var inviter = Guid.NewGuid();

        var invitation = AdminInvitationEntity.Create("invitee@example.com", inviter, CreatedAt, TimeSpan.FromDays(7));

        Assert.NotEqual(Guid.Empty, invitation.Id);
        Assert.Equal("invitee@example.com", invitation.Email);
        Assert.Equal(AdminInvitationStatus.Pending, invitation.Status);
        Assert.Equal(inviter, invitation.CreatedByUserId);
        Assert.Equal(CreatedAt, invitation.CreatedAt);
        Assert.Equal(CreatedAt.AddDays(7), invitation.ExpiresAt);
    }

    [Fact]
    public void Create_RaisesAdminInvitationCreatedDomainEvent_CarryingInvitee()
    {
        var invitation = Create();

        var raised = Assert.IsType<AdminInvitationCreatedDomainEvent>(Assert.Single(invitation.DomainEvents));
        Assert.Equal(invitation.Id, raised.InvitationId);
        Assert.Equal("member@example.com", raised.Email);
    }

    [Fact]
    public void ClearDomainEvents_RemovesTheRaisedEvent()
    {
        var invitation = Create();

        invitation.ClearDomainEvents();

        Assert.Empty(invitation.DomainEvents);
    }

    [Fact]
    public void Accept_PendingUnexpiredInvitation_RecordsAcceptance()
    {
        var invitation = Create();
        var userId = Guid.NewGuid();
        var acceptedAt = CreatedAt.AddDays(1);

        var result = invitation.Accept(userId, acceptedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(AdminInvitationStatus.Accepted, invitation.Status);
        Assert.Equal(userId, invitation.AcceptedByUserId);
        Assert.Equal(acceptedAt, invitation.AcceptedAt);
    }

    [Fact]
    public void Accept_NonPendingInvitation_ReturnsNotPendingWithoutMutation()
    {
        var invitation = Create();
        Assert.True(invitation.Revoke().IsSuccess);

        var result = invitation.Accept(Guid.NewGuid(), CreatedAt.AddDays(1));

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<AdminInvitationAcceptanceError.NotPending>(error);
        Assert.Equal(AdminInvitationStatus.Revoked, invitation.Status);
        Assert.Null(invitation.AcceptedByUserId);
        Assert.Null(invitation.AcceptedAt);
    }

    [Fact]
    public void Accept_ExpiredInvitation_ReturnsExpiredWithoutMutation()
    {
        var invitation = Create();

        var result = invitation.Accept(Guid.NewGuid(), invitation.ExpiresAt);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<AdminInvitationAcceptanceError.Expired>(error);
        Assert.Equal(AdminInvitationStatus.Pending, invitation.Status);
        Assert.Null(invitation.AcceptedByUserId);
        Assert.Null(invitation.AcceptedAt);
    }

    [Fact]
    public void Revoke_PendingInvitation_RecordsRevocation()
    {
        var invitation = Create();

        var result = invitation.Revoke();

        Assert.True(result.IsSuccess);
        Assert.Equal(AdminInvitationStatus.Revoked, invitation.Status);
    }

    [Fact]
    public void Revoke_NonPendingInvitation_ReturnsNotPendingWithoutMutation()
    {
        var invitation = Create();
        Assert.True(invitation.Accept(Guid.NewGuid(), CreatedAt.AddDays(1)).IsSuccess);

        var result = invitation.Revoke();

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<AdminInvitationRevocationError.NotPending>(error);
        Assert.Equal(AdminInvitationStatus.Accepted, invitation.Status);
    }

    [Fact]
    public void IsActive_PendingAndUnexpired_ReturnsTrue()
    {
        var invitation = Create();

        Assert.True(invitation.IsActive(CreatedAt.AddDays(1)));
    }

    [Fact]
    public void IsActive_PendingButExpired_ReturnsFalse()
    {
        var invitation = Create();

        Assert.False(invitation.IsActive(invitation.ExpiresAt));
    }

    [Fact]
    public void Expire_PendingInvitation_MarksExpired()
    {
        var invitation = Create();

        invitation.Expire();

        Assert.Equal(AdminInvitationStatus.Expired, invitation.Status);
    }

    [Fact]
    public void Expire_NonPendingInvitation_StillThrowsInvariantException()
    {
        var invitation = Create();
        Assert.True(invitation.Revoke().IsSuccess);

        Assert.Throws<DomainException>(invitation.Expire);
    }

    private static AdminInvitationEntity Create() =>
        AdminInvitationEntity.Create("member@example.com", Guid.NewGuid(), CreatedAt, TimeSpan.FromDays(7));
}
