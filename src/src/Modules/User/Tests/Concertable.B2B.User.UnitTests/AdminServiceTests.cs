using Concertable.B2B.User.Application.Errors;
using Concertable.B2B.User.Application.Interfaces;
using Concertable.B2B.User.Application.Requests;
using Concertable.B2B.User.Contracts;
using Concertable.B2B.User.Domain.Entities;
using Concertable.B2B.User.Domain.Errors;
using Concertable.B2B.User.Infrastructure.Services;
using Concertable.Kernel.Identity;
using Moq;

namespace Concertable.B2B.User.UnitTests;

public sealed class AdminServiceTests
{
    private readonly Mock<IAdminRepository> repository = new();
    private readonly Mock<ICurrentUser> currentUser = new();
    private readonly Mock<IUserModule> userModule = new();

    public AdminServiceTests()
    {
        userModule.Setup(value => value.GetEmailsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, string>());
    }

    [Fact]
    public async Task RevokeAdminAsync_LastAdmin_ReturnsLastAdminWithoutRemoving()
    {
        var sub = Guid.NewGuid();
        repository.Setup(value => value.IsAdminAsync(sub, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(value => value.CountAdminsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await CreateService().RevokeAdminAsync(sub);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<RevokeAdminError.LastAdmin>(error);
        repository.Verify(value => value.RemoveAdmin(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task RevokeAdminAsync_NotAnAdmin_ReturnsAdminNotFoundWithoutRemoving()
    {
        var sub = Guid.NewGuid();
        repository.Setup(value => value.IsAdminAsync(sub, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateService().RevokeAdminAsync(sub);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<RevokeAdminError.AdminNotFound>(error);
        repository.Verify(value => value.RemoveAdmin(It.IsAny<Guid>()), Times.Never);
        repository.Verify(value => value.CountAdminsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RevokeAdminAsync_NotLastAdmin_RemovesAndSaves()
    {
        var sub = Guid.NewGuid();
        repository.Setup(value => value.IsAdminAsync(sub, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(value => value.CountAdminsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var result = await CreateService().RevokeAdminAsync(sub);

        Assert.True(result.IsSuccess);
        repository.Verify(value => value.RemoveAdmin(sub), Times.Once);
        repository.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InviteAsync_EmailAlreadyBelongsToAnAdmin_ReturnsAlreadyAdminWithoutCreatingInvitation()
    {
        var adminSub = Guid.NewGuid();
        repository.Setup(value => value.ListAdminSubsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([adminSub]);
        userModule.Setup(value => value.GetEmailsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, string> { [adminSub] = "admin@example.com" });

        var result = await CreateService().InviteAsync(new CreateAdminInvitationRequest { Email = "Admin@Example.com" });

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<InviteAdminError.AlreadyAdmin>(error);
        repository.Verify(value => value.InsertAsync(It.IsAny<AdminInvitationEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InviteAsync_PendingInvitationAlreadyExists_ReturnsInvitationPendingWithoutCreatingAnother()
    {
        repository.Setup(value => value.ListAdminSubsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var existing = AdminInvitationEntity.Create("invitee@example.com", Guid.NewGuid(), DateTime.UtcNow, TimeSpan.FromDays(7));
        repository.Setup(value => value.GetPendingInvitationByEmailAsync("invitee@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await CreateService().InviteAsync(new CreateAdminInvitationRequest { Email = "invitee@example.com" });

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<InviteAdminError.InvitationPending>(error);
        repository.Verify(value => value.InsertAsync(It.IsAny<AdminInvitationEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InviteAsync_UnauthenticatedUser_ReturnsForbiddenWithoutCreatingInvitation()
    {
        repository.Setup(value => value.ListAdminSubsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await CreateService().InviteAsync(new CreateAdminInvitationRequest { Email = "invitee@example.com" });

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<InviteAdminError.Unauthenticated>(error);
        repository.Verify(value => value.InsertAsync(It.IsAny<AdminInvitationEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InviteAsync_NoConflict_CreatesInvitationAndSaves()
    {
        var inviterId = Guid.NewGuid();
        currentUser.SetupGet(user => user.Id).Returns(inviterId);
        repository.Setup(value => value.ListAdminSubsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await CreateService().InviteAsync(new CreateAdminInvitationRequest { Email = "invitee@example.com" });

        Assert.True(result.TryGetValue(out var dto));
        Assert.Equal("invitee@example.com", dto.Email);
        repository.Verify(value => value.InsertAsync(It.IsAny<AdminInvitationEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeInvitationAsync_InvitationNotFound_ReturnsInvitationNotFound()
    {
        var id = Guid.NewGuid();
        repository.Setup(value => value.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminInvitationEntity?)null);

        var result = await CreateService().RevokeInvitationAsync(id);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<RevokeAdminInvitationError.InvitationNotFound>(error);
        repository.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RevokeInvitationAsync_AlreadyAccepted_ReturnsInvitationNotPendingWithoutSaving()
    {
        var invitation = AdminInvitationEntity.Create("invitee@example.com", Guid.NewGuid(), DateTime.UtcNow, TimeSpan.FromDays(7));
        invitation.Accept(Guid.NewGuid(), DateTime.UtcNow);
        repository.Setup(value => value.GetByIdAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var result = await CreateService().RevokeInvitationAsync(invitation.Id);

        Assert.True(result.TryGetError(out var error));
        var revocationFailed = Assert.IsType<RevokeAdminInvitationError.RevocationFailed>(error);
        Assert.IsType<AdminInvitationRevocationError.NotPending>(revocationFailed.Error);
        repository.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RevokeInvitationAsync_Pending_RevokesAndSaves()
    {
        var invitation = AdminInvitationEntity.Create("invitee@example.com", Guid.NewGuid(), DateTime.UtcNow, TimeSpan.FromDays(7));
        repository.Setup(value => value.GetByIdAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var result = await CreateService().RevokeInvitationAsync(invitation.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(AdminInvitationStatus.Revoked, invitation.Status);
        repository.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsCurrentUserAdminAsync_NoCurrentUser_ReturnsFalseWithoutQuerying()
    {
        currentUser.SetupGet(user => user.Id).Returns((Guid?)null);

        var result = await CreateService().IsCurrentUserAdminAsync();

        Assert.False(result);
        repository.Verify(value => value.IsAdminAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IsCurrentUserAdminAsync_CurrentUserIsAdmin_ReturnsTrue()
    {
        var sub = Guid.NewGuid();
        currentUser.SetupGet(user => user.Id).Returns(sub);
        repository.Setup(value => value.IsAdminAsync(sub, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateService().IsCurrentUserAdminAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task GetOverviewAsync_JoinsAdminEmailsAndPendingInvitations()
    {
        var adminSub = Guid.NewGuid();
        repository.Setup(value => value.ListAdminSubsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([adminSub]);
        userModule.Setup(value => value.GetEmailsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, string> { [adminSub] = "admin@example.com" });
        var invitation = AdminInvitationEntity.Create("invitee@example.com", Guid.NewGuid(), DateTime.UtcNow, TimeSpan.FromDays(7));
        repository.Setup(value => value.ListPendingInvitationsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([invitation]);

        var overview = await CreateService().GetOverviewAsync();

        var admin = Assert.Single(overview.Admins);
        Assert.Equal(adminSub, admin.Sub);
        Assert.Equal("admin@example.com", admin.Email);
        var pending = Assert.Single(overview.PendingInvitations);
        Assert.Equal(invitation.Id, pending.Id);
    }

    private AdminService CreateService() => new(
        repository.Object,
        currentUser.Object,
        userModule.Object,
        TimeProvider.System);
}
