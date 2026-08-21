using Concertable.B2B.Admin.Application.Errors;
using Concertable.B2B.Admin.Application.Interfaces;
using Concertable.B2B.Admin.Application.Requests;
using Concertable.B2B.Admin.Domain.Entities;
using Concertable.B2B.Admin.Domain.Errors;
using Concertable.B2B.Admin.Infrastructure.Services;
using Concertable.B2B.Admin.Infrastructure.Settings;
using Concertable.B2B.User.Contracts;
using Concertable.Kernel.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Concertable.B2B.Admin.UnitTests;

public sealed class AdminServiceTests
{
    private readonly Mock<IAdminRepository> repository;
    private readonly Mock<ICurrentUser> currentUser;
    private readonly Mock<IUserModule> userModule;
    private readonly AdminService service;

    public AdminServiceTests()
    {
        this.repository = new Mock<IAdminRepository>();
        this.currentUser = new Mock<ICurrentUser>();
        this.userModule = new Mock<IUserModule>();
        this.userModule.Setup(value => value.GetEmailsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, string>());
        this.service = new AdminService(
            this.repository.Object,
            this.currentUser.Object,
            this.userModule.Object,
            TimeProvider.System,
            Options.Create(new AdminOptions()),
            NullLogger<AdminService>.Instance);
    }

