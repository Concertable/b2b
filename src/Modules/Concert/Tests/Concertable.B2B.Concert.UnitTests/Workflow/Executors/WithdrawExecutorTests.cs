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

public sealed class WithdrawExecutorTests
{
    private const int ApplicationId = 42;

    private readonly Mock<IApplicationRepository> applicationRepository = new();
    private readonly Mock<IConcertStateMachineRegistry> registry = new();
    private readonly Mock<IApplicationCancelStep> cancelStep = new();
    private readonly WithdrawExecutor executor;

    public WithdrawExecutorTests()
    {
        var transitioner = new LifecycleTransitioner(
            this.applicationRepository.Object,
            this.registry.Object);
        this.executor = new WithdrawExecutor(transitioner, this.cancelStep.Object);
    }

    [Fact]
    public async Task WithdrawAsync_RefundFails_ReturnsFailureWithoutSavingTransition()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var application = StandardApplication.Create(
            1,
            2,
            DealType.FlatFee,
            Guid.NewGuid(),
            Guid.NewGuid());
        application.Transition(LifecycleState.Accepted);
        this.applicationRepository
            .Setup(repository => repository.GetByIdAsync(ApplicationId, cancellationToken))
            .ReturnsAsync(application);
        this.registry
            .Setup(registry => registry.Get(DealType.FlatFee))
            .Returns(new LifecycleStateMachine(new Dictionary<(LifecycleState, Trigger), LifecycleState>
            {
                [(LifecycleState.Accepted, Trigger.Withdraw)] = LifecycleState.Withdrawn
            }));
        this.cancelStep
            .Setup(step => step.ExecuteAsync(application.Id, cancellationToken))
            .Returns(Task.FromResult(UnitResult.Failure<CancelApplicationError>(
                new CancelApplicationError.EscrowRefundFailure(
                    new EscrowRefundError.EscrowNotRefundable()))));

        var result = await this.executor.WithdrawAsync(ApplicationId, cancellationToken);

        Assert.True(result.TryGetError(out var error));
        var refundFailure = Assert.IsType<CancelApplicationError.EscrowRefundFailure>(error);
        Assert.IsType<EscrowRefundError.EscrowNotRefundable>(refundFailure.Error);
        Assert.Equal(LifecycleState.Accepted, application.State);
        this.cancelStep.Verify(
            step => step.ExecuteAsync(application.Id, cancellationToken),
            Times.Once);
        this.applicationRepository.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
