using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Strategies;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.Kernel;
using Concertable.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Moq;
using Reunion;

namespace Concertable.B2B.Concert.UnitTests.Services;

public sealed class ConcertWorkflowTests
{
    private readonly Mock<IConcertRepository> concertRepository = new();
    private readonly Mock<ISettlementService> settlementService = new();
    private readonly Mock<IDealStrategyFactory<ICancel>> cancelFactory = new();
    private readonly Mock<IDealStrategyFactory<IComplete>> completeFactory = new();
    private readonly Mock<IUnitOfWork> unitOfWork = new();
    private readonly ImmediateBehavior immediateBehavior;
    private readonly ConcertWorkflow workflow;

    public ConcertWorkflowTests()
    {
        immediateBehavior = new ImmediateBehavior();
        workflow = new ConcertWorkflow(
            concertRepository.Object,
            settlementService.Object,
            cancelFactory.Object,
            completeFactory.Object,
            unitOfWork.Object,
            immediateBehavior,
            immediateBehavior);
    }

    [Fact]
    public async Task CancelAsync_CallerCancellation_Rethrows()
    {
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();
        var cancellationToken = cancellationSource.Token;
        concertRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<int>(), cancellationToken))
            .Returns(Task.FromCanceled<ConcertEntity?>(cancellationToken));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => workflow.CancelAsync(42, cancellationToken));
    }

    [Fact]
    public async Task CancelAsync_ConcertNotFound_ReturnsTypedError()
    {
        concertRepository
            .Setup(repository => repository.GetByIdAsync(42, default))
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
            .Setup(repository => repository.GetByIdAsync(42, default))
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
        var strategy = new Mock<ICancel>();
        cancelFactory
            .Setup(factory => factory.Create(DealType.FlatFee))
            .Returns(strategy.Object);
        concertRepository
            .Setup(repository => repository.GetByIdAsync(42, default))
            .ReturnsAsync(concert);
        var result = await workflow.CancelAsync(42);

        Assert.False(result.TryGetError(out _));
        strategy.Verify(value => value.CancelAsync(concert, default));
        this.unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(default));
    }

    [Fact]
    public async Task CancelAsync_SaveRaceLost_ReturnsSuperseded()
    {
        immediateBehavior.ClassifiesSaveFailureAsConflict = true;
        var strategy = new Mock<ICancel>();
        cancelFactory
            .Setup(factory => factory.Create(DealType.FlatFee))
            .Returns(strategy.Object);
        concertRepository
            .Setup(repository => repository.GetByIdAsync(42, default))
            .ReturnsAsync(CreateBooking());
        this.unitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(default))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        concertRepository
            .Setup(repository => repository.GetStateByIdAsync(42, default))
            .ReturnsAsync(ConcertState.Posted);

        var result = await workflow.CancelAsync(42);

        Assert.True(result.TryGetError(out var error));
        var superseded = Assert.IsType<CancelConcertError.Superseded>(error);
        Assert.Equal(42, superseded.ConcertId);
    }

    [Fact]
    public async Task CancelAsync_SaveRaceLostToAnotherCancellation_ReturnsSuccess()
    {
        immediateBehavior.ClassifiesSaveFailureAsConflict = true;
        var strategy = new Mock<ICancel>();
        cancelFactory
            .Setup(factory => factory.Create(DealType.FlatFee))
            .Returns(strategy.Object);
        concertRepository
            .Setup(repository => repository.GetByIdAsync(42, default))
            .ReturnsAsync(CreateBooking());
        this.unitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync(default))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        concertRepository
            .Setup(repository => repository.GetStateByIdAsync(42, default))
            .ReturnsAsync(ConcertState.CancellationPending);

        var result = await workflow.CancelAsync(42);

        Assert.False(result.TryGetError(out _));
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
            Guid.NewGuid(),
            Guid.NewGuid(),
            Money.Gbp(125m),
            "pm_test");
        Result<SettlementPreparation, FinishConcertError> prepared = ready;
        Result<SettlementConfirmation, FinishConcertError> executed =
            new SettlementConfirmation.ManagerPaid("pi_test");
        Result<SettlementOutcome, FinishConcertError> completed = SettlementOutcome.Settled;
        settlementService
            .Setup(service => service.ReserveAsync(42, default))
            .ReturnsAsync(prepared);
        var strategy = new Mock<IComplete>();
        completeFactory
            .Setup(factory => factory.Create(It.IsAny<DealType>()))
            .Returns(strategy.Object);
        strategy
            .Setup(value => value.CompleteAsync(ready, default))
            .ReturnsAsync(executed);
        settlementService
            .Setup(service => service.CompleteAsync(
                42,
                operationId,
                It.Is<SettlementConfirmation.ManagerPaid>(value => value.TransactionId == "pi_test"),
                default))
            .ReturnsAsync(completed);

        var result = await workflow.CompleteAsync(42);

        Assert.True(result.TryGetValue(out var outcome));
        Assert.Equal(SettlementOutcome.Settled, outcome);
        completeFactory.Verify(factory => factory.Create(DealType.DoorSplit));
        strategy.Verify(value => value.CompleteAsync(ready, default));
    }

    private static ConcertEntity CreateBooking() => ConcertEntity.CreateDraft(
        new ConfirmedBooking(
            Guid.NewGuid(),
            1,
            2,
            3,
            4,
            5,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DealType.FlatFee,
            false,
            new DateTime(2030, 1, 1, 19, 0, 0, DateTimeKind.Utc),
            new DateTime(2030, 1, 1, 22, 0, 0, DateTimeKind.Utc),
            [],
            new FlatFeeBookingTerms(100m)),
        "Concert",
        "About",
        []);

    private sealed class ImmediateBehavior : IUnitOfWorkBehavior, IOutboxUnitOfWorkBehavior
    {
        /// <summary>
        /// Stands in for the real behaviour's predicate. Fabricating a <see cref="DbUpdateException"/> with
        /// populated <c>Entries</c> needs a live EF context, so the predicate itself is covered by the
        /// integration race tests; this flag supplies its verdict.
        /// </summary>
        public bool ClassifiesSaveFailureAsConflict { get; set; }

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
            catch (DbUpdateException exception)
                when (ClassifiesSaveFailureAsConflict || isExpected(exception))
            {
                return await onExpectedFailure(exception);
            }
        }
    }
}
