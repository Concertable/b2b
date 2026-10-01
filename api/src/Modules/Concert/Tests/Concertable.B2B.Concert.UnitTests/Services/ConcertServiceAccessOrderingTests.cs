using ITenantResolver = Concertable.B2B.Tenant.Contracts.ITenantResolver;
using System.Collections.Immutable;

using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Repositories;
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
        1,
        1,
        ImmutableDictionary<TenantPermission, ResourceAudience>.Empty.Add(
            TenantPermission.ResourcesShare, ResourceAudience.TenantResources));

    [Fact]
    public async Task RevokeSummaryShare_WithoutConcertAuthority_DoesNotLoadGrants()
    {
        var fixture = CreateFixture<RevokeConcertSummaryShareError>();

        var result = await fixture.Service.RevokeSummaryShareAsync(1, Guid.NewGuid(), 0);

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<RevokeConcertSummaryShareError.NotPermitted>(error);
        Assert.Equal(["tenant", "resource"], fixture.Steps);
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
        Assert.Equal(["tenant", "resource"], fixture.Steps);
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
        Assert.Equal(["tenant", "resource"], fixture.Steps);
        fixture.Repository.Verify(
            value => value.GetWithGrantsByIdForUpdateAsync(1, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Post_WithOpsEditOnly_DoesNotLoadConcert()
    {
        var editOnly = actor with
        {
            Permissions = ImmutableDictionary<TenantPermission, ResourceAudience>.Empty.Add(
                TenantPermission.ConcertsOpsEdit, ResourceAudience.TenantResources),
        };
        var fixture = CreateFixture<PostConcertError>(editOnly);

        var result = await fixture.Service.PostAsync(
            1,
            new UpdateConcertRequest { Name = "Test", About = "Test" });

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<PostConcertError.NotPermitted>(error);
        fixture.Repository.Verify(
            value => value.GetByIdForUpdateAsync(1, It.IsAny<CancellationToken>()), Times.Never);
        fixture.Repository.Verify(
            value => value.GetStateByIdAsync(1, It.IsAny<CancellationToken>()), Times.Never);
        fixture.Authorization.Verify(
            value => value.RequireAsync(
                It.IsAny<AuthorizationRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Share_WithInvalidRecipientAndNoConcertAuthority_DoesNotRevealRecipient()
    {
        var fixture = CreateFixture<ShareConcertSummaryError>(targetTenantExists: false);

        var result = await fixture.Service.ShareSummaryAsync(
            1,
            new ShareConcertSummaryRequest
            {
                RequestId = Guid.NewGuid(),
                RecipientTenantId = Guid.NewGuid(),
            });

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<ShareConcertSummaryError.NotPermitted>(error);
        Assert.Equal(["tenant", "resource"], fixture.Steps);
        fixture.Repository.Verify(
            value => value.GetWithGrantsByIdForUpdateAsync(1, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private Fixture CreateFixture<TError>(MembershipSnapshot? principal = null, bool targetTenantExists = true)
        where TError : notnull
    {
        var currentActor = principal ?? actor;
        var repository = new Mock<IConcertPrivilegedRepository>();
        var unitOfWork = new Mock<IPrivilegedOutboxUnitOfWorkBehavior>();
        var membership = new Mock<IMembershipContext>();
        var resolution = new Mock<ITenantResolver>();
        var membershipResolver = new Mock<IMembershipResolver>();
        var authorization = new Mock<IResourceAuthorization>();
        var commandAuthorization = new Mock<ICommandAuthorizationContext>();
        var steps = new List<string>();
        var executor = new ImmediateTransactionRunner();
        authorization
            .Setup(value => value.RequireAsync(
                It.Is<AuthorizationRequest>(request =>
                    request.Permission == TenantPermission.ResourcesShare
                    && request.Resource.Kind == ResourceKind.Concert
                    && request.Resource.Id == 1
                    && request.Facet == null),
                It.IsAny<CancellationToken>()))
            .Callback<AuthorizationRequest, CancellationToken>((_, _) => steps.Add("resource"))
            .ReturnsAsync(AuthorizationDecision.Denied);
        unitOfWork
            .Setup(value => value.ExecuteAsync(
                It.IsAny<Func<Task<UnitResult<TError>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<Task<UnitResult<TError>>> action, CancellationToken _) => action());
        unitOfWork
            .Setup(value => value.ExecuteAsync(
                It.IsAny<Func<Task<Result<ConcertSummaryShare, ShareConcertSummaryError>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<Task<Result<ConcertSummaryShare, ShareConcertSummaryError>>> action,
                CancellationToken _) => action());
        membership.SetupGet(value => value.Membership).Returns(currentActor);
        membershipResolver
            .Setup(value => value.ResolveSnapshotAsync(
                currentActor, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Option<MembershipSnapshot>)currentActor);
        resolution
            .Setup(value => value.ResolveAsync(
                currentActor,
                It.IsAny<Guid>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .Callback<MembershipSnapshot, Guid, Guid?, CancellationToken>(
                (_, _, _, _) => steps.Add("tenant"))
            .ReturnsAsync((Option<TenantResolution>)new TenantResolution(
                currentActor, targetTenantExists, currentActor));

        var service = new ConcertService(
            Mock.Of<IConcertRepository>(),
            Mock.Of<IConcertPrivateReadRepository>(),
            authorization.Object,
            commandAuthorization.Object,
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
            resolution.Object,
            Mock.Of<ITenantContext>(),
            membership.Object,
            membershipResolver.Object,
            executor,
            Mock.Of<IResourceAccessContext>(value => value.UtcNow == DateTime.UnixEpoch),
            Mock.Of<ILogger<ConcertService>>());
        executor.Service = service;
        return new Fixture(service, repository, authorization, commandAuthorization, steps);
    }

    private sealed record Fixture(
        ConcertService Service,
        Mock<IConcertPrivilegedRepository> Repository,
        Mock<IResourceAuthorization> Authorization,
        Mock<ICommandAuthorizationContext> CommandAuthorization,
        IReadOnlyList<string> Steps);

    private sealed class ImmediateTransactionRunner : ITransactionRunner
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
