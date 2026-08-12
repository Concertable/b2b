using Concertable.B2B.Infrastructure.Uris;
using Concertable.B2B.Tenant.Application.Errors;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.B2B.Tenant.Infrastructure.Services;
using Concertable.B2B.User.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Shared.Email.Application;
using Moq;

namespace Concertable.B2B.Tenant.UnitTests;

public sealed class InvitationServiceTests
{
    private readonly Mock<ITenantRepository> repository = new();
    private readonly Mock<ITenantContext> tenantContext = new();
    private readonly Mock<ICurrentUser> currentUser = new();
    private readonly Mock<IUserModule> userModule = new();
    private readonly Mock<IEmailTransport> emailTransport = new();
    private readonly Mock<IFrontendUriGenerator> uris = new();

    [Fact]
    public async Task AcceptInvitationAsync_ExpiredInvitation_MapsDomainFailureWithoutCreatingMembership()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var invitation = TenantInvitationEntity.Create(
            tenantId,
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
            .Setup(value => value.GetInvitationByIdAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        repository
            .Setup(value => value.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        repository
            .Setup(value => value.IsMemberAsync(tenantId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateService().AcceptInvitationAsync(invitation.Id);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<AcceptInvitationError.InvitationExpired>(error);
        repository.Verify(
            value => value.AddMembership(It.IsAny<TenantMembershipEntity>()),
            Times.Never);
        repository.Verify(
            value => value.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RevokeInvitationAsync_NonPendingInvitation_MapsDomainFailureWithoutSaving()
    {
        var tenantId = Guid.NewGuid();
        var invitation = TenantInvitationEntity.Create(
            tenantId,
            "member@example.com",
            TenantRole.Staff,
            Guid.NewGuid(),
            DateTime.UtcNow,
            TimeSpan.FromDays(7));
        Assert.True(invitation.Accept(Guid.NewGuid(), DateTime.UtcNow.AddMinutes(1)).IsSuccess);
        tenantContext.SetupGet(context => context.TenantId).Returns(tenantId);
        repository
            .Setup(value => value.GetInvitationByIdAsync(invitation.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        var result = await CreateService().RevokeInvitationAsync(invitation.Id);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<RevokeInvitationError.InvitationNotPending>(error);
        repository.Verify(
            value => value.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private InvitationService CreateService() => new(
        repository.Object,
        tenantContext.Object,
        currentUser.Object,
        userModule.Object,
        emailTransport.Object,
        uris.Object,
        TimeProvider.System);
}
