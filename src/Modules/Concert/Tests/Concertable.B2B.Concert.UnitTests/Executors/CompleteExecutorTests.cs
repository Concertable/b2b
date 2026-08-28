using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Steps;
using Concertable.B2B.Concert.Infrastructure.Services.Executors;
using Concertable.Kernel.ValueObjects;
using Moq;
using Reunion;

namespace Concertable.B2B.Concert.UnitTests;

public sealed class CompleteExecutorTests
{
    private readonly Mock<ISettlementService> settlementService = new();
    private readonly Mock<IDealTypeStrategyFactory<ICompleteStep>> completeStepFactory = new();
    private readonly Mock<ICompleteStep> completeStep = new();
    private readonly CompleteExecutor executor;

    public CompleteExecutorTests()
    {
        this.completeStepFactory
            .Setup(factory => factory.Create(It.IsAny<DealType>()))
            .Returns(this.completeStep.Object);
        this.executor = new CompleteExecutor(
            this.settlementService.Object,
            this.completeStepFactory.Object);
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
            () => this.executor.CompleteAsync(42, cancellationToken));
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
        this.completeStep
            .Setup(step => step.ExecuteAsync(ready, default))
            .ReturnsAsync(executed);
        this.settlementService
            .Setup(service => service.CompleteAsync(
                42,
                operationId,
                It.Is<SettlementConfirmation.ManagerPaid>(value => value.TransactionId == "pi_test"),
                default))
            .ReturnsAsync(completed);

        var result = await this.executor.CompleteAsync(42);

        Assert.True(result.TryGetValue(out var outcome));
        Assert.Equal(SettlementOutcome.Settled, outcome);
        this.completeStepFactory.Verify(factory => factory.Create(DealType.DoorSplit));
        this.completeStep.Verify(step => step.ExecuteAsync(ready, default));
    }
}
