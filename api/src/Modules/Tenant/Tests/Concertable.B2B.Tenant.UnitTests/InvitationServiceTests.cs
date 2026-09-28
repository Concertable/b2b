using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Application.Errors;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.B2B.Tenant.Infrastructure.Services;
using Concertable.B2B.User.Contracts;
using Concertable.Kernel.Identity;
using Moq;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class InvitationServiceTests
{
    private readonly Mock<ITenantRepository> tenantRepository;
    private readonly Mock<IMembershipRepository> membershipRepository;
    private readonly Mock<IInvitationRepository> repository;
    private readonly Mock<ITenantContext> tenantContext;
    private readonly Mock<IMembershipContext> membershipContext;
    private readonly Mock<ICurrentUser> currentUser;
    private readonly InvitationService service;

    public InvitationServiceTests()
    {
        this.tenantRepository = new Mock<ITenantRepository>();
        this.membershipRepository = new Mock<IMembershipRepository>();
        this.repository = new Mock<IInvitationRepository>();
        this.tenantContext = new Mock<ITenantContext>();
        this.membershipContext = new Mock<IMembershipContext>();
        this.currentUser = new Mock<ICurrentUser>();
        this.service = new InvitationService(
            this.tenantRepository.Object,
            this.membershipRepository.Object,
            this.repository.Object,
            this.tenantContext.Object,
            this.membershipContext.Object,
            this.currentUser.Object,
            Mock.Of<IUserModule>(),
            TimeProvider.System,
            Mock.Of<IPermissionCatalog>(),
            new ImmediateUnitOfWorkBehavior());
    }

    [Fact]
    public async Task AcceptInvitationAsync_ExpiredInvitation_MapsDomainFailureWithoutCreatingMembership()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var inviter = TenantMembershipEntity.Create(
            tenantId,
            Guid.NewGuid(),
            TenantRole.Owner,
            invitedBy: null,
            DateTime.UtcNow.AddDays(-10));
        var invitation = TenantInvitationEntity.Create(
            tenantId,
            "member@example.com",
            TenantRole.Staff,
            inviter.Id,
            inviter.PermissionVersion,
            DateTime.UtcNow.AddDays(-8),
            TimeSpan.FromDays(7));
        var tenant = TenantEntity.Create(
            "Acme Ltd",
            "contact@acme.test",
            Guid.NewGuid(),
            DateTime.UtcNow,
            tenantId);
        this.currentUser.SetupGet(user => user.Id).Returns(userId);
        this.currentUser.SetupGet(user => user.Email).Returns(invitation.Email);
        this.repository
            .Setup(value => value.GetByIdAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        this.repository
            .Setup(value => value.GetByIdForUpdateAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        this.tenantRepository
            .Setup(value => value.GetByIdForAdministrationAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        this.membershipRepository
            .Setup(value => value.IsMemberAsync(tenantId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        this.membershipRepository
            .Setup(value => value.FindMembershipByIdAsync(tenantId, inviter.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inviter);

        var result = await this.service.AcceptInvitationAsync(invitation.Id);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<AcceptInvitationError.InvitationExpired>(error);
        this.membershipRepository.Verify(
            value => value.InsertAsync(It.IsAny<TenantMembershipEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InviteAsync_ManagerAssigningManager_ReturnsNotPermitted()
    {
        var tenantId = Guid.NewGuid();
        var actor = TenantMembershipEntity.Create(
            tenantId,
            Guid.NewGuid(),
            TenantRole.Manager,
            invitedBy: null,
            DateTime.UtcNow);
        var tenant = TenantEntity.Create(
            "Acme Ltd",
            "contact@acme.test",
            Guid.NewGuid(),
            DateTime.UtcNow,
            tenantId);
        this.tenantContext.SetupGet(context => context.TenantId).Returns(tenantId);
        this.membershipContext
            .SetupGet(context => context.Membership)
            .Returns(new MembershipSnapshot(
                actor.Id,
                actor.TenantId,
                actor.UserId,
                actor.Role,
                actor.PermissionVersion));
        this.tenantRepository
            .Setup(value => value.GetByIdForAdministrationAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        this.membershipRepository
            .Setup(value => value.FindMembershipByIdAsync(tenantId, actor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(actor);

        var result = await this.service.InviteAsync(new InviteMemberRequest
        {
            Email = "member@example.com",
            Role = TenantRole.Manager,
        });

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<InviteMemberError.NotPermitted>(error);
        this.repository.Verify(
            value => value.InsertAsync(It.IsAny<TenantInvitationEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AcceptInvitationAsync_InviterNoLongerAuthorized_ReturnsInviterNotAuthorized()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var inviter = TenantMembershipEntity.Create(
            tenantId,
            Guid.NewGuid(),
            TenantRole.Staff,
            invitedBy: null,
            DateTime.UtcNow);
        var invitation = TenantInvitationEntity.Create(
            tenantId,
            "member@example.com",
            TenantRole.Manager,
            inviter.Id,
            1,
            DateTime.UtcNow,
            TimeSpan.FromDays(7));
        var tenant = TenantEntity.Create(
            "Acme Ltd",
            "contact@acme.test",
            Guid.NewGuid(),
            DateTime.UtcNow,
            tenantId);
        this.currentUser.SetupGet(user => user.Id).Returns(userId);
        this.currentUser.SetupGet(user => user.Email).Returns(invitation.Email);
        this.repository
            .Setup(value => value.GetByIdAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        this.repository
            .Setup(value => value.GetByIdForUpdateAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        this.tenantRepository
            .Setup(value => value.GetByIdForAdministrationAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        this.membershipRepository
            .Setup(value => value.IsMemberAsync(tenantId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        this.membershipRepository
            .Setup(value => value.FindMembershipByIdAsync(tenantId, inviter.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inviter);

        var result = await this.service.AcceptInvitationAsync(invitation.Id);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<AcceptInvitationError.InviterNotAuthorized>(error);
        this.membershipRepository.Verify(
            value => value.InsertAsync(It.IsAny<TenantMembershipEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AcceptInvitationAsync_UnauthenticatedUser_ReturnsForbiddenWithoutLoadingInvitation()
    {
        var result = await this.service.AcceptInvitationAsync(Guid.NewGuid());

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<AcceptInvitationError.Unauthenticated>(error);
        this.repository.Verify(
            value => value.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