    [Fact]
    public async Task RevokeAdminAsync_LastAdmin_ReturnsLastAdminWithoutRemoving()
    {
        var sub = Guid.NewGuid();
        this.repository.Setup(value => value.IsAdminAsync(sub, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        this.repository.Setup(value => value.CountAdminsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await this.service.RevokeAdminAsync(sub);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<RevokeAdminError.LastAdmin>(error);
        this.repository.Verify(value => value.RemoveAdmin(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task RevokeAdminAsync_NotAnAdmin_ReturnsAdminNotFoundWithoutRemoving()
    {
        var sub = Guid.NewGuid();
        this.repository.Setup(value => value.IsAdminAsync(sub, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await this.service.RevokeAdminAsync(sub);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<RevokeAdminError.AdminNotFound>(error);
        this.repository.Verify(value => value.RemoveAdmin(It.IsAny<Guid>()), Times.Never);
        this.repository.Verify(value => value.CountAdminsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RevokeAdminAsync_NotLastAdmin_RemovesAndSaves()
    {
        var sub = Guid.NewGuid();
        this.repository.Setup(value => value.IsAdminAsync(sub, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        this.repository.Setup(value => value.CountAdminsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var result = await this.service.RevokeAdminAsync(sub);

        Assert.True(result.IsSuccess);
        this.repository.Verify(value => value.RemoveAdmin(sub), Times.Once);
        this.repository.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InviteAsync_EmailAlreadyBelongsToAnAdmin_ReturnsAlreadyAdminWithoutCreatingInvitation()
    {
        var adminSub = Guid.NewGuid();
        this.repository.Setup(value => value.ListAdminSubsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([adminSub]);
        this.userModule.Setup(value => value.GetEmailsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, string> { [adminSub] = "admin@example.com" });

        var result = await this.service.InviteAsync(new CreateAdminInvitationRequest { Email = "Admin@Example.com" });

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<InviteAdminError.AlreadyAdmin>(error);
        this.repository.Verify(value => value.InsertAsync(It.IsAny<AdminInvitationEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InviteAsync_PendingInvitationAlreadyExists_ReturnsInvitationPendingWithoutCreatingAnother()
    {
        this.repository.Setup(value => value.ListAdminSubsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var existing = AdminInvitationEntity.Create("invitee@example.com", Guid.NewGuid(), DateTime.UtcNow, TimeSpan.FromDays(7));
        this.repository.Setup(value => value.GetPendingInvitationByEmailAsync("invitee@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await this.service.InviteAsync(new CreateAdminInvitationRequest { Email = "invitee@example.com" });

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<InviteAdminError.InvitationPending>(error);
        this.repository.Verify(value => value.InsertAsync(It.IsAny<AdminInvitationEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InviteAsync_UnauthenticatedUser_ReturnsForbiddenWithoutCreatingInvitation()
    {
        this.repository.Setup(value => value.ListAdminSubsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await this.service.InviteAsync(new CreateAdminInvitationRequest { Email = "invitee@example.com" });

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<InviteAdminError.Unauthenticated>(error);
        this.repository.Verify(value => value.InsertAsync(It.IsAny<AdminInvitationEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InviteAsync_NoConflict_CreatesInvitationAndSaves()
    {
        var inviterId = Guid.NewGuid();
        this.currentUser.SetupGet(user => user.Id).Returns(inviterId);
        this.repository.Setup(value => value.ListAdminSubsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await this.service.InviteAsync(new CreateAdminInvitationRequest { Email = "invitee@example.com" });

        Assert.True(result.TryGetValue(out var dto));
        Assert.Equal("invitee@example.com", dto.Email);
        this.repository.Verify(value => value.InsertAsync(It.IsAny<AdminInvitationEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RevokeInvitationAsync_InvitationNotFound_ReturnsInvitationNotFound()
    {
        var id = Guid.NewGuid();
        this.repository.Setup(value => value.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminInvitationEntity?)null);

        var result = await this.service.RevokeInvitationAsync(id);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<RevokeAdminInvitationError.InvitationNotFound>(error);
        this.repository.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RevokeInvitationAsync_AlreadyAccepted_ReturnsInvitationNotPendingWithoutSaving()
    {
        var invitation = AdminInvitationEntity.Create("invitee@example.com", Guid.NewGuid(), DateTime.UtcNow, TimeSpan.FromDays(7));
        invitation.Accept(Guid.NewGuid(), DateTime.UtcNow);
        this.repository.Setup(value => value.GetByIdAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var result = await this.service.RevokeInvitationAsync(invitation.Id);

        Assert.True(result.TryGetError(out var error));
        var revocationFailed = Assert.IsType<RevokeAdminInvitationError.RevocationFailed>(error);
        Assert.IsType<AdminInvitationRevocationError.NotPending>(revocationFailed.Error);
        this.repository.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RevokeInvitationAsync_Pending_RevokesAndSaves()
    {
        var invitation = AdminInvitationEntity.Create("invitee@example.com", Guid.NewGuid(), DateTime.UtcNow, TimeSpan.FromDays(7));
        this.repository.Setup(value => value.GetByIdAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var result = await this.service.RevokeInvitationAsync(invitation.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(AdminInvitationStatus.Revoked, invitation.Status);
        this.repository.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsCurrentUserAdminAsync_NoCurrentUser_ReturnsFalseWithoutQuerying()
    {
        this.currentUser.SetupGet(user => user.Id).Returns((Guid?)null);

        var result = await this.service.IsCurrentUserAdminAsync();

        Assert.False(result);
        this.repository.Verify(value => value.IsAdminAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IsCurrentUserAdminAsync_CurrentUserIsAdmin_ReturnsTrue()
    {
        var sub = Guid.NewGuid();
        this.currentUser.SetupGet(user => user.Id).Returns(sub);
        this.repository.Setup(value => value.IsAdminAsync(sub, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await this.service.IsCurrentUserAdminAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task GetOverviewAsync_JoinsAdminEmailsAndPendingInvitations()
    {
        var adminSub = Guid.NewGuid();
        this.repository.Setup(value => value.ListAdminSubsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([adminSub]);
        this.userModule.Setup(value => value.GetEmailsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, string> { [adminSub] = "admin@example.com" });
        var invitation = AdminInvitationEntity.Create("invitee@example.com", Guid.NewGuid(), DateTime.UtcNow, TimeSpan.FromDays(7));
        this.repository.Setup(value => value.ListPendingInvitationsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([invitation]);

        var overview = await this.service.GetOverviewAsync();

        var admin = Assert.Single(overview.Admins);
        Assert.Equal(adminSub, admin.Sub);
        Assert.Equal("admin@example.com", admin.Email);
        var pending = Assert.Single(overview.PendingInvitations);
        Assert.Equal(invitation.Id, pending.Id);
    }

}
