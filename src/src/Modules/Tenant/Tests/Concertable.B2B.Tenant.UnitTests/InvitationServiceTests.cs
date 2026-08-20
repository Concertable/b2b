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
    private readonly Mock<ITenantRepository> tenantRepository = new();
    private readonly Mock<IMembershipRepository> membershipRepository = new();
    private readonly Mock<IInvitationRepository> repository = new();
    private readonly Mock<ITenantContext> tenantContext = new();
    private readonly Mock<ICurrentUser> currentUser = new();
    private readonly Mock<IUserModule> userModule = new();

    [Fact]
    public async Task AcceptInvitationAsync_ExpiredInvitation_MapsDomainFailureWithoutCreatingMembership()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var invitation = TenantInvitationEntity.Create(
            tenantId,
            TenantType.Venue,
            "member@example.com",
            TenantRole.Staff,
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(-8),
            TimeSpan.FromDays(7));
        var tenant = TenantEntity.Create(
            "Acme Ltd",
            Guid.NewGuid(),
            TenantType.Venue,
            DateTime.UtcNow);
        currentUser.SetupGet(user => user.Id).Returns(userId);
        currentUser.SetupGet(user => user.Email).Returns(invitation.Email);
        repository
            .Setup(value => value.GetByIdAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        tenantRepository
            .Setup(value => value.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        membershipRepository
            .Setup(value => value.IsMemberAsync(tenantId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateService().AcceptInvitationAsync(invitation.Id);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<AcceptInvitationError.InvitationExpired>(error);
        membershipRepository.Verify(
            value => value.InsertAsync(It.IsAny<TenantMembershipEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RevokeInvitationAsync_NonPendingInvitation_MapsDomainFailureWithoutSaving()
    {
        var tenantId = Guid.NewGuid();
        var invitation = TenantInvitationEntity.Create(
            tenantId,
            TenantType.Venue,
            "member@example.com",
            TenantRole.Staff,
            Guid.NewGuid(),
            DateTime.UtcNow,
            TimeSpan.FromDays(7));
        Assert.True(invitation.Accept(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(1)).IsSuccess);
        tenantContext.SetupGet(context => context.TenantId).Returns(tenantId);
        repository
            .Setup(value => value.GetByIdAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var result = await CreateService().RevokeInvitationAsync(invitation.Id);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<RevokeInvitationError.InvitationNotPending>(error);
        repository.Verify(
            value => value.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task InviteAsync_UnauthenticatedUser_ReturnsForbiddenWithoutCreatingInvitation()
    {
        var tenantId = Guid.NewGuid();
        var tenant = TenantEntity.Create("Acme Ltd", Guid.NewGuid(), TenantType.Venue, DateTime.UtcNow);
        tenantContext.SetupGet(context => context.TenantId).Returns(tenantId);
        tenantRepository.Setup(value => value.GetByIdAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        membershipRepository.Setup(value => value.ListMembershipsByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        userModule.Setup(value => value.GetEmailsByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, string>());

        var result = await CreateService().InviteAsync(new InviteMemberRequest
        {
            Email = "member@example.com",
            Role = TenantRole.Staff
        });

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<InviteMemberError.Unauthenticated>(error);
        repository.Verify(
            value => value.InsertAsync(It.IsAny<TenantInvitationEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AcceptInvitationAsync_UnauthenticatedUser_ReturnsForbiddenWithoutLoadingInvitation()
    {
        var result = await CreateService().AcceptInvitationAsync(Guid.NewGuid());

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<AcceptInvitationError.Unauthenticated>(error);
        repository.Verify(
            value => value.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private InvitationService CreateService() => new(
        tenantRepository.Object,
        membershipRepository.Object,
        repository.Object,
        tenantContext.Object,
        currentUser.Object,
        userModule.Object,
        TimeProvider.System);
}
