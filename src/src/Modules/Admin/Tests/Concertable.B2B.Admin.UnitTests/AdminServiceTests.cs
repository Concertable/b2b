using Concertable.B2B.Admin.Application.Errors;
using Concertable.B2B.Admin.Application.Interfaces;
using Concertable.B2B.Admin.Application.Requests;
using Concertable.B2B.Admin.Domain.Entities;
using Concertable.B2B.Admin.Domain.Errors;
using Concertable.B2B.Admin.Infrastructure.Services;
using Concertable.B2B.Admin.Infrastructure.Settings;
using Concertable.B2B.User.Contracts;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Concertable.B2B.Admin.UnitTests;

public sealed class AdminServiceTests
{
    private const string BootstrapEmail = "admin@bootstrap.test";

    private readonly Mock<IAdminInvitationRepository> invitationRepository;
    private readonly Mock<IAdminProfileRepository> profileRepository;
    private readonly Mock<ICurrentUser> currentUser;
    private readonly Mock<IUserModule> userModule;
    private readonly AdminService service;

    public AdminServiceTests()
    {
        this.invitationRepository = new Mock<IAdminInvitationRepository>();
        this.profileRepository = new Mock<IAdminProfileRepository>();
        this.currentUser = new Mock<ICurrentUser>();
        this.userModule = new Mock<IUserModule>();
        this.userModule
            .Setup(value => value.GetEmailsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, string>());
        this.service = new AdminService(
            invitationRepository.Object,
            profileRepository.Object,
            currentUser.Object,
            userModule.Object,
            TimeProvider.System,
            Options.Create(new AdminOptions { BootstrapEmail = BootstrapEmail }),
            NullLogger<AdminService>.Instance);
    }

    #region RevokeAdminAsync

