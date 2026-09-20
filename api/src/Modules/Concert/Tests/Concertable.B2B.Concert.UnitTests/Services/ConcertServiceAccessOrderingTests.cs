using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Authorization.Contracts.Enums;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Contracts;
using Microsoft.Extensions.Logging;
using Moq;
using Reunion;

namespace Concertable.B2B.Concert.UnitTests.Services;

public sealed class ConcertServiceAccessOrderingTests
{
    private readonly MembershipSnapshot actor = new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        TenantRole.Owner,
        1);

    [Fact]
    public async Task RevokeSummaryShare_WithoutConcertAuthority_DoesNotLoadGrants()
    {
        var fixture = CreateFixture<RevokeConcertSummaryShareError>();

        var result = await fixture.Service.RevokeSummaryShareAsync(1, Guid.NewGuid(), 0);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<RevokeConcertSummaryShareError.NotPermitted>(error);
        fixture.Repository.Verify(
            value => value.GetWithGrantsByIdForUpdateAsync(1, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AssignMember_WithoutConcertAuthority_DoesNotLoadGrants()
    {
        var fixture = CreateFixture<AssignConcertMemberError>();

        var result = await fixture.Service.AssignMemberAsync(
            1,
            new AssignConcertMemberRequest
            {
                MembershipId = actor.MembershipId,
                ExpectedAccessVersion = 0,
            });

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<AssignConcertMemberError.NotPermitted>(error);
        fixture.Repository.Verify(
            value => value.GetWithGrantsByIdForUpdateAsync(1, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveMemberAssignment_WithoutConcertAuthority_DoesNotLoadGrants()
    {
        var fixture = CreateFixture<AssignConcertMemberError>();

        var result = await fixture.Service.RemoveMemberAssignmentAsync(1, actor.MembershipId, 0);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<AssignConcertMemberError.NotPermitted>(error);
        fixture.Repository.Verify(
            value => value.GetWithGrantsByIdForUpdateAsync(1, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private Fixture CreateFixture<TError>()
        where TError : notnull
    {
        var repository = new Mock<IConcertPrivilegedRepository>();
        var unitOfWork = new Mock<IPrivilegedOutboxUnitOfWorkBehavior>();
        var membership = new Mock<IMembershipContext>();
        var facts = new Mock<ITenantCommandFacts>();
        var permissions = new Mock<IPermissionCatalog>();
        var executor = new ImmediateCommandExecutor();
        repository
            .Setup(value => value.GetIdentityByIdForUpdateAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConcertAccessIdentity(1, Guid.NewGuid(), Guid.NewGuid(), 0));
        repository
            .Setup(value => value.CanShareAsync(
                1,
                actor,
                ResourceAudience.TenantResources,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        unitOfWork
            .Setup(value => value.ExecuteAsync(
                It.IsAny<Func<Task<UnitResult<TError>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<Task<UnitResult<TError>>> action, CancellationToken _) => action());
        membership.SetupGet(value => value.Membership).Returns(actor);
        facts
            .Setup(value => value.ResolveAsync(
                actor,
                It.IsAny<Guid>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantCommandFacts(actor, true, actor));
        permissions
            .Setup(value => value.Grants(actor.Role, TenantPermission.ResourcesShare))
            .Returns(true);
        permissions
            .Setup(value => value.AudienceFor(actor.Role, TenantPermission.ResourcesShare))
            .Returns(ResourceAudience.TenantResources);

        var service = new ConcertService(
            Mock.Of<IConcertRepository>(),
            repository.Object,
            unitOfWork.Object,
            Mock.Of<IConcertReadRepository>(),
            Mock.Of<IConcertValidator>(),
            Mock.Of<IConcertWorkflow>(),
            Mock.Of<IArtistReadModelRepository>(),
            Mock.Of<IVenueReadModelRepository>(),
            Mock.Of<IBookingConfirmationEmailSender>(),
            Mock.Of<IBus>(),
            Mock.Of<IUnitOfWork>(),
            Mock.Of<IPrivilegedUnitOfWork>(),
            TimeProvider.System,
            Mock.Of<IConcertCommandReceiptRepository>(),
            facts.Object,
            Mock.Of<ITenantContext>(),
            membership.Object,
            Mock.Of<IMembershipAuthorityFence>(),
            permissions.Object,
            executor,
            Mock.Of<IResourceAccessContext>(value => value.UtcNow == DateTime.UnixEpoch),
            Mock.Of<ILogger<ConcertService>>());
        executor.Service = service;
        return new Fixture(service, repository);
    }

    private sealed record Fixture(
        ConcertService Service,
        Mock<IConcertPrivilegedRepository> Repository);

    private sealed class ImmediateCommandExecutor : ICommandExecutor
    {
        public ConcertService Service { get; set; } = null!;

        public Task<TResult> ExecuteAsync<TService, TResult>(
            Func<TService, CancellationToken, Task<TResult>> command,
            CancellationToken ct = default)
            where TService : notnull =>
            command((TService)(object)Service, ct);

        public Task<TResult> ExecuteAsync<TService, TResult>(
            Func<TService, CancellationToken, Task<TResult>> command,
            Func<TService, TResult, CancellationToken, Task<bool>> validateAuthority,
            Func<TResult> authorityFailure,
            CancellationToken ct = default)
            where TService : notnull =>
            command((TService)(object)Service, ct);
    }
}
