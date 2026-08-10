using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;
using Concertable.Payment.Contracts.Errors;
using Moq;
using Reunion;

namespace Concertable.B2B.Concert.UnitTests.Workflow.Executors;

public sealed class EscrowExecutorTests
{
    private const int ApplicationId = 42;
    private const int BookingId = 84;

    private readonly Mock<IApplicationRepository> applicationRepository = new();
    private readonly Mock<IConcertStateMachineRegistry> registry = new();
    private readonly Mock<IBookingRepository> bookingRepository = new();
    private readonly Mock<IApplicationCancelStep> cancelStep = new();
    private readonly EscrowExecutor executor;

    public EscrowExecutorTests()
    {
        var transitioner = new LifecycleTransitioner(
            this.applicationRepository.Object,
            this.registry.Object);
        this.executor = new EscrowExecutor(
            transitioner,
            Mock.Of<IConcertWorkflowFactory>(),
            this.bookingRepository.Object,
            Mock.Of<IPublicBookingRepository>(),
            this.cancelStep.Object);
    }

    [Fact]
    public async Task SucceededAsync_LateCaptureRefundFails_ThrowsWithoutSavingTransition()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var application = StandardApplication.Create(
            1,
            2,
            DealType.FlatFee,
            Guid.NewGuid(),
            Guid.NewGuid());
        application.Transition(LifecycleState.Cancelled);
        this.bookingRepository
            .Setup(repository => repository.GetApplicationIdByIdAsync(BookingId, cancellationToken))
            .ReturnsAsync(ApplicationId);
        this.applicationRepository
            .Setup(repository => repository.GetByIdAsync(ApplicationId, cancellationToken))
            .ReturnsAsync(application);
        this.registry
            .Setup(registry => registry.Get(DealType.FlatFee))
            .Returns(new LifecycleStateMachine(new Dictionary<(LifecycleState, Trigger), LifecycleState>
            {
                [(LifecycleState.Cancelled, Trigger.EscrowPaymentSucceeded)] = LifecycleState.Cancelled
            }));
        this.cancelStep
            .Setup(step => step.ExecuteAsync(application.Id, cancellationToken))
            .Returns(Task.FromResult(UnitResult.Failure<CancelApplicationError>(
                new CancelApplicationError.EscrowRefundFailure(
                    new EscrowRefundError.EscrowNotRefundable()))));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => this.executor.SucceededAsync(BookingId, cancellationToken));

        Assert.Contains("escrow.refund_not_allowed", exception.Message);
        Assert.Equal(LifecycleState.Cancelled, application.State);
        this.cancelStep.Verify(
            step => step.ExecuteAsync(application.Id, cancellationToken),
            Times.Once);
        this.applicationRepository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
