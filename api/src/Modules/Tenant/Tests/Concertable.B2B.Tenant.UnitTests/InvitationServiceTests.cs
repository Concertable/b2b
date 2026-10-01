using System.Collections.Immutable;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Application.Errors;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.B2B.Tenant.Infrastructure.Services;
using Concertable.B2B.Tenant.Infrastructure.Authorization;
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
    private readonly Mock<IRoleRepository> roles;
    private readonly Mock<IMembershipResolver> membershipResolver;
    private readonly Mock<ICommandAuthorizationContext> command;
    private readonly InvitationService service;

    public InvitationServiceTests()
    {
        tenantRepository = new Mock<ITenantRepository>();
        membershipRepository = new Mock<IMembershipRepository>();
        repository = new Mock<IInvitationRepository>();
        tenantContext = new Mock<ITenantContext>();
        membershipContext = new Mock<IMembershipContext>();
        currentUser = new Mock<ICurrentUser>();
        roles = new Mock<IRoleRepository>();
        membershipResolver = new Mock<IMembershipResolver>();
        membershipResolver.Setup(value => value.ResolveSnapshotAsync(
                It.IsAny<MembershipSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MembershipSnapshot expected, CancellationToken _) => expected);
        command = new Mock<ICommandAuthorizationContext>();
        command.SetupGet(value => value.IsActive).Returns(true);
        command.SetupGet(value => value.TransactionId).Returns(Guid.NewGuid());
        tenantRepository.Setup(value => value.GetExistingIdsForShareAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                (IReadOnlySet<Guid>)ids.ToHashSet());
        tenantRepository.Setup(value => value.GetAuthorizationCatalogRevisionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthorizationCatalog.Revision);
        membershipRepository.Setup(value => value.GetSnapshotsByIdsForShareAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .Returns((IReadOnlyCollection<Guid> _, CancellationToken _) =>
                Task.FromResult<IReadOnlyList<MembershipSnapshot>>(
                    membershipContext.Object.Membership is { } actor
                        ? new[] { actor }
                        : Array.Empty<MembershipSnapshot>()));
        var authority = new TenantAuthorityResolver(tenantRepository.Object, membershipRepository.Object,
            roles.Object, repository.Object, membershipContext.Object, command.Object, TimeProvider.System);
        service = new InvitationService(
            tenantRepository.Object,
            membershipRepository.Object,
            repository.Object,
            tenantContext.Object,
            currentUser.Object,
            Mock.Of<IUserModule>(),
            TimeProvider.System,
            new ImmediateUnitOfWorkBehavior(),
            roles.Object,
            membershipContext.Object,
            membershipResolver.Object,
            authority,
            command.Object);
    }

    [Fact]
    public async Task AcceptInvitationAsync_ExpiredInvitation_MapsDomainFailureWithoutCreatingMembership()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var inviterRole = Preset(tenantId, "Owner");
        var invitedRole = Preset(tenantId, "Staff");
        var inviter = TenantMembershipEntity.Create(tenantId, Guid.NewGuid(), [inviterRole.Id], null, DateTime.UtcNow.AddDays(-10));
        var invitation = TenantInvitationEntity.Create(
            tenantId, "member@example.com", [invitedRole.Id], inviter.Id, 1, 1,
            DateTime.UtcNow.AddDays(-8), TimeSpan.FromDays(7));
        var tenant = TenantEntity.Create("Acme Ltd", "contact@acme.test", Guid.NewGuid(), DateTime.UtcNow, tenantId);
        currentUser.SetupGet(value => value.Id).Returns(userId);
        currentUser.SetupGet(value => value.Email).Returns(invitation.Email);
        repository.Setup(value => value.GetByIdAsync(invitation.Id, It.IsAny<CancellationToken>())).ReturnsAsync(invitation);
        repository.Setup(value => value.GetByIdForUpdateAsync(invitation.Id, It.IsAny<CancellationToken>())).ReturnsAsync(invitation);
        tenantRepository.Setup(value => value.GetByIdForAdministrationAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        membershipRepository.Setup(value => value.IsMemberAsync(tenantId, userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        membershipRepository.Setup(value => value.GetSnapshotsByIdsForShareAsync(It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(inviter.Id)), It.IsAny<CancellationToken>()))
            .ReturnsAsync([Snapshot(inviter, "Owner")]);
        roles.Setup(value => value.ResolveActiveAsync(tenantId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([invitedRole]);
        roles.Setup(value => value.HasProtectedOwnerAsync(tenantId, inviter.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await service.AcceptInvitationAsync(invitation.Id);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<AcceptInvitationError.InvitationExpired>(error);
        membershipRepository.Verify(
            value => value.InsertAsync(It.IsAny<TenantMembershipEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InviteAsync_ManagerAssigningManager_ReturnsNotPermitted()
    {
        var tenantId = Guid.NewGuid();
        var actorRole = Preset(tenantId, "Manager");
        var targetRole = Preset(tenantId, "Manager");
        var actor = Snapshot(TenantMembershipEntity.Create(tenantId, Guid.NewGuid(), [actorRole.Id], null, DateTime.UtcNow), "Manager");
        var tenant = TenantEntity.Create("Acme Ltd", "contact@acme.test", Guid.NewGuid(), DateTime.UtcNow, tenantId);
        tenantContext.SetupGet(value => value.TenantId).Returns(tenantId);
        membershipContext.SetupGet(value => value.Membership).Returns(actor);
        tenantRepository.Setup(value => value.GetByIdForAdministrationAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        roles.Setup(value => value.ResolveActiveAsync(tenantId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([targetRole]);
        roles.Setup(value => value.HasProtectedOwnerAsync(tenantId, actor.MembershipId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await service.InviteAsync(new InviteMemberRequest
        {
            Email = "member@example.com",
            RoleIds = [targetRole.Id],
        });

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<InviteMemberError.NotPermitted>(error);
        repository.Verify(
            value => value.InsertAsync(It.IsAny<TenantInvitationEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AcceptInvitationAsync_InviterNoLongerAuthorized_ReturnsInviterNotAuthorized()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var inviterRole = Preset(tenantId, "Staff");
        var invitedRole = Preset(tenantId, "Manager");
        var inviter = TenantMembershipEntity.Create(tenantId, Guid.NewGuid(), [inviterRole.Id], null, DateTime.UtcNow);
        var invitation = TenantInvitationEntity.Create(
            tenantId, "member@example.com", [invitedRole.Id], inviter.Id, 1, 1,
            DateTime.UtcNow, TimeSpan.FromDays(7));
        var tenant = TenantEntity.Create("Acme Ltd", "contact@acme.test", Guid.NewGuid(), DateTime.UtcNow, tenantId);
        currentUser.SetupGet(value => value.Id).Returns(userId);
        currentUser.SetupGet(value => value.Email).Returns(invitation.Email);
        repository.Setup(value => value.GetByIdAsync(invitation.Id, It.IsAny<CancellationToken>())).ReturnsAsync(invitation);
        repository.Setup(value => value.GetByIdForUpdateAsync(invitation.Id, It.IsAny<CancellationToken>())).ReturnsAsync(invitation);
        tenantRepository.Setup(value => value.GetByIdForAdministrationAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        membershipRepository.Setup(value => value.IsMemberAsync(tenantId, userId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        membershipRepository.Setup(value => value.GetSnapshotsByIdsForShareAsync(It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(inviter.Id)), It.IsAny<CancellationToken>()))
            .ReturnsAsync([Snapshot(inviter, "Staff")]);
        roles.Setup(value => value.ResolveActiveAsync(tenantId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([invitedRole]);
        roles.Setup(value => value.HasProtectedOwnerAsync(tenantId, inviter.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await service.AcceptInvitationAsync(invitation.Id);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<AcceptInvitationError.InviterNotAuthorized>(error);
        membershipRepository.Verify(
            value => value.InsertAsync(It.IsAny<TenantMembershipEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AcceptInvitationAsync_UnauthenticatedUser_ReturnsForbiddenWithoutLoadingInvitation()
    {
        var result = await service.AcceptInvitationAsync(Guid.NewGuid());

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<AcceptInvitationError.Unauthenticated>(error);
        repository.Verify(
            value => value.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static TenantRoleDefinition Preset(Guid tenantId, string key)
    {
        var preset = AuthorizationCatalog.Presets[key];
        return TenantRoleDefinition.CreateSystemPreset(tenantId, key);
    }

    private static MembershipSnapshot Snapshot(TenantMembershipEntity membership, string key) =>
        new(
            membership.Id,
            membership.TenantId,
            membership.UserId,
            membership.PermissionVersion,
            1,
            AuthorizationCatalog.Presets[key].Permissions.ToImmutableDictionary());
}
