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
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Services;

public sealed class ConcertWorkflowTests
{
    private readonly Mock<IConcertRepository> concertRepository = new();
    private readonly Mock<ISettlementService> settlementService = new();
    private readonly Mock<IDealStrategyFactory<ICancel>> cancelFactory = new();
    private readonly Mock<IDealStrategyFactory<IComplete>> completeFactory = new();
    private readonly ConcertWorkflow workflow;

    public ConcertWorkflowTests()
    {
        var behavior = new ImmediateBehavior();
        this.workflow = new ConcertWorkflow(
            this.concertRepository.Object,
            this.settlementService.Object,
            this.cancelFactory.Object,
            this.completeFactory.Object,
            behavior,
            behavior);
    }

    [Fact]
    public async Task CancelAsync_CallerCancellation_Rethrows()
    {
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();
        var cancellationToken = cancellationSource.Token;
        this.concertRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<int>(), cancellationToken))
            .Returns(Task.FromCanceled<ConcertEntity?>(cancellationToken));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => this.workflow.CancelAsync(42, cancellationToken));
    }

    [Fact]
    public async Task CancelAsync_ConcertNotFound_ReturnsTypedError()
    {
        this.concertRepository
            .Setup(repository => repository.GetByIdAsync(42, default))
            .ReturnsAsync((ConcertEntity?)null);

        var result = await this.workflow.CancelAsync(42);

        Assert.True(result.TryGetError(out var error));
        var notFound = Assert.IsType<CancelConcertError.ConcertNotFound>(error);
        Assert.Equal(42, notFound.ConcertId);
    }

    [Fact]
    public async Task CancelAsync_RejectedTransition_ReturnsInvalidTransition()
    {
        var concert = CreateBooking();
        Assert.True(concert.BeginSettlement().TryGetValue(out _));
        this.concertRepository
            .Setup(repository => repository.GetByIdAsync(42, default))
            .ReturnsAsync(concert);

        var result = await this.workflow.CancelAsync(42);

        Assert.True(result.TryGetError(out var error));
        var invalidTransition = Assert.IsType<CancelConcertError.InvalidTransition>(error);
        Assert.Equal(new TransitionError<ConcertState, ConcertTrigger>(ConcertState.AwaitingSettlement, ConcertTrigger.BeginCancellation), invalidTransition.Error);
        this.cancelFactory.Verify(factory => factory.Create(It.IsAny<DealType>()), Times.Never);
    }

    [Fact]
    public async Task CancelAsync_ValidTransition_ExecutesStrategyAndSaves()
    {
        var concert = CreateBooking();
        var strategy = new Mock<ICancel>();
        this.cancelFactory
            .Setup(factory => factory.Create(DealType.FlatFee))
            .Returns(strategy.Object);
        this.concertRepository
            .Setup(repository => repository.GetByIdAsync(42, default))
            .ReturnsAsync(concert);
        this.concertRepository
            .Setup(repository => repository.TrySaveChangesAsync(default))
            .ReturnsAsync(true);

        var result = await this.workflow.CancelAsync(42);

        Assert.False(result.TryGetError(out _));
        strategy.Verify(value => value.CancelAsync(concert, default));
        this.concertRepository.Verify(repository => repository.TrySaveChangesAsync(default));
    }

    [Fact]
    public async Task CancelAsync_SaveRaceLost_ReturnsSuperseded()
    {
        var strategy = new Mock<ICancel>();
        this.cancelFactory
            .Setup(factory => factory.Create(DealType.FlatFee))
            .Returns(strategy.Object);
        this.concertRepository
            .SetupSequence(repository => repository.GetByIdAsync(42, default))
            .ReturnsAsync(CreateBooking())
            .ReturnsAsync(CreateBooking());
        this.concertRepository
            .Setup(repository => repository.TrySaveChangesAsync(default))
            .ReturnsAsync(false);

        var result = await this.workflow.CancelAsync(42);

        Assert.True(result.TryGetError(out var error));
        var superseded = Assert.IsType<CancelConcertError.Superseded>(error);
        Assert.Equal(42, superseded.ConcertId);
    }

    [Fact]
    public async Task CompleteAsync_CallerCancellation_Rethrows()
    {
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();
        var cancellationToken = cancellationSource.Token;
        this.settlementService
            .Setup(service => service.ReserveAsync(It.IsAny<int>(), cancellationToken))
            .Returns(Task.FromCanceled<Result<SettlementPreparation, FinishConcertError>>(
                cancellationToken));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => this.workflow.CompleteAsync(42, cancellationToken));
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
        this.settlementService
            .Setup(service => service.ReserveAsync(42, default))
            .ReturnsAsync(prepared);
        var strategy = new Mock<IComplete>();
        this.completeFactory
            .Setup(factory => factory.Create(It.IsAny<DealType>()))
            .Returns(strategy.Object);
        strategy
            .Setup(value => value.CompleteAsync(ready, default))
            .ReturnsAsync(executed);
        this.settlementService
            .Setup(service => service.CompleteAsync(
                42,
                operationId,
                It.Is<SettlementConfirmation.ManagerPaid>(value => value.TransactionId == "pi_test"),
                default))
            .ReturnsAsync(completed);

        var result = await this.workflow.CompleteAsync(42);

        Assert.True(result.TryGetValue(out var outcome));
        Assert.Equal(SettlementOutcome.Settled, outcome);
        this.completeFactory.Verify(factory => factory.Create(DealType.DoorSplit));
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
        public Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default) => action();

        public Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default) => action();
    }
}
