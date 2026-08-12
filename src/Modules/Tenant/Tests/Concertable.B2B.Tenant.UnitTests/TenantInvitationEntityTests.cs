using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.B2B.Tenant.Domain.Errors;
using Concertable.Kernel;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class TenantInvitationEntityTests
{
    private static readonly DateTime CreatedAt = new(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Accept_PendingUnexpiredInvitation_RecordsAcceptance()
    {
        var invitation = Create();
        var userId = Guid.NewGuid();
        var acceptedAt = CreatedAt.AddDays(1);

        var result = invitation.Accept(userId, acceptedAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
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
        Assert.IsType<InvitationAcceptanceError.NotPending>(error);
        Assert.Equal(InvitationStatus.Revoked, invitation.Status);
        Assert.Null(invitation.AcceptedByUserId);
        Assert.Null(invitation.AcceptedAt);
    }

    [Fact]
    public void Accept_ExpiredInvitation_ReturnsExpiredWithoutMutation()
    {
        var invitation = Create();

        var result = invitation.Accept(Guid.NewGuid(), invitation.ExpiresAt);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<InvitationAcceptanceError.Expired>(error);
        Assert.Equal(InvitationStatus.Pending, invitation.Status);
        Assert.Null(invitation.AcceptedByUserId);
        Assert.Null(invitation.AcceptedAt);
    }

    [Fact]
    public void Revoke_PendingInvitation_RecordsRevocation()
    {
        var invitation = Create();

        var result = invitation.Revoke();

        Assert.True(result.IsSuccess);
        Assert.Equal(InvitationStatus.Revoked, invitation.Status);
    }

    [Fact]
    public void Revoke_NonPendingInvitation_ReturnsNotPendingWithoutMutation()
    {
        var invitation = Create();
        Assert.True(invitation.Accept(Guid.NewGuid(), CreatedAt.AddDays(1)).IsSuccess);

        var result = invitation.Revoke();

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<InvitationRevocationError.NotPending>(error);
        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
    }

    [Fact]
    public void Expire_NonPendingInvitation_StillThrowsInvariantException()
    {
        var invitation = Create();
        Assert.True(invitation.Revoke().IsSuccess);

        Assert.Throws<DomainException>(invitation.Expire);
    }

    private static TenantInvitationEntity Create() => TenantInvitationEntity.Create(
        Guid.NewGuid(),
        "member@example.com",
        TenantRole.Staff,
        Guid.NewGuid(),
        CreatedAt,
        TimeSpan.FromDays(7));
}
