using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Steps;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Services.Executors;
using Concertable.Kernel;
using Moq;

namespace Concertable.B2B.Concert.UnitTests;

public sealed class CancelExecutorTests
{
    private readonly Mock<IConcertRepository> concertRepository = new();
    private readonly Mock<IDealTypeStrategyFactory<ICancelStep>> cancelStepFactory = new();
    private readonly CancelExecutor executor;

    public CancelExecutorTests()
    {
        var behavior = new ImmediateBehavior();
        this.executor = new CancelExecutor(
            this.concertRepository.Object,
            this.cancelStepFactory.Object,
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
            () => this.executor.CancelAsync(42, cancellationToken));
    }

    [Fact]
    public async Task CancelAsync_ConcertNotFound_ReturnsTypedError()
    {
        this.concertRepository
            .Setup(repository => repository.GetByIdAsync(42, default))
            .ReturnsAsync((ConcertEntity?)null);

        var result = await this.executor.CancelAsync(42);

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

        var result = await this.executor.CancelAsync(42);

        Assert.True(result.TryGetError(out var error));
        var invalidTransition = Assert.IsType<CancelConcertError.InvalidTransition>(error);
        Assert.Equal(new TransitionError<State, Trigger>(State.AwaitingSettlement, Trigger.BeginCancellation), invalidTransition.Error);
        this.cancelStepFactory.Verify(factory => factory.Create(It.IsAny<DealType>()), Times.Never);
    }

    [Fact]
    public async Task CancelAsync_ValidTransition_ExecutesStepAndSaves()
    {
        var concert = CreateBooking();
        var step = new Mock<ICancelStep>();
        this.cancelStepFactory
            .Setup(factory => factory.Create(DealType.FlatFee))
            .Returns(step.Object);
        this.concertRepository
            .Setup(repository => repository.GetByIdAsync(42, default))
            .ReturnsAsync(concert);
        this.concertRepository
            .Setup(repository => repository.TrySaveChangesAsync(default))
            .ReturnsAsync(true);

        var result = await this.executor.CancelAsync(42);

        Assert.False(result.TryGetError(out _));
        step.Verify(value => value.ExecuteAsync(concert, default));
        this.concertRepository.Verify(repository => repository.TrySaveChangesAsync(default));
    }

    [Fact]
    public async Task CancelAsync_SaveRaceLost_ReturnsSuperseded()
    {
        var step = new Mock<ICancelStep>();
        this.cancelStepFactory
            .Setup(factory => factory.Create(DealType.FlatFee))
            .Returns(step.Object);
        this.concertRepository
            .SetupSequence(repository => repository.GetByIdAsync(42, default))
            .ReturnsAsync(CreateBooking())
            .ReturnsAsync(CreateBooking());
        this.concertRepository
            .Setup(repository => repository.TrySaveChangesAsync(default))
            .ReturnsAsync(false);

        var result = await this.executor.CancelAsync(42);

        Assert.True(result.TryGetError(out var error));
        var superseded = Assert.IsType<CancelConcertError.Superseded>(error);
        Assert.Equal(42, superseded.ConcertId);
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
