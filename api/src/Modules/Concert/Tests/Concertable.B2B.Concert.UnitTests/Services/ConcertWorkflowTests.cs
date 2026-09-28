using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Authorization.Contracts.Enums;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Strategies;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Kernel;
using Concertable.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Moq;
using Reunion;

namespace Concertable.B2B.Concert.UnitTests.Services;

public sealed class ConcertWorkflowTests
{
    private readonly Mock<IConcertPrivilegedRepository> concertRepository = new();
    private readonly Mock<ISettlementService> settlementService = new();
    private readonly Mock<IDealStrategyFactory<ICancelStep>> cancelFactory = new();
    private readonly Mock<IDealStrategyFactory<ICompleteStep>> completeFactory = new();
    private readonly ImmediateBehavior immediateBehavior;
    private readonly ConcertWorkflow workflow;
    private readonly MembershipSnapshot actor = new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        TenantRole.Owner,
        1);

    public ConcertWorkflowTests()
    {
        immediateBehavior = new ImmediateBehavior();
        var membership = new Mock<IMembershipContext>();
        membership.SetupGet(context => context.Membership).Returns(actor);
        var authorityFence = new Mock<IMembershipAuthorityFence>();
        authorityFence
            .Setup(fence => fence.RequireCurrentAsync(actor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(actor);
        var permissionCatalog = new Mock<IPermissionCatalog>();
        permissionCatalog
            .Setup(catalog => catalog.Grants(actor.Role, TenantPermission.ConcertsManage))
            .Returns(true);
        permissionCatalog
            .Setup(catalog => catalog.AudienceFor(actor.Role, TenantPermission.ConcertsManage))
            .Returns(ResourceAudience.TenantResources);
        concertRepository
            .Setup(repository => repository.CanManageAsync(
                It.IsAny<int>(),
                actor,
                ResourceAudience.TenantResources,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var commandExecutor = new ImmediateCommandExecutor(settlementService.Object);
        workflow = new ConcertWorkflow(
            concertRepository.Object,
            commandExecutor,
            cancelFactory.Object,
            completeFactory.Object,
            immediateBehavior,
            membership.Object,
            authorityFence.Object,
            permissionCatalog.Object,
            TimeProvider.System);
        commandExecutor.Workflow = workflow;
    }

    [Fact]
    public async Task CancelAsync_CallerCancellation_Rethrows()
    {
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();
        var cancellationToken = cancellationSource.Token;
        concertRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(It.IsAny<int>(), cancellationToken))
            .Returns(Task.FromCanceled<ConcertEntity?>(cancellationToken));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => workflow.CancelAsync(42, cancellationToken));
    }

    [Fact]
    public async Task CancelAsync_ConcertNotFound_ReturnsTypedError()
    {
        concertRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(42, default))
            .ReturnsAsync((ConcertEntity?)null);

        var result = await workflow.CancelAsync(42);

        Assert.True(result.TryGetError(out var error));
        var notFound = Assert.IsType<CancelConcertError.ConcertNotFound>(error);
        Assert.Equal(42, notFound.ConcertId);
    }

    [Fact]
    public async Task CancelAsync_RejectedTransition_ReturnsInvalidTransition()
    {
        var concert = CreateBooking();
        Assert.True(concert.BeginSettlement().TryGetValue(out _));
        concertRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(42, default))
            .ReturnsAsync(concert);

        var result = await workflow.CancelAsync(42);

        Assert.True(result.TryGetError(out var error));
        var invalidTransition = Assert.IsType<CancelConcertError.InvalidTransition>(error);
        Assert.Equal(new TransitionError<ConcertState, ConcertTrigger>(ConcertState.AwaitingSettlement, ConcertTrigger.BeginCancellation), invalidTransition.Error);
        cancelFactory.Verify(factory => factory.Create(It.IsAny<DealType>()), Times.Never);
    }

    [Fact]
    public async Task CancelAsync_ValidTransition_ExecutesStrategyAndSaves()
    {
        var concert = CreateBooking();
        var strategy = new Mock<ICancelStep>();
        cancelFactory
            .Setup(factory => factory.Create(DealType.FlatFee))
            .Returns(strategy.Object);
        concertRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(42, default))
            .ReturnsAsync(concert);
        var result = await workflow.CancelAsync(42);

        Assert.False(result.TryGetError(out _));
        strategy.Verify(value => value.CancelAsync(concert, default));
        concertRepository.Verify(repository => repository.SaveChangesAsync(default));
    }

    [Fact]
    public async Task CompleteAsync_CallerCancellation_Rethrows()
    {
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();
        var cancellationToken = cancellationSource.Token;
        settlementService
            .Setup(service => service.ReserveAsync(It.IsAny<int>(), cancellationToken))
            .Returns(Task.FromCanceled<Result<SettlementPreparation, FinishConcertError>>(
                cancellationToken));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => workflow.CompleteAsync(42, cancellationToken));
    }

    [Fact]
    public async Task CompleteAsync_ExecutesPaymentBetweenReservationAndCompletion()
    {
        var operationId = Guid.NewGuid();
        var ready = new SettlementPreparation.Ready(
            operationId,
            42,
            DealType.DoorSplit,
            7,
            PaymentOperationReferences.MethodVerification(7),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Money.Gbp(125m));
        Result<SettlementPreparation, FinishConcertError> prepared = ready;
        UnitResult<FinishConcertError> executed = new Success();
        Result<SettlementOutcome, FinishConcertError> completed = SettlementOutcome.Settled;
        settlementService
            .Setup(service => service.ReserveAsync(42, default))
            .ReturnsAsync(prepared);
        var strategy = new Mock<ICompleteStep>();
        completeFactory
            .Setup(factory => factory.Create(It.IsAny<DealType>()))
            .Returns(strategy.Object);
        strategy
            .Setup(value => value.CompleteAsync(ready, default))
            .ReturnsAsync(executed);
        settlementService
            .Setup(service => service.CompleteAsync(42, operationId, default))
            .ReturnsAsync(completed);

        var result = await workflow.CompleteAsync(42);

        Assert.True(result.TryGetValue(out var outcome));
        Assert.Equal(SettlementOutcome.Settled, outcome);
        completeFactory.Verify(factory => factory.Create(DealType.DoorSplit));
        strategy.Verify(value => value.CompleteAsync(ready, default));
    }

    private static ConcertEntity CreateBooking() => ConcertEntity.CreateDraft(
        ConfirmedBookings.FlatFee(100m),
        new ConcertDraft("Concert", "About", []),
        DateTime.UnixEpoch);

    private sealed class ImmediateBehavior : IPrivilegedOutboxUnitOfWorkBehavior
    {
        public Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default) => action();

        public Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default) => action();

        public async Task<T> TryExecuteAsync<T>(
            Func<Task<T>> action,
            Func<DbUpdateException, bool> isExpected,
            Func<DbUpdateException, Task<T>> onExpectedFailure,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await action();
            }
            catch (DbUpdateException exception) when (isExpected(exception))
            {
                return await onExpectedFailure(exception);
            }
        }
    }

    private sealed class ImmediateCommandExecutor(ISettlementService settlementService) : ICommandExecutor
    {
        public ConcertWorkflow Workflow { get; set; } = null!;

        public Task<TResult> ExecuteAsync<TService, TResult>(
            Func<TService, CancellationToken, Task<TResult>> command,
            CancellationToken ct = default)
            where TService : notnull =>
            command(Resolve<TService>(), ct);

        public async Task<TResult> ExecuteAsync<TService, TResult>(
            Func<TService, CancellationToken, Task<TResult>> command,
            Func<TService, TResult, CancellationToken, Task<bool>> validateAuthority,
            Func<TResult> authorityFailure,
            CancellationToken ct = default)
            where TService : notnull
        {
            var service = Resolve<TService>();
            var result = await command(service, ct);
            return await validateAuthority(service, result, ct) ? result : authorityFailure();
        }

        private TService Resolve<TService>() where TService : notnull =>
            typeof(TService) == typeof(ConcertWorkflow)
                ? (TService)(object)Workflow
                : (TService)(object)settlementService;
    }
}