    [Fact]
    public async Task RevokeAdminAsync_LastAdmin_ReturnsLastAdminWithoutRemoving()
    {
        var sub = Guid.NewGuid();
        profileRepository.Setup(value => value.IsAdminAsync(sub, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        profileRepository.Setup(value => value.CountAdminsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await service.RevokeAdminAsync(sub);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<RevokeAdminError.LastAdmin>(error);
        profileRepository.Verify(value => value.RemoveAdmin(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task RevokeAdminAsync_NotAnAdmin_ReturnsAdminNotFoundWithoutRemoving()
    {
        var sub = Guid.NewGuid();
        profileRepository.Setup(value => value.IsAdminAsync(sub, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await service.RevokeAdminAsync(sub);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<RevokeAdminError.AdminNotFound>(error);
        profileRepository.Verify(value => value.RemoveAdmin(It.IsAny<Guid>()), Times.Never);
        profileRepository.Verify(value => value.CountAdminsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RevokeAdminAsync_NotLastAdmin_RemovesAndSaves()
    {
        var sub = Guid.NewGuid();
        profileRepository.Setup(value => value.IsAdminAsync(sub, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        profileRepository.Setup(value => value.CountAdminsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);

        var result = await service.RevokeAdminAsync(sub);

        Assert.True(result.IsSuccess);
        profileRepository.Verify(value => value.RemoveAdmin(sub), Times.Once);
        invitationRepository.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region InviteAsync

    [Fact]
    public async Task InviteAsync_EmailAlreadyBelongsToAnAdmin_ReturnsAlreadyAdminWithoutCreatingInvitation()
    {
        var adminId = Guid.NewGuid();
        userModule.Setup(value => value.GetIdByEmailAsync("admin@example.com")).ReturnsAsync(adminId);
        profileRepository.Setup(value => value.IsAdminAsync(adminId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await service.InviteAsync(new CreateAdminInvitationRequest { Email = "Admin@Example.com" });

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<InviteAdminError.AlreadyAdmin>(error);
        invitationRepository.Verify(value => value.InsertAsync(It.IsAny<AdminInvitationEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InviteAsync_EmailBelongsToANonAdminUser_DoesNotReturnAlreadyAdmin()
    {
        var userId = Guid.NewGuid();
        userModule.Setup(value => value.GetIdByEmailAsync("invitee@example.com")).ReturnsAsync(userId);
        profileRepository.Setup(value => value.IsAdminAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        currentUser.SetupGet(user => user.Id).Returns(Guid.NewGuid());

        var result = await service.InviteAsync(new CreateAdminInvitationRequest { Email = "invitee@example.com" });

        Assert.False(result.TryGetError(out var error) && error is InviteAdminError.AlreadyAdmin);
    }

    [Fact]
    public async Task InviteAsync_PendingInvitationAlreadyExists_ReturnsInvitationPendingWithoutCreatingAnother()
    {
        var existing = AdminInvitationEntity.Create("invitee@example.com", Guid.NewGuid(), DateTime.UtcNow, TimeSpan.FromDays(7));
        invitationRepository.Setup(value => value.GetPendingInvitationByEmailAsync("invitee@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await service.InviteAsync(new CreateAdminInvitationRequest { Email = "invitee@example.com" });

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<InviteAdminError.InvitationPending>(error);
        invitationRepository.Verify(value => value.InsertAsync(It.IsAny<AdminInvitationEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InviteAsync_UnauthenticatedUser_ReturnsForbiddenWithoutCreatingInvitation()
    {
        var result = await service.InviteAsync(new CreateAdminInvitationRequest { Email = "invitee@example.com" });

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<InviteAdminError.Unauthenticated>(error);
        invitationRepository.Verify(value => value.InsertAsync(It.IsAny<AdminInvitationEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InviteAsync_NoConflict_CreatesInvitationAndSaves()
    {
        var inviterId = Guid.NewGuid();
        currentUser.SetupGet(user => user.Id).Returns(inviterId);

        var result = await service.InviteAsync(new CreateAdminInvitationRequest { Email = "invitee@example.com" });

        Assert.True(result.TryGetValue(out var dto));
        Assert.Equal("invitee@example.com", dto.Email);
        invitationRepository.Verify(value => value.InsertAsync(It.IsAny<AdminInvitationEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region RevokeInvitationAsync

    [Fact]
    public async Task RevokeInvitationAsync_InvitationNotFound_ReturnsInvitationNotFound()
    {
        var id = Guid.NewGuid();
        invitationRepository.Setup(value => value.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminInvitationEntity?)null);

        var result = await service.RevokeInvitationAsync(id);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<RevokeAdminInvitationError.InvitationNotFound>(error);
        invitationRepository.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RevokeInvitationAsync_AlreadyAccepted_ReturnsInvitationNotPendingWithoutSaving()
    {
        var invitation = AdminInvitationEntity.Create("invitee@example.com", Guid.NewGuid(), DateTime.UtcNow, TimeSpan.FromDays(7));
        invitation.Accept(Guid.NewGuid(), DateTime.UtcNow);
        invitationRepository.Setup(value => value.GetByIdAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var result = await service.RevokeInvitationAsync(invitation.Id);

        Assert.True(result.TryGetError(out var error));
        var revocationFailed = Assert.IsType<RevokeAdminInvitationError.RevocationFailed>(error);
        Assert.IsType<AdminInvitationRevocationError.NotPending>(revocationFailed.Error);
        invitationRepository.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RevokeInvitationAsync_Pending_RevokesAndSaves()
    {
        var invitation = AdminInvitationEntity.Create("invitee@example.com", Guid.NewGuid(), DateTime.UtcNow, TimeSpan.FromDays(7));
        invitationRepository.Setup(value => value.GetByIdAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var result = await service.RevokeInvitationAsync(invitation.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(AdminInvitationStatus.Revoked, invitation.Status);
        invitationRepository.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region IsCurrentUserAdminAsync

    [Fact]
    public async Task IsCurrentUserAdminAsync_NoCurrentUser_ReturnsFalseWithoutQuerying()
    {
        currentUser.SetupGet(user => user.Id).Returns((Guid?)null);

        var result = await service.IsCurrentUserAdminAsync();

        Assert.False(result);
        profileRepository.Verify(value => value.IsAdminAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IsCurrentUserAdminAsync_CurrentUserIsAdmin_ReturnsTrue()
    {
        var sub = Guid.NewGuid();
        currentUser.SetupGet(user => user.Id).Returns(sub);
        profileRepository.Setup(value => value.IsAdminAsync(sub, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await service.IsCurrentUserAdminAsync();

        Assert.True(result);
    }

    #endregion

    #region GetOverviewAsync

    [Fact]
    public async Task GetOverviewAsync_JoinsAdminEmailsAndPendingInvitations()
    {
        var adminSub = Guid.NewGuid();
        profileRepository.Setup(value => value.ListAdminSubsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([adminSub]);
        userModule.Setup(value => value.GetEmailsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, string> { [adminSub] = "admin@example.com" });
        var invitation = AdminInvitationEntity.Create("invitee@example.com", Guid.NewGuid(), DateTime.UtcNow, TimeSpan.FromDays(7));
        invitationRepository.Setup(value => value.ListPendingInvitationsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([invitation]);

        var overview = await service.GetOverviewAsync();

        var admin = Assert.Single(overview.Admins);
        Assert.Equal(adminSub, admin.Sub);
        Assert.Equal("admin@example.com", admin.Email);
        var pending = Assert.Single(overview.PendingInvitations);
        Assert.Equal(invitation.Id, pending.Id);
    }

    #endregion

    #region EnsureCurrentUserAdminGrantedIfEligibleAsync

    [Fact]
    public async Task EnsureCurrentUserAdminGrantedIfEligibleAsync_NoCurrentUser_DoesNothing()
    {
        currentUser.SetupGet(user => user.Id).Returns((Guid?)null);

        var result = await service.EnsureCurrentUserAdminGrantedIfEligibleAsync();

        Assert.False(result);
        profileRepository.Verify(value => value.IsAdminAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        profileRepository.Verify(value => value.GrantAdmin(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task EnsureCurrentUserAdminGrantedIfEligibleAsync_AlreadyAdmin_ReturnsTrueWithoutGranting()
    {
        var sub = Guid.NewGuid();
        currentUser.SetupGet(user => user.Id).Returns(sub);
        currentUser.SetupGet(user => user.Email).Returns("someone@example.com");
        profileRepository.Setup(value => value.IsAdminAsync(sub, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await service.EnsureCurrentUserAdminGrantedIfEligibleAsync();

        Assert.True(result);
        profileRepository.Verify(value => value.GrantAdmin(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task EnsureCurrentUserAdminGrantedIfEligibleAsync_MatchingPendingInvitation_GrantsAndAcceptsInvitation()
    {
        var sub = Guid.NewGuid();
        currentUser.SetupGet(user => user.Id).Returns(sub);
        currentUser.SetupGet(user => user.Email).Returns("invitee@example.com");
        profileRepository.Setup(value => value.IsAdminAsync(sub, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var invitation = AdminInvitationEntity.Create("invitee@example.com", Guid.NewGuid(), DateTime.UtcNow, TimeSpan.FromDays(7));
        invitationRepository.Setup(value => value.GetPendingInvitationByEmailAsync("invitee@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var result = await service.EnsureCurrentUserAdminGrantedIfEligibleAsync();

        Assert.True(result);
        Assert.Equal(AdminInvitationStatus.Accepted, invitation.Status);
        Assert.Equal(sub, invitation.AcceptedByUserId);
        profileRepository.Verify(value => value.GrantAdmin(sub), Times.Once);
        invitationRepository.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnsureCurrentUserAdminGrantedIfEligibleAsync_MatchingPendingInvitation_NonDuplicateSaveFailure_Propagates()
    {
        var sub = Guid.NewGuid();
        currentUser.SetupGet(user => user.Id).Returns(sub);
        currentUser.SetupGet(user => user.Email).Returns("invitee@example.com");
        profileRepository.Setup(value => value.IsAdminAsync(sub, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var invitation = AdminInvitationEntity.Create("invitee@example.com", Guid.NewGuid(), DateTime.UtcNow, TimeSpan.FromDays(7));
        invitationRepository.Setup(value => value.GetPendingInvitationByEmailAsync("invitee@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        var exception = new DbUpdateException();
        invitationRepository.Setup(value => value.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(exception);

        var actual = await Assert.ThrowsAsync<DbUpdateException>(
            () => service.EnsureCurrentUserAdminGrantedIfEligibleAsync());

        Assert.Same(exception, actual);
    }

    [Fact]
    public async Task EnsureCurrentUserAdminGrantedIfEligibleAsync_BootstrapEmail_GrantsWhenNoAdminsExist()
    {
        var sub = Guid.NewGuid();
        currentUser.SetupGet(user => user.Id).Returns(sub);
        currentUser.SetupGet(user => user.Email).Returns(BootstrapEmail.ToUpperInvariant());
        profileRepository.Setup(value => value.IsAdminAsync(sub, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        invitationRepository.Setup(value => value.GetPendingInvitationByEmailAsync(BootstrapEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminInvitationEntity?)null);
        profileRepository.Setup(value => value.CountAdminsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var result = await service.EnsureCurrentUserAdminGrantedIfEligibleAsync();

        Assert.True(result);
        profileRepository.Verify(value => value.GrantAdmin(sub), Times.Once);
        invitationRepository.Verify(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnsureCurrentUserAdminGrantedIfEligibleAsync_BootstrapEmail_ReturnsFalseWhenAnAdminAlreadyExists()
    {
        var sub = Guid.NewGuid();
        currentUser.SetupGet(user => user.Id).Returns(sub);
        currentUser.SetupGet(user => user.Email).Returns(BootstrapEmail);
        profileRepository.Setup(value => value.IsAdminAsync(sub, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        invitationRepository.Setup(value => value.GetPendingInvitationByEmailAsync(BootstrapEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminInvitationEntity?)null);
        profileRepository.Setup(value => value.CountAdminsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await service.EnsureCurrentUserAdminGrantedIfEligibleAsync();

        Assert.False(result);
        profileRepository.Verify(value => value.GrantAdmin(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task EnsureCurrentUserAdminGrantedIfEligibleAsync_NoInvitationAndNonBootstrapEmail_ReturnsFalse()
    {
        var sub = Guid.NewGuid();
        currentUser.SetupGet(user => user.Id).Returns(sub);
        currentUser.SetupGet(user => user.Email).Returns("uninvited@example.com");
        profileRepository.Setup(value => value.IsAdminAsync(sub, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        invitationRepository.Setup(value => value.GetPendingInvitationByEmailAsync("uninvited@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminInvitationEntity?)null);

        var result = await service.EnsureCurrentUserAdminGrantedIfEligibleAsync();

        Assert.False(result);
        profileRepository.Verify(value => value.GrantAdmin(It.IsAny<Guid>()), Times.Never);
        profileRepository.Verify(value => value.CountAdminsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
